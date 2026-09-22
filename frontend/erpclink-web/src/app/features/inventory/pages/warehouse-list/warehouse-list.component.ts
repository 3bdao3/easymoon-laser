import { Component, inject, signal } from '@angular/core';
import { InventoryApiService } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-warehouse-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <app-page-header title="المستودعات" subtitle="Warehouses" />
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (w of items(); track w.id) {
          <li>{{ w.warehouseCode }} — {{ w.name }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class WarehouseListComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(true);
  readonly items = signal<{ id: string; warehouseCode: string; name: string }[]>([]);

  constructor() {
    this.api.searchWarehouses().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
