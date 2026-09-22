import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Permissions } from '../../../../core/permissions/permissions';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { DoctorListItem } from '../../../../shared/models/reference.models';
import { ReferenceApiService } from '../../../../shared/services/reference-api.service';
import { formatDateOnly, formatTimeOnly } from '../../../../shared/utils/date.utils';
import { DAY_OF_WEEK_LABELS } from '../../../../shared/utils/time.utils';
import { ToastService } from '../../../../core/services/toast.service';
import { DoctorSchedule } from '../../models/scheduling.models';
import { SchedulingApiService } from '../../services/scheduling-api.service';

@Component({
  selector: 'app-schedule-list',
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
  templateUrl: './schedule-list.component.html',
  styleUrl: './schedule-list.component.scss'
})
export class ScheduleListComponent {
  readonly permissions = Permissions;
  readonly formatDateOnly = formatDateOnly;
  readonly formatTimeOnly = formatTimeOnly;
  readonly dayLabel = (day: number) => DAY_OF_WEEK_LABELS[day] ?? String(day);

  private readonly referenceApi = inject(ReferenceApiService);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly doctorQuery = signal('');
  readonly doctorResults = signal<DoctorListItem[]>([]);
  readonly selectedDoctor = signal<DoctorListItem | null>(null);
  readonly schedules = signal<DoctorSchedule[]>([]);
  readonly loadingDoctors = signal(false);
  readonly loadingSchedules = signal(false);
  readonly actionId = signal<string | null>(null);

  searchDoctors(): void {
    const query = this.doctorQuery().trim();
    if (!query) {
      this.doctorResults.set([]);
      return;
    }
    this.loadingDoctors.set(true);
    this.referenceApi
      .searchDoctors({ query, isActive: true, page: 1, pageSize: 10 })
      .pipe(finalize(() => this.loadingDoctors.set(false)))
      .subscribe({
        next: (result) => this.doctorResults.set(result.items),
        error: () => this.doctorResults.set([])
      });
  }

  selectDoctor(doctor: DoctorListItem): void {
    this.selectedDoctor.set(doctor);
    this.doctorResults.set([]);
    this.doctorQuery.set(`${doctor.displayName} (${doctor.doctorNumber})`);
    this.loadSchedules();
  }

  loadSchedules(): void {
    const doctor = this.selectedDoctor();
    if (!doctor) {
      this.schedules.set([]);
      return;
    }
    this.loadingSchedules.set(true);
    this.schedulingApi
      .listByDoctor(doctor.id)
      .pipe(finalize(() => this.loadingSchedules.set(false)))
      .subscribe({
        next: (items) => this.schedules.set(items),
        error: () => this.schedules.set([])
      });
  }

  async toggleActive(schedule: DoctorSchedule): Promise<void> {
    const activate = !schedule.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل الجدول' : 'إيقاف الجدول',
      message: activate
        ? 'هل تريد تفعيل هذا الجدول؟'
        : 'هل تريد إيقاف هذا الجدول؟',
      variant: activate ? 'default' : 'danger'
    });
    if (!ok) {
      return;
    }
    this.actionId.set(schedule.id);
    const request$ = activate
      ? this.schedulingApi.activateSchedule(schedule.id)
      : this.schedulingApi.deactivateSchedule(schedule.id);
    request$.pipe(finalize(() => this.actionId.set(null))).subscribe({
      next: () => {
        this.toast.success(activate ? 'تم تفعيل الجدول' : 'تم إيقاف الجدول');
        this.loadSchedules();
      }
    });
  }
}
