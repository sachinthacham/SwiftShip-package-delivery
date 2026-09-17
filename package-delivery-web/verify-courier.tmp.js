const { chromium } = require('playwright');

function base64url(obj) {
  return Buffer.from(JSON.stringify(obj)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const header = base64url({ alg: 'HS256', typ: 'JWT' });
const payload = base64url({
  sub: '11111111-1111-1111-1111-111111111111',
  email: 'courier@test.local',
  [ROLE_CLAIM]: 'Courier',
  exp: Math.floor(Date.now() / 1000) + 3600
});
const fakeJwt = `${header}.${payload}.fakesig`;

const routes = ['/courier/dashboard', '/courier/deliveries', '/courier/deliveries/22222222-2222-2222-2222-222222222222', '/courier/route-map'];

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', (err) => errors.push(`[pageerror] ${err.message}`));
  page.on('console', (msg) => {
    if (msg.type() === 'error') errors.push(`[console.error] ${msg.text()}`);
  });

  await page.goto('http://localhost:4200/auth/login');
  await page.evaluate(
    ([token]) => {
      localStorage.setItem('pds.accessToken', token);
      localStorage.setItem('pds.refreshToken', 'fake-refresh-token');
    },
    [fakeJwt]
  );

  for (const route of routes) {
    errors.length = 0;
    await page.goto(`http://localhost:4200${route}`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(500);
    console.log(`--- ${route} ---`);
    console.log('URL after load:', page.url());
    if (errors.length) {
      errors.forEach((e) => console.log(e));
    } else {
      console.log('No console/page errors.');
    }
  }

  await browser.close();
})();
