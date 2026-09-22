import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { Permissions } from './core/permissions/permissions';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'app/dashboard' },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./features/forbidden/forbidden.component').then((m) => m.ForbiddenComponent)
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        canActivate: [permissionGuard(Permissions.LaserDashboardView)],
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'customers',
        canActivate: [permissionGuard(Permissions.LaserCustomersView)],
        loadChildren: () =>
          import('./features/customers/customers.routes').then((m) => m.CUSTOMERS_ROUTES)
      },
      {
        path: 'appointments',
        canActivate: [permissionGuard(Permissions.LaserAppointmentsView)],
        loadChildren: () =>
          import('./features/appointments/appointments.routes').then((m) => m.APPOINTMENTS_ROUTES)
      },
      {
        path: 'calendar',
        canActivate: [permissionGuard(Permissions.LaserAppointmentsView)],
        loadChildren: () =>
          import('./features/calendar/calendar.routes').then((m) => m.CALENDAR_ROUTES)
      },
      {
        path: 'book',
        canActivate: [permissionGuard(Permissions.LaserAppointmentsCreate)],
        loadChildren: () => import('./features/book/book.routes').then((m) => m.BOOK_ROUTES)
      },
      {
        path: 'laser-services',
        canActivate: [permissionGuard(Permissions.LaserServicesManage)],
        loadChildren: () =>
          import('./features/laser-services/laser-services.routes').then(
            (m) => m.LASER_SERVICES_ROUTES
          )
      },
      {
        path: 'offers',
        canActivate: [permissionGuard(Permissions.LaserOffersView)],
        loadChildren: () => import('./features/offers/offers.routes').then((m) => m.OFFERS_ROUTES)
      },
      {
        path: 'reports',
        canActivate: [permissionGuard(Permissions.LaserReportsView)],
        loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORTS_ROUTES)
      },
      {
        path: 'settings',
        canActivate: [permissionGuard(Permissions.LaserSettingsView)],
        loadChildren: () =>
          import('./features/settings/settings.routes').then((m) => m.SETTINGS_ROUTES)
      },
      {
        path: 'administration',
        canActivate: [permissionGuard(Permissions.AdministrationUsersView)],
        loadChildren: () =>
          import('./features/administration/administration.routes').then(
            (m) => m.ADMINISTRATION_ROUTES
          )
      }
    ]
  },
  { path: '**', redirectTo: 'app/dashboard' }
];
