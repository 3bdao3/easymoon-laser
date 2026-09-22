import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FinanceApiService, GeneralLedgerLineDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-general-ledger',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="دفتر الأستاذ العام" subtitle="General ledger" />
    <a routerLink="/app/finance/accounts">← دليل الحسابات</a>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="fromDate" name="from" type="date" required />
      <input [(ngModel)]="toDate" name="to" type="date" required />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (line of items(); track $index) {
          <li>{{ line.journalNumber }} — {{ line.journalDate }} — {{ line.debit }} / {{ line.credit }}</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class GeneralLedgerComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(false);
  readonly items = signal<GeneralLedgerLineDto[]>([]);
  fromDate = '2026-01-01';
  toDate = '2026-12-31';

  load(): void {
    this.loading.set(true);
    this.api.searchGeneralLedger(this.fromDate, this.toDate).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
