import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  CustomerPulseBalanceDto,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  appointmentPulsesConsumed,
  formatDateAr,
  formatTimeAr
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-session-details',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './session-details.component.html',
  styleUrl: './session-details.component.scss'
})
export class SessionDetailsComponent implements OnInit, OnDestroy {
  private readonly customersApi = inject(CustomersApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroy$ = new Subject<void>();

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly appointment = signal<LaserAppointmentDto | null>(null);
  readonly pulseBalance = signal<CustomerPulseBalanceDto | null>(null);
  readonly customerId = signal<string | null>(null);
  readonly pulsesInput = signal<number | null>(null);
  readonly durationInput = signal<number | null>(null);

  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;
  readonly statusLabels = LASER_APPOINTMENT_STATUS_LABELS;

  readonly form = new FormGroup({
    pulsesConsumed: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(1), Validators.max(5000)]
    }),
    durationMinutes: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(1), Validators.max(480)]
    }),
    amountPaid: new FormControl<number | null>(null, {
      validators: [Validators.min(0)]
    })
  });

  readonly remainingBeforeEdit = computed(() => {
    const balance = this.pulseBalance();
    const appt = this.appointment();
    if (!balance?.packageTotal) {
      return null;
    }
    const saved = appt ? appointmentPulsesConsumed(appt) ?? 0 : 0;
    return (balance.remaining ?? 0) + saved;
  });

  readonly remainingAfter = computed(() => {
    const before = this.remainingBeforeEdit();
    if (before == null) {
      return null;
    }
    const pulses = this.pulsesInput();
    if (pulses == null || pulses < 1) {
      return before;
    }
    return before - pulses;
  });

  readonly endTimePreview = computed(() => {
    const appt = this.appointment();
    const minutes = this.durationInput();
    if (!appt || minutes == null || minutes < 1) {
      return appt ? formatTimeAr(appt.endTime) : '—';
    }
    const [h, m] = appt.startTime.slice(0, 5).split(':').map(Number);
    const total = h * 60 + m + minutes;
    const eh = Math.floor(total / 60) % 24;
    const em = total % 60;
    const pad = (n: number) => String(n).padStart(2, '0');
    return formatTimeAr(`${pad(eh)}:${pad(em)}:00`);
  });

  ngOnInit(): void {
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe((v) => {
      this.pulsesInput.set(v.pulsesConsumed ?? null);
      this.durationInput.set(v.durationMinutes ?? null);
    });

    const customerId = this.route.snapshot.paramMap.get('id');
    const appointmentId = this.route.snapshot.paramMap.get('appointmentId');
    if (!customerId || !appointmentId) {
      return;
    }
    this.customerId.set(customerId);
    this.load(customerId, appointmentId);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(customerId: string, appointmentId: string): void {
    this.loading.set(true);
    forkJoin({
      appointment: this.appointmentsApi.getById(appointmentId),
      customer: this.customersApi.getById(customerId)
    }).subscribe({
      next: ({ appointment, customer }) => {
        if (appointment.customerId !== customerId) {
          this.toast.error('الجلسة لا تتبع هذه العميلة');
          void this.router.navigate(['/app/customers', customerId, 'edit']);
          return;
        }
        this.appointment.set(appointment);
        this.pulseBalance.set(customer.pulseBalance ?? null);
        const savedPulses = appointmentPulsesConsumed(appointment);
        this.form.patchValue({
          pulsesConsumed: savedPulses,
          durationMinutes: appointment.durationMinutes > 0 ? appointment.durationMinutes : null,
          amountPaid: appointment.amountPaid ?? null
        });
        this.pulsesInput.set(savedPulses);
        this.durationInput.set(appointment.durationMinutes > 0 ? appointment.durationMinutes : null);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('تعذّر تحميل تفاصيل الجلسة');
      }
    });
  }

  serviceNames(appt: LaserAppointmentDto): string {
    return appt.services.map((s) => s.serviceName).join(' + ');
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return this.statusLabels[status] ?? String(status);
  }

  save(): void {
    const appt = this.appointment();
    if (!appt) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.error('أكملي النبضات والمدة (والمبلغ إن وُجد)');
      return;
    }

    const pulses = this.form.controls.pulsesConsumed.value!;
    const duration = this.form.controls.durationMinutes.value!;
    const amount = this.form.controls.amountPaid.value;
    const after = this.remainingAfter();
    if (after != null && after < 0) {
      this.toast.error(`النبضات أكبر من المتبقي (${this.remainingBeforeEdit()})`);
      return;
    }

    this.saving.set(true);
    this.appointmentsApi
      .recordSession(appt.id, {
        durationMinutes: duration,
        pulsesConsumed: pulses,
        amountPaid: amount,
        status: LaserAppointmentStatus.Attended
      })
      .subscribe({
        next: (result) => {
          this.appointment.set(result.appointment);
          this.pulseBalance.set(result.pulseBalance);
          this.saving.set(false);
          this.toast.success(
            `تم الحفظ — المتبقي ${result.pulseBalance.remaining ?? 0} من ${result.pulseBalance.packageTotal ?? '—'}`
          );
          const customerId = this.customerId();
          if (customerId) {
            void this.router.navigate(['/app/customers', customerId, 'edit']);
          }
        },
        error: (err) => {
          this.saving.set(false);
          const msg =
            err?.error?.detail || err?.error?.title || err?.error?.message || 'تعذّر حفظ تفاصيل الجلسة';
          this.toast.error(msg);
        }
      });
  }
}
