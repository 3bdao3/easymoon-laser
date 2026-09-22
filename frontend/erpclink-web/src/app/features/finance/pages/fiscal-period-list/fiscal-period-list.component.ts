import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FinanceApiService, FiscalPeriodDto, FiscalYearDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-fiscal-period-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="الفترات المالية" subtitle="Fiscal periods" />
    <a routerLink="/app/finance/accounts">← دليل الحسابات</a>
    <form class="feature-form" (ngSubmit)="create()">
      <select [(ngModel)]="fiscalYearId" name="year" required>
        @for (y of years(); track y.id) {
          <option [value]="y.id">{{ y.name }}</option>
        }
      </select>
      <input [(ngModel)]="name" name="name" placeholder="اسم الفترة" required />
      <input [(ngModel)]="startDate" name="start" type="date" required />
      <input [(ngModel)]="endDate" name="end" type="date" required />
      <button type="submit" class="btn btn-primary">إضافة</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (p of items(); track p.id) {
          <li>{{ p.name }} — {{ p.startDate }} → {{ p.endDate }} ({{ p.status }})</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class FiscalPeriodListComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(true);
  readonly items = signal<FiscalPeriodDto[]>([]);
  readonly years = signal<FiscalYearDto[]>([]);
  fiscalYearId = '';
  name = '';
  startDate = '2026-01-01';
  endDate = '2026-01-31';

  constructor() {
    this.api.searchFiscalYears().subscribe({
      next: (page) => {
        this.years.set(page.items);
        if (page.items[0]) this.fiscalYearId = page.items[0].id;
      }
    });
    this.reload();
  }

  create(): void {
    if (!this.fiscalYearId) return;
    this.api
      .createFiscalPeriod({
        fiscalYearId: this.fiscalYearId,
        name: this.name,
        startDate: this.startDate,
        endDate: this.endDate
      })
      .subscribe({ next: () => this.reload() });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchFiscalPeriods().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
