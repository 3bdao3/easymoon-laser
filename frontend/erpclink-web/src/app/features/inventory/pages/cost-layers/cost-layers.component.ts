import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CostLayerDto, InventoryApiService } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-cost-layers',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="طبقات التكلفة" subtitle="FIFO cost layers (read-only)" />
    <nav class="feature-subnav">
      <a routerLink="/app/inventory/valuation">التقييم</a>
    </nav>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="itemId" name="itemId" placeholder="ItemId" />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <ul class="feature-list">
        @for (l of items(); track l.id) {
          <li>
            {{ l.receiptDate }} | متبقي {{ l.remainingQuantity }} × {{ l.unitCost }} = {{ l.remainingValue }} |
            {{ l.status }}
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class CostLayersComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(false);
  readonly items = signal<CostLayerDto[]>([]);
  itemId = '';

  load(): void {
    this.loading.set(true);
    this.api.getCostLayers(this.itemId.trim() || undefined).subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
