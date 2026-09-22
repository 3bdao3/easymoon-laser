import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MedicalVisitsApiService } from '../../services/medical-visits-api.service';
import {
  MedicalVisitListItemDto,
  MedicalVisitStatus,
  PagedMedicalVisitsResult
} from '../../models/medical-visit.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { formatDateOnly } from '../../../../shared/utils/date.utils';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

const STATUS_OPTIONS: MedicalVisitStatus[] = ['Open', 'InProgress', 'Completed', 'Cancelled'];

@Component({
  selector: 'app-patient-visit-history',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent
  ],
  templateUrl: './patient-visit-history.component.html',
  styleUrls: ['./patient-visit-history.component.scss', '../../_feature-layout.scss']
})
export class PatientVisitHistoryComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MedicalVisitsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly patientId = this.route.snapshot.paramMap.get('patientId') ?? '';
  readonly statusOptions = STATUS_OPTIONS;
  readonly formatDate = formatDateOnly;

  readonly loading = signal(false);
  readonly result = signal<PagedMedicalVisitsResult | null>(null);

  readonly filters = this.fb.nonNullable.group({
    status: ['' as MedicalVisitStatus | ''],
    dateFrom: [''],
    dateTo: [''],
    page: [1],
    pageSize: [20]
  });

  constructor() {
    if (this.patientId) {
      this.search();
    }
  }

  search(resetPage = true): void {
    if (!this.patientId) {
      return;
    }
    if (resetPage) {
      this.filters.patchValue({ page: 1 });
    }

    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api
      .patientHistory(this.patientId, {
        status: raw.status || undefined,
        dateFrom: raw.dateFrom || undefined,
        dateTo: raw.dateTo || undefined,
        page: raw.page,
        pageSize: raw.pageSize
      })
      .subscribe({
        next: (page) => {
          this.result.set(page);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.toast.error(this.errors.resolveMessage(err));
        }
      });
  }

  goToPage(page: number): void {
    this.filters.patchValue({ page });
    this.search(false);
  }

  trackById(_index: number, item: MedicalVisitListItemDto): string {
    return item.id;
  }

  totalPages(): number {
    const r = this.result();
    if (!r || r.pageSize <= 0) {
      return 0;
    }
    return Math.max(1, Math.ceil(r.totalCount / r.pageSize));
  }
}
