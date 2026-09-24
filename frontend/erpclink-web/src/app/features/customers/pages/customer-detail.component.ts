import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Permissions } from '../../../core/permissions/permissions';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  CustomerDto,
  CustomerHistoryDto,
  CustomerPulseBalanceDto,
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  appointmentPulsesConsumed,
  formatDateAr,
  formatTimeAr,
  relativeDayLabel
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ToastService } from '../../../core/services/toast.service';
import { TimeSpanComponent } from '../../../shared/components/time-span/time-span.component';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent, HasPermissionDirective, TimeSpanComponent],
  templateUrl: './customer-detail.component.html',
  styles: `
    .detail-head {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      margin-bottom: var(--space-4);
    }
    .next-hint {
      display: inline-block;
      margin-inline-end: 0.35rem;
      padding: 0.1rem 0.45rem;
      border-radius: 999px;
      background: var(--color-primary-light);
      color: var(--color-primary-hover);
      font-size: var(--text-xs);
      font-weight: 700;
    }
    .next-appt-card {
      border-color: color-mix(in srgb, var(--color-primary) 25%, var(--color-border));
    }
    .status-select {
      min-width: 9rem;
      max-width: 100%;
      padding: 0.4rem 0.55rem;
      font-size: 0.85rem;
      font-weight: 700;
    }
    .pulse-balance-card {
      border-color: color-mix(in srgb, var(--color-primary) 25%, var(--color-border));
    }
    .pulse-remaining {
      color: var(--color-primary-hover);
      font-weight: 800;
    }
    .remaining-of {
      font-size: 0.85rem;
      font-weight: 600;
    }
    .remaining-hero .detail-item__value {
      font-size: 1.35rem;
    }
    .pulse-balance-hint {
      margin-top: var(--space-3);
      margin-bottom: 0;
      font-size: var(--text-sm);
    }
    .session-history-hint {
      margin-top: calc(var(--space-2) * -1);
      margin-bottom: var(--space-4);
    }
    .session-history-total {
      margin: var(--space-4) 0 0;
      font-weight: 800;
      color: var(--color-primary-hover);
    }
    @media (max-width: 40rem) {
      .detail-head {
        flex-direction: column;
        align-items: stretch;
      }
      .detail-head .btn {
        width: 100%;
        justify-content: center;
      }
      .status-select {
        width: 100%;
        min-width: 0;
      }
      .remaining-hero .detail-item__value {
        font-size: 1.15rem;
      }
    }
  `
})
export class CustomerDetailComponent implements OnInit {
  private readonly api = inject(CustomersApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly updatingStatus = signal(false);
  readonly customer = signal<CustomerDto | null>(null);
  readonly history = signal<CustomerHistoryDto | null>(null);

  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;
  readonly appointmentPulsesConsumed = appointmentPulsesConsumed;
  readonly editableStatuses = [
    LaserAppointmentStatus.Pending,
    LaserAppointmentStatus.Confirmed,
    LaserAppointmentStatus.Attended,
    LaserAppointmentStatus.NoShow
  ];

  readonly nextAppointment = computed(() => this.history()?.upcoming[0] ?? null);

  readonly pulseBalance = computed((): CustomerPulseBalanceDto | null => {
    return this.customer()?.pulseBalance ?? this.history()?.pulseBalance ?? null;
  });

  readonly pastAppointments = computed(() => {
    const h = this.history();
    if (!h) {
      return [];
    }
    return [...h.previous].sort((a, b) => {
      const d = b.appointmentDate.localeCompare(a.appointmentDate);
      if (d !== 0) {
        return d;
      }
      return b.startTime.localeCompare(a.startTime);
    });
  });

  readonly allSessions = computed(() => {
    const h = this.history();
    if (!h) {
      return [] as LaserAppointmentDto[];
    }
    return [...h.previous, ...h.upcoming]
      .filter((a) => a.status !== LaserAppointmentStatus.Cancelled)
      .sort((a, b) => {
        const d = a.appointmentDate.localeCompare(b.appointmentDate);
        if (d !== 0) {
          return d;
        }
        return a.startTime.localeCompare(b.startTime);
      });
  });

  readonly totalPastPaid = computed(() =>
    this.allSessions().reduce((sum, row) => sum + (row.amountPaid ?? 0), 0)
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.reload(id);
  }

  reload(id: string): void {
    this.loading.set(true);
    forkJoin({
      customer: this.api.getById(id),
      history: this.api.getHistory(id)
    }).subscribe({
      next: ({ customer, history }) => {
        this.customer.set(customer);
        this.history.set(history);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_LABELS[status];
  }

  badgeClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BADGE[status];
  }

  onNextStatusChange(appt: LaserAppointmentDto, raw: LaserAppointmentStatus | string): void {
    const status = Number(raw) as LaserAppointmentStatus;
    if (status === appt.status) {
      return;
    }
    this.updatingStatus.set(true);
    this.appointmentsApi.updateStatus(appt.id, status).subscribe({
      next: (updated) => {
        const h = this.history();
        if (h) {
          this.history.set({
            ...h,
            upcoming: h.upcoming.map((a) => (a.id === updated.id ? updated : a))
          });
        }
        this.updatingStatus.set(false);
        this.toast.success('تم تحديث الحالة');
      },
      error: (err) => {
        this.updatingStatus.set(false);
        const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
        this.toast.error(detail || 'تسجيل «حضرت» يتم في يوم الموعد وبعد وقت البداية فقط.');
        const id = this.customer()?.id;
        if (id) {
          this.reload(id);
        }
      }
    });
  }

  serviceNames(row: LaserAppointmentDto): string {
    return row.services.map((s) => s.serviceName).join(' + ') || '—';
  }

  money(value: number | null | undefined): string {
    if (value == null || Number.isNaN(Number(value))) {
      return '—';
    }
    return `${value} ج.م`;
  }

  dayHint(isoDate: string): string | null {
    const rel = relativeDayLabel(isoDate);
    if (rel === 'today') {
      return 'اليوم';
    }
    if (rel === 'tomorrow') {
      return 'غدًا';
    }
    return null;
  }
}
