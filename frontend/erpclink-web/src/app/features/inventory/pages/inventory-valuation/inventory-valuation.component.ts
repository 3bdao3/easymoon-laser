import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApiService, ValuationResult } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-inventory-valuation',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="تقييم المخزون" subtitle="Inventory valuation (provisional FIFO)" />
    <nav class="feature-subnav">
      <a routerLink="/app/inventory/warehouses">المخازن</a>
      <a routerLink="/app/inventory/stock">الأرصدة</a>
      <a routerLink="/app/inventory/cost-layers">طبقات التكلفة</a>
      <a routerLink="/app/inventory/cost-history">سجل التكلفة</a>
      <a routerLink="/app/inventory/issue-costs">تكلفة الصرف</a>
    </nav>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="warehouseId" name="warehouseId" placeholder="WarehouseId (اختياري)" />
      <input [(ngModel)]="itemId" name="itemId" placeholder="ItemId (اختياري)" />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else if (result()) {
      <p>
        الطريقة: {{ result()!.valuationMethod }} — الكمية: {{ result()!.totalQuantity }} — القيمة:
        {{ result()!.totalValue }}
      </p>
      <ul class="feature-list">
        @for (row of result()!.items; track row.inventoryItemId + row.warehouseId) {
          <li>
            {{ row.itemCode }} — {{ row.itemName }} | كمية {{ row.quantity }} | قيمة {{ row.inventoryValue }} | متوسط
            {{ row.averageUnitCost ?? '-' }}
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class InventoryValuationComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ValuationResult | null>(null);
  warehouseId = '';
  itemId = '';

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getValuation(this.warehouseId.trim() || undefined, this.itemId.trim() || undefined).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذر تحميل التقييم');
        this.loading.set(false);
      }
    });
  }
}
