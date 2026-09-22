import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

/**
 * Route guard foundation: requires authenticated session.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

/**
 * Permission guard foundation: requires one permission code.
 */
export const permissionGuard = (permissionCode: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (auth.hasPermission(permissionCode)) {
      return true;
    }

    return router.createUrlTree(['/forbidden']);
  };
};
