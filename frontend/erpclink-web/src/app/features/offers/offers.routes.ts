import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const OFFERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/offers-list.component').then((m) => m.OffersListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.LaserOffersManage)],
    loadComponent: () =>
      import('./pages/offer-form.component').then((m) => m.OfferFormComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.LaserOffersManage)],
    loadComponent: () =>
      import('./pages/offer-form.component').then((m) => m.OfferFormComponent)
  }
];
