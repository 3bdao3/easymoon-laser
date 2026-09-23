import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import {
  CustomerDto,
  LaserServiceDto,
  SessionDetailDto,
  LaserAppointmentDto,
  appointmentPulsesConsumed,
  formatDateAr,
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
  readonly nextId = signal<string | null>(null);
  readonly formatDateAr = formatDateAr;
  readonly minDate = new Date().toISOString().slice(0, 10);

  readonly form = new FormGroup({
    amountPaid: new FormControl<number | null>(null, { validators: [Validators.min(0)] }),
    pulsesConsumed: new FormControl<number | null>(null, { validators: [Validators.min(1), Validators.max(5000)] }),
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

  ngOnInit(): void {
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe((value) => {
      this.amountInput.set(value.amountPaid ?? null);
      this.pulsesInput.set(value.pulsesConsumed ?? null);
    });

    this.form.controls.nextDate.valueChanges.pipe(debounceTime(400), takeUntil(this.destroy$)).subscribe(() => this.scheduleNext());
    this.form.controls.nextTime.valueChanges.pipe(debounceTime(400), takeUntil(this.destroy$)).subscribe(() => this.scheduleNext());

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
        this.form.patchValue(
          {
            amountPaid: row.appointment.amountPaid ?? null,
            pulsesConsumed: savedPulses
          },
          { emitEvent: false }
        );
        this.amountInput.set(row.appointment.amountPaid ?? null);
        this.pulsesInput.set(savedPulses);
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
      this.refreshNextNotes();
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
    const duration = appt && appt.durationMinutes > 0 ? appt.durationMinutes : 30;
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
    const serviceDurationOverrides = appt.services.map((s) => ({
      serviceId: s.laserServiceId,
      durationMinutes: s.durationMinutes > 0 ? s.durationMinutes : appt.durationMinutes || 30,
      pulsesConsumed: null
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
        if (createdNow) {
          this.toast.success('اتعملت الجلسة الجاية واتكتب فيها المتبقي');
        }
      },
      error: (err) => {
        this.scheduling.set(false);
        this.saving.set(false);
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
        serviceDurationOverrides: appt.services.map((s) => ({
          serviceId: s.laserServiceId,
          durationMinutes: s.durationMinutes > 0 ? s.durationMinutes : appt.durationMinutes || 30,
          pulsesConsumed: null
        }))
      })
      .subscribe({ error: () => undefined });
  }

  private leftoverNote(): string {
    const pulses = this.pulsesLeft();
    const money = this.moneyLeft();
    const pulseLine = pulses == null ? 'متبقي النبضات: —' : `متبقي النبضات: ${pulses}`;
    const moneyLine = money == null ? 'متبقي الفلوس: —' : `متبقي الفلوس: ${money} ج.م`;
    return `${pulseLine}\n${moneyLine}`;
  }
}
