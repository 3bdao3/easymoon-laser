import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AgingResult,
  FinanceApiService,
  PartyBalanceDto,
  StatementResult,
  SubledgerTransactionDto
} from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-ap-overview',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="الذمم الدائنة (AP)" subtitle="Accounts payable subledger" />
    <nav class="feature-subnav">
      <a routerLink="/app/finance/accounts">دليل الحسابات</a>
      <a routerLink="/app/finance/ar">الذمم المدينة</a>
    </nav>
    <p class="hint">
      أوامر الشراء ليست التزامات AP تلقائياً. أدخل SupplierId لعرض الحركات عند توفر مصادر مالية.
    </p>
    <form class="feature-form" (ngSubmit)="load()">
      <input [(ngModel)]="partyId" name="partyId" placeholder="معرّف المورد (SupplierId)" required />
      <button type="submit" class="btn btn-primary">عرض</button>
    </form>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else if (balance()) {
      <section class="feature-card">
        <h3>الرصيد</h3>
        <p>
          {{ balance()!.partyDisplayName || balance()!.partyId }} —
          {{ balance()!.balance }} {{ balance()!.currencyCode }}
        </p>
      </section>
      <section class="feature-card">
        <h3>كشف الحساب</h3>
        <p>افتتاحي: {{ statement()?.openingBalance }} — ختامي: {{ statement()?.closingBalance }}</p>
        <ul class="feature-list">
          @for (line of statement()?.items ?? []; track line.reference + line.date) {
            <li>
              {{ line.date }} | {{ line.source }} | مدين {{ line.debit }} | دائن {{ line.credit }} | رصيد
              {{ line.runningBalance }}
            </li>
          }
        </ul>
      </section>
      <section class="feature-card">
        <h3>الحركات</h3>
        <ul class="feature-list">
          @for (t of transactions(); track t.id) {
            <li>{{ t.transactionDate }} {{ t.eventType }} مدين {{ t.debit }} دائن {{ t.credit }}</li>
          }
        </ul>
      </section>
      <section class="feature-card">
        <h3>أعمار الديون</h3>
        <p>الإجمالي المستحق: {{ aging()?.totalOutstanding }}</p>
        <ul class="feature-list">
          @for (b of aging()?.buckets ?? []; track b.bucket) {
            <li>{{ b.bucket }}: {{ b.amount }} ({{ b.itemCount }})</li>
          }
        </ul>
      </section>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class ApOverviewComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly balance = signal<PartyBalanceDto | null>(null);
  readonly statement = signal<StatementResult | null>(null);
  readonly transactions = signal<SubledgerTransactionDto[]>([]);
  readonly aging = signal<AgingResult | null>(null);
  partyId = '';

  load(): void {
    const id = this.partyId.trim();
    if (!id) return;
    this.loading.set(true);
    this.error.set(null);
    this.api.getApBalance(id).subscribe({
      next: (b) => {
        this.balance.set(b);
        this.api.getApStatement(id).subscribe({ next: (s) => this.statement.set(s) });
        this.api.searchApTransactions(id).subscribe({ next: (r) => this.transactions.set(r.items) });
        this.api.getApAging(id).subscribe({
          next: (a) => {
            this.aging.set(a);
            this.loading.set(false);
          },
          error: (e) => this.fail(e)
        });
      },
      error: (e) => this.fail(e)
    });
  }

  private fail(err: unknown): void {
    this.loading.set(false);
    this.error.set('تعذر تحميل بيانات AP');
    console.error(err);
  }
}
