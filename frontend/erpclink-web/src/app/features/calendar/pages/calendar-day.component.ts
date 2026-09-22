import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LaserAppointmentsApi } from '../../laser-clinic/services/laser-appointments-api.service';
import {
  LASER_APPOINTMENT_STATUS_BADGE,
  LASER_APPOINTMENT_STATUS_BLOCK,
  LASER_APPOINTMENT_STATUS_LABELS,
  LaserAppointmentDto,
  LaserAppointmentStatus,
  formatTime
} from '../../laser-clinic/models/laser-clinic.models';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-calendar-day',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './calendar-day.component.html',
  styleUrl: './calendar-day.component.scss'
})
export class CalendarDayComponent implements OnInit {
  private readonly api = inject(LaserAppointmentsApi);

  readonly loading = signal(true);
  readonly items = signal<LaserAppointmentDto[]>([]);
  readonly date = new FormControl(new Date().toISOString().slice(0, 10), { nonNullable: true });
  readonly formatTime = formatTime;

  ngOnInit(): void {
    this.load();
    this.date.valueChanges.subscribe(() => this.load());
  }

  load(): void {
    this.loading.set(true);
    this.api.list(this.date.value).subscribe({
      next: (rows) => {
        const sorted = [...rows].sort((a, b) => a.startTime.localeCompare(b.startTime));
        this.items.set(sorted);
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

  blockClass(status: LaserAppointmentStatus): string {
    return LASER_APPOINTMENT_STATUS_BLOCK[status];
  }

  serviceNames(row: LaserAppointmentDto): string {
    return row.services.map((s) => s.serviceName).join('، ');
  }
}
