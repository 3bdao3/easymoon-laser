import { Routes } from '@angular/router';

export const BOOK_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('../customers/pages/new-customer-booking.component').then(
        (m) => m.NewCustomerBookingComponent
      )
  }
];
