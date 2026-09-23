import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Permissions } from '../../../core/permissions/permissions';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  formatTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { TimeSpanComponent } from '../../../shared/components/time-span/time-span.component';

@Component({
  selector: 'app-laser-appointment-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    HasPermissionDirective,
    TimeSpanComponent
  ],
  templateUrl: './laser-appointment-list.component.html',
  styleUrl: './laser-appointment-list.component.scss'
})
export class LaserAppointmentListComponent implements OnInit {
  private readonly api = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly items = signal<LaserAppointmentDto[]>([]);
  readonly date = new FormControl(new Date().toISOString().slice(0, 10), { nonNullable: true });
  readonly statusLabels = LASER_APPOINTMENT_STATUS_LABELS;
  readonly statusBadge = LASER_APPOINTMENT_STATUS_BADGE;
  readonly statuses = [
    LaserAppointmentStatus.Pending,
    LaserAppointmentStatus.Confirmed,
    LaserAppointmentStatus.Attended,
    LaserAppointmentStatus.Cancelled,
    LaserAppointmentStatus.NoShow
  ];
  readonly formatTime = formatTime;

  ngOnInit(): void {
    this.load();
    this.date.valueChanges.subscribe(() => this.load());
  }

  load(): void {
    this.loading.set(true);
    this.api.list(this.date.value).subscribe({
      next: (rows) => {
        this.items.set(rows);
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

  serviceNames(row: LaserAppointmentDto): string {
    return row.services.map((s) => s.serviceName).join('، ');
  }

  onStatusChange(row: LaserAppointmentDto, value: LaserAppointmentStatus | string): void {
    this.updateStatus(row, Number(value) as LaserAppointmentStatus);
  }

  updateStatus(row: LaserAppointmentDto, status: LaserAppointmentStatus): void {
    if (row.status === status) {
      return;
    }
    this.api.updateStatus(row.id, status).subscribe({
      next: () => {
        this.toast.success('تم تحديث الحالة');
        this.load();
      },
      error: (err) => {
        const detail = err?.error?.detail || err?.error?.title || err?.error?.message;
        this.toast.error(detail || 'تسجيل «حضرت» يتم في يوم الموعد وبعد وقت البداية فقط.');
        this.load();
      }
    });
  }

  async cancel(row: LaserAppointmentDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'إلغاء الموعد',
      message: `إلغاء موعد ${row.customerName}؟`,
      variant: 'danger',
      confirmLabel: 'إلغاء الموعد'
    });
    if (!ok) {
      return;
    }
    this.api.cancel(row.id).subscribe({
      next: () => {
        this.toast.success('تم إلغاء الموعد');
        this.load();
      }
    });
  }
}
