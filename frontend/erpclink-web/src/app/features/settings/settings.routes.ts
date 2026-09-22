import { Routes } from '@angular/router';

export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/clinic-settings.component').then((m) => m.ClinicSettingsComponent)
  }
];
