import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FinanceApiService, JournalEntryDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-journal-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink],
  template: `
    <app-page-header title="قيود اليومية" subtitle="Manual journals" />
    <a routerLink="/app/finance/accounts">← دليل الحسابات</a>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (j of items(); track j.id) {
          <li>
            <a [routerLink]="['/app/finance/journals', j.id]">{{ j.journalNumber }}</a>
            — {{ j.journalDate }} — {{ j.status }} ({{ j.totalDebit }} / {{ j.totalCredit }})
          </li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class JournalListComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(true);
  readonly items = signal<JournalEntryDto[]>([]);

  constructor() {
    this.api.searchJournals().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
