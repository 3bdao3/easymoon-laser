import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { BillingApiService } from '../../services/billing-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './invoice-form.component.html',
  styleUrls: ['./invoice-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class InvoiceFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BillingApiService);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly form = this.fb.nonNullable.group({
    patientId: ['', Validators.required],
    serviceId: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    invoiceDate: [new Date().toISOString().slice(0, 10), Validators.required]
  });

  submit(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.api
      .createDraft({
        patientId: raw.patientId,
        invoiceDate: raw.invoiceDate,
        currencyCode: 'EGP',
        lines: [{ serviceId: raw.serviceId, quantity: raw.quantity }]
      })
      .subscribe({
        next: (inv) => void this.router.navigate(['/app/billing/invoices', inv.id]),
        error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
      });
  }
}
