import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const CLINICS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/clinic-list.component').then((m) => m.ClinicListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.ClinicsCreate)],
    loadComponent: () => import('./pages/clinic-create.component').then((m) => m.ClinicCreateComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.ClinicsUpdate)],
    loadComponent: () => import('./pages/clinic-edit.component').then((m) => m.ClinicEditComponent)
  }
];
