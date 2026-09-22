import { Routes } from '@angular/router';

export const MEDICAL_VISITS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/visit-list/visit-list.component').then((m) => m.VisitListComponent)
  },
  {
    path: 'start',
    loadComponent: () =>
      import('./pages/visit-start/visit-start.component').then((m) => m.VisitStartComponent)
  },
  {
    path: 'patients/:patientId/history',
    loadComponent: () =>
      import('./pages/patient-visit-history/patient-visit-history.component').then(
        (m) => m.PatientVisitHistoryComponent
      )
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/visit-detail/visit-detail.component').then((m) => m.VisitDetailComponent)
  }
];
