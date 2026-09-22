import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const BILLING_ROUTES: Routes = [
  {
    path: '',
    canActivate: [permissionGuard(Permissions.FinanceInvoicesView)],
    loadComponent: () =>
      import('./pages/invoice-list/invoice-list.component').then((m) => m.InvoiceListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.FinanceInvoicesCreate)],
    loadComponent: () =>
      import('./pages/invoice-form/invoice-form.component').then((m) => m.InvoiceFormComponent)
  },
  {
    path: ':id',
    canActivate: [permissionGuard(Permissions.FinanceInvoicesView)],
    loadComponent: () =>
      import('./pages/invoice-detail/invoice-detail.component').then((m) => m.InvoiceDetailComponent)
  }
];
