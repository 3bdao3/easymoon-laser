import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const LASER_SERVICES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/laser-services-list.component').then((m) => m.LaserServicesListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.LaserServicesManage)],
    loadComponent: () =>
      import('./pages/laser-service-form.component').then((m) => m.LaserServiceFormComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.LaserServicesManage)],
    loadComponent: () =>
      import('./pages/laser-service-form.component').then((m) => m.LaserServiceFormComponent)
  }
];
