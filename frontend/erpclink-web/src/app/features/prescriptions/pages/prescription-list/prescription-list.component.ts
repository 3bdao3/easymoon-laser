import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PrescriptionsApiService } from '../../services/prescriptions-api.service';
import {
  PagedPrescriptionsResult,
  PrescriptionListItemDto,
  PrescriptionStatus
} from '../../models/prescription.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { formatDateOnly } from '../../../../shared/utils/date.utils';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

const STATUS_OPTIONS: PrescriptionStatus[] = ['Draft', 'Issued', 'Cancelled'];

@Component({
  selector: 'app-prescription-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './prescription-list.component.html',
  styleUrls: [
    './prescription-list.component.scss',
    '../../../medical-visits/_feature-layout.scss',
    '../../_feature-layout.scss'
  ]
})
export class PrescriptionListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PrescriptionsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly statusOptions = STATUS_OPTIONS;
  readonly formatDate = formatDateOnly;

  readonly loading = signal(false);
  readonly result = signal<PagedPrescriptionsResult | null>(null);

  readonly filters = this.fb.nonNullable.group({
    prescriptionNumber: [''],
    patientId: [''],
    medicalVisitId: [''],
    status: ['' as PrescriptionStatus | ''],
    dateFrom: [''],
    dateTo: [''],
    page: [1],
    pageSize: [20]
  });

  constructor() {
    this.search();
  }

  search(resetPage = true): void {
    if (resetPage) {
      this.filters.patchValue({ page: 1 });
    }
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api
      .search({
        prescriptionNumber: raw.prescriptionNumber || undefined,
        patientId: raw.patientId || undefined,
        medicalVisitId: raw.medicalVisitId || undefined,
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

  trackById(_index: number, item: PrescriptionListItemDto): string {
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
