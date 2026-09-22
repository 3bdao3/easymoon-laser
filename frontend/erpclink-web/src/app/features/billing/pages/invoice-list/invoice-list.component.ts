import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { BillingApiService } from '../../services/billing-api.service';
import { PagedInvoicesResult } from '../../models/invoice.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PageHeaderComponent, LoadingSpinnerComponent, HasPermissionDirective],
  templateUrl: './invoice-list.component.html',
  styleUrls: ['./invoice-list.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class InvoiceListComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BillingApiService);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(false);
  readonly result = signal<PagedInvoicesResult | null>(null);

  readonly filters = this.fb.nonNullable.group({
    invoiceNumber: [''],
    status: [''],
    page: [1]
  });

  constructor() {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    const raw = this.filters.getRawValue();
    this.api
      .search({
        invoiceNumber: raw.invoiceNumber || undefined,
        status: raw.status || undefined,
        page: raw.page
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
}
