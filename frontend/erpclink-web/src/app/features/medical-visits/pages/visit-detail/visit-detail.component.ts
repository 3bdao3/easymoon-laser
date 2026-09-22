import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MedicalVisitsApiService } from '../../services/medical-visits-api.service';
import {
  isMedicalVisitNotesEditable,
  MedicalVisitDto
} from '../../models/medical-visit.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { formatDateOnly, formatDateTimeUtc } from '../../../../shared/utils/date.utils';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-visit-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './visit-detail.component.html',
  styleUrls: ['./visit-detail.component.scss', '../../_feature-layout.scss']
})
export class VisitDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MedicalVisitsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly formatDate = formatDateOnly;
  readonly formatDateTime = formatDateTimeUtc;

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly visit = signal<MedicalVisitDto | null>(null);

  readonly notesForm = this.fb.nonNullable.group({
    chiefComplaint: [''],
    clinicalNotes: [''],
    examinationFindings: [''],
    diagnosisNotes: [''],
    followUpNotes: ['']
  });

  readonly notesEditable = computed(() => {
    const v = this.visit();
    return v ? isMedicalVisitNotesEditable(v.status) : false;
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.load(id);
    } else {
      this.loading.set(false);
    }
  }

  load(id: string): void {
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (dto) => {
        this.visit.set(dto);
        this.patchNotes(dto);
        this.applyNotesDisabled();
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  saveNotes(): void {
    const v = this.visit();
    if (!v || !this.notesEditable() || this.saving()) {
      return;
    }

    this.saving.set(true);
    const raw = this.notesForm.getRawValue();
    this.api
      .updateClinicalNotes(v.id, {
        chiefComplaint: raw.chiefComplaint || null,
        clinicalNotes: raw.clinicalNotes || null,
        examinationFindings: raw.examinationFindings || null,
        diagnosisNotes: raw.diagnosisNotes || null,
        followUpNotes: raw.followUpNotes || null,
        rowVersion: v.rowVersion
      })
      .subscribe({
        next: (updated) => {
          this.visit.set(updated);
          this.patchNotes(updated);
          this.saving.set(false);
          this.toast.success('تم حفظ الملاحظات السريرية.');
        },
        error: (err: HttpErrorResponse) => {
          this.saving.set(false);
          this.toast.error(this.errors.resolveMessage(err));
          if (err.status === 409 && v.id) {
            this.load(v.id);
          }
        }
      });
  }

  async completeVisit(): Promise<void> {
    const v = this.visit();
    if (!v) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'إكمال الزيارة',
      message: 'هل تريد تأكيد إكمال هذه الزيارة؟',
      confirmLabel: 'إكمال'
    });
    if (!ok) {
      return;
    }

    this.api.complete(v.id).subscribe({
      next: () => {
        this.toast.success('تم إكمال الزيارة.');
        this.load(v.id);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  async cancelVisit(): Promise<void> {
    const v = this.visit();
    if (!v) {
      return;
    }
    const ok = await this.confirm.confirm({
      title: 'إلغاء الزيارة',
      message: 'هل تريد إلغاء هذه الزيارة؟',
      confirmLabel: 'إلغاء الزيارة',
      variant: 'danger'
    });
    if (!ok) {
      return;
    }

    this.api.cancel(v.id, { reason: null }).subscribe({
      next: () => {
        this.toast.success('تم إلغاء الزيارة.');
        this.load(v.id);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  prescriptionCreateLink(): string[] {
    const v = this.visit();
    return v ? ['/app/prescriptions/new'] : ['/app/prescriptions/new'];
  }

  prescriptionQueryParams(): { medicalVisitId: string } | null {
    const v = this.visit();
    return v ? { medicalVisitId: v.id } : null;
  }

  private patchNotes(v: MedicalVisitDto): void {
    this.notesForm.patchValue({
      chiefComplaint: v.chiefComplaint ?? '',
      clinicalNotes: v.clinicalNotes ?? '',
      examinationFindings: v.examinationFindings ?? '',
      diagnosisNotes: v.diagnosisNotes ?? '',
      followUpNotes: v.followUpNotes ?? ''
    });
  }

  private applyNotesDisabled(): void {
    const editable = this.notesEditable();
    if (editable) {
      this.notesForm.enable({ emitEvent: false });
    } else {
      this.notesForm.disable({ emitEvent: false });
    }
  }
}
