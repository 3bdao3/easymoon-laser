import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { BillingApiService } from '../../services/billing-api.service';
import { InvoiceDto } from '../../models/invoice.models';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, LoadingSpinnerComponent, HasPermissionDirective],
  templateUrl: './invoice-detail.component.html',
  styleUrls: ['./invoice-detail.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class InvoiceDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BillingApiService);
  private readonly fb = inject(FormBuilder);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly invoice = signal<InvoiceDto | null>(null);

  readonly paymentForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0.01)]],
    method: ['Cash', Validators.required]
  });

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.reload(id);
  }

  reload(id: string): void {
    this.loading.set(true);
    this.api.getById(id).subscribe({
      next: (inv) => {
        this.invoice.set(inv);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.toast.error(this.errors.resolveMessage(err));
      }
    });
  }

  issue(): void {
    const inv = this.invoice();
    if (!inv) return;
    this.api.issue(inv.id).subscribe({
      next: (updated) => {
        this.invoice.set(updated);
        this.toast.success('تم إصدار الفاتورة');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  voidInvoice(): void {
    const inv = this.invoice();
    if (!inv) return;
    this.api.void(inv.id, 'Void from UI', inv.rowVersion).subscribe({
      next: (updated) => {
        this.invoice.set(updated);
        this.toast.success('تم إلغاء الفاتورة');
      },
      error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
    });
  }

  pay(): void {
    const inv = this.invoice();
    if (!inv || this.paymentForm.invalid) return;
    const raw = this.paymentForm.getRawValue();
    this.api
      .recordPayment({
        invoiceId: inv.id,
        paymentDate: new Date().toISOString().slice(0, 10),
        amount: raw.amount,
        method: raw.method,
        invoiceRowVersion: inv.rowVersion
      })
      .subscribe({
        next: () => {
          this.toast.success('تم تسجيل الدفع');
          this.reload(inv.id);
        },
        error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
      });
  }
}
