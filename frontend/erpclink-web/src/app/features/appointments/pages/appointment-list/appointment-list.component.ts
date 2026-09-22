import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Permissions } from '../../../../core/permissions/permissions';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { formatDateOnly, formatTimeOnly } from '../../../../shared/utils/date.utils';
import { AppointmentListItem } from '../../models/appointment.models';
import { AppointmentsApiService } from '../../services/appointments-api.service';
import { APPOINTMENT_STATUS_OPTIONS } from '../../utils/appointment-actions.util';

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './appointment-list.component.html',
  styleUrl: './appointment-list.component.scss'
})
export class AppointmentListComponent {
  readonly permissions = Permissions;
  readonly statusOptions = APPOINTMENT_STATUS_OPTIONS;
  readonly formatDateOnly = formatDateOnly;
  readonly formatTimeOnly = formatTimeOnly;

  private readonly api = inject(AppointmentsApiService);

  readonly filters = signal({
    appointmentNumber: '',
    status: '',
    date: '',
    fromDate: '',
    toDate: '',
    page: 1,
    pageSize: 20
  });

  readonly items = signal<AppointmentListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  readonly totalPages = computed(() => {
    const f = this.filters();
    return Math.max(1, Math.ceil(this.totalCount() / f.pageSize));
  });

  readonly hasPrevious = computed(() => this.filters().page > 1);
  readonly hasNext = computed(() => this.filters().page < this.totalPages());

  constructor() {
    this.load();
  }

  applyFilters(): void {
    this.filters.update((f) => ({ ...f, page: 1 }));
    this.load();
  }

  prevPage(): void {
    if (!this.hasPrevious()) {
      return;
    }
    this.filters.update((f) => ({ ...f, page: f.page - 1 }));
    this.load();
  }

  nextPage(): void {
    if (!this.hasNext()) {
      return;
    }
    this.filters.update((f) => ({ ...f, page: f.page + 1 }));
    this.load();
  }

  updateFilter(
    field: 'appointmentNumber' | 'status' | 'date' | 'fromDate' | 'toDate',
    value: string
  ): void {
    this.filters.update((f) => ({ ...f, [field]: value }));
  }

  private load(): void {
    const f = this.filters();
    this.loading.set(true);
    this.api
      .search({
        appointmentNumber: f.appointmentNumber.trim() || undefined,
        status: f.status || undefined,
        date: f.date || undefined,
        fromDate: f.fromDate || undefined,
        toDate: f.toDate || undefined,
        page: f.page,
        pageSize: f.pageSize,
        sortBy: 'appointmentdate',
        sortDescending: true
      })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: () => {
          this.items.set([]);
          this.totalCount.set(0);
        }
      });
  }
}
