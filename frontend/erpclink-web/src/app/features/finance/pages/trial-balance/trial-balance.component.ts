import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FinanceApiService, TrialBalanceResult } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-trial-balance',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="ميزان المراجعة" subtitle="Trial balance" />
    <a routerLink="/app/finance/accounts">← دليل الحسابات</a>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="fromDate" name="from" type="date" required />
      <input [(ngModel)]="toDate" name="to" type="date" required />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else if (result()) {
      @let r = result()!;
      <p>إجمالي مدين: {{ r.grandTotalDebit }} — إجمالي دائن: {{ r.grandTotalCredit }}</p>
      <ul class="feature-list">
        @for (line of r.lines; track line.accountCode) {
          <li>{{ line.accountCode }} {{ line.accountName }} — {{ line.totalDebit }} / {{ line.totalCredit }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class TrialBalanceComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(false);
  readonly result = signal<TrialBalanceResult | null>(null);
  fromDate = '2026-01-01';
  toDate = '2026-12-31';

  load(): void {
    this.loading.set(true);
    this.api.getTrialBalance(this.fromDate, this.toDate).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
