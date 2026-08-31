import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { UserRole } from '../models';

/** Configure via route data: `{ roles: [UserRole.Admin, UserRole.Dispatcher] }`. Assumes authGuard already ran. */
export const roleGuard: CanActivateFn = (route) => {
  const tokenService = inject(TokenService);
  const router = inject(Router);

  const requiredRoles = (route.data['roles'] as UserRole[] | undefined) ?? [];
  const currentRole = tokenService.getRoleFromToken();

  if (requiredRoles.length === 0 || (currentRole && requiredRoles.includes(currentRole as UserRole))) {
    return true;
  }

  return router.createUrlTree(['/']);
};
