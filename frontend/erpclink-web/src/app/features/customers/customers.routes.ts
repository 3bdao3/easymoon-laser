import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const CUSTOMERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/customer-list.component').then((m) => m.CustomerListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.LaserCustomersCreate)],
    loadComponent: () =>
      import('./pages/new-customer-booking.component').then((m) => m.NewCustomerBookingComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.LaserCustomersUpdate)],
    loadComponent: () =>
      import('./pages/customer-form.component').then((m) => m.CustomerFormComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/customer-detail.component').then((m) => m.CustomerDetailComponent)
  }
];
