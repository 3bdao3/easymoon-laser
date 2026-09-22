import { Routes } from '@angular/router';

export const APPOINTMENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/laser-appointment-list.component').then((m) => m.LaserAppointmentListComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/laser-appointment-detail.component').then((m) => m.LaserAppointmentDetailComponent)
  }
];
