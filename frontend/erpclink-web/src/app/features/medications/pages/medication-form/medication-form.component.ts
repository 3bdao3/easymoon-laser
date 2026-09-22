import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MedicationsApiService } from '../../services/medications-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-medication-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './medication-form.component.html',
  styleUrls: ['./medication-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class MedicationFormComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MedicationsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly medicationId = this.route.snapshot.paramMap.get('id');
  readonly isEdit = !!this.medicationId;

  readonly loading = signal(this.isEdit);
  readonly submitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    code: [{ value: '', disabled: this.isEdit }, [Validators.required, Validators.maxLength(64)]],
    name: ['', [Validators.required, Validators.maxLength(256)]],
    genericName: [''],
    strength: [''],
    dosageForm: [''],
    route: ['']
  });

  constructor() {
    if (this.medicationId) {
      this.api.getById(this.medicationId).subscribe({
        next: (med) => {
          this.form.patchValue({
            code: med.code,
            name: med.name,
            genericName: med.genericName ?? '',
            strength: med.strength ?? '',
            dosageForm: med.dosageForm ?? '',
            route: med.route ?? ''
          });
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
    }
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const raw = this.form.getRawValue();

    if (this.isEdit && this.medicationId) {
      this.api
        .update(this.medicationId, {
          name: raw.name,
          genericName: raw.genericName || null,
          strength: raw.strength || null,
          dosageForm: raw.dosageForm || null,
          route: raw.route || null
        })
        .subscribe({
          next: () => {
            this.submitting.set(false);
            this.toast.success('تم تحديث الدواء.');
            void this.router.navigate(['/app/medications']);
          },
          error: (err: HttpErrorResponse) => {
            this.submitting.set(false);
            this.toast.error(this.errors.resolveMessage(err));
          }
        });
    } else {
      this.api
        .create({
          code: raw.code,
          name: raw.name,
          genericName: raw.genericName || null,
          strength: raw.strength || null,
          dosageForm: raw.dosageForm || null,
          route: raw.route || null
        })
        .subscribe({
          next: () => {
            this.submitting.set(false);
            this.toast.success('تم إنشاء الدواء.');
            void this.router.navigate(['/app/medications']);
          },
          error: (err: HttpErrorResponse) => {
            this.submitting.set(false);
            this.toast.error(this.errors.resolveMessage(err));
          }
        });
    }
  }
}
