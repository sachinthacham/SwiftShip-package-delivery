import { ActivatedRouteSnapshot } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { roleGuard } from './role.guard';
import { TokenService } from '../services/token.service';
import { UserRole } from '../models';

describe('roleGuard', () => {
  let tokenService: jasmine.SpyObj<TokenService>;
  let router: Router;

  beforeEach(() => {
    tokenService = jasmine.createSpyObj<TokenService>('TokenService', ['getRoleFromToken']);

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TokenService, useValue: tokenService }]
    });
    router = TestBed.inject(Router);
  });

  function runGuard(roles: UserRole[] | undefined) {
    const route = { data: { roles } } as unknown as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => roleGuard(route, {} as never));
  }

  it('allows navigation when the token role is in the required list', () => {
    tokenService.getRoleFromToken.and.returnValue(UserRole.Admin);
    expect(runGuard([UserRole.Dispatcher, UserRole.Admin])).toBeTrue();
  });

  it('allows navigation when no roles are required', () => {
    tokenService.getRoleFromToken.and.returnValue(UserRole.Customer);
    expect(runGuard(undefined)).toBeTrue();
  });

  it('redirects to / when the token role is not in the required list', () => {
    tokenService.getRoleFromToken.and.returnValue(UserRole.Customer);
    const result = runGuard([UserRole.Admin]);
    const expectedTree = router.createUrlTree(['/']);
    expect(router.serializeUrl(result as never)).toBe(router.serializeUrl(expectedTree));
  });

  it('redirects to / when there is no role on the token at all', () => {
    tokenService.getRoleFromToken.and.returnValue(null);
    const result = runGuard([UserRole.Admin]);
    const expectedTree = router.createUrlTree(['/']);
    expect(router.serializeUrl(result as never)).toBe(router.serializeUrl(expectedTree));
  });
});
