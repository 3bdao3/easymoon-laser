import { Routes } from '@angular/router';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/laser-reports.component').then((m) => m.LaserReportsComponent)
  }
];
