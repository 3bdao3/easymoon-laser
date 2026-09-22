import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { ReportsApiService } from '../../services/reports-api.service';

type ReportKey =
  | 'gl'
  | 'trialBalance'
  | 'journals'
  | 'arStatement'
  | 'arAging'
  | 'apStatement'
  | 'apAging'
  | 'billingInvoices'
  | 'billingPayments'
  | 'billingOutstanding'
  | 'stock'
  | 'movements'
  | 'invValuation'
  | 'invCogs'
  | 'assetRegister'
  | 'assetDepreciation'
  | 'assetValuation'
  | 'purchaseOrders'
  | 'receiving';

const TITLES: Record<ReportKey, { ar: string; en: string; note?: string }> = {
  gl: { ar: 'دفتر الأستاذ', en: 'General ledger' },
  trialBalance: { ar: 'ميزان المراجعة', en: 'Trial balance' },
  journals: { ar: 'سجل القيود', en: 'Journal register' },
  arStatement: { ar: 'كشف ذمم مدينة', en: 'AR statement' },
  arAging: { ar: 'أعمار الذمم المدينة', en: 'AR aging', note: 'DueDate when present; otherwise TransactionDate.' },
  apStatement: { ar: 'كشف ذمم دائنة', en: 'AP statement' },
  apAging: { ar: 'أعمار الذمم الدائنة', en: 'AP aging' },
  billingInvoices: { ar: 'سجل الفواتير', en: 'Invoice register' },
  billingPayments: { ar: 'سجل المدفوعات', en: 'Payment register' },
  billingOutstanding: { ar: 'فواتير مستحقة', en: 'Outstanding invoices' },
  stock: { ar: 'أرصدة المخزون', en: 'Stock balances' },
  movements: { ar: 'حركات المخزون', en: 'Stock movements' },
  invValuation: { ar: 'تقييم المخزون', en: 'Inventory valuation' },
  invCogs: {
    ar: 'تكلفة الصرف',
    en: 'Issue cost / COGS foundation',
    note: 'Not GL COGS.'
  },
  assetRegister: { ar: 'سجل الأصول', en: 'Asset register' },
  assetDepreciation: { ar: 'تقرير الإهلاك', en: 'Depreciation report' },
  assetValuation: { ar: 'تقييم الأصول', en: 'Asset valuation' },
  purchaseOrders: {
    ar: 'أوامر الشراء',
    en: 'PO register',
    note: 'Operational order value — not AP obligation.'
  },
  receiving: { ar: 'ملخص الاستلام', en: 'Receiving summary' }
};

@Component({
  selector: 'app-report-page',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, FormsModule, RouterLink],
  template: `
    <app-page-header [title]="meta.ar" [subtitle]="meta.en" />
    <a routerLink="/app/reports">← التقارير</a>
    @if (meta.note) {
      <p class="note">{{ meta.note }}</p>
    }
    <form class="feature-form" (ngSubmit)="load()">
      @if (needsDates()) {
        <label>من <input type="date" name="from" [(ngModel)]="fromDate" /></label>
        <label>إلى <input type="date" name="to" [(ngModel)]="toDate" /></label>
      }
      @if (needsParty()) {
        <label>Party Id <input name="party" [(ngModel)]="partyId" required /></label>
      }
      @if (needsAccount()) {
        <label>Account Id <input name="account" [(ngModel)]="accountId" /></label>
      }
      @if (needsStatus()) {
        <label>Status <input name="status" [(ngModel)]="status" /></label>
      }
      <button type="submit" class="btn btn-primary" [disabled]="loading()">عرض</button>
    </form>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else if (rows().length === 0 && !error()) {
      <app-empty-state title="لا توجد نتائج" message="جرّب تغيير عوامل التصفية." />
    } @else {
      <p class="meta">Page {{ page() }} · {{ total() }} rows · source {{ source() }}</p>
      <div class="table-wrap">
        <table class="report-table">
          <thead>
            <tr>
              @for (col of columns(); track col) {
                <th>{{ col }}</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track $index) {
              <tr>
                @for (col of columns(); track col) {
                  <td>{{ formatCell(row[col]) }}</td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (total() > pageSize) {
        <div class="pager">
          <button type="button" class="btn" [disabled]="page() <= 1" (click)="prev()">السابق</button>
          <button type="button" class="btn" [disabled]="page() * pageSize >= total()" (click)="next()">
            التالي
          </button>
        </div>
      }
    }
  `,
  styles: `
    .feature-form {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin: 1rem 0;
      align-items: end;
    }
    .table-wrap {
      overflow: auto;
      max-width: 100%;
    }
    .report-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.85rem;
    }
    .report-table th,
    .report-table td {
      border-bottom: 1px solid #dde2e8;
      padding: 0.4rem 0.5rem;
      text-align: start;
      white-space: nowrap;
    }
    .note,
    .meta {
      color: #5c6570;
      font-size: 0.85rem;
    }
    .error {
      color: #b42318;
    }
    .pager {
      display: flex;
      gap: 0.5rem;
      margin-top: 0.75rem;
    }
  `
})
export class ReportPageComponent {
  private readonly api = inject(ReportsApiService);
  private readonly route = inject(ActivatedRoute);

  readonly reportKey = (this.route.snapshot.data['reportKey'] ?? 'gl') as ReportKey;
  readonly meta = TITLES[this.reportKey];

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly rows = signal<Record<string, unknown>[]>([]);
  readonly columns = signal<string[]>([]);
  readonly page = signal(1);
  readonly total = signal(0);
  readonly source = signal('');
  readonly pageSize = 50;

