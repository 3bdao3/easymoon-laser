import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const DOCTORS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/doctor-list.component').then((m) => m.DoctorListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.DoctorsCreate)],
    loadComponent: () => import('./pages/doctor-create.component').then((m) => m.DoctorCreateComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.DoctorsUpdate)],
    loadComponent: () => import('./pages/doctor-edit.component').then((m) => m.DoctorEditComponent)
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/doctor-detail.component').then((m) => m.DoctorDetailComponent)
  }
];
