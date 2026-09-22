import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LaserDashboardApi } from '../../laser-clinic/services/laser-dashboard-api.service';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  LaserDashboardDto,
  formatTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-laser-reports',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './laser-reports.component.html',
  styleUrl: './laser-reports.component.scss'
})
export class LaserReportsComponent implements OnInit {
  private readonly dashboardApi = inject(LaserDashboardApi);
  private readonly appointmentsApi = inject(LaserAppointmentsApi);

  readonly loading = signal(true);
  readonly stats = signal<LaserDashboardDto | null>(null);
  readonly appointments = signal<LaserAppointmentDto[]>([]);
  readonly date = new FormControl(new Date().toISOString().slice(0, 10), { nonNullable: true });
  readonly formatTime = formatTime;

  ngOnInit(): void {
    this.load();
    this.date.valueChanges.subscribe(() => this.load());
  }

  load(): void {
    this.loading.set(true);
    const d = this.date.value;
    this.dashboardApi.get(d).subscribe({
      next: (dash) => {
        this.stats.set(dash);
        this.appointmentsApi.list(d).subscribe({
          next: (rows) => {
            this.appointments.set(rows);
            this.loading.set(false);
          },
          error: () => this.loading.set(false)
        });
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
}
