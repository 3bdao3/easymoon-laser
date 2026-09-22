import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PackagesApiService } from '../../services/packages-api.service';
import { HealthcarePackageListItemDto, PagedHealthcarePackagesResult } from '../../models/package.models';
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
  selector: 'app-package-list',
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
  templateUrl: './package-list.component.html',
  styleUrls: ['./package-list.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PackageListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PackagesApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly permissions = Permissions;
  readonly loading = signal(false);
  readonly result = signal<PagedHealthcarePackagesResult | null>(null);

  readonly filters = this.fb.nonNullable.group({
    query: [''],
    isActive: [true],
    page: [1],
    pageSize: [20]
  });

  constructor() {
    this.search();
  }

  search(resetPage = true): void {
    if (resetPage) this.filters.patchValue({ page: 1 });
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api.search({ query: raw.query || undefined, isActive: raw.isActive, page: raw.page, pageSize: raw.pageSize }).subscribe({
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

  totalPages(): number {
    const page = this.result();
    if (!page) return 1;
    return Math.max(1, Math.ceil(page.totalCount / page.pageSize));
  }

  statusLabel(row: HealthcarePackageListItemDto): string {
    return row.isActive ? 'Active' : 'Inactive';
  }

  async toggleActive(row: HealthcarePackageListItemDto): Promise<void> {
    const activating = !row.isActive;
    const ok = await this.confirm.confirm({
      title: activating ? 'تفعيل الباقة' : 'إيقاف الباقة',
      message: activating ? `تفعيل «${row.name}»؟` : `إيقاف «${row.name}»؟`,
      confirmLabel: activating ? 'تفعيل' : 'إيقاف',
      variant: activating ? 'default' : 'danger'
    });
    if (!ok) return;
    const call = activating ? this.api.activate(row.id) : this.api.deactivate(row.id);
    call.subscribe({
      next: () => {
        this.toast.success(activating ? 'تم التفعيل.' : 'تم الإيقاف.');
        this.search(false);
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }
}
