import { Routes } from '@angular/router';

export const PRESCRIPTIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/prescription-list/prescription-list.component').then((m) => m.PrescriptionListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/prescription-create/prescription-create.component').then((m) => m.PrescriptionCreateComponent)
  },
  {
    path: 'patients/:patientId/history',
    loadComponent: () =>
      import('./pages/patient-prescription-history/patient-prescription-history.component').then(
        (m) => m.PatientPrescriptionHistoryComponent
      )
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/prescription-detail/prescription-detail.component').then((m) => m.PrescriptionDetailComponent)
  }
];
