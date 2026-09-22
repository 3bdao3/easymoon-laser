import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { DoctorListItem } from '../../../../shared/models/reference.models';
import { ReferenceApiService } from '../../../../shared/services/reference-api.service';
import { formatDateOnly, formatTimeOnly } from '../../../../shared/utils/date.utils';
import { DoctorAvailability } from '../../models/scheduling.models';
import { SchedulingApiService } from '../../services/scheduling-api.service';

@Component({
  selector: 'app-availability-view',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './availability-view.component.html',
  styleUrl: './availability-view.component.scss'
})
export class AvailabilityViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly schedulingApi = inject(SchedulingApiService);
  private readonly referenceApi = inject(ReferenceApiService);

  readonly formatDateOnly = formatDateOnly;
  readonly formatTimeOnly = formatTimeOnly;

  readonly doctorQuery = signal('');
  readonly doctorResults = signal<DoctorListItem[]>([]);
  readonly selectedDoctorId = signal('');
  readonly selectedDoctorLabel = signal('');
  readonly date = signal(this.todayIso());
  readonly clinicId = signal('');
  readonly clinicOptions = signal<{ id: string; label: string }[]>([]);
  readonly availability = signal<DoctorAvailability | null>(null);
  readonly loading = signal(false);
  readonly searchingDoctors = signal(false);

  constructor() {
    const doctorId = this.route.snapshot.queryParamMap.get('doctorId');
    if (doctorId) {
      this.selectedDoctorId.set(doctorId);
      this.referenceApi.getDoctor(doctorId).subscribe({
        next: (d) => {
          this.selectedDoctorLabel.set(`${d.displayName} (${d.doctorNumber})`);
          this.doctorQuery.set(this.selectedDoctorLabel());
        }
      });
    }
    this.referenceApi.searchClinics({ isActive: true, page: 1, pageSize: 100 }).subscribe({
      next: (r) =>
        this.clinicOptions.set(r.items.map((c) => ({ id: c.id, label: `${c.name} (${c.code})` })))
    });
  }

  searchDoctors(): void {
    const query = this.doctorQuery().trim();
    if (!query) {
      return;
    }
    this.searchingDoctors.set(true);
    this.referenceApi
      .searchDoctors({ query, isActive: true, page: 1, pageSize: 8 })
      .pipe(finalize(() => this.searchingDoctors.set(false)))
      .subscribe({ next: (r) => this.doctorResults.set(r.items) });
  }

  pickDoctor(doctor: DoctorListItem): void {
    this.selectedDoctorId.set(doctor.id);
    this.selectedDoctorLabel.set(`${doctor.displayName} (${doctor.doctorNumber})`);
    this.doctorQuery.set(this.selectedDoctorLabel());
    this.doctorResults.set([]);
  }

  loadAvailability(): void {
    const doctorId = this.selectedDoctorId().trim();
    const date = this.date().trim();
    if (!doctorId || !date) {
      return;
    }
    this.loading.set(true);
    const clinic = this.clinicId().trim() || undefined;
    this.schedulingApi
      .getAvailability(doctorId, date, clinic)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({ next: (a) => this.availability.set(a) });
  }

  private todayIso(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
