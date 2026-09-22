import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { DoctorFormComponent } from '../components/doctor-form.component';
import { DoctorsApi } from '../services/doctors.api';
import { SpecialtiesApi } from '../../specialties/services/specialties.api';
import { SpecialtyDto } from '../../specialties/models/specialty.models';
import { ToastService } from '../../../core/services/toast.service';
import {
  createDoctorForm,
  formToUpdateRequest,
  patchDoctorForm
} from '../utils/doctor-form.factory';

@Component({
  selector: 'app-doctor-edit',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, LoadingSpinnerComponent, DoctorFormComponent],
  templateUrl: './doctor-edit.component.html'
})
export class DoctorEditComponent implements OnInit {
  private readonly api = inject(DoctorsApi);
  private readonly specialtiesApi = inject(SpecialtiesApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly form = createDoctorForm();
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly specialties = signal<SpecialtyDto[]>([]);
  private doctorId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/doctors']);
      return;
    }
    this.doctorId = id;
    this.specialtiesApi.list(true).subscribe({ next: (list) => this.specialties.set(list) });
    this.api.getById(id).subscribe({
      next: (doctor) => {
        patchDoctorForm(this.form, doctor);
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/doctors'])
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    this.submitting.set(true);
    this.api.update(this.doctorId, formToUpdateRequest(this.form)).subscribe({
      next: () => {
        this.toast.success('تم الحفظ');
        void this.router.navigate(['/app/doctors', this.doctorId]);
      },
      error: () => this.submitting.set(false)
    });
  }
}
