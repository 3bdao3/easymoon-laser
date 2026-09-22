import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { ProcurementApiService } from '../../services/procurement-api.service';
import { SupplierDto } from '../../models/supplier.models';
import { previewLineTotal } from '../../purchase-orders/purchase-order-actions';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { ApiErrorService } from '../../../../core/services/api-error.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, DecimalPipe],
  templateUrl: './purchase-order-form.component.html',
  styleUrls: ['./purchase-order-form.component.scss', '../../../medical-visits/_feature-layout.scss']
})
export class PurchaseOrderFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProcurementApiService);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  private readonly toast = inject(ToastService);

  readonly suppliers = signal<SupplierDto[]>([]);
  readonly previewTotal = signal(0);

  readonly form = this.fb.nonNullable.group({
    supplierId: ['', Validators.required],
    orderDate: [new Date().toISOString().slice(0, 10), Validators.required],
    descriptionSnapshot: ['', Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    unitCost: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.api.searchSuppliers({ isActive: true, pageSize: 100 }).subscribe({
      next: (page) => this.suppliers.set(page.items),
      error: () => this.toast.error('تعذر تحميل الموردين')
    });
    this.form.valueChanges.subscribe(() => this.refreshPreview());
    this.refreshPreview();
  }

  refreshPreview(): void {
    const raw = this.form.getRawValue();
    this.previewTotal.set(previewLineTotal(raw.quantity, raw.unitCost));
  }

  submit(): void {
    if (this.form.invalid) return;
    const raw = this.form.getRawValue();
    this.api
      .createPurchaseOrderDraft({
        supplierId: raw.supplierId,
        orderDate: raw.orderDate,
        currencyCode: 'EGP',
        lines: [
          {
            descriptionSnapshot: raw.descriptionSnapshot,
            quantity: raw.quantity,
            unitCost: raw.unitCost
          }
        ]
      })
      .subscribe({
        next: (po) => void this.router.navigate(['/app/procurement/purchase-orders', po.id]),
        error: (err: HttpErrorResponse) => this.toast.error(this.errors.resolveMessage(err))
      });
  }
}
