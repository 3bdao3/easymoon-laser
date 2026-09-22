import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { authGuard, permissionGuard } from './auth.guard';
import { AuthService } from '../auth/auth.service';

function mockAuth(opts: { authenticated: boolean; permissions?: string[] }): AuthService {
  const authenticated = signal(opts.authenticated);
  const permissions = opts.permissions ?? [];
  return {
    isAuthenticated: authenticated.asReadonly(),
    hasPermission: (code: string) =>
      permissions.some((p) => p.toLowerCase() === code.toLowerCase())
  } as unknown as AuthService;
}

describe('authGuard', () => {
  it('redirects unauthenticated users to login', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        { provide: AuthService, useValue: mockAuth({ authenticated: false }) }
      ]
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(String(result)).toContain('login');
  });

  it('allows authenticated users', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuth({ authenticated: true }) }
      ]
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBeTrue();
  });
});

describe('permissionGuard', () => {
  it('allows when permission is present', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'forbidden', children: [] }]),
        {
          provide: AuthService,
          useValue: mockAuth({ authenticated: true, permissions: ['Patients.View'] })
        }
      ]
    });

    const guard = permissionGuard('Patients.View');
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(result).toBeTrue();
  });

  it('redirects to forbidden when permission missing', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'forbidden', children: [] }, { path: 'login', children: [] }]),
        { provide: AuthService, useValue: mockAuth({ authenticated: true, permissions: [] }) }
      ]
    });

    const guard = permissionGuard('Patients.View');
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(String(result)).toContain('forbidden');
  });
});
