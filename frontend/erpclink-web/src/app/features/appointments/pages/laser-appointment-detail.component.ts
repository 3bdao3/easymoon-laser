import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import {
  AvailabilitySlotDto,
  AvailabilitySlotStatus,
  CustomerDto,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  LaserServiceDto,
  SessionDetailDto,
  appointmentPulsesConsumed,
  formatDateAr,
  formatTimeAr,
  toApiTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-laser-appointment-detail',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './laser-appointment-detail.component.html',
  styleUrl: './laser-appointment-detail.component.scss'
})
export class LaserAppointmentDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(LaserAppointmentsApi);
  private readonly customersApi = inject(CustomersApi);
  private readonly servicesApi = inject(LaserServicesApi);
  private readonly toast = inject(ToastService);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly scheduling = signal(false);
  readonly detail = signal<SessionDetailDto | null>(null);
  readonly customer = signal<CustomerDto | null>(null);
  readonly catalog = signal<LaserServiceDto[]>([]);
  readonly amountInput = signal<number | null>(null);
  readonly pulsesInput = signal<number | null>(null);
  readonly durationInput = signal<number | null>(null);
  readonly nextId = signal<string | null>(null);
  readonly loadingSlots = signal(false);
  readonly slots = signal<AvailabilitySlotDto[]>([]);
  readonly selectedSlot = signal<AvailabilitySlotDto | null>(null);
  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;
  readonly SlotStatus = AvailabilitySlotStatus;
  readonly minDate = new Date().toISOString().slice(0, 10);

  readonly form = new FormGroup({
    amountPaid: new FormControl<number | null>(null, { validators: [Validators.min(0)] }),
    pulsesConsumed: new FormControl<number | null>(null, { validators: [Validators.min(1), Validators.max(5000)] }),
    sessionMinutes: new FormControl<number | null>(null, { validators: [Validators.min(1), Validators.max(480)] }),
    nextDate: new FormControl('', { nonNullable: true }),
    nextTime: new FormControl('', { nonNullable: true })
  });

  readonly item = computed(() => this.detail()?.appointment ?? null);
  readonly pulseBalance = computed(() => this.detail()?.pulseBalance ?? this.customer()?.pulseBalance ?? null);

  readonly isPulseSession = computed(() => {
    const names = this.item()?.services.map((s) => s.serviceName).join(' ') ?? '';
    return names.includes('نبضة') || this.pulseBalance()?.packageTotal != null;
  });

  readonly dueAmount = computed(() => {
    const packagePrice = this.pulseBalance()?.packagePriceTotal;
    if (packagePrice != null && packagePrice > 0) {
      return packagePrice;
    }
    const lines = this.item()?.services ?? [];
    const catalog = this.catalog();
    if (!lines.length || !catalog.length) {
      return null;
    }
    return lines.reduce((sum, line) => sum + (catalog.find((service) => service.id === line.laserServiceId)?.price ?? 0), 0);
  });

  readonly pulsesBefore = computed(() => {
    const balance = this.pulseBalance();
    const saved = this.item() ? appointmentPulsesConsumed(this.item()!) ?? 0 : 0;
    if (balance?.remaining == null && balance?.packageTotal == null) {
      return null;
    }
    return (balance?.remaining ?? 0) + saved;
  });

  readonly pulsesLeft = computed(() => {
    const before = this.pulsesBefore();
    if (before == null) {
      return null;
    }
    const used = this.pulsesInput();
    if (used == null || used < 1) {
      return before;
    }
    return before - used;
  });

  readonly moneyBefore = computed(() => {
    const owed = this.dueAmount();
    if (owed == null) {
      return null;
    }
    const paidAll = this.pulseBalance()?.amountPaid ?? 0;
    const paidThis = this.item()?.amountPaid ?? 0;
    return Math.max(0, owed - (paidAll - paidThis));
  });

  readonly moneyLeft = computed(() => {
    const before = this.moneyBefore();
    if (before == null) {
      return null;
    }
    const pay = this.amountInput() ?? 0;
    return Math.max(0, before - pay);
  });

  readonly timeTotal = computed(() => {
    const packageMinutes = this.pulseBalance()?.packageDurationTotalMinutes;
    if (packageMinutes != null && packageMinutes > 0) {
      return packageMinutes;
    }
    const booked = this.item()?.durationMinutes ?? 0;
    return booked > 0 ? booked : null;
  });

  readonly timeLeft = computed(() => {
    const total = this.timeTotal();
    if (total == null) {
      return null;
    }
    const usedAll = this.pulseBalance()?.durationUsedMinutes ?? 0;
    const savedHere = this.item()?.status === LaserAppointmentStatus.Attended ? this.item()!.durationMinutes : 0;
    const usedElsewhere = Math.max(0, usedAll - savedHere);
    const typed = this.durationInput();
    const usedNow = typed != null && typed >= 1 ? typed : 0;
    return Math.max(0, total - usedElsewhere - usedNow);
  });

  ngOnInit(): void {
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe((value) => {
      this.amountInput.set(value.amountPaid ?? null);
      this.pulsesInput.set(value.pulsesConsumed ?? null);
      this.durationInput.set(value.sessionMinutes ?? null);
    });

    this.form.controls.sessionMinutes.valueChanges.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => {
      if (!this.form.controls.nextDate.value) {
        return;
      }
      this.selectedSlot.set(null);
      this.form.controls.nextTime.setValue('', { emitEvent: false });
      this.loadSlots();
    });

    this.form.controls.nextDate.valueChanges.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => {
      this.selectedSlot.set(null);
      this.form.controls.nextTime.setValue('', { emitEvent: false });
      this.loadSlots();
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/appointments']);
      return;
    }
    this.load(id);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(id: string): void {
    this.loading.set(true);
    this.servicesApi.list(false).subscribe({
      next: (rows) => this.catalog.set(rows),
      error: () => this.catalog.set([])
    });
    this.api.getSession(id).subscribe({
      next: (row) => {
        this.detail.set(row);
        const savedPulses = appointmentPulsesConsumed(row.appointment);
        const savedMinutes = row.appointment.durationMinutes > 0 ? row.appointment.durationMinutes : null;
        this.form.patchValue(
          {
            amountPaid: row.appointment.amountPaid ?? null,
            pulsesConsumed: savedPulses,
            sessionMinutes: savedMinutes
          },
          { emitEvent: false }
        );
        this.amountInput.set(row.appointment.amountPaid ?? null);
        this.pulsesInput.set(savedPulses);
        this.durationInput.set(savedMinutes);
        this.customersApi.getById(row.appointment.customerId).subscribe({
          next: (customer) => {
            this.customer.set(customer);
            this.loading.set(false);
          },
          error: () => this.loading.set(false)
        });
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('تعذّر تحميل الجلسة');
        void this.router.navigate(['/app/appointments']);
      }
    });
  }

  money(value: number | null | undefined): string {
    if (value == null || Number.isNaN(Number(value))) {
      return '—';
    }
    return `${value} ج.م`;
  }

  loadSlots(): void {
    const appt = this.item();
    const date = this.form.controls.nextDate.value;
    if (!appt || !date) {
      this.slots.set([]);
      return;
    }
    const typed = this.form.controls.sessionMinutes.value;
    if (typed == null || typed < 1) {
      this.slots.set([]);
      return;
    }
    const minutes = this.timeLeft();
    if (minutes == null || minutes < 1) {
      this.slots.set([]);
      this.loadingSlots.set(false);
      return;
    }
    const ids = appt.services.map((s) => s.laserServiceId);
    const overrides = this.splitDuration(appt, minutes);
    this.loadingSlots.set(true);
    this.api.getAvailability(date, ids, undefined, overrides).subscribe({
      next: (result) => {
        this.slots.set(result.slots ?? []);
        this.loadingSlots.set(false);
      },
      error: () => {
        this.slots.set([]);
        this.loadingSlots.set(false);
        this.toast.error('تعذّر تحميل المواعيد المتاحة');
      }
    });
  }

  selectSlot(slot: AvailabilitySlotDto): void {
    if (slot.status !== AvailabilitySlotStatus.Available || this.scheduling()) {
      return;
    }
    if (!this.canRecord() || (this.form.controls.sessionMinutes.value ?? 0) < 1) {
      this.toast.error('اكتب المبلغ والنبضات ووقت الجلسة دي الأول');
      return;
    }
    this.selectedSlot.set(slot);
    this.form.controls.nextTime.setValue(slot.startTime.slice(0, 5), { emitEvent: false });
    this.scheduleNext();
  }

  slotClass(slot: AvailabilitySlotDto): string {
    if (this.selectedSlot()?.startTime === slot.startTime) {
      return 'day-slot day-slot--selected';
    }
    if (slot.status === AvailabilitySlotStatus.Available) {
      return 'day-slot day-slot--available';
    }
    if (slot.status === AvailabilitySlotStatus.Booked) {
      return 'day-slot day-slot--booked';
    }
    return 'day-slot day-slot--unavailable';
  }

  saveSession(): void {
    const appt = this.item();
    if (!appt || !this.canRecord()) {
      this.form.markAllAsTouched();
      this.toast.error(this.isPulseSession() ? 'اكتب المبلغ وعدد نبضات الجلسة' : 'اكتب المبلغ');
      return;
    }
    this.saving.set(true);
    this.recordCurrent(appt.id, () => {
      this.saving.set(false);
      this.toast.success('اتحفظت الجلسة');
      if (this.nextId()) {
        this.refreshNextNotes();
        void this.router.navigate(['/app/customers', appt.customerId]);
        return;
      }
      void this.router.navigate(['/app/customers', appt.customerId]);
    });
  }

  back(): void {
    void this.router.navigate(['/app/appointments']);
  }

  private canRecord(): boolean {
    const amount = this.form.controls.amountPaid.value;
    if (amount == null || amount < 0) {
      return false;
    }
    if (!this.isPulseSession()) {
      return true;
    }
    const pulses = this.form.controls.pulsesConsumed.value;
    return pulses != null && pulses >= 1 && (this.pulsesLeft() ?? 0) >= 0;
  }

  private scheduleNext(): void {
    const appt = this.item();
    const date = this.form.controls.nextDate.value;
    const time = this.form.controls.nextTime.value;
    if (!appt || !date || !time || this.scheduling()) {
      return;
    }
    if (!this.canRecord()) {
      this.toast.error('اكتب المبلغ وعدد النبضات الأول، وبعدين التاريخ والوقت');
      return;
    }
    if ((this.pulsesLeft() ?? 0) < 0) {
      this.toast.error('نبضات الجلسة أكبر من المتبقي');
      return;
    }

    this.scheduling.set(true);
    this.recordCurrent(appt.id, () => this.createOrUpdateNext(appt, date, time));
  }

  private recordCurrent(id: string, done: () => void): void {
    const appt = this.item();
    const typed = this.form.controls.sessionMinutes.value;
    const duration = typed != null && typed >= 1 ? typed : appt && appt.durationMinutes > 0 ? appt.durationMinutes : 30;
    const pulses = this.isPulseSession() ? this.form.controls.pulsesConsumed.value : null;
    this.api
      .recordSession(id, {
        durationMinutes: duration,
        pulsesConsumed: pulses,
        amountPaid: this.form.controls.amountPaid.value,
        markAttended: true
      })
      .subscribe({
        next: (result) => {
          this.detail.set(result);
          done();
        },
        error: (err) => {
          this.saving.set(false);
          this.scheduling.set(false);
          const msg = err?.error?.detail || err?.error?.title || err?.error?.message || 'تعذّر حفظ الجلسة';
          this.toast.error(msg);
        }
      });
  }

  private createOrUpdateNext(appt: LaserAppointmentDto, date: string, time: string): void {
    const notes = this.leftoverNote();
    const laserServiceIds = appt.services.map((s) => s.laserServiceId);
    const nextMinutes = this.timeLeft();
    if (nextMinutes == null || nextMinutes < 1) {
      this.scheduling.set(false);
      this.toast.error('مفيش وقت متبقي للجلسة الجاية');
      return;
    }
    const serviceDurationOverrides = Object.entries(this.splitDuration(appt, nextMinutes)).map(([serviceId, durationMinutes]) => ({
      serviceId,
      durationMinutes,
      pulsesConsumed: null as number | null
    }));
    const body = {
      appointmentDate: date,
      startTime: toApiTime(time),
      laserServiceIds,
      notes,
      serviceDurationOverrides
    };
    const existing = this.nextId();
    const request = existing
      ? this.api.update(existing, body)
      : this.api.create({ customerId: appt.customerId, ...body });

    request.subscribe({
      next: (created) => {
        const createdNow = !existing;
        this.nextId.set(created.id);
        this.scheduling.set(false);
        this.saving.set(false);
        this.toast.success(createdNow ? 'اتحفظت الجلسة واتعملت الجلسة الجاية' : 'اتحدثت الجلسة الجاية');
        void this.router.navigate(['/app/customers', appt.customerId]);
      },
      error: (err) => {
        this.scheduling.set(false);
        this.saving.set(false);
        this.selectedSlot.set(null);
        this.form.controls.nextTime.setValue('', { emitEvent: false });
        const msg = err?.error?.detail || err?.error?.title || err?.error?.message || 'المعاد ده مش متاح، اختار وقت تاني';
        this.toast.error(msg);
      }
    });
  }

  private refreshNextNotes(): void {
    const id = this.nextId();
    const appt = this.item();
    const date = this.form.controls.nextDate.value;
    const time = this.form.controls.nextTime.value;
    if (!id || !appt || !date || !time) {
      return;
    }
    this.api
      .update(id, {
        appointmentDate: date,
        startTime: toApiTime(time),
        laserServiceIds: appt.services.map((s) => s.laserServiceId),
        notes: this.leftoverNote(),
        serviceDurationOverrides: Object.entries(this.splitDuration(appt, this.timeLeft() ?? appt.durationMinutes)).map(
          ([serviceId, durationMinutes]) => ({
            serviceId,
            durationMinutes,
            pulsesConsumed: null
          })
        )
      })
      .subscribe({ error: () => undefined });
  }

  private leftoverNote(): string {
    const pulses = this.pulsesLeft();
    const money = this.moneyLeft();
    const time = this.timeLeft();
    const pulseLine = pulses == null ? 'متبقي النبضات: —' : `متبقي النبضات: ${pulses}`;
    const moneyLine = money == null ? 'متبقي الفلوس: —' : `متبقي الفلوس: ${money} ج.م`;
    const timeLine = time == null ? 'متبقي الوقت: —' : `متبقي الوقت: ${time} د`;
    return `${pulseLine}\n${moneyLine}\n${timeLine}`;
  }

  private splitDuration(appt: LaserAppointmentDto, minutes: number): Record<string, number> {
    const lines = appt.services;
    const overrides: Record<string, number> = {};
    if (!lines.length) {
      return overrides;
    }
    const original = lines.reduce((sum, line) => sum + (line.durationMinutes > 0 ? line.durationMinutes : 0), 0) || lines.length;
    let left = minutes;
    lines.forEach((line, index) => {
      if (index === lines.length - 1) {
        overrides[line.laserServiceId] = Math.max(1, left);
        return;
      }
      const share = Math.max(1, Math.round(((line.durationMinutes > 0 ? line.durationMinutes : 1) / original) * minutes));
      overrides[line.laserServiceId] = share;
      left -= share;
    });
    return overrides;
  }
}
