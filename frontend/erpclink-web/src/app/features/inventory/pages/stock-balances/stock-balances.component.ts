import { Component, inject, signal } from '@angular/core';
import { InventoryApiService } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-stock-balances',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <app-page-header title="أرصدة المخزون" subtitle="Stock balances" />
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (b of balances(); track b.inventoryItemId) {
          <li>Item {{ b.inventoryItemId }} — Qty {{ b.quantity }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class StockBalancesComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(true);
  readonly balances = signal<{ inventoryItemId: string; quantity: number }[]>([]);

  constructor() {
    this.api.searchBalances().subscribe({
      next: (page) => {
        this.balances.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
