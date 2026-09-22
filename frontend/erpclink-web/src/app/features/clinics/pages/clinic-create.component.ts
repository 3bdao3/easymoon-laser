import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ToastService } from '../../../core/services/toast.service';
import { ClinicsApi } from '../services/clinics.api';
import { createClinicForm, formToCreateRequest } from '../utils/clinic-form.factory';

@Component({
  selector: 'app-clinic-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent],
  templateUrl: './clinic-create.component.html'
})
export class ClinicCreateComponent {
  private readonly api = inject(ClinicsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly form = createClinicForm();
  readonly submitting = signal(false);

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.api.create(formToCreateRequest(this.form)).subscribe({
      next: () => {
        this.toast.success('تم إنشاء العيادة');
        void this.router.navigate(['/app/clinics']);
      },
      error: () => this.submitting.set(false)
    });
  }
}
