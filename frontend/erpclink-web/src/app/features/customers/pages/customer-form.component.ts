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
  CustomerHistoryDto,
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  LaserServiceDto,
  formatDateAr,
  formatTimeAr,
  appointmentPulsesConsumed,
  egyptianMobileValidator,
  normalizeEgyptianMobile,
  serviceRequiresManualDuration,
  toApiTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { LaserServiceAdminCardComponent } from '../../laser-services/components/laser-service-admin-card.component';
import { TimeSpanComponent } from '../../../shared/components/time-span/time-span.component';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent, LaserServiceAdminCardComponent, TimeSpanComponent],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss'
})
export class CustomerFormComponent implements OnInit, OnDestroy {
  private readonly api = inject(CustomersApi);
  private readonly servicesApi = inject(LaserServicesApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly destroy$ = new Subject<void>();

  readonly SlotStatus = AvailabilitySlotStatus;
  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;
  readonly appointmentPulsesConsumed = appointmentPulsesConsumed;
  readonly minDate = new Date().toISOString().slice(0, 10);
  readonly statusLabels = LASER_APPOINTMENT_STATUS_LABELS;
  readonly statusBadge = LASER_APPOINTMENT_STATUS_BADGE;

  readonly loading = signal(false);
  readonly savingProfile = signal(false);
  readonly savingAppointment = signal(false);
  readonly loadingServices = signal(false);
  readonly loadingAvailability = signal(false);
  readonly customerId = signal<string | null>(null);

  readonly services = signal<LaserServiceDto[]>([]);
  readonly selectedServiceIds = signal<string[]>([]);
  readonly pulseDurationByServiceId = signal<Record<string, number | null>>({});
  readonly pulsesConsumedByServiceId = signal<Record<string, number | null>>({});
  readonly availability = signal<AvailabilityResultDto | null>(null);
  readonly selectedSlot = signal<AvailabilitySlotDto | null>(null);
  readonly editingAppointment = signal<LaserAppointmentDto | null>(null);
  readonly history = signal<CustomerHistoryDto | null>(null);

  readonly profileForm = new FormGroup({
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, egyptianMobileValidator]
    }),
    age: new FormControl<number | null>(null),
    notes: new FormControl('', { nonNullable: true })
  });

  readonly appointmentDate = new FormControl(this.minDate, { nonNullable: true });
  readonly appointmentNotes = new FormControl('', { nonNullable: true });
  readonly appointmentStatus = new FormControl<LaserAppointmentStatus>(LaserAppointmentStatus.Pending, {
    nonNullable: true
  });

  readonly sessionDuration = computed(() => {
    if (this.selectedPulseServices().length && !this.pulseDurationsReady()) {
      return 0;
    }
    const avail = this.availability();
    if (avail?.clinicalDurationMinutes) return avail.clinicalDurationMinutes;
    const custom = this.pulseDurationByServiceId();
    return this.services()
      .filter((s) => this.selectedServiceIds().includes(s.id))
      .reduce((sum, s) => {
        if (serviceRequiresManualDuration(s)) {
          return sum + (custom[s.id] ?? 0);
        }
        return sum + s.recommendedDurationMinutes;
      }, 0);
  });

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

  readonly durationOverrideList = computed(() => {
    const durations = this.pulseDurationByServiceId();
    const pulses = this.pulsesConsumedByServiceId();
    return this.selectedPulseServices()
      .map((s) => {
        const durationMinutes = durations[s.id];
        if (durationMinutes == null || durationMinutes < 1) {
          return null;
        }
        const pulsesConsumed = pulses[s.id];
        return {
          serviceId: s.id,
          durationMinutes,
          pulsesConsumed: pulsesConsumed != null && pulsesConsumed > 0 ? pulsesConsumed : null
        };
      })
      .filter(
        (x): x is { serviceId: string; durationMinutes: number; pulsesConsumed: number | null } => x != null
      );
  });

  readonly timelineSlots = computed(() => this.availability()?.slots ?? []);
  readonly isToday = computed(() => this.appointmentDate.value === this.minDate);
  readonly isEdit = computed(() => !!this.customerId());

  /** All customer sessions oldest→newest, each as its own numbered record. */
  readonly sessionList = computed(() => {
    const h = this.history();
    if (!h) {
      return [] as { appt: LaserAppointmentDto; sessionNumber: number; pulses: number | null }[];
    }
    const all = [...h.upcoming, ...h.previous].sort((a, b) => {
      const d = a.appointmentDate.localeCompare(b.appointmentDate);
      if (d !== 0) {
        return d;
      }
      return a.startTime.localeCompare(b.startTime);
    });
    return all.map((appt, index) => ({
      appt,
      sessionNumber: index + 1,
      pulses: appointmentPulsesConsumed(appt)
    }));
  });

  readonly pulsePackageSummary = computed(() => {
    const h = this.history();
    const bal = h?.pulseBalance;
    if (!bal || bal.packageTotal == null) {
      return null;
    }
    return {
      packageTotal: bal.packageTotal,
      consumed: bal.consumed,
      remaining: bal.remaining ?? 0,
      packagePriceTotal: bal.packagePriceTotal ?? null,
      remainingAmount: bal.remainingAmount ?? 0,
      amountPaid: bal.amountPaid ?? 0,
      packageDurationTotalMinutes: bal.packageDurationTotalMinutes ?? null,
      remainingDurationMinutes: bal.remainingDurationMinutes ?? 0,
      durationUsedMinutes: bal.durationUsedMinutes ?? 0
    };
  });

  readonly editableStatuses = [
    LaserAppointmentStatus.Pending,
    LaserAppointmentStatus.Confirmed,
    LaserAppointmentStatus.Attended,
    LaserAppointmentStatus.NoShow
  ];

  ngOnInit(): void {
    this.loadServices();
    this.appointmentDate.valueChanges
      .pipe(debounceTime(150), takeUntil(this.destroy$))
      .subscribe(() => {
        this.selectedSlot.set(null);
        this.reloadAvailability();
      });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.customerId.set(id);
    this.loadCustomerHub(id);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCustomerHub(id: string): void {
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (c) => {
        this.profileForm.patchValue({
          fullName: c.fullName,
          phoneNumber: c.phoneNumber,
          age: c.age,
          notes: c.notes ?? ''
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.api.getHistory(id).subscribe({
      next: (h) => {
        this.history.set(h);
        // Open the next open slot if any; otherwise leave blank for a new session.
        const nextOpen =
          h.upcoming.find(
            (a) =>
              a.status === LaserAppointmentStatus.Pending || a.status === LaserAppointmentStatus.Confirmed
          ) ?? null;
        if (nextOpen) {
          this.beginEditAppointment(nextOpen);
        } else {
          this.clearAppointmentEditor();
        }
      }
    });
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

  beginEditAppointment(appt: LaserAppointmentDto): void {
    this.editingAppointment.set(appt);
    this.appointmentDate.setValue(appt.appointmentDate.slice(0, 10), { emitEvent: false });
    this.appointmentNotes.setValue(appt.notes ?? '');
    this.appointmentStatus.setValue(appt.status);
    this.selectedServiceIds.set(appt.services.map((s) => s.laserServiceId));
    const pulseMap: Record<string, number | null> = {};
    const consumedMap: Record<string, number | null> = {};
    for (const line of appt.services) {
      const svc = this.services().find((s) => s.id === line.laserServiceId);
      if (svc && serviceRequiresManualDuration(svc)) {
        pulseMap[line.laserServiceId] = line.durationMinutes;
        consumedMap[line.laserServiceId] =
          line.pulsesConsumed != null && line.pulsesConsumed > 0 ? line.pulsesConsumed : null;
      } else if (line.serviceName.includes('نبضة')) {
        pulseMap[line.laserServiceId] = line.durationMinutes;
        consumedMap[line.laserServiceId] =
          line.pulsesConsumed != null && line.pulsesConsumed > 0 ? line.pulsesConsumed : null;
      }
    }
    this.pulseDurationByServiceId.set(pulseMap);
    this.pulsesConsumedByServiceId.set(consumedMap);
    this.selectedSlot.set({
      startTime: appt.startTime,
      endTime: appt.endTime,
      durationMinutes: appt.durationMinutes,
      status: AvailabilitySlotStatus.Available,
      appointmentId: appt.id,
      customerName: appt.customerName,
      serviceNames: appt.services.map((s) => s.serviceName).join(' + '),
      bookedDurationMinutes: appt.durationMinutes
    });
    this.reloadAvailability();
  }

  clearAppointmentEditor(): void {
    this.editingAppointment.set(null);
    this.selectedServiceIds.set([]);
    this.pulseDurationByServiceId.set({});
    this.pulsesConsumedByServiceId.set({});
    this.selectedSlot.set(null);
    this.availability.set(null);
    this.appointmentDate.setValue(this.minDate, { emitEvent: false });
    this.appointmentNotes.setValue('');
    this.appointmentStatus.setValue(LaserAppointmentStatus.Pending);
  }

  startNewAppointment(showToast = true): void {
    this.clearAppointmentEditor();
    if (showToast) {
      this.toast.success('جاهزة لتسجيل جلسة جديدة منفصلة');
    }
  }

  openSession(appt: LaserAppointmentDto): void {
    void this.router.navigate(['/app/appointments', appt.id]);
  }

  sessionLabel(apptId: string): string {
    const found = this.sessionList().find((s) => s.appt.id === apptId);
    return found ? `جلسة ${found.sessionNumber}` : 'جلسة';
  }

  toggleService(id: string): void {
    const previousStart = this.selectedSlot()?.startTime ?? null;
    const current = this.selectedServiceIds();
    const removing = current.includes(id);
    this.selectedServiceIds.set(removing ? current.filter((x) => x !== id) : [...current, id]);
    if (removing) {
      const nextDur = { ...this.pulseDurationByServiceId() };
      const nextPulses = { ...this.pulsesConsumedByServiceId() };
      delete nextDur[id];
      delete nextPulses[id];
      this.pulseDurationByServiceId.set(nextDur);
      this.pulsesConsumedByServiceId.set(nextPulses);
    } else {
      const svc = this.services().find((s) => s.id === id);
      if (svc && serviceRequiresManualDuration(svc)) {
        this.pulseDurationByServiceId.set({ ...this.pulseDurationByServiceId(), [id]: null });
        this.pulsesConsumedByServiceId.set({ ...this.pulsesConsumedByServiceId(), [id]: null });
      }
    }
    this.selectedSlot.set(null);
    this.reloadAvailability(previousStart);
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

  setPulsesConsumed(serviceId: string, raw: string): void {
    const parsed = Number(raw);
    const pulses = Number.isFinite(parsed) && parsed > 0 ? Math.round(parsed) : null;
    this.pulsesConsumedByServiceId.set({ ...this.pulsesConsumedByServiceId(), [serviceId]: pulses });
  }

  pulseDurationValue(serviceId: string): string {
    const value = this.pulseDurationByServiceId()[serviceId];
    return value != null && value > 0 ? String(value) : '';
  }

  pulsesConsumedValue(serviceId: string): string {
    const value = this.pulsesConsumedByServiceId()[serviceId];
    return value != null && value > 0 ? String(value) : '';
  }

  selectedServiceNames(): string {
    const names = this.selectedServices().map((s) => s.name);
    return names.length ? names.join(' + ') : '—';
  }

  reloadAvailability(preferStartTime?: string | null): void {
    const date = this.appointmentDate.value;
    const ids = this.selectedServiceIds();
    if (!date || ids.length === 0 || !this.pulseDurationsReady()) {
      this.availability.set(null);
      return;
    }
    const excludeId = this.editingAppointment()?.id;
    const overrides = this.durationOverrides();
    this.loadingAvailability.set(true);
    this.appointmentsApi
      .getAvailability(date, ids, excludeId, Object.keys(overrides).length ? overrides : undefined)
      .subscribe({
        next: (result) => {
          this.availability.set(result);
          this.loadingAvailability.set(false);
          const preferred = preferStartTime ?? this.selectedSlot()?.startTime ?? null;
          if (!preferred) {
            return;
          }
          const match = result.slots.find(
            (s) =>
              s.startTime === preferred &&
              (s.status === AvailabilitySlotStatus.Available ||
                (excludeId && s.appointmentId === excludeId))
          );
          if (match && match.status === AvailabilitySlotStatus.Available) {
            this.selectedSlot.set(match);
          } else if (match && excludeId && match.appointmentId === excludeId) {
            this.selectedSlot.set({
              startTime: preferred,
              endTime: match.endTime || preferred,
              durationMinutes: result.clinicalDurationMinutes,
              status: AvailabilitySlotStatus.Available,
              appointmentId: excludeId,
              customerName: match.customerName,
              serviceNames: match.serviceNames,
              bookedDurationMinutes: match.bookedDurationMinutes
            });
          } else {
            const still = result.slots.find(
              (s) => s.startTime === preferred && s.status === AvailabilitySlotStatus.Available
            );
            this.selectedSlot.set(still ?? null);
          }
        },
        error: () => {
          this.availability.set(null);
          this.loadingAvailability.set(false);
        }
      });
  }

  selectSlot(slot: AvailabilitySlotDto): void {
    if (slot.status !== AvailabilitySlotStatus.Available) return;
    this.selectedSlot.set(slot);
  }

  slotClass(slot: AvailabilitySlotDto): string {
    const selected = this.selectedSlot()?.startTime === slot.startTime;
    if (selected) return 'day-slot day-slot--selected';
    if (slot.status === AvailabilitySlotStatus.Available) return 'day-slot day-slot--available';
    if (slot.status === AvailabilitySlotStatus.Booked) return 'day-slot day-slot--booked';
    return 'day-slot day-slot--unavailable';
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_LABELS[status];
  }

  badgeClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BADGE[status];
  }

  saveProfile(options?: { navigate?: boolean }): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.toast.error(
        this.profileForm.controls.phoneNumber.hasError('egyptMobile')
          ? 'رقم الموبايل لازم يكون مصري، مثال: 01012345678'
          : 'أكملي بيانات العميلة'
      );
      return;
    }
    const id = this.customerId();
    if (!id) return;
    const raw = this.profileForm.getRawValue();
    this.savingProfile.set(true);
    this.api
      .update(id, {
        fullName: raw.fullName.trim(),
        phoneNumber: normalizeEgyptianMobile(raw.phoneNumber) ?? raw.phoneNumber.trim(),
        age: raw.age,
        notes: raw.notes.trim() || null
      })
      .subscribe({
        next: () => {
          this.toast.success('تم حفظ بيانات العميلة');
          this.savingProfile.set(false);
          if (options?.navigate) {
            void this.router.navigate(['/app/customers']);
          }
        },
        error: () => {
          this.savingProfile.set(false);
        }
      });
  }

  saveAppointment(options?: { navigate?: boolean }): void {
    const customerId = this.customerId();
    if (!customerId) return;
    if (this.selectedServiceIds().length === 0) {
      this.toast.error('اختاري منطقة واحدة على الأقل');
      return;
    }
    if (!this.pulseDurationsReady()) {
      this.toast.error('أدخلي مدة الجلسة للنبضات');
      return;
    }
    const slot = this.selectedSlot();
    if (!slot) {
      this.toast.error('اختاري موعداً متاحاً');
      return;
    }

    const navigate = options?.navigate ?? false;
    this.savingAppointment.set(true);
    const editing = this.editingAppointment();
    const overrides = this.durationOverrideList();
    const payload = {
      appointmentDate: this.appointmentDate.value,
      startTime: toApiTime(slot.startTime),
      laserServiceIds: [...this.selectedServiceIds()],
      notes: this.appointmentNotes.value.trim() || null,
      serviceDurationOverrides: overrides.length ? overrides : null
    };

    const afterSave = (appt: LaserAppointmentDto) => {
      const finish = (finalAppt: LaserAppointmentDto, statusSaved = true) => {
        const desiredStatus = Number(this.appointmentStatus.value) as LaserAppointmentStatus;
        const appliedStatus = statusSaved ? desiredStatus : finalAppt.status;
        const completed =
          appliedStatus === LaserAppointmentStatus.Attended ||
          appliedStatus === LaserAppointmentStatus.NoShow ||
          appliedStatus === LaserAppointmentStatus.Cancelled;

        if (statusSaved) {
          this.toast.success(
            editing
              ? completed
                ? `تم حفظ ${this.sessionLabel(finalAppt.id)} — يمكنك فتح جلسة جديدة`
                : `تم تحديث ${this.sessionLabel(finalAppt.id)}`
              : 'تم إنشاء جلسة جديدة'
          );
        }
        this.savingAppointment.set(false);

        this.api.getHistory(customerId).subscribe({
          next: (h) => {
            this.history.set(h);
            if (navigate) {
              void this.router.navigate(['/app/customers']);
              return;
            }
            if (completed) {
              this.startNewAppointment(false);
            } else {
              this.beginEditAppointment(finalAppt);
            }
          }
        });
      };

      const desiredStatus = Number(this.appointmentStatus.value) as LaserAppointmentStatus;
      if (desiredStatus !== appt.status) {
        this.appointmentsApi.updateStatus(appt.id, desiredStatus).subscribe({
          next: (updated) => finish(updated),
          error: (err) => {
            const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
            this.toast.error(detail || 'تسجيل «حضرت» يتم في يوم الموعد وبعد وقت البداية فقط.');
            finish(appt, false);
          }
        });
      } else {
        finish(appt);
      }
    };

    if (editing) {
      this.appointmentsApi.update(editing.id, payload).subscribe({
        next: afterSave,
        error: () => {
          this.savingAppointment.set(false);
          this.reloadAvailability();
        }
      });
    } else {
      this.appointmentsApi
        .create({
          customerId,
          ...payload
        })
        .subscribe({
          next: afterSave,
          error: () => {
            this.savingAppointment.set(false);
            this.reloadAvailability();
          }
        });
    }
  }

  async cancelAppointment(): Promise<void> {
    const editing = this.editingAppointment();
    const customerId = this.customerId();
    if (!editing || !customerId) return;
    const ok = await this.confirm.confirm({
      title: 'إلغاء الموعد',
      message: `إلغاء موعد ${formatDateAr(editing.appointmentDate)} ${formatTimeAr(editing.startTime)}؟`,
      variant: 'danger',
      confirmLabel: 'إلغاء الموعد'
    });
    if (!ok) return;
    this.appointmentsApi.cancel(editing.id).subscribe({
      next: () => {
        this.toast.success('تم إلغاء الموعد');
        void this.router.navigate(['/app/customers']);
      }
    });
  }

  saveAll(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.toast.error(
        this.profileForm.controls.phoneNumber.hasError('egyptMobile')
          ? 'رقم الموبايل لازم يكون مصري، مثال: 01012345678'
          : 'أكملي بيانات العميلة'
      );
      return;
    }
    const id = this.customerId();
    if (!id) return;

    const hasAppointment = this.selectedServiceIds().length > 0 && !!this.selectedSlot();
    const raw = this.profileForm.getRawValue();
    this.savingProfile.set(true);

    this.api
      .update(id, {
        fullName: raw.fullName.trim(),
        phoneNumber: normalizeEgyptianMobile(raw.phoneNumber) ?? raw.phoneNumber.trim(),
        age: raw.age,
        notes: raw.notes.trim() || null
      })
      .subscribe({
        next: () => {
          this.savingProfile.set(false);
          if (hasAppointment) {
            this.saveAppointment({ navigate: true });
          } else {
            this.toast.success('تم حفظ بيانات العميلة');
            void this.router.navigate(['/app/customers']);
          }
        },
        error: () => {
          this.savingProfile.set(false);
        }
      });
  }
}
