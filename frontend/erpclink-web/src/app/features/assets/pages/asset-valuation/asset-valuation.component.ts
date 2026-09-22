import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AssetsApiService } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-asset-valuation',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink],
  template: `
    <app-page-header title="تقييم الأصول" subtitle="Fixed asset valuation (NBV)" />
    <nav class="feature-subnav"><a routerLink="/app/assets/accounting">محاسبة الأصول</a></nav>
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <p>التكلفة: {{ totalCap() }} | مجمع الإهلاك: {{ totalAcc() }} | NBV: {{ totalNbv() }}</p>
      <ul class="feature-list">
        @for (row of items(); track row.assetNumber) {
          <li>{{ row.assetNumber }} — {{ row.assetName }} | NBV {{ row.netBookValue }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetValuationComponent {
  private readonly api = inject(AssetsApiService);
  readonly loading = signal(true);
  readonly totalCap = signal(0);
  readonly totalAcc = signal(0);
  readonly totalNbv = signal(0);
  readonly items = signal<{ assetNumber: string; assetName: string; netBookValue: number }[]>([]);

  constructor() {
    this.api.getValuation().subscribe({
      next: (r) => {
        this.totalCap.set(r.totalCapitalizedCost);
        this.totalAcc.set(r.totalAccumulatedDepreciation);
        this.totalNbv.set(r.totalNetBookValue);
        this.items.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
