import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  catchError,
  finalize,
  Observable,
  shareReplay,
  switchMap,
  throwError
} from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { AuthResponse } from '../auth/auth.models';

const AUTH_RETRY_HEADER = 'X-Auth-Retry';

function isAuthBypassUrl(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh');
}

let refreshInFlight: Observable<AuthResponse> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (isAuthBypassUrl(req.url)) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const outgoing =
    token && !req.headers.has('Authorization')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      if (req.headers.has(AUTH_RETRY_HEADER)) {
        auth.clearSession();
        void router.navigate(['/login']);
        return throwError(() => error);
      }

      if (!refreshInFlight) {
        refreshInFlight = auth.refresh().pipe(
          shareReplay({ bufferSize: 1, refCount: false }),
          finalize(() => {
            refreshInFlight = null;
          })
        );
      }

      return refreshInFlight.pipe(
        switchMap((response) => {
          const retryReq = req.clone({
            setHeaders: {
              Authorization: `Bearer ${response.accessToken}`,
              [AUTH_RETRY_HEADER]: '1'
            }
          });
          return next(retryReq);
        }),
        catchError((refreshError) => {
          auth.clearSession();
          void router.navigate(['/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
