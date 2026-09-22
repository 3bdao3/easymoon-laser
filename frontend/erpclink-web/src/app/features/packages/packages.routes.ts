import { Routes } from '@angular/router';

export const PACKAGES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/package-list/package-list.component').then((m) => m.PackageListComponent)
  },
  {
    path: 'new',
    loadComponent: () =>
      import('./pages/package-form/package-form.component').then((m) => m.PackageFormComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/package-detail/package-detail.component').then((m) => m.PackageDetailComponent)
  }
];