  fromDate = '2026-01-01';
  toDate = '2026-12-31';
  partyId = '';
  accountId = '';
  status = '';

  needsDates(): boolean {
    return !['stock', 'invValuation', 'assetRegister', 'assetValuation', 'arAging', 'apAging'].includes(
      this.reportKey
    );
  }

  needsParty(): boolean {
    return this.reportKey === 'arStatement' || this.reportKey === 'apStatement';
  }

  needsAccount(): boolean {
    return this.reportKey === 'gl';
  }

  needsStatus(): boolean {
    return ['journals', 'billingInvoices', 'billingPayments', 'purchaseOrders', 'assetRegister'].includes(
      this.reportKey
    );
  }

  formatCell(v: unknown): string {
    if (v === null || v === undefined) {
      return '—';
    }
    if (typeof v === 'object') {
      return JSON.stringify(v);
    }
    return String(v);
  }

  prev(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.load();
    }
  }

  next(): void {
    this.page.update((p) => p + 1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = {
      fromDate: this.fromDate,
      toDate: this.toDate,
      page: this.page(),
      pageSize: this.pageSize,
      status: this.status || undefined,
      accountId: this.accountId || undefined
    };

    const finishPage = (page: { items: Record<string, unknown>[]; totalCount: number; page: number; sourceModule: string }) => {
      this.applyRows(page.items);
      this.total.set(page.totalCount);
      this.page.set(page.page);
      this.source.set(page.sourceModule);
      this.loading.set(false);
    };

    const fail = () => {
      this.error.set('تعذر تحميل التقرير');
      this.loading.set(false);
    };

    switch (this.reportKey) {
      case 'gl':
        this.api.getGl(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'trialBalance':
        this.api.getTrialBalance(this.fromDate, this.toDate).subscribe({
          next: (r) => {
            const lines = (r['lines'] as Record<string, unknown>[]) ?? [];
            this.applyRows(lines);
            this.total.set(lines.length);
            this.source.set(String(r['sourceModule'] ?? 'Finance'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'journals':
        this.api.getJournals(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'arStatement':
        if (!this.partyId) {
          this.error.set('Party Id مطلوب');
          this.loading.set(false);
          return;
        }
        this.api.getArStatement(this.partyId, q).subscribe({
          next: (r) => {
            this.applyRows((r['items'] as Record<string, unknown>[]) ?? []);
            this.total.set(Number(r['totalCount'] ?? 0));
            this.source.set(String(r['sourceModule'] ?? 'Finance'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'arAging':
        this.api.getArAging({ asOfDate: this.toDate }).subscribe({
          next: (r) => {
            this.applyRows((r['items'] as Record<string, unknown>[]) ?? []);
            this.total.set(((r['items'] as unknown[]) ?? []).length);
            this.source.set(String(r['sourceModule'] ?? 'Finance'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'apStatement':
        if (!this.partyId) {
          this.error.set('Party Id مطلوب');
          this.loading.set(false);
          return;
        }
        this.api.getApStatement(this.partyId, q).subscribe({
          next: (r) => {
            this.applyRows((r['items'] as Record<string, unknown>[]) ?? []);
            this.total.set(Number(r['totalCount'] ?? 0));
            this.source.set(String(r['sourceModule'] ?? 'Finance'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'apAging':
        this.api.getApAging({ asOfDate: this.toDate }).subscribe({
          next: (r) => {
            this.applyRows((r['items'] as Record<string, unknown>[]) ?? []);
            this.total.set(((r['items'] as unknown[]) ?? []).length);
            this.source.set(String(r['sourceModule'] ?? 'Finance'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'billingInvoices':
        this.api.getBillingInvoices(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'billingPayments':
        this.api.getBillingPayments(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'billingOutstanding':
        this.api.getBillingOutstanding(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'stock':
        this.api.getStock({ page: this.page(), pageSize: this.pageSize }).subscribe({ next: finishPage, error: fail });
        break;
      case 'movements':
        this.api.getMovements(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'invValuation':
        this.api
          .getInventoryValuation({ page: this.page(), pageSize: this.pageSize })
          .subscribe({ next: finishPage, error: fail });
        break;
      case 'invCogs':
        this.api.getInventoryCogs(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'assetRegister':
        this.api
          .getAssetRegister({
            page: this.page(),
            pageSize: this.pageSize,
            financialStatus: this.status || undefined
          })
          .subscribe({ next: finishPage, error: fail });
        break;
      case 'assetDepreciation':
        this.api
          .getAssetDepreciation({ page: this.page(), pageSize: this.pageSize })
          .subscribe({ next: finishPage, error: fail });
        break;
      case 'assetValuation':
        this.api.getAssetValuation(this.page(), this.pageSize).subscribe({
          next: (r) => {
            this.applyRows((r['items'] as Record<string, unknown>[]) ?? []);
            this.total.set(Number(r['totalCount'] ?? 0));
            this.source.set(String(r['sourceModule'] ?? 'Assets'));
            this.loading.set(false);
          },
          error: fail
        });
        break;
      case 'purchaseOrders':
        this.api.getPurchaseOrders(q).subscribe({ next: finishPage, error: fail });
        break;
      case 'receiving':
        this.api.getReceiving(q).subscribe({ next: finishPage, error: fail });
        break;
    }
  }

  private applyRows(items: Record<string, unknown>[]): void {
    this.rows.set(items);
    this.columns.set(items.length ? Object.keys(items[0]) : []);
  }
}
