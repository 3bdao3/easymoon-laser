import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AssetsApiService, AssetAccountingListItem } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-asset-accounting-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="محاسبة الأصول" subtitle="Asset accounting (provisional straight-line)" />
    <nav class="feature-subnav">
      <a routerLink="/app/assets">الأصول</a>
      <a routerLink="/app/assets/valuation">التقييم</a>
      <a routerLink="/app/assets/disposals">الاستبعادات</a>
    </nav>
    <button type="button" class="btn btn-primary" (click)="load()">تحديث</button>
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <ul class="feature-list">
        @for (row of items(); track row.assetId) {
          <li>
            <a [routerLink]="['/app/assets/accounting', row.assetId]">{{ row.assetNumber }} — {{ row.assetName }}</a>
            | {{ row.financialStatus }} | NBV {{ row.netBookValue ?? '-' }}
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetAccountingListComponent {
  private readonly api = inject(AssetsApiService);
  readonly loading = signal(false);
  readonly items = signal<AssetAccountingListItem[]>([]);

  load(): void {
    this.loading.set(true);
    this.api.searchAccounting().subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  constructor() {
    this.load();
  }
}
