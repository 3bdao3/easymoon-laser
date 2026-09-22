import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { PagedSuppliersResult } from '../../models/supplier.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-supplier-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './supplier-list.component.html',
  styleUrls: ['./supplier-list.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class SupplierListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProcurementApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(false);
  readonly result = signal<PagedSuppliersResult | null>(null);

  readonly filters = this.fb.nonNullable.group({ search: [''], activeOnly: [true] });

  constructor() {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api
      .searchSuppliers({
        search: raw.search || undefined,
        isActive: raw.activeOnly ? true : undefined
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

  trackById(_: number, row: { id: string }) {
    return row.id;
  }
}
