import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../../shared/components/toast/toast.service';

/**
 * Backend errors arrive in one of two RFC7807-ish shapes:
 *  - FluentValidation's ASP.NET auto-validation: { title, status, errors: { field: string[] } }
 *  - BuildingBlocks' global exception handler: { type, title, status, detail, traceId }
 */
interface ProblemDetailsBody {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

function extractMessage(error: HttpErrorResponse): string {
  const body = error.error as ProblemDetailsBody | undefined;

  if (body?.errors) {
    const messages = Object.values(body.errors).flat();
    if (messages.length) return messages.join(' ');
  }

  if (body?.detail) return body.detail;
  if (body?.title) return body.title;

  if (error.status === 0) return 'Could not reach the server. Check your connection and try again.';
  if (error.status === 401) return 'You need to sign in to do that.';
  if (error.status === 403) return "You don't have permission to do that.";
  if (error.status === 404) return 'The requested resource was not found.';

  return `Something went wrong (HTTP ${error.status}).`;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // 401s on the auth endpoints themselves are expected (bad credentials) — let callers handle those directly.
        const isAuthEndpoint = req.url.includes('/api/auth/login') || req.url.includes('/api/auth/register');
        if (!(error.status === 401 && isAuthEndpoint)) {
          toast.error(extractMessage(error));
        }
      }
      return throwError(() => error);
    })
  );
};
