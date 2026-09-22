import { Routes } from '@angular/router';

export const SCHEDULING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/schedule-list/schedule-list.component').then((m) => m.ScheduleListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/schedule-form/schedule-form.component').then((m) => m.ScheduleFormComponent)
  },
  {
    path: 'availability',
    loadComponent: () =>
      import('./pages/availability-view/availability-view.component').then((m) => m.AvailabilityViewComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () =>
      import('./pages/schedule-form/schedule-form.component').then((m) => m.ScheduleFormComponent)
  }
];
