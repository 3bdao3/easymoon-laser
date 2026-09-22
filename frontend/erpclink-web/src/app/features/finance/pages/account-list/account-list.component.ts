import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FinanceApiService, AccountDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-account-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="دليل الحسابات" subtitle="Chart of accounts" />
    <nav class="feature-subnav">
      <a routerLink="/app/finance/fiscal-years">السنوات المالية</a>
      <a routerLink="/app/finance/fiscal-periods">الفترات</a>
      <a routerLink="/app/finance/journals">قيود اليومية</a>
      <a routerLink="/app/finance/general-ledger">دفتر الأستاذ</a>
      <a routerLink="/app/finance/trial-balance">ميزان المراجعة</a>
      <a routerLink="/app/finance/ar">الذمم المدينة</a>
      <a routerLink="/app/finance/ap">الذمم الدائنة</a>
    </nav>
    <form class="feature-form" (ngSubmit)="create()">
      <input [(ngModel)]="code" name="code" placeholder="رمز الحساب" required />
      <input [(ngModel)]="name" name="name" placeholder="اسم الحساب" required />
      <select [(ngModel)]="accountType" name="type">
        <option value="Asset">Asset</option>
        <option value="Liability">Liability</option>
        <option value="Equity">Equity</option>
        <option value="Revenue">Revenue</option>
        <option value="Expense">Expense</option>
      </select>
      <button type="submit" class="btn btn-primary">إضافة</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (a of items(); track a.id) {
          <li>{{ a.code }} — {{ a.name }} ({{ a.accountType }})</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class AccountListComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(true);
  readonly items = signal<AccountDto[]>([]);
  code = '';
  name = '';
  accountType = 'Asset';

  constructor() {
    this.reload();
  }

  create(): void {
    if (!this.code.trim() || !this.name.trim()) return;
    this.api
      .createAccount({
        code: this.code.trim(),
        name: this.name.trim(),
        accountType: this.accountType,
        isPostable: true
      })
      .subscribe({
        next: () => {
          this.code = '';
          this.name = '';
          this.reload();
        }
      });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchAccounts().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
