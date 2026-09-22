import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Permissions } from '../../../core/permissions/permissions';
import { CustomersApi } from '../../laser-clinic/services/customers-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  CustomerListItemDto,
  CustomerListSort,
  CustomerNextAppointmentDto,
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentStatus,
  formatDateAr,
  formatTimeAr,
  relativeDayLabel
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    HasPermissionDirective
  ],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {
  private readonly api = inject(CustomersApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly router = inject(Router);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly updatingStatusId = signal<string | null>(null);
  readonly items = signal<CustomerListItemDto[]>([]);
  readonly query = new FormControl('', { nonNullable: true });
  readonly sort = new FormControl<CustomerListSort>('Name', { nonNullable: true });

  readonly formatDateAr = formatDateAr;
  readonly formatTimeAr = formatTimeAr;
  readonly editableStatuses = [
    LaserAppointmentStatus.Pending,
    LaserAppointmentStatus.Confirmed,
    LaserAppointmentStatus.Attended,
    LaserAppointmentStatus.NoShow
  ];

  ngOnInit(): void {
    this.load();
    this.query.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => this.load());
    this.sort.valueChanges.subscribe(() => this.load());
  }

  load(): void {
    this.loading.set(true);
    this.api.search(this.query.value.trim() || undefined, true, this.sort.value).subscribe({
      next: (rows) => {
        this.items.set(rows);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  goNewCustomer(): void {
    void this.router.navigate(['/app/customers/new']);
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_LABELS[status];
  }

  badgeClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BADGE[status];
  }

  onStatusChange(row: CustomerListItemDto, next: CustomerNextAppointmentDto, raw: string): void {
    const status = Number(raw) as LaserAppointmentStatus;
    if (status === next.status) {
      return;
    }
    this.updatingStatusId.set(next.id);
    this.appointmentsApi.updateStatus(next.id, status).subscribe({
      next: (updated) => {
        this.items.set(
          this.items().map((item) =>
            item.id !== row.id || !item.nextAppointment
              ? item
              : {
                  ...item,
                  nextAppointment: {
                    ...item.nextAppointment,
                    status: updated.status
                  }
                }
          )
        );
        this.updatingStatusId.set(null);
        this.toast.success('تم تحديث الحالة');
      },
      error: () => {
        this.updatingStatusId.set(null);
        this.load();
      }
    });
  }

  servicesPreview(next: CustomerNextAppointmentDto): string {
    const names = next.serviceNames ?? [];
    return names.length ? names.join(' + ') : '—';
  }

  servicesTitle(next: CustomerNextAppointmentDto): string {
    return (next.serviceNames ?? []).join(' + ');
  }

  isToday(isoDate: string): boolean {
    return relativeDayLabel(isoDate) === 'today';
  }

  async deactivate(row: CustomerListItemDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'إيقاف العميلة',
      message: `إيقاف ${row.fullName}؟`,
      variant: 'danger',
      confirmLabel: 'إيقاف'
    });
    if (!ok) {
      return;
    }
    this.api.deactivate(row.id).subscribe({
      next: () => {
        this.toast.success('تم إيقاف العميلة');
        this.load();
      }
    });
  }
}
