import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header/page-header.component';
import { Permissions } from '../../../../core/permissions/permissions';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-reports-hub',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink],
  template: `
    <app-page-header title="التقارير" subtitle="Financial & management reporting — API-backed only" />
    <p class="muted">الأرقام تُحسب على الخادم. لا تُحتسب مؤشرات الربح هنا إن لم تتوفر مصادرها.</p>
    <nav class="report-nav">
      @if (canFinance()) {
        <section>
          <h3>المالية</h3>
          @if (auth.hasPermission(P.ReportsFinanceGeneralLedgerView)) {
            <a routerLink="/app/reports/finance/general-ledger">دفتر الأستاذ</a>
          }
          @if (auth.hasPermission(P.ReportsFinanceTrialBalanceView)) {
            <a routerLink="/app/reports/finance/trial-balance">ميزان المراجعة</a>
          }
          @if (auth.hasPermission(P.ReportsFinanceJournalsView)) {
            <a routerLink="/app/reports/finance/journals">سجل القيود</a>
          }
        </section>
      }
      @if (canAr()) {
        <section>
          <h3>الذمم المدينة</h3>
          @if (auth.hasPermission(P.ReportsArStatementView)) {
            <a routerLink="/app/reports/ar/statement">كشف حساب</a>
          }
          @if (auth.hasPermission(P.ReportsArAgingView)) {
            <a routerLink="/app/reports/ar/aging">أعمار الديون</a>
          }
        </section>
      }
      @if (canAp()) {
        <section>
          <h3>الذمم الدائنة</h3>
          @if (auth.hasPermission(P.ReportsApStatementView)) {
            <a routerLink="/app/reports/ap/statement">كشف حساب</a>
          }
          @if (auth.hasPermission(P.ReportsApAgingView)) {
            <a routerLink="/app/reports/ap/aging">أعمار الديون</a>
          }
        </section>
      }
      @if (canBilling()) {
        <section>
          <h3>الفوترة</h3>
          @if (auth.hasPermission(P.ReportsBillingInvoicesView)) {
            <a routerLink="/app/reports/billing/invoices">سجل الفواتير</a>
          }
          @if (auth.hasPermission(P.ReportsBillingPaymentsView)) {
            <a routerLink="/app/reports/billing/payments">سجل المدفوعات</a>
          }
          @if (auth.hasPermission(P.ReportsBillingOutstandingView)) {
            <a routerLink="/app/reports/billing/outstanding">فواتير مستحقة</a>
          }
        </section>
      }
      @if (canInventory()) {
        <section>
          <h3>المخزون</h3>
          @if (auth.hasPermission(P.ReportsInventoryStockView)) {
            <a routerLink="/app/reports/inventory/stock">أرصدة</a>
          }
          @if (auth.hasPermission(P.ReportsInventoryMovementsView)) {
            <a routerLink="/app/reports/inventory/movements">حركات</a>
          }
          @if (auth.hasPermission(P.ReportsInventoryValuationView)) {
            <a routerLink="/app/reports/inventory/valuation">تقييم</a>
          }
          @if (auth.hasPermission(P.ReportsInventoryCogsView)) {
            <a routerLink="/app/reports/inventory/cogs">تكلفة الصرف (ليس GL COGS)</a>
          }
        </section>
      }
      @if (canAssets()) {
        <section>
          <h3>الأصول</h3>
          @if (auth.hasPermission(P.ReportsAssetsRegisterView)) {
            <a routerLink="/app/reports/assets/register">سجل الأصول</a>
          }
          @if (auth.hasPermission(P.ReportsAssetsDepreciationView)) {
            <a routerLink="/app/reports/assets/depreciation">الإهلاك</a>
          }
          @if (auth.hasPermission(P.ReportsAssetsValuationView)) {
            <a routerLink="/app/reports/assets/valuation">التقييم / NBV</a>
          }
        </section>
      }
      @if (canProcurement()) {
        <section>
          <h3>المشتريات (تشغيلية — ليست AP)</h3>
          @if (auth.hasPermission(P.ReportsProcurementPurchaseOrdersView)) {
            <a routerLink="/app/reports/procurement/purchase-orders">أوامر الشراء</a>
          }
          @if (auth.hasPermission(P.ReportsProcurementReceivingView)) {
            <a routerLink="/app/reports/procurement/receiving">الاستلام</a>
          }
        </section>
      }
      @if (auth.hasPermission(P.ReportsManagementSummaryView)) {
        <section>
          <h3>ملخص الإدارة</h3>
          <a routerLink="/app/reports/management/summary">مؤشرات متاحة / غير متاحة</a>
        </section>
      }
    </nav>
  `,
  styles: `
    .report-nav {
      display: grid;
      gap: 1.25rem;
      margin-top: 1rem;
    }
    .report-nav section {
      display: flex;
      flex-wrap: wrap;
      gap: 0.75rem 1rem;
      align-items: baseline;
    }
    .report-nav h3 {
      flex: 0 0 100%;
      margin: 0;
      font-size: 1rem;
    }
    .muted {
      color: #5c6570;
      font-size: 0.9rem;
    }
  `
})
export class ReportsHubComponent {
  readonly auth = inject(AuthService);
  readonly P = Permissions;

  canFinance(): boolean {
    return this.auth.hasAnyPermission(
      Permissions.ReportsFinanceGeneralLedgerView,
      Permissions.ReportsFinanceTrialBalanceView,
      Permissions.ReportsFinanceJournalsView
    );
  }

  canAr(): boolean {
    return this.auth.hasAnyPermission(Permissions.ReportsArStatementView, Permissions.ReportsArAgingView);
  }

  canAp(): boolean {
    return this.auth.hasAnyPermission(Permissions.ReportsApStatementView, Permissions.ReportsApAgingView);
  }

  canBilling(): boolean {
    return this.auth.hasAnyPermission(
      Permissions.ReportsBillingInvoicesView,
      Permissions.ReportsBillingPaymentsView,
      Permissions.ReportsBillingOutstandingView
    );
  }

  canInventory(): boolean {
    return this.auth.hasAnyPermission(
      Permissions.ReportsInventoryStockView,
      Permissions.ReportsInventoryMovementsView,
      Permissions.ReportsInventoryValuationView,
      Permissions.ReportsInventoryCogsView
    );
  }

  canAssets(): boolean {
    return this.auth.hasAnyPermission(
      Permissions.ReportsAssetsRegisterView,
      Permissions.ReportsAssetsDepreciationView,
      Permissions.ReportsAssetsValuationView
    );
  }

  canProcurement(): boolean {
    return this.auth.hasAnyPermission(
      Permissions.ReportsProcurementPurchaseOrdersView,
      Permissions.ReportsProcurementReceivingView
    );
  }
}
