import { Routes } from '@angular/router';

export const MEDICATIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/medication-list/medication-list.component').then((m) => m.MedicationListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/medication-form/medication-form.component').then((m) => m.MedicationFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./pages/medication-form/medication-form.component').then((m) => m.MedicationFormComponent)
  }
];
