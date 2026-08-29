import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';
import { AuthResponse } from '../models';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let tokenService: jasmine.SpyObj<TokenService>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    tokenService = jasmine.createSpyObj<TokenService>('TokenService', ['getAccessToken', 'getRefreshToken']);
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['refresh', 'logout']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: TokenService, useValue: tokenService },
        { provide: AuthService, useValue: authService }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  afterEach(() => httpMock.verify());

  it('attaches the Authorization header when a token exists', () => {
    tokenService.getAccessToken.and.returnValue('abc123');

    http.get('/api/package/api/packages').subscribe();

    const req = httpMock.expectOne('/api/package/api/packages');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc123');
    req.flush({});
  });

  it('does not attach the Authorization header to the login endpoint', () => {
    tokenService.getAccessToken.and.returnValue('abc123');

    http.post('/api/auth/login', {}).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('on 401 with a refresh token, refreshes and retries the original request with the new token', () => {
    tokenService.getAccessToken.and.returnValue('expired-token');
    tokenService.getRefreshToken.and.returnValue('refresh-token');
    authService.refresh.and.returnValue(
      new Observable<AuthResponse>((subscriber) => {
        subscriber.next({ accessToken: 'new-token', refreshToken: 'new-refresh' } as AuthResponse);
        subscriber.complete();
      })
    );

    let result: unknown;
    http.get('/api/package/api/packages').subscribe((r) => (result = r));

    httpMock.expectOne('/api/package/api/packages').flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    const retriedReq = httpMock.expectOne('/api/package/api/packages');
    expect(retriedReq.request.headers.get('Authorization')).toBe('Bearer new-token');
    retriedReq.flush({ ok: true });

    expect(result).toEqual({ ok: true });
  });

  it('shares exactly one underlying refresh execution across concurrent 401s (not just one spy call)', () => {
    tokenService.getAccessToken.and.returnValue('expired-token');
    tokenService.getRefreshToken.and.returnValue('refresh-token');

    // A cold observable that (a) counts how many times something subscribes to it and (b) does NOT emit
    // synchronously — it holds the subscriber so the test controls exactly when the "network call" resolves,
    // mirroring how a real pending HttpClient request stays open across both concurrent 401s. This is the
    // scenario that requires actual multicast (shareReplay): if the interceptor's "shared" refresh observable
    // isn't truly shared, the second 401 subscribes to the same observable instance independently and this
    // producer function runs again, incrementing subscriptionCount to 2.
    let subscriptionCount = 0;
    const pendingSubscribers: Array<(response: AuthResponse) => void> = [];
    authService.refresh.and.returnValue(
      new Observable<AuthResponse>((subscriber) => {
        subscriptionCount++;
        pendingSubscribers.push((response) => {
          subscriber.next(response);
          subscriber.complete();
        });
      })
    );

    http.get('/api/a').subscribe();
    http.get('/api/b').subscribe();

    const reqA = httpMock.expectOne('/api/a');
    const reqB = httpMock.expectOne('/api/b');
    reqA.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });
    reqB.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    // Both 401s are now waiting on the (still-unresolved) refresh call. Resolve it now, for everyone at once.
    expect(subscriptionCount).toBe(1);
    expect(authService.refresh).toHaveBeenCalledTimes(1);
    pendingSubscribers.forEach((resolve) => resolve({ accessToken: 'new-token', refreshToken: 'new-refresh' } as AuthResponse));

    httpMock.expectOne('/api/a').flush({});
    httpMock.expectOne('/api/b').flush({});
  });

  it('passes a 401 straight through without attempting refresh when there is no refresh token', () => {
    tokenService.getAccessToken.and.returnValue('expired-token');
    tokenService.getRefreshToken.and.returnValue(null);

    let errored = false;
    http.get('/api/package/api/packages').subscribe({ error: () => (errored = true) });

    httpMock.expectOne('/api/package/api/packages').flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBeTrue();
    expect(authService.refresh).not.toHaveBeenCalled();
  });

  it('logs out and redirects to login when the refresh call itself fails', () => {
    tokenService.getAccessToken.and.returnValue('expired-token');
    tokenService.getRefreshToken.and.returnValue('refresh-token');
    authService.refresh.and.returnValue(throwError(() => new Error('refresh failed')));

    let errored = false;
    http.get('/api/package/api/packages').subscribe({ error: () => (errored = true) });

    httpMock.expectOne('/api/package/api/packages').flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBeTrue();
    expect(authService.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });
});
