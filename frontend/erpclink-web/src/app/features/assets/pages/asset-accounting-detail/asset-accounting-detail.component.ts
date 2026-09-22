import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  AssetsApiService,
  AssetFinancialHistoryLineDto,
  AssetFinancialProfileDto,
  DepreciationScheduleLineDto,
  DepreciationTransactionDto
} from '../../services/assets-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-asset-accounting-detail',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="تفاصيل مالية للأصل" subtitle="Financial profile / schedule / history" />
    <nav class="feature-subnav">
      <a routerLink="/app/assets/accounting">محاسبة الأصول</a>
    </nav>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else if (profile()) {
      <p>
        الحالة: {{ profile()!.status }} | التكلفة: {{ profile()!.capitalizedCost }} | مجمع الإهلاك:
        {{ profile()!.accumulatedDepreciation }} | NBV: {{ profile()!.netBookValue }} | الطريقة:
        {{ profile()!.depreciationMethod }}
      </p>
      <button type="button" class="btn btn-primary" (click)="postDep()">ترحيل إهلاك الفترة التالية</button>
    } @else {
      <form class="feature-form" (ngSubmit)="capitalize()">
        <input type="number" [(ngModel)]="capitalizedCost" name="capitalizedCost" placeholder="Capitalized cost" required />
        <input type="number" [(ngModel)]="residualValue" name="residualValue" placeholder="Residual" />
        <input type="number" [(ngModel)]="usefulLifeMonths" name="usefulLifeMonths" placeholder="Useful life months" />
        <input type="date" [(ngModel)]="capDate" name="capDate" required />
        <button type="submit" class="btn btn-primary">رسملة</button>
      </form>
    }
    <h3>الجدول</h3>
    <ul class="feature-list">
      @for (s of schedule(); track s.periodKey) {
        <li>{{ s.periodKey }} — {{ s.plannedAmount }} — {{ s.status }}</li>
      }
    </ul>
    <h3>الإهلاك</h3>
    <ul class="feature-list">
      @for (d of deps(); track d.id) {
        <li>{{ d.periodKey }} — {{ d.depreciationAmount }} → NBV {{ d.closingNetBookValue }}</li>
      }
    </ul>
    <h3>السجل المالي</h3>
    <ul class="feature-list">
      @for (h of history(); track h.transactionDate + h.eventType) {
        <li>{{ h.transactionDate }} | {{ h.eventType }} | {{ h.amount }}</li>
      }
    </ul>
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AssetAccountingDetailComponent {
  private readonly api = inject(AssetsApiService);
  private readonly route = inject(ActivatedRoute);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly profile = signal<AssetFinancialProfileDto | null>(null);
  readonly schedule = signal<DepreciationScheduleLineDto[]>([]);
  readonly deps = signal<DepreciationTransactionDto[]>([]);
  readonly history = signal<AssetFinancialHistoryLineDto[]>([]);
  assetId = '';
  capitalizedCost = 0;
  residualValue = 0;
  usefulLifeMonths = 12;
  capDate = new Date().toISOString().slice(0, 10);

  constructor() {
    this.assetId = this.route.snapshot.paramMap.get('assetId') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getFinancialProfile(this.assetId).subscribe({
      next: (p) => {
        this.profile.set(p);
        this.loading.set(false);
        this.loadRelated();
      },
      error: () => {
        this.profile.set(null);
        this.loading.set(false);
      }
    });
  }

  loadRelated(): void {
    this.api.getSchedule(this.assetId).subscribe({ next: (r) => this.schedule.set(r.items) });
    this.api.getDepreciation(this.assetId).subscribe({ next: (r) => this.deps.set(r.items) });
    this.api.getFinancialHistory(this.assetId).subscribe({ next: (r) => this.history.set(r.items) });
  }

  capitalize(): void {
    this.api
      .capitalize({
        assetId: this.assetId,
        capitalizedCost: this.capitalizedCost,
        residualValue: this.residualValue,
        capitalizationDate: this.capDate,
        depreciationStartDate: this.capDate,
        usefulLifeMonths: this.usefulLifeMonths,
        acquisitionCost: this.capitalizedCost
      })
      .subscribe({
        next: () => this.reload(),
        error: () => this.error.set('تعذر الرسملة')
      });
  }

  postDep(): void {
    this.api.postDepreciation(this.assetId).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('تعذر ترحيل الإهلاك')
    });
  }
}
