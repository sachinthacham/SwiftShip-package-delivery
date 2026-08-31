import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'pds.accessToken';
const REFRESH_TOKEN_KEY = 'pds.refreshToken';

/**
 * Raw JWT payload as issued by IdentityService's JwtService. Note the role claim is written using the
 * long ClaimTypes.Role URI (not a short "role" key) because the server signs it via `new Claim(ClaimTypes.Role, ...)`.
 */
export const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface JwtPayload {
  sub?: string;
  email?: string;
  exp?: number;
  [key: string]: unknown;
}

/** Thin wrapper around localStorage so the storage mechanism can be swapped later without touching callers. */
@Injectable({ providedIn: 'root' })
export class TokenService {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  decodeAccessToken(): JwtPayload | null {
    const token = this.getAccessToken();
    if (!token) return null;
    return this.decode(token);
  }

  isAccessTokenExpired(): boolean {
    const payload = this.decodeAccessToken();
    const exp = payload?.['exp'] as number | undefined;
    if (!exp) return true;
    return Date.now() >= exp * 1000;
  }

  getUserIdFromToken(): string | null {
    const payload = this.decodeAccessToken();
    return (payload?.['sub'] as string | undefined) ?? null;
  }

  getRoleFromToken(): string | null {
    const payload = this.decodeAccessToken();
    return (payload?.[ROLE_CLAIM] as string | undefined) ?? null;
  }

  private decode(token: string): JwtPayload | null {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
          .join('')
      );
      return JSON.parse(json);
    } catch {
      return null;
    }
  }
}
