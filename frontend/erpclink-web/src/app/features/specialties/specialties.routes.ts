import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const SPECIALTIES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/specialty-list.component').then((m) => m.SpecialtyListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.SpecialtiesManage)],
    loadComponent: () =>
      import('./pages/specialty-create.component').then((m) => m.SpecialtyCreateComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.SpecialtiesManage)],
    loadComponent: () =>
      import('./pages/specialty-edit.component').then((m) => m.SpecialtyEditComponent)
  }
];
