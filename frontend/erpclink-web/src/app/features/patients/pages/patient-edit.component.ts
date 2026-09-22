import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { PatientFormComponent } from '../components/patient-form.component';
import { PatientsApi } from '../services/patients.api';
import { ToastService } from '../../../core/services/toast.service';
import { createPatientForm, patchPatientForm } from '../utils/patient-form.factory';
import { formToUpdateRequest } from '../utils/patient-payload.util';

@Component({
  selector: 'app-patient-edit',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, LoadingSpinnerComponent, PatientFormComponent],
  templateUrl: './patient-edit.component.html'
})
export class PatientEditComponent implements OnInit {
  private readonly api = inject(PatientsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly form = createPatientForm();
  readonly loading = signal(true);
  readonly submitting = signal(false);
  private patientId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/patients']);
      return;
    }
    this.patientId = id;
    this.api.getById(id).subscribe({
      next: (patient) => {
        patchPatientForm(this.form, patient);
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/patients'])
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    this.submitting.set(true);
    this.api.update(this.patientId, formToUpdateRequest(this.form)).subscribe({
      next: () => {
        this.toast.success('تم حفظ التعديلات');
        void this.router.navigate(['/app/patients', this.patientId]);
      },
      error: () => this.submitting.set(false)
    });
  }
}
