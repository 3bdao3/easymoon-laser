import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { MetricValue, ReportsApiService } from '../../services/reports-api.service';

@Component({
  selector: 'app-management-summary',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, FormsModule, RouterLink],
  template: `
    <app-page-header title="ملخص الإدارة" subtitle="Real metrics only — unavailable KPIs stay unavailable" />
    <a routerLink="/app/reports">← التقارير</a>
    <form class="feature-form" (ngSubmit)="load()">
      <label>من <input type="date" name="from" [(ngModel)]="fromDate" /></label>
      <label>إلى <input type="date" name="to" [(ngModel)]="toDate" /></label>
      <button type="submit" class="btn btn-primary">تحديث</button>
    </form>
    @if (notes()) {
      <p class="notes">{{ notes() }}</p>
    }
    @if (loading()) {
      <app-loading-spinner />
    } @else if (error()) {
      <p class="error">{{ error() }}</p>
    } @else {
      <div class="metrics">
        @for (m of metrics(); track m.metric) {
          <article [class.unavailable]="!m.available">
            <h3>{{ m.metric }}</h3>
            @if (m.available) {
              <p class="value">{{ m.value }} {{ m.currencyCode }}</p>
              <p class="src">{{ m.source }}</p>
              @if (m.reason) {
                <p class="reason">{{ m.reason }}</p>
              }
            } @else {
              <p class="value">غير متاح</p>
              <p class="reason">{{ m.reason }}</p>
            }
          </article>
        }
      </div>
    }
  `,
  styles: `
    .feature-form {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin: 1rem 0;
    }
    .metrics {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 0.75rem;
    }
    article {
      border: 1px solid #dde2e8;
      padding: 0.85rem;
      background: #f8fafc;
    }
    article.unavailable {
      background: #faf7f5;
      border-style: dashed;
    }
    h3 {
      margin: 0 0 0.35rem;
      font-size: 0.95rem;
    }
    .value {
      margin: 0;
      font-size: 1.15rem;
      font-weight: 600;
    }
    .src,
    .reason,
    .notes {
      margin: 0.35rem 0 0;
      color: #5c6570;
      font-size: 0.8rem;
    }
    .error {
      color: #b42318;
    }
  `
})
export class ManagementSummaryComponent {
  private readonly api = inject(ReportsApiService);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly metrics = signal<MetricValue[]>([]);
  readonly notes = signal('');
  fromDate = '2026-01-01';
  toDate = '2026-12-31';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getManagementSummary(this.fromDate, this.toDate).subscribe({
      next: (r) => {
        this.metrics.set(r.metrics);
        this.notes.set(r.notes);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذر تحميل الملخص');
        this.loading.set(false);
      }
    });
  }
}
