import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApiService } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-cost-history',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="سجل التكلفة" subtitle="Inventory cost history (read-only)" />
    <nav class="feature-subnav">
      <a routerLink="/app/inventory/valuation">التقييم</a>
    </nav>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="itemId" name="itemId" placeholder="InventoryItemId" required />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <ul class="feature-list">
        @for (row of items(); track row.id) {
          <li>
            {{ row.transactionDate }} | {{ row.eventType }} | qty {{ row.quantity }} × {{ row.unitCost }} = {{ row.totalCost }}
            ({{ row.sourceType }}/{{ row.sourceId }})
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class CostHistoryComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<
    {
      id: string;
      transactionDate: string;
      eventType: string;
      quantity: number;
      unitCost: number;
      totalCost: number;
      sourceType: string;
      sourceId: string;
    }[]
  >([]);
  itemId = '';

  load(): void {
    if (!this.itemId.trim()) return;
    this.loading.set(true);
    this.error.set(null);
    this.api.getCostHistory(this.itemId.trim()).subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذر تحميل سجل التكلفة');
        this.loading.set(false);
      }
    });
  }
}
