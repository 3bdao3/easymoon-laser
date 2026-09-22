import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ToastService } from '../../../core/services/toast.service';
import { ClinicsApi } from '../services/clinics.api';
import {
  createClinicEditForm,
  formToUpdateRequest,
  patchClinicEditForm
} from '../utils/clinic-form.factory';

@Component({
  selector: 'app-clinic-edit',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent],
  templateUrl: './clinic-edit.component.html'
})
export class ClinicEditComponent implements OnInit {
  private readonly api = inject(ClinicsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly form = createClinicEditForm();
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly code = signal('');
  private clinicId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/clinics']);
      return;
    }
    this.clinicId = id;
    this.api.getById(id).subscribe({
      next: (clinic) => {
        this.code.set(clinic.code);
        patchClinicEditForm(this.form, clinic);
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/clinics'])
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.api.update(this.clinicId, formToUpdateRequest(this.form)).subscribe({
      next: () => {
        this.toast.success('تم الحفظ');
        void this.router.navigate(['/app/clinics']);
      },
      error: () => this.submitting.set(false)
    });
  }
}
