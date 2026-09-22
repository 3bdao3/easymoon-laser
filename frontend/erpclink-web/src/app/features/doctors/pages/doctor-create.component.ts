import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { DoctorFormComponent } from '../components/doctor-form.component';
import { DoctorsApi } from '../services/doctors.api';
import { SpecialtiesApi } from '../../specialties/services/specialties.api';
import { SpecialtyDto } from '../../specialties/models/specialty.models';
import { ToastService } from '../../../core/services/toast.service';
import { createDoctorForm, formToRegisterRequest } from '../utils/doctor-form.factory';

@Component({
  selector: 'app-doctor-create',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent, DoctorFormComponent],
  templateUrl: './doctor-create.component.html'
})
export class DoctorCreateComponent implements OnInit {
  private readonly api = inject(DoctorsApi);
  private readonly specialtiesApi = inject(SpecialtiesApi);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly form = createDoctorForm();
  readonly submitting = signal(false);
  readonly specialties = signal<SpecialtyDto[]>([]);

  ngOnInit(): void {
    this.specialtiesApi.list(true).subscribe({
      next: (list) => this.specialties.set(list)
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }
    this.submitting.set(true);
    this.api.register(formToRegisterRequest(this.form)).subscribe({
      next: (doctor) => {
        this.toast.success('تم تسجيل الطبيب');
        void this.router.navigate(['/app/doctors', doctor.id]);
      },
      error: () => this.submitting.set(false)
    });
  }
}
