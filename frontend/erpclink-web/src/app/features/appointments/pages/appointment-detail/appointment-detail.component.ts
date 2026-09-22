import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { Permissions } from '../../../../core/permissions/permissions';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { formatDateOnly, formatDateTimeUtc, formatTimeOnly } from '../../../../shared/utils/date.utils';
import { toTimeOnlyPayload, toHtmlTimeValue } from '../../../../shared/utils/time.utils';
import { SchedulingApiService } from '../../../scheduling/services/scheduling-api.service';
import { Appointment } from '../../models/appointment.models';
import { AppointmentsApiService } from '../../services/appointments-api.service';
import { appointmentActionFlags } from '../../utils/appointment-actions.util';

@Component({
  selector: 'app-appointment-detail',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './appointment-detail.component.html',
  styleUrl: './appointment-detail.component.scss'
})
export class AppointmentDetailComponent {
  readonly permissions = Permissions;
  readonly formatDateOnly = formatDateOnly;
  readonly formatTimeOnly = formatTimeOnly;
  readonly formatDateTimeUtc = formatDateTimeUtc;

  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(AppointmentsApiService);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly appointment = signal<Appointment | null>(null);
  readonly loading = signal(true);
  readonly actionBusy = signal(false);
  readonly showReschedule = signal(false);
  readonly rescheduleDate = signal('');
  readonly rescheduleTime = signal('');
  readonly cancelReason = signal('');

  readonly actions = computed(() => {
    const appt = this.appointment();
    if (!appt) {
      return appointmentActionFlags('Scheduled');
    }
    return appointmentActionFlags(appt.status);
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.load(id);
    } else {
      this.loading.set(false);
    }
  }

  load(id?: string): void {
    const appointmentId = id ?? this.appointment()?.id;
    if (!appointmentId) {
      return;
    }
    this.loading.set(true);
    this.api
      .getById(appointmentId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: (a) => this.appointment.set(a) });
  }

  async confirmAppointment(): Promise<void> {
    const appt = this.appointment();
    if (!appt) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'تأكيد الموعد',
      message: `تأكيد الموعد ${appt.appointmentNumber}؟`
    });
    if (!ok) {
      return;
    }
    this.runAction(() => this.api.confirm(appt.id), 'تم تأكيد الموعد');
  }

  async cancelAppointment(): Promise<void> {
    const appt = this.appointment();
    if (!appt) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'إلغاء الموعد',
      message: 'هل تريد إلغاء هذا الموعد؟',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.runAction(
      () => this.api.cancel(appt.id, { reason: this.cancelReason().trim() || null }),
      'تم إلغاء الموعد'
    );
  }

  async markNoShow(): Promise<void> {
    const appt = this.appointment();
    if (!appt) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'لم يحضر',
      message: 'تسجيل الموعد كـ «لم يحضر»؟',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.runAction(() => this.api.markNoShow(appt.id), 'تم تسجيل عدم الحضور');
  }

  openReschedule(): void {
    const appt = this.appointment();
    if (!appt) {
      return;
    }
    this.rescheduleDate.set(appt.appointmentDate);
    this.rescheduleTime.set(toHtmlTimeValue(appt.startTime));
    this.showReschedule.set(true);
  }

  submitReschedule(): void {
    const appt = this.appointment();
    const date = this.rescheduleDate().trim();
    const time = this.rescheduleTime().trim();
    if (!appt || !date || !time) {
      return;
    }
    this.actionBusy.set(true);
    this.api
      .reschedule(appt.id, { date, startTime: toTimeOnlyPayload(time) })
      .pipe(finalize(() => this.actionBusy.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('تم إعادة جدولة الموعد');
          this.showReschedule.set(false);
          this.load();
        },
        error: (err: unknown) => {
          if (err instanceof HttpErrorResponse && err.status === 409 && appt) {
            this.reloadAvailabilityHint(appt);
          }
        }
      });
  }

  private reloadAvailabilityHint(appt: Appointment): void {
    this.schedulingApi.getAvailability(appt.doctorId, appt.appointmentDate, appt.clinicId).subscribe();
  }

  private runAction(request: () => ReturnType<AppointmentsApiService['confirm']>, successMsg: string): void {
    this.actionBusy.set(true);
    request()
      .pipe(finalize(() => this.actionBusy.set(false)))
      .subscribe({
        next: () => {
          this.toast.success(successMsg);
          this.load();
        }
      });
  }
}
