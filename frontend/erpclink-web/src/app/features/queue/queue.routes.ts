import { Routes } from '@angular/router';

export const QUEUE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/queue-today/queue-today.component').then((m) => m.QueueTodayComponent)
  },
  {
    path: 'check-in',
    loadComponent: () =>
      import('./pages/queue-check-in/queue-check-in.component').then((m) => m.QueueCheckInComponent)
  }
];
