import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const ADMINISTRATION_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/user-list.component').then((m) => m.UserListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.AdministrationUsersCreate)],
    loadComponent: () => import('./pages/user-create.component').then((m) => m.UserCreateComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.AdministrationUsersUpdate)],
    loadComponent: () => import('./pages/user-edit.component').then((m) => m.UserEditComponent)
  }
];
