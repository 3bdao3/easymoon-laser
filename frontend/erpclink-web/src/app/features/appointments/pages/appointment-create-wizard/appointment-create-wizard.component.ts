import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import {
  ClinicListItem,
  DoctorListItem,
  PatientListItem
} from '../../../../shared/models/reference.models';
import { ReferenceApiService } from '../../../../shared/services/reference-api.service';
import { formatTimeOnly } from '../../../../shared/utils/date.utils';
import { toTimeOnlyPayload } from '../../../../shared/utils/time.utils';
import { AvailabilitySlot } from '../../../scheduling/models/scheduling.models';
import { SchedulingApiService } from '../../../scheduling/services/scheduling-api.service';
import { AppointmentsApiService } from '../../services/appointments-api.service';

type WizardStep = 1 | 2 | 3 | 4 | 5 | 6;

@Component({
  selector: 'app-appointment-create-wizard',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './appointment-create-wizard.component.html',
  styleUrl: './appointment-create-wizard.component.scss'
})
export class AppointmentCreateWizardComponent {
  private readonly referenceApi = inject(ReferenceApiService);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly appointmentsApi = inject(AppointmentsApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly formatTimeOnly = formatTimeOnly;
  readonly step = signal<WizardStep>(1);
  readonly submitting = signal(false);
  readonly loadingSlots = signal(false);

  readonly patientQuery = signal('');
  readonly patientResults = signal<PatientListItem[]>([]);
  readonly selectedPatient = signal<PatientListItem | null>(null);

  readonly doctorQuery = signal('');
  readonly doctorResults = signal<DoctorListItem[]>([]);
  readonly selectedDoctor = signal<DoctorListItem | null>(null);

  readonly clinicResults = signal<ClinicListItem[]>([]);
  readonly selectedClinic = signal<ClinicListItem | null>(null);

  readonly appointmentDate = signal('');
  readonly slots = signal<AvailabilitySlot[]>([]);
  readonly selectedSlot = signal<AvailabilitySlot | null>(null);
  readonly reason = signal('');
  readonly notes = signal('');

  stepLabel(s: WizardStep): string {
    const labels: Record<WizardStep, string> = {
      1: 'المريض',
      2: 'الطبيب',
      3: 'العيادة',
      4: 'التاريخ',
      5: 'الموعد',
      6: 'التأكيد'
    };
    return labels[s];
  }

  goTo(step: WizardStep): void {
    this.step.set(step);
  }

  nextFromPatient(): void {
    if (!this.selectedPatient()) {
      return;
    }
    this.step.set(2);
  }

  nextFromDoctor(): void {
    if (!this.selectedDoctor()) {
      return;
    }
    this.step.set(3);
    this.loadClinicsForDoctor();
  }

  nextFromClinic(): void {
    if (!this.selectedClinic()) {
      return;
    }
    this.step.set(4);
    if (!this.appointmentDate()) {
      this.appointmentDate.set(this.todayIso());
    }
  }

  loadSlots(): void {
    const doctor = this.selectedDoctor();
    const clinic = this.selectedClinic();
    const date = this.appointmentDate().trim();
    if (!doctor || !clinic || !date) {
      return;
    }
    this.loadingSlots.set(true);
    this.selectedSlot.set(null);
    this.schedulingApi
      .getAvailability(doctor.id, date, clinic.id)
      .pipe(finalize(() => this.loadingSlots.set(false)))
      .subscribe({
        next: (avail) => {
          this.slots.set(avail.slots);
          this.step.set(5);
        }
      });
  }

  pickSlot(slot: AvailabilitySlot): void {
    this.selectedSlot.set(slot);
    this.step.set(6);
  }

  searchPatients(): void {
    const q = this.patientQuery().trim();
    if (!q) {
      return;
    }
    this.referenceApi.searchPatients({ query: q, isActive: true, page: 1, pageSize: 8 }).subscribe({
      next: (r) => this.patientResults.set(r.items)
    });
  }

  searchDoctors(): void {
    const q = this.doctorQuery().trim();
    if (!q) {
      return;
    }
    this.referenceApi.searchDoctors({ query: q, isActive: true, page: 1, pageSize: 8 }).subscribe({
      next: (r) => this.doctorResults.set(r.items)
    });
  }

  book(): void {
    const patient = this.selectedPatient();
    const doctor = this.selectedDoctor();
    const clinic = this.selectedClinic();
    const slot = this.selectedSlot();
    const date = this.appointmentDate();
    if (!patient || !doctor || !clinic || !slot || !date) {
      return;
    }
    this.submitting.set(true);
    this.appointmentsApi
      .book({
        patientId: patient.id,
        doctorId: doctor.id,
        clinicId: clinic.id,
        date,
        startTime: toTimeOnlyPayload(slot.start),
        reason: this.reason().trim() || null,
        notes: this.notes().trim() || null
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (created) => {
          this.toast.success('تم حجز الموعد');
          void this.router.navigate(['/app/appointments', created.id]);
        },
        error: (err: unknown) => {
          if (err instanceof HttpErrorResponse && err.status === 409) {
            this.loadSlots();
          }
        }
      });
  }

  private loadClinicsForDoctor(): void {
    const doctor = this.selectedDoctor();
    if (!doctor) {
      return;
    }
    this.referenceApi
      .searchClinics({ isActive: true, page: 1, pageSize: 100 })
      .subscribe({ next: (r) => this.clinicResults.set(r.items) });
  }

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
