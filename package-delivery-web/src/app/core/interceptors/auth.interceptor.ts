import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';
import { AuthResponse } from '../models';

/** Shared across all concurrent requests in this module so a wave of parallel 401s triggers exactly one refresh call. */
let refreshInProgress$: Observable<AuthResponse> | null = null;

const NO_AUTH_HEADER_PATTERNS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh', '/api/tracking/public/'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  const skipAuthHeader = NO_AUTH_HEADER_PATTERNS.some((p) => req.url.includes(p));
  const accessToken = tokenService.getAccessToken();

  const authedReq = !skipAuthHeader && accessToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      const isAuthEndpoint = NO_AUTH_HEADER_PATTERNS.some((p) => req.url.includes(p));
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuthEndpoint || !tokenService.getRefreshToken()) {
        return throwError(() => error);
      }

      if (!refreshInProgress$) {
        // authService.refresh() wraps a raw HttpClient call (cold observable, no built-in multicast) — without
        // shareReplay, each concurrent 401 subscribing to "the shared" observable would independently re-trigger
        // its own HTTP request instead of actually sharing one.
        refreshInProgress$ = authService.refresh().pipe(shareReplay(1));
      }

      return refreshInProgress$.pipe(
        switchMap((response) => {
          refreshInProgress$ = null;
          const retriedReq = req.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } });
          return next(retriedReq);
        }),
        catchError((refreshError) => {
          refreshInProgress$ = null;
          authService.logout();
          router.navigate(['/auth/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
