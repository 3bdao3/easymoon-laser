import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const FINANCE_ROUTES: Routes = [
  {
    path: 'accounts',
    canActivate: [permissionGuard(Permissions.FinanceAccountsView)],
    loadComponent: () =>
      import('./pages/account-list/account-list.component').then((m) => m.AccountListComponent)
  },
  {
    path: 'fiscal-years',
    canActivate: [permissionGuard(Permissions.FinanceFiscalYearsView)],
    loadComponent: () =>
      import('./pages/fiscal-year-list/fiscal-year-list.component').then((m) => m.FiscalYearListComponent)
  },
  {
    path: 'fiscal-periods',
    canActivate: [permissionGuard(Permissions.FinanceFiscalPeriodsView)],
    loadComponent: () =>
      import('./pages/fiscal-period-list/fiscal-period-list.component').then((m) => m.FiscalPeriodListComponent)
  },
  {
    path: 'journals',
    canActivate: [permissionGuard(Permissions.FinanceJournalsView)],
    loadComponent: () =>
      import('./pages/journal-list/journal-list.component').then((m) => m.JournalListComponent)
  },
  {
    path: 'journals/:id',
    canActivate: [permissionGuard(Permissions.FinanceJournalsView)],
    loadComponent: () =>
      import('./pages/journal-detail/journal-detail.component').then((m) => m.JournalDetailComponent)
  },
  {
    path: 'general-ledger',
    canActivate: [permissionGuard(Permissions.FinanceGeneralLedgerView)],
    loadComponent: () =>
      import('./pages/general-ledger/general-ledger.component').then((m) => m.GeneralLedgerComponent)
  },
  {
    path: 'trial-balance',
    canActivate: [permissionGuard(Permissions.FinanceTrialBalanceView)],
    loadComponent: () =>
      import('./pages/trial-balance/trial-balance.component').then((m) => m.TrialBalanceComponent)
  },
  {
    path: 'ar',
    canActivate: [permissionGuard(Permissions.FinanceArView)],
    loadComponent: () =>
      import('./pages/ar-overview/ar-overview.component').then((m) => m.ArOverviewComponent)
  },
  {
    path: 'ap',
    canActivate: [permissionGuard(Permissions.FinanceApView)],
    loadComponent: () =>
      import('./pages/ap-overview/ap-overview.component').then((m) => m.ApOverviewComponent)
  },
  { path: '', pathMatch: 'full', redirectTo: 'accounts' }
];
