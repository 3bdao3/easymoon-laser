import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, map, throwError } from 'rxjs';
import {
  AuthResponse,
  AuthUser,
  LoginRequest,
  LogoutRequest,
  RefreshTokenRequest
} from './auth.models';
import { TokenStorage } from './token-storage';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storage = new TokenStorage();
  private readonly userSignal = signal<AuthUser | null>(this.readUser());
  private readonly accessTokenSignal = signal<string | null>(this.storage.getAccessToken());

  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());
  readonly permissions = computed(() => this.userSignal()?.permissions ?? []);
  readonly roles = computed(() => this.userSignal()?.roles ?? []);

  private readonly apiRoot = `${environment.apiBaseUrl}/api`;

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiRoot}/auth/login`, request).pipe(
      tap((response) => this.persist(response))
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.storage.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const body: RefreshTokenRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${this.apiRoot}/auth/refresh`, body).pipe(
      tap((response) => this.persist(response))
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.storage.getRefreshToken();
    const body: LogoutRequest = { refreshToken: refreshToken ?? '' };

    return this.http.post<void>(`${this.apiRoot}/auth/logout`, body).pipe(
      tap(() => this.clearSession()),
      map(() => void 0)
    );
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  hasPermission(permissionCode: string): boolean {
    return this.permissions().some((p) => p.toLowerCase() === permissionCode.toLowerCase());
  }

  hasAnyPermission(...codes: string[]): boolean {
    return codes.some((code) => this.hasPermission(code));
  }

  clearSession(): void {
    this.clear();
  }

  private persist(response: AuthResponse): void {
    this.storage.save(response);
    this.accessTokenSignal.set(response.accessToken);
    this.userSignal.set(response.user);
  }

  private clear(): void {
    this.storage.clear();
    this.accessTokenSignal.set(null);
    this.userSignal.set(null);
  }

  private readUser(): AuthUser | null {
    const raw = this.storage.getUserJson();
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
