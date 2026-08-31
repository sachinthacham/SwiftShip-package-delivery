import { TokenService, ROLE_CLAIM } from './token.service';

function makeJwt(payload: Record<string, unknown>): string {
  const b64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64url({ alg: 'HS256', typ: 'JWT' })}.${b64url(payload)}.signature`;
}

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    service = new TokenService();
    localStorage.clear();
  });

  afterEach(() => localStorage.clear());

  it('stores and retrieves access/refresh tokens', () => {
    service.setTokens('access-1', 'refresh-1');
    expect(service.getAccessToken()).toBe('access-1');
    expect(service.getRefreshToken()).toBe('refresh-1');
  });

  it('clear() removes both tokens', () => {
    service.setTokens('access-1', 'refresh-1');
    service.clear();
    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
  });

  it('returns null from decode helpers when no token is stored', () => {
    expect(service.decodeAccessToken()).toBeNull();
    expect(service.getUserIdFromToken()).toBeNull();
    expect(service.getRoleFromToken()).toBeNull();
    expect(service.isAccessTokenExpired()).toBeTrue();
  });

  it('decodes sub and the long ClaimTypes.Role URI from a real-shaped JWT', () => {
    const token = makeJwt({
      sub: 'user-123',
      [ROLE_CLAIM]: 'Admin',
      exp: Math.floor(Date.now() / 1000) + 3600
    });
    service.setTokens(token, 'refresh');

    expect(service.getUserIdFromToken()).toBe('user-123');
    expect(service.getRoleFromToken()).toBe('Admin');
    expect(service.isAccessTokenExpired()).toBeFalse();
  });

  it('treats a past exp claim as expired', () => {
    const token = makeJwt({ sub: 'user-123', exp: Math.floor(Date.now() / 1000) - 60 });
    service.setTokens(token, 'refresh');

    expect(service.isAccessTokenExpired()).toBeTrue();
  });

  it('treats a malformed token as undecodable rather than throwing', () => {
    service.setTokens('not-a-jwt', 'refresh');

    expect(service.decodeAccessToken()).toBeNull();
    expect(service.isAccessTokenExpired()).toBeTrue();
  });
});
