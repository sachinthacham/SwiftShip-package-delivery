import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { TokenService } from '../services/token.service';

describe('authGuard', () => {
  let tokenService: jasmine.SpyObj<TokenService>;
  let router: Router;

  beforeEach(() => {
    tokenService = jasmine.createSpyObj<TokenService>('TokenService', ['getAccessToken']);

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TokenService, useValue: tokenService }]
    });
    router = TestBed.inject(Router);
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  }

  it('allows navigation when an access token is present', () => {
    tokenService.getAccessToken.and.returnValue('some-token');
    expect(runGuard()).toBeTrue();
  });

  it('redirects to /auth/login when there is no access token', () => {
    tokenService.getAccessToken.and.returnValue(null);
    const result = runGuard();
    const expectedTree = router.createUrlTree(['/auth/login']);
    expect(router.serializeUrl(result as never)).toBe(router.serializeUrl(expectedTree));
  });
});
