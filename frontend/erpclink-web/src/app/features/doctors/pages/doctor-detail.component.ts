import { Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ConfirmService } from '../../../shared/components/confirm-dialog/confirm.service';
import { ToastService } from '../../../core/services/toast.service';
import { Permissions } from '../../../core/permissions/permissions';
import { trimToNull } from '../../../shared/utils/form.util';
import { DoctorsApi } from '../services/doctors.api';
import { ClinicsApi } from '../../clinics/services/clinics.api';
import { ClinicListItemDto } from '../../clinics/models/clinic.models';
import { DoctorClinicAssignmentDto, DoctorDto } from '../models/doctor.models';

@Component({
  selector: 'app-doctor-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    StatusBadgeComponent,
    HasPermissionDirective
  ],
  templateUrl: './doctor-detail.component.html'
})
export class DoctorDetailComponent implements OnInit {
  private readonly api = inject(DoctorsApi);
  private readonly clinicsApi = inject(ClinicsApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly doctor = signal<DoctorDto | null>(null);
  readonly assignments = signal<DoctorClinicAssignmentDto[]>([]);
  readonly clinics = signal<ClinicListItemDto[]>([]);
  readonly assigning = signal(false);

  readonly assignForm = new FormGroup({
    clinicId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    startDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    endDate: new FormControl('', { nonNullable: true })
  });

  private doctorId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/doctors']);
      return;
    }
    this.doctorId = id;
    this.clinicsApi.listActive().subscribe({ next: (c) => this.clinics.set(c) });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.getById(this.doctorId).subscribe({
      next: (d) => {
        this.doctor.set(d);
        this.loading.set(false);
      },
      error: () => void this.router.navigate(['/app/doctors'])
    });
    this.api.getClinics(this.doctorId).subscribe({
      next: (list) => this.assignments.set(list)
    });
  }

  assignClinic(): void {
    this.assignForm.markAllAsTouched();
    if (this.assignForm.invalid) {
      return;
    }
    const v = this.assignForm.getRawValue();
    this.assigning.set(true);
    this.api
      .assignClinic(this.doctorId, {
        clinicId: v.clinicId,
        startDate: v.startDate,
        endDate: trimToNull(v.endDate)
      })
      .subscribe({
        next: () => {
          this.toast.success('تم ربط العيادة');
          this.assignForm.reset({ clinicId: '', startDate: '', endDate: '' });
          this.assigning.set(false);
          this.api.getClinics(this.doctorId).subscribe({
            next: (list) => this.assignments.set(list)
          });
        },
        error: () => this.assigning.set(false)
      });
  }

  async deactivateAssignment(row: DoctorClinicAssignmentDto): Promise<void> {
    const ok = await this.confirm.confirm({
      title: 'إنهاء الربط',
      message: `إيقاف ربط ${row.clinicName}؟`,
      variant: 'danger'
    });
    if (!ok) {
      return;
    }
    this.api.deactivateClinicAssignment(this.doctorId, row.clinicId).subscribe({
      next: () => {
        this.toast.success('تم إيقاف الربط');
        this.api.getClinics(this.doctorId).subscribe({
          next: (list) => this.assignments.set(list)
        });
      }
    });
  }

  async toggleActive(): Promise<void> {
    const d = this.doctor();
    if (!d) {
      return;
    }
    const activate = !d.isActive;
    const ok = await this.confirm.confirm({
      title: activate ? 'تفعيل' : 'إيقاف',
      message: `${activate ? 'تفعيل' : 'إيقاف'} ${d.displayName}؟`,
      variant: activate ? 'default' : 'danger'
    });
    if (!ok) {
      return;
    }
    const req = activate ? this.api.activate(d.id) : this.api.deactivate(d.id);
    req.subscribe({ next: () => this.reload() });
  }
}
