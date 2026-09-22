import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FinanceApiService, JournalEntryDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { sumJournalTotals } from '../../utils/journal-balance.util';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Permissions } from '../../../../core/permissions/permissions';

@Component({
  selector: 'app-journal-detail',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, RouterLink, HasPermissionDirective],
  template: `
    <app-page-header title="تفاصيل القيد" subtitle="Journal entry" />
    <a routerLink="/app/finance/journals">← القيود</a>
    @if (loading()) { <app-loading-spinner /> } @else if (journal()) {
      @let j = journal()!;
      @let totals = balance();
      <p>{{ j.journalNumber }} — {{ j.status }}</p>
      <p>مدين: {{ totals.totalDebit }} | دائن: {{ totals.totalCredit }} | فرق: {{ totals.difference }}</p>
      <ul class="feature-list">
        @for (line of j.lines; track line.id) {
          <li>{{ line.debit }} / {{ line.credit }} — حساب {{ line.accountId }}</li>
        }
      </ul>
      @if (j.status === 'Draft') {
        <button
          type="button"
          class="btn btn-primary"
          [disabled]="!totals.isBalanced"
          *appHasPermission="permissions.FinanceJournalsPost"
          (click)="post()"
        >
          ترحيل
        </button>
      }
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class JournalDetailComponent {
  private readonly api = inject(FinanceApiService);
  private readonly route = inject(ActivatedRoute);
  readonly permissions = Permissions;
  readonly loading = signal(true);
  readonly journal = signal<JournalEntryDto | null>(null);

  balance() {
    const j = this.journal();
    if (!j) return sumJournalTotals([]);
    return sumJournalTotals(j.lines.map((l) => ({ debit: l.debit, credit: l.credit })));
  }

  constructor() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getJournal(id).subscribe({
      next: (j) => {
        this.journal.set(j);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  post(): void {
    const j = this.journal();
    if (!j) return;
    this.api.postJournal(j.id, j.rowVersion).subscribe({
      next: (updated) => this.journal.set(updated)
    });
  }
}
