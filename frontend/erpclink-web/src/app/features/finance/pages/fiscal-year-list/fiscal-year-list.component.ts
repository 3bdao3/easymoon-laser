import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { FinanceApiService, FiscalYearDto } from '../../services/finance-api.service';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-fiscal-year-list',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="السنوات المالية" subtitle="Fiscal years" />
    <a routerLink="/app/finance/accounts">← دليل الحسابات</a>
    <form class="feature-form" (ngSubmit)="create()">
      <input [(ngModel)]="name" name="name" placeholder="اسم السنة" required />
      <input [(ngModel)]="startDate" name="start" type="date" required />
      <input [(ngModel)]="endDate" name="end" type="date" required />
      <button type="submit" class="btn btn-primary">إضافة</button>
    </form>
    @if (loading()) { <app-loading-spinner /> } @else {
      <ul class="feature-list">
        @for (y of items(); track y.id) {
          <li>{{ y.name }} — {{ y.startDate }} → {{ y.endDate }} ({{ y.status }})</li>
        }
      </ul>
    }
  `,
  styleUrls: ['../../../medical-visits/_feature-layout.scss']
})
export class FiscalYearListComponent {
  private readonly api = inject(FinanceApiService);
  readonly loading = signal(true);
  readonly items = signal<FiscalYearDto[]>([]);
  name = '';
  startDate = '2026-01-01';
  endDate = '2026-12-31';

  constructor() {
    this.reload();
  }

  create(): void {
    this.api.createFiscalYear({ name: this.name, startDate: this.startDate, endDate: this.endDate }).subscribe({
      next: () => this.reload()
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.searchFiscalYears().subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
