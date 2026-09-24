import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { Permissions } from '../../core/permissions/permissions';
import { LaserDashboardApi } from '../laser-clinic/services/laser-dashboard-api.service';
import { CustomersApi } from '../laser-clinic/services/customers-api.service';
import {
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  LaserDashboardDto,
  formatTimeAr
} from '../laser-clinic/models/laser-clinic.models';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';
import { TimeSpanComponent } from '../../shared/components/time-span/time-span.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinnerComponent, HasPermissionDirective, DecimalPipe, TimeSpanComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly api = inject(LaserDashboardApi);
  private readonly customersApi = inject(CustomersApi);

  readonly permissions = Permissions;
  readonly user = this.auth.user;
  readonly loading = signal(true);
  readonly data = signal<LaserDashboardDto | null>(null);
  readonly customerCount = signal(0);
  readonly date = new FormControl(new Date().toISOString().slice(0, 10), { nonNullable: true });
  readonly formatTimeAr = formatTimeAr;

  ngOnInit(): void {
    this.load();
    this.date.valueChanges.subscribe(() => this.load());
  }

  greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'صباح الخير';
    if (hour < 18) return 'مساء الخير';
    return 'مساء الخير';
  }

  firstName(): string {
    const name = this.user()?.fullName?.trim();
    if (!name) return 'ضيف';
    return name.split(/\s+/)[0];
  }

  load(): void {
    this.loading.set(true);
    forkJoin({
      dash: this.api.get(this.date.value),
      customers: this.customersApi.search(undefined, true, 'LastAppointment')
    }).subscribe({
      next: ({ dash, customers }) => {
        this.data.set(dash);
        this.customerCount.set(customers.length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  serviceNames(row: LaserAppointmentDto): string {
    return row.services?.map((s) => s.serviceName).join(' + ') || '—';
  }

  statusLabel(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_LABELS[status];
  }

  badgeClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BADGE[status];
  }
}
