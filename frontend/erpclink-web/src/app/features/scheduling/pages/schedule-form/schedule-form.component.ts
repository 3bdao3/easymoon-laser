import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ReferenceApiService } from '../../../../shared/services/reference-api.service';
import { toHtmlTimeValue, toTimeOnlyPayload } from '../../../../shared/utils/time.utils';
import {
  CreateDoctorScheduleRequest,
  UpdateDoctorScheduleRequest
} from '../../models/scheduling.models';
import { SchedulingApiService } from '../../services/scheduling-api.service';

@Component({
  selector: 'app-schedule-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './schedule-form.component.html',
  styleUrl: './schedule-form.component.scss'
})
export class ScheduleFormComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly toast = inject(ToastService);

  readonly scheduleId = signal<string | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly clinicOptions = signal<{ id: string; label: string }[]>([]);

  readonly dayOptions = [
    { value: 0, label: 'الأحد' },
    { value: 1, label: 'الإثنين' },
    { value: 2, label: 'الثلاثاء' },
    { value: 3, label: 'الأربعاء' },
    { value: 4, label: 'الخميس' },
    { value: 5, label: 'الجمعة' },
    { value: 6, label: 'السبت' }
  ];

  readonly form = this.fb.group({
    doctorId: ['', Validators.required],
    clinicId: ['', Validators.required],
    dayOfWeek: [1, [Validators.required, Validators.min(0), Validators.max(6)]],
    startTime: ['09:00', Validators.required],
    endTime: ['17:00', Validators.required],
    slotDurationMinutes: [15, [Validators.required, Validators.min(5), Validators.max(240)]],
    effectiveFrom: ['', Validators.required],
    effectiveTo: ['']
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    const doctorId = this.route.snapshot.queryParamMap.get('doctorId') ?? '';
    if (doctorId) {
      this.form.controls.doctorId.setValue(doctorId);
      this.form.controls.doctorId.disable();
    }
    this.loadClinics();
    if (id) {
      this.scheduleId.set(id);
      this.form.controls.doctorId.disable();
      this.form.controls.clinicId.disable();
      this.loadSchedule(id);
    } else if (!this.form.controls.effectiveFrom.value) {
      this.form.controls.effectiveFrom.setValue(this.todayIso());
    }
  }

  get isEdit(): boolean {
    return this.scheduleId() !== null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const raw = this.form.getRawValue();
    this.submitting.set(true);
    if (this.isEdit) {
      const body: UpdateDoctorScheduleRequest = {
        dayOfWeek: raw.dayOfWeek,
        startTime: toTimeOnlyPayload(raw.startTime),
        endTime: toTimeOnlyPayload(raw.endTime),
        slotDurationMinutes: raw.slotDurationMinutes,
        effectiveFrom: raw.effectiveFrom,
        effectiveTo: raw.effectiveTo.trim() ? raw.effectiveTo : null
      };
      this.schedulingApi
        .updateSchedule(this.scheduleId()!, body)
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe({
          next: () => {
            this.toast.success('تم تحديث الجدول');
            void this.router.navigate(['/app/scheduling']);
          }
        });
      return;
    }

    const body: CreateDoctorScheduleRequest = {
      doctorId: raw.doctorId,
      clinicId: raw.clinicId,
      dayOfWeek: raw.dayOfWeek,
      startTime: toTimeOnlyPayload(raw.startTime),
      endTime: toTimeOnlyPayload(raw.endTime),
      slotDurationMinutes: raw.slotDurationMinutes,
      effectiveFrom: raw.effectiveFrom,
      effectiveTo: raw.effectiveTo.trim() ? raw.effectiveTo : null
    };
    this.schedulingApi
      .createSchedule(body)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.toast.success('تم إنشاء الجدول');
          void this.router.navigate(['/app/scheduling']);
        }
      });
  }

  private loadSchedule(id: string): void {
    this.loading.set(true);
    this.schedulingApi
      .getSchedule(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (schedule) => {
          this.form.patchValue({
            doctorId: schedule.doctorId,
            clinicId: schedule.clinicId,
            dayOfWeek: schedule.dayOfWeek,
            startTime: toHtmlTimeValue(schedule.startTime),
            endTime: toHtmlTimeValue(schedule.endTime),
            slotDurationMinutes: schedule.slotDurationMinutes,
            effectiveFrom: schedule.effectiveFrom,
            effectiveTo: schedule.effectiveTo ?? ''
          });
        }
      });
  }

  private loadClinics(): void {
    this.referenceApi.searchClinics({ isActive: true, page: 1, pageSize: 100 }).subscribe({
      next: (result) =>
        this.clinicOptions.set(
          result.items.map((c) => ({ id: c.id, label: `${c.name} (${c.code})` }))
        )
    });
  }

  private todayIso(): string {
    const d = new Date();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }
}
