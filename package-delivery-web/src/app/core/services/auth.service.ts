import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, MeResponse, RegisterRequest, UserRole } from '../models';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(TokenService);

  private readonly base = `${environment.apiBaseUrl}/identity/api/auth`;

  private readonly _currentUser = signal<MeResponse | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly role = computed(() => this._currentUser()?.role ?? null);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, request).pipe(
      tap((response) => this.tokenService.setTokens(response.accessToken, response.refreshToken))
    );
  }

  /** Registration issues no tokens (backend returns 200 with an empty body) — caller must log in afterwards. */
  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/register`, request);
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.tokenService.getRefreshToken();
    return this.http.post<AuthResponse>(`${this.base}/refresh`, { refreshToken }).pipe(
      tap((response) => this.tokenService.setTokens(response.accessToken, response.refreshToken))
    );
  }

  loadCurrentUser(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.base}/me`).pipe(tap((user) => this._currentUser.set(user)));
  }

  logout(): void {
    const refreshToken = this.tokenService.getRefreshToken();
    if (refreshToken) {
      this.http.post(`${this.base}/logout`, { refreshToken }).subscribe({ error: () => void 0 });
    }
    this.tokenService.clear();
    this._currentUser.set(null);
  }

  hasRole(...roles: UserRole[]): boolean {
    const current = this.role();
    return current !== null && roles.includes(current);
  }
}
