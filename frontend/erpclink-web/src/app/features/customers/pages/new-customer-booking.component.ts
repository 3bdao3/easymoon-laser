import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserServicesApi } from '../../laser-clinic/services/laser-services-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  AvailabilityResultDto,
  AvailabilitySlotDto,
  AvailabilitySlotStatus,
  CustomerDto,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentStatus,
  LaserServiceDto,
  formatDateAr,
  formatTimeAr,
  egyptianMobileValidator,
  normalizeEgyptianMobile,
  serviceRequiresManualDuration,
  toApiTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { LaserServiceAdminCardComponent } from '../../laser-services/components/laser-service-admin-card.component';
import { ToastService } from '../../../core/services/toast.service';

type CustomerMode = 'new' | 'existing';

@Component({
  selector: 'app-new-customer-booking',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent, LaserServiceAdminCardComponent],
  templateUrl: './new-customer-booking.component.html',
  styleUrl: './new-customer-booking.component.scss'
})
export class NewCustomerBookingComponent implements OnInit, OnDestroy {
  private readonly customersApi = inject(CustomersApi);
  private readonly servicesApi = inject(LaserServicesApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroy$ = new Subject<void>();

  readonly statusLabel = LASER_APPOINTMENT_STATUS_LABELS[LaserAppointmentStatus.Pending];
  readonly SlotStatus = AvailabilitySlotStatus;
  readonly formatTimeAr = formatTimeAr;
  readonly formatDateAr = formatDateAr;
  readonly minDate = new Date().toISOString().slice(0, 10);
  readonly statusLabels = LASER_APPOINTMENT_STATUS_LABELS;

  readonly mode = signal<CustomerMode>('new');
  readonly loadingServices = signal(false);
  readonly searchingCustomers = signal(false);
  readonly loadingAvailability = signal(false);
  readonly submitting = signal(false);

  readonly services = signal<LaserServiceDto[]>([]);
  readonly selectedServiceIds = signal<string[]>([]);
  readonly pulseDurationByServiceId = signal<Record<string, number | null>>({});
  readonly existingMatches = signal<CustomerDto[]>([]);
  readonly selectedExisting = signal<CustomerDto | null>(null);
  readonly availability = signal<AvailabilityResultDto | null>(null);
  readonly selectedSlot = signal<AvailabilitySlotDto | null>(null);

  readonly customerForm = new FormGroup({
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, egyptianMobileValidator]
    }),
    age: new FormControl<number | null>(null),
    notes: new FormControl('', { nonNullable: true })
  });

  readonly appointmentForm = new FormGroup({
    date: new FormControl(this.minDate, {
      nonNullable: true,
      validators: [Validators.required]
    }),
    notes: new FormControl('', { nonNullable: true }),
    status: new FormControl<LaserAppointmentStatus>(LaserAppointmentStatus.Pending, {
      nonNullable: true
    })
  });

  readonly existingQuery = new FormControl('', { nonNullable: true });
  readonly editableStatuses = [
    LaserAppointmentStatus.Pending,
    LaserAppointmentStatus.Confirmed,
    LaserAppointmentStatus.Attended,
    LaserAppointmentStatus.NoShow
  ];

  readonly selectedServices = computed(() => {
    const ids = new Set(this.selectedServiceIds());
    return this.services().filter((s) => ids.has(s.id));
  });

  readonly selectedPulseServices = computed(() =>
    this.selectedServices().filter((s) => serviceRequiresManualDuration(s))
  );

  readonly durationOverrides = computed(() => {
    const map: Record<string, number> = {};
    const custom = this.pulseDurationByServiceId();
    for (const s of this.selectedPulseServices()) {
      const minutes = custom[s.id];
      if (minutes != null && minutes > 0) {
        map[s.id] = minutes;
      }
    }
    return map;
  });

  readonly pulseDurationsReady = computed(() => {
    const custom = this.pulseDurationByServiceId();
    return this.selectedPulseServices().every((s) => {
      const minutes = custom[s.id];
      return minutes != null && minutes > 0;
    });
  });

  readonly durationOverrideList = computed(() =>
    Object.entries(this.durationOverrides()).map(([serviceId, durationMinutes]) => ({
      serviceId,
      durationMinutes
    }))
  );

  /** Prefer backend clinical duration when availability loaded. */
  readonly sessionDuration = computed(() => {
    if (this.selectedPulseServices().length && !this.pulseDurationsReady()) {
      return 0;
    }
    const avail = this.availability();
    if (avail?.clinicalDurationMinutes) {
      return avail.clinicalDurationMinutes;
    }
    const custom = this.pulseDurationByServiceId();
    return this.selectedServices().reduce((sum, s) => {
      if (serviceRequiresManualDuration(s)) {
        return sum + (custom[s.id] ?? 0);
      }
      return sum + s.recommendedDurationMinutes;
    }, 0);
  });

  readonly endTimeDisplay = computed(() => {
    const slot = this.selectedSlot();
    if (slot?.endTime) {
      return formatTimeAr(slot.endTime);
    }
    return '—';
  });

  private readonly typedName = signal('');

  readonly summaryName = computed(() => {
    if (this.mode() === 'existing') {
      return this.selectedExisting()?.fullName?.trim() || '—';
    }
    return this.typedName().trim() || '—';
  });

  readonly isToday = computed(() => this.appointmentForm.controls.date.value === this.minDate);

  readonly timelineSlots = computed(() => this.availability()?.slots ?? []);

  ngOnInit(): void {
    this.loadServices();
    this.typedName.set(this.customerForm.controls.fullName.value);
    this.customerForm.controls.fullName.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.typedName.set(value));

    this.existingQuery.valueChanges
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => this.searchExisting());

    this.appointmentForm.controls.date.valueChanges
      .pipe(debounceTime(150), takeUntil(this.destroy$))
      .subscribe(() => {
        this.selectedSlot.set(null);
        this.reloadAvailability();
      });

    const preselectId = this.route.snapshot.queryParamMap.get('customerId');
    if (preselectId) {
      this.mode.set('existing');
      this.customersApi.getById(preselectId).subscribe({
        next: (c) => {
          this.selectedExisting.set(c);
          this.existingMatches.set([c]);
          this.existingQuery.setValue(c.fullName, { emitEvent: false });
          this.customerForm.patchValue({
            fullName: c.fullName,
            phoneNumber: c.phoneNumber,
            age: c.age,
            notes: c.notes ?? ''
          });
          this.applyCustomerPreviousSelection(c.id);
        }
      });
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setMode(mode: CustomerMode): void {
    this.mode.set(mode);
    this.selectedExisting.set(null);
    this.existingMatches.set([]);
    if (mode === 'new') {
      this.customerForm.reset({ fullName: '', phoneNumber: '', age: null, notes: '' });
    }
  }

  loadServices(): void {
    this.loadingServices.set(true);
    this.servicesApi.list(true).subscribe({
      next: (rows) => {
        this.services.set(rows);
        this.loadingServices.set(false);
      },
      error: () => this.loadingServices.set(false)
    });
  }

  searchExisting(): void {
    const q = this.existingQuery.value.trim();
    if (!q) {
      this.existingMatches.set([]);
      return;
    }
    this.searchingCustomers.set(true);
    this.customersApi.search(q, true).subscribe({
      next: (rows) => {
        this.existingMatches.set(rows);
        this.searchingCustomers.set(false);
      },
      error: () => this.searchingCustomers.set(false)
    });
  }

  selectExisting(c: CustomerDto): void {
    this.selectedExisting.set(c);
    this.customerForm.patchValue({
      fullName: c.fullName,
      phoneNumber: c.phoneNumber,
      age: c.age,
      notes: c.notes ?? ''
    });
    this.selectedServiceIds.set([]);
    this.pulseDurationByServiceId.set({});
    this.selectedSlot.set(null);
    this.availability.set(null);
    this.applyCustomerPreviousSelection(c.id);
  }

  /** Prefill areas (and pulse durations) from the customer's upcoming or last appointment. */
  private applyCustomerPreviousSelection(customerId: string): void {
    this.customersApi.getHistory(customerId).subscribe({
      next: (history) => {
        const source = history.upcoming[0] ?? history.previous[0] ?? null;
        if (!source || !source.services?.length) {
          return;
        }

        const ids = source.services.map((s) => s.laserServiceId);
        this.selectedServiceIds.set(ids);

        const pulseMap: Record<string, number | null> = {};
        for (const line of source.services) {
          const svc =
            this.services().find((s) => s.id === line.laserServiceId) ??
            ({ name: line.serviceName, requiresManualDuration: line.serviceName.includes('نبضة') } as LaserServiceDto);
          if (serviceRequiresManualDuration(svc) || line.serviceName.includes('نبضة')) {
            pulseMap[line.laserServiceId] = line.durationMinutes > 0 ? line.durationMinutes : null;
          }
        }
        this.pulseDurationByServiceId.set(pulseMap);

        if (source.appointmentDate) {
          const date = source.appointmentDate.slice(0, 10);
          if (date >= this.minDate) {
            this.appointmentForm.controls.date.setValue(date, { emitEvent: false });
          }
        }

        this.selectedSlot.set(null);
        this.reloadAvailability();
        this.toast.success('تم تحميل المناطق من موعد العميلة السابق');
      }
    });
  }

  toggleService(id: string): void {
    const current = this.selectedServiceIds();
    const removing = current.includes(id);
    this.selectedServiceIds.set(removing ? current.filter((x) => x !== id) : [...current, id]);
    if (removing) {
      const next = { ...this.pulseDurationByServiceId() };
      delete next[id];
      this.pulseDurationByServiceId.set(next);
    } else {
      const svc = this.services().find((s) => s.id === id);
      if (svc && serviceRequiresManualDuration(svc)) {
        this.pulseDurationByServiceId.set({ ...this.pulseDurationByServiceId(), [id]: null });
      }
    }
    this.selectedSlot.set(null);
    this.reloadAvailability();
  }

  isSelected(id: string): boolean {
    return this.selectedServiceIds().includes(id);
  }

  durationLabel(s: LaserServiceDto): string {
    if (serviceRequiresManualDuration(s)) {
      return 'أدخلي المدة';
    }
    return `${s.recommendedDurationMinutes} دقيقة`;
  }

  setPulseDuration(serviceId: string, raw: string): void {
    const parsed = Number(raw);
    const minutes = Number.isFinite(parsed) && parsed > 0 ? Math.round(parsed) : null;
    this.pulseDurationByServiceId.set({ ...this.pulseDurationByServiceId(), [serviceId]: minutes });
    this.selectedSlot.set(null);
    this.reloadAvailability();
  }

  pulseDurationValue(serviceId: string): string {
    const value = this.pulseDurationByServiceId()[serviceId];
    return value != null && value > 0 ? String(value) : '';
  }

  reloadAvailability(): void {
    const date = this.appointmentForm.controls.date.value;
    const ids = this.selectedServiceIds();
    if (!date || ids.length === 0 || !this.pulseDurationsReady()) {
      this.availability.set(null);
      return;
    }

    const overrides = this.durationOverrides();
    this.loadingAvailability.set(true);
    this.appointmentsApi
      .getAvailability(date, ids, undefined, Object.keys(overrides).length ? overrides : undefined)
      .subscribe({
        next: (result) => {
          this.availability.set(result);
          this.loadingAvailability.set(false);
          const selected = this.selectedSlot();
          if (selected) {
            const stillOk = result.slots.find(
              (s) =>
                s.startTime === selected.startTime && s.status === AvailabilitySlotStatus.Available
            );
            if (!stillOk) {
              this.selectedSlot.set(null);
            } else {
              this.selectedSlot.set(stillOk);
            }
          }
        },
        error: () => {
          this.availability.set(null);
          this.loadingAvailability.set(false);
        }
      });
  }

  selectSlot(slot: AvailabilitySlotDto): void {
    if (slot.status !== AvailabilitySlotStatus.Available) {
      return;
    }
    this.selectedSlot.set(slot);
  }

  slotClass(slot: AvailabilitySlotDto): string {
    const selected = this.selectedSlot()?.startTime === slot.startTime;
    if (selected) return 'day-slot day-slot--selected';
    if (slot.status === AvailabilitySlotStatus.Available) return 'day-slot day-slot--available';
    if (slot.status === AvailabilitySlotStatus.Booked) return 'day-slot day-slot--booked';
    return 'day-slot day-slot--unavailable';
  }

  submit(): void {
    if (this.mode() === 'new') {
      if (this.customerForm.invalid) {
        this.customerForm.markAllAsTouched();
        this.focusCustomerSection();
        this.toast.error(
          this.customerForm.controls.phoneNumber.hasError('egyptMobile')
            ? 'رقم الموبايل لازم يكون مصري، مثال: 01012345678'
            : 'اكتبي اسم العميلة ورقم الهاتف فوق عشان يتكمل الحجز'
        );
        return;
      }
    } else if (!this.selectedExisting()) {
      this.focusCustomerSection();
      this.toast.error('اختاري عميلة موجودة من البحث، أو اضغطي «إضافة عميلة جديدة»');
      return;
    } else if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      this.focusCustomerSection();
      this.toast.error(
        this.customerForm.controls.phoneNumber.hasError('egyptMobile')
          ? 'رقم الموبايل لازم يكون مصري، مثال: 01012345678'
          : 'أكملي اسم ورقم هاتف العميلة المختارة'
      );
      return;
    }

    if (this.selectedServiceIds().length === 0) {
      this.toast.error('يجب اختيار منطقة واحدة على الأقل.');
      return;
    }
    if (!this.pulseDurationsReady()) {
      this.toast.error('أدخلي مدة الجلسة للنبضات');
      return;
    }
    if (this.appointmentForm.controls.date.invalid) {
      this.toast.error('حددي التاريخ');
      return;
    }
    const slot = this.selectedSlot();
    if (!slot || slot.status !== AvailabilitySlotStatus.Available) {
      this.toast.error('اختاري موعداً متاحاً من القائمة');
      return;
    }

    const appt = this.appointmentForm.getRawValue();
    const cust = this.customerForm.getRawValue();
    const existing = this.selectedExisting();

    this.submitting.set(true);

    const book = () =>
      this.appointmentsApi
        .bookWithCustomer({
          existingCustomerId: this.mode() === 'existing' ? existing!.id : null,
          customer:
            this.mode() === 'new'
              ? {
                  fullName: cust.fullName.trim(),
                  phoneNumber: normalizeEgyptianMobile(cust.phoneNumber) ?? cust.phoneNumber.trim(),
                  age: cust.age,
                  notes: cust.notes.trim() || null
                }
              : null,
          appointmentDate: appt.date,
          startTime: toApiTime(slot.startTime),
          laserServiceIds: this.selectedServiceIds(),
          appointmentNotes: appt.notes.trim() || null,
          serviceDurationOverrides: this.durationOverrideList().length
            ? this.durationOverrideList()
            : null
        })
        .subscribe({
          next: (created) => {
            const desired = Number(this.appointmentForm.controls.status.value) as LaserAppointmentStatus;
            const finish = () => {
              this.toast.success('تم تأكيد وحفظ الحجز');
              this.submitting.set(false);
              void this.router.navigate(['/app/customers'], { queryParams: { sort: 'Newest' } });
            };
            if (desired !== created.status) {
              this.appointmentsApi.updateStatus(created.id, desired).subscribe({
                next: finish,
                error: (err) => {
                  const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
                  this.toast.error(detail || 'تسجيل «حضرت» يتم في يوم الموعد وبعد وقت البداية فقط.');
                  finish();
                }
              });
            } else {
              finish();
            }
          },
          error: (err) => {
            this.submitting.set(false);
            const msg =
              err?.error?.detail ||
              err?.error?.title ||
              err?.error?.message ||
              'تعذّر حفظ الحجز';
            if (typeof msg === 'string' && (msg.includes('حجز') || msg.includes('موعد') || msg.includes('هاتف') || msg.includes('نبض'))) {
              this.toast.error(msg);
            } else {
              this.toast.error('هذا الموعد تم حجزه بالفعل، يرجى اختيار موعد آخر.');
            }
            this.reloadAvailability();
          }
        });

    // Existing customer: persist profile edits (name/phone/age/notes) then book
    if (this.mode() === 'existing' && existing) {
      this.customersApi
        .update(existing.id, {
          fullName: cust.fullName.trim(),
          phoneNumber: normalizeEgyptianMobile(cust.phoneNumber) ?? cust.phoneNumber.trim(),
          age: cust.age,
          notes: cust.notes.trim() || null
        })
        .subscribe({
          next: () => book(),
          error: (err) => {
            this.submitting.set(false);
            const msg = err?.error?.detail || err?.error?.message || 'تعذّر تحديث بيانات العميلة';
            this.toast.error(typeof msg === 'string' ? msg : 'تعذّر تحديث بيانات العميلة');
          }
        });
      return;
    }

    book();
  }

  private focusCustomerSection(): void {
    const el = document.getElementById('booking-customer-section');
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}
