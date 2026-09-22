import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MedicationsApiService } from '../../services/medications-api.service';
import { MedicationDto, PagedMedicationsResult } from '../../models/medication.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ConfirmService } from '../../../../shared/components/confirm-dialog/confirm.service';

@Component({
  selector: 'app-medication-list',
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
  templateUrl: './medication-list.component.html',
  styleUrls: ['./medication-list.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class MedicationListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MedicationsApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly loading = signal(false);
  readonly result = signal<PagedMedicationsResult | null>(null);

  readonly filters = this.fb.nonNullable.group({
    q: [''],
    code: [''],
    activeOnly: [true],
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
        q: raw.q || undefined,
        code: raw.code || undefined,
        activeOnly: raw.activeOnly,
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

  statusLabel(med: MedicationDto): string {
    return med.isActive ? 'Active' : 'Inactive';
  }

  async toggleActive(med: MedicationDto): Promise<void> {
    const activating = !med.isActive;
    const ok = await this.confirm.confirm({
      title: activating ? 'تفعيل الدواء' : 'إيقاف الدواء',
      message: activating
        ? `تفعيل «${med.name}»؟`
        : `إيقاف «${med.name}»؟ لن يُستخدم في وصفات جديدة.`,
      confirmLabel: activating ? 'تفعيل' : 'إيقاف',
      variant: activating ? 'default' : 'danger'
    });
    if (!ok) {
      return;
    }

    const call = activating ? this.api.activate(med.id) : this.api.deactivate(med.id);
    call.subscribe({
      next: () => {
        this.toast.success(activating ? 'تم التفعيل.' : 'تم الإيقاف.');
        this.search(false);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  trackById(_index: number, item: MedicationDto): string {
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
