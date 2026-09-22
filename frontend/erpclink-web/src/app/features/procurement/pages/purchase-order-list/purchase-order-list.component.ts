import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { PagedPurchaseOrdersResult } from '../../models/purchase-order.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-purchase-order-list',
  standalone: true,
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    PageHeaderComponent,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    HasPermissionDirective
  ],
  templateUrl: './purchase-order-list.component.html',
  styleUrls: ['./purchase-order-list.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PurchaseOrderListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProcurementApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(false);
  readonly result = signal<PagedPurchaseOrdersResult | null>(null);

  readonly filters = this.fb.nonNullable.group({ purchaseOrderNumber: [''], status: [''] });

  constructor() {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api
      .searchPurchaseOrders({
        purchaseOrderNumber: raw.purchaseOrderNumber || undefined,
        status: raw.status || undefined
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
