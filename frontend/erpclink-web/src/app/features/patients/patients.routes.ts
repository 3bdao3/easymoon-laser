import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/auth.guard';
import { Permissions } from '../../core/permissions/permissions';

export const PATIENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/patient-list.component').then((m) => m.PatientListComponent)
  },
  {
    path: 'new',
    canActivate: [permissionGuard(Permissions.PatientsCreate)],
    loadComponent: () =>
      import('./pages/patient-create.component').then((m) => m.PatientCreateComponent)
  },
  {
    path: ':id/edit',
    canActivate: [permissionGuard(Permissions.PatientsUpdate)],
    loadComponent: () =>
      import('./pages/patient-edit.component').then((m) => m.PatientEditComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/patient-detail.component').then((m) => m.PatientDetailComponent)
  }
];
