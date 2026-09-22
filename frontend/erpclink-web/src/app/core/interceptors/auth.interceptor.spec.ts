import { HttpErrorResponse, HttpRequest, HttpResponse, HttpHeaders } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../auth/auth.service';
import { AuthResponse } from '../auth/auth.models';

describe('authInterceptor', () => {
  let auth: jasmine.SpyObj<AuthService>;
  let router: Router;

  const authResponse: AuthResponse = {
    accessToken: 'new-token',
    refreshToken: 'refresh',
    expiresAtUtc: new Date().toISOString(),
    user: {
      id: '1',
      email: 'a@b.c',
      fullName: 'Admin',
      isActive: true,
      roles: [],
      permissions: []
    }
  };

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', [
      'getAccessToken',
      'refresh',
      'clearSession'
    ]);
    auth.getAccessToken.and.returnValue('old-token');

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        { provide: AuthService, useValue: auth }
      ]
    });
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
  });

  it('attaches bearer token', (done) => {
    const req = new HttpRequest('GET', '/api/v1/patients/search');
    TestBed.runInInjectionContext(() => {
      authInterceptor(req, (outgoing) => {
        expect(outgoing.headers.get('Authorization')).toBe('Bearer old-token');
        return of(new HttpResponse({ status: 200 }));
      }).subscribe({
        next: () => done(),
        error: done.fail
      });
    });
  });

  it('refreshes once on 401 then retries', (done) => {
    auth.refresh.and.returnValue(of(authResponse));
    let calls = 0;
    const req = new HttpRequest('GET', '/api/v1/patients/search');

    TestBed.runInInjectionContext(() => {
      authInterceptor(req, (outgoing) => {
        calls += 1;
        if (calls === 1) {
          return throwError(
            () => new HttpErrorResponse({ status: 401, url: outgoing.url })
          );
        }
        expect(outgoing.headers.get('Authorization')).toBe('Bearer new-token');
        return of(new HttpResponse({ status: 200 }));
      }).subscribe({
        next: () => {
          expect(auth.refresh).toHaveBeenCalledTimes(1);
          expect(calls).toBe(2);
          done();
        },
        error: done.fail
      });
    });
  });

  it('logs out when refresh fails', (done) => {
    auth.refresh.and.returnValue(throwError(() => new Error('refresh failed')));
    const req = new HttpRequest('GET', '/api/v1/patients/search');

    TestBed.runInInjectionContext(() => {
      authInterceptor(req, () =>
        throwError(() => new HttpErrorResponse({ status: 401 }))
      ).subscribe({
        next: () => done.fail('expected error'),
        error: () => {
          expect(auth.clearSession).toHaveBeenCalled();
          expect(router.navigate).toHaveBeenCalledWith(['/login']);
          done();
        }
      });
    });
  });

  it('does not attach token to login requests', (done) => {
    const req = new HttpRequest('POST', '/api/auth/login', {});
    TestBed.runInInjectionContext(() => {
      authInterceptor(req, (outgoing) => {
        expect(outgoing.headers.has('Authorization')).toBeFalse();
        return of(new HttpResponse({ status: 200 }));
      }).subscribe({
        next: () => done(),
        error: done.fail
      });
    });
  });
});
