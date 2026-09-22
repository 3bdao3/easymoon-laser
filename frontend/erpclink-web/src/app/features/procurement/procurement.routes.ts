import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const PROCUREMENT_ROUTES: Routes = [
  {
    path: 'suppliers',
    canActivate: [permissionGuard(Permissions.ProcurementSuppliersView)],
    loadComponent: () =>
      import('./pages/supplier-list/supplier-list.component').then((m) => m.SupplierListComponent)
  },
  {
    path: 'suppliers/new',
    canActivate: [permissionGuard(Permissions.ProcurementSuppliersCreate)],
    loadComponent: () =>
      import('./pages/supplier-form/supplier-form.component').then((m) => m.SupplierFormComponent)
  },
  {
    path: 'suppliers/:id',
    canActivate: [permissionGuard(Permissions.ProcurementSuppliersView)],
    loadComponent: () =>
      import('./pages/supplier-detail/supplier-detail.component').then((m) => m.SupplierDetailComponent)
  },
  {
    path: 'purchase-orders',
    canActivate: [permissionGuard(Permissions.ProcurementPurchaseOrdersView)],
    loadComponent: () =>
      import('./pages/purchase-order-list/purchase-order-list.component').then((m) => m.PurchaseOrderListComponent)
  },
  {
    path: 'purchase-orders/new',
    canActivate: [permissionGuard(Permissions.ProcurementPurchaseOrdersCreate)],
    loadComponent: () =>
      import('./pages/purchase-order-form/purchase-order-form.component').then((m) => m.PurchaseOrderFormComponent)
  },
  {
    path: 'purchase-orders/:id',
    canActivate: [permissionGuard(Permissions.ProcurementPurchaseOrdersView)],
    loadComponent: () =>
      import('./pages/purchase-order-detail/purchase-order-detail.component').then(
        (m) => m.PurchaseOrderDetailComponent
      )
  },
  { path: '', pathMatch: 'full', redirectTo: 'suppliers' }
];
