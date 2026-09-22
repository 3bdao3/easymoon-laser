import { Component, inject, signal } from '@angular/core';
import { InventoryApiService } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-item-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent],
  template: `
    <app-page-header title="أصناف المخزون" subtitle="Inventory items" />
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (i of items(); track i.id) {
          <li>{{ i.itemCode }} — {{ i.name }} ({{ i.unitOfMeasure }})</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class ItemListComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(true);
  readonly items = signal<{ id: string; itemCode: string; name: string; unitOfMeasure: string }[]>([]);

  constructor() {
    this.api.searchItems().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
