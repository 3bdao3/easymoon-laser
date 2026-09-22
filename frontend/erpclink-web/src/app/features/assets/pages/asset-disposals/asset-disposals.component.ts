import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AssetsApiService, AssetDisposalDto } from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-asset-disposals',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink],
  template: `
    <app-page-header title="استبعاد الأصول" subtitle="Disposal financial view (no GL)" />
    <nav class="feature-subnav"><a routerLink="/app/assets/accounting">محاسبة الأصول</a></nav>
    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <ul class="feature-list">
        @for (d of items(); track d.assetNumber + d.disposedDate) {
          <li>
            {{ d.assetNumber }} — {{ d.assetName }} | {{ d.disposedDate }} | NBV {{ d.netBookValueAtDisposal }} |
            proceeds {{ d.proceeds ?? '-' }} | G/L {{ d.gainLoss ?? '-' }}
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetDisposalsComponent {
  private readonly api = inject(AssetsApiService);
  readonly loading = signal(true);
  readonly items = signal<AssetDisposalDto[]>([]);

  constructor() {
    this.api.getDisposals().subscribe({
      next: (r) => {
        this.items.set(r.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
