import { AuthResponse } from './auth.models';

const ACCESS_TOKEN_KEY = 'erpclink.accessToken';
const REFRESH_TOKEN_KEY = 'erpclink.refreshToken';
const EXPIRES_AT_KEY = 'erpclink.expiresAtUtc';
const USER_KEY = 'erpclink.user';

/**
 * Token storage abstraction. Swap implementation later (memory-only, sessionStorage, etc.).
 * Do not store tokens in scattered components.
 */
export class TokenStorage {
  save(auth: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, auth.expiresAtUtc);
    localStorage.setItem(USER_KEY, JSON.stringify(auth.user));
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
    localStorage.removeItem(USER_KEY);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  getExpiresAtUtc(): string | null {
    return localStorage.getItem(EXPIRES_AT_KEY);
  }

  getUserJson(): string | null {
    return localStorage.getItem(USER_KEY);
  }
}
