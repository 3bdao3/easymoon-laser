import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InventoryApiService, ReceivablePo } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { lineRemaining, requiresBatch } from '../../goods-receipts/goods-receipt.utils';

@Component({
  selector: 'app-goods-receipt-form',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="استلام بضاعة" subtitle="Goods receipt" />
    <form [formGroup]="form" (ngSubmit)="loadPo()" class="feature-form">
      <label>Purchase order ID <input formControlName="purchaseOrderId" /></label>
      <button type="submit">Load receivable PO</button>
    </form>
    @if (po()) {
      <p>PO {{ po()!.purchaseOrderNumber }}</p>
      @for (line of po()!.lines; track line.lineId) {
        <div>
          {{ line.descriptionSnapshot }} — Ordered {{ line.quantityOrdered }} / Received
          {{ line.quantityReceived }} / Remaining {{ remaining(line) }}
        </div>
      }
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class GoodsReceiptFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(InventoryApiService);

  readonly form = this.fb.nonNullable.group({ purchaseOrderId: ['', Validators.required] });
  readonly po = signal<ReceivablePo | null>(null);

  loadPo(): void {
    const id = this.form.getRawValue().purchaseOrderId;
    this.api.getReceivablePo(id).subscribe({ next: (p) => this.po.set(p) });
  }

  remaining(line: ReceivablePo['lines'][number]): number {
    return lineRemaining(line.quantityOrdered, line.quantityReceived);
  }

  trackExpiry = requiresBatch;
}
