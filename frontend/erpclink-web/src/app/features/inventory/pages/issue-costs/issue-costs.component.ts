import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InventoryApiService, IssueCostLineDto } from '../../services/inventory-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-issue-costs',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="تكلفة الصرف" subtitle="Inventory issue costs (not GL COGS)" />
    <nav class="feature-subnav">
      <a routerLink="/app/inventory/valuation">التقييم</a>
    </nav>
    <form class="feature-form" (ngSubmit)="load()">
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <p>إجمالي تكلفة الصرف: {{ total() }}</p>
      <ul class="feature-list">
        @for (i of items(); track i.id) {
          <li>{{ i.transactionDate }} | {{ i.eventType }} | {{ i.quantity }} × {{ i.unitCost }} = {{ i.totalCost }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class IssueCostsComponent {
  private readonly api = inject(InventoryApiService);
  readonly loading = signal(false);
  readonly items = signal<IssueCostLineDto[]>([]);
  readonly total = signal(0);

  load(): void {
    this.loading.set(true);
    this.api.getIssueCosts().subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.total.set(r.totalIssueCost);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
