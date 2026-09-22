import { Routes } from '@angular/router';

export const CALENDAR_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/calendar-day.component').then((m) => m.CalendarDayComponent)
  }
];
