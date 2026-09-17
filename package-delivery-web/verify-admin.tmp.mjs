import { chromium } from 'playwright';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function base64url(obj) {
  return Buffer.from(JSON.stringify(obj)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fakeJwt(role) {
  const header = base64url({ alg: 'HS256', typ: 'JWT' });
  const payload = base64url({
    sub: '11111111-1111-1111-1111-111111111111',
    email: 'admin@test.local',
    [ROLE_CLAIM]: role,
    exp: Math.floor(Date.now() / 1000) + 3600
  });
  return `${header}.${payload}.fakesig`;
}

const routes = [
  '/admin/dashboard',
  '/admin/dispatch',
  '/admin/shipments',
  '/admin/shipments/00000000-0000-0000-0000-000000000000',
  '/admin/couriers',
  '/admin/customers',
  '/admin/analytics'
];

const browser = await chromium.launch();
const page = await browser.newPage();

const errors = [];
page.on('pageerror', (err) => errors.push(`[pageerror] ${err.message}`));
page.on('console', (msg) => {
  if (msg.type() === 'error') errors.push(`[console.error] ${msg.text()}`);
});

await page.goto('http://localhost:4200/');
await page.evaluate((token) => {
  localStorage.setItem('pds.accessToken', token);
  localStorage.setItem('pds.refreshToken', 'fake-refresh');
}, fakeJwt('Admin'));

for (const route of routes) {
  errors.length = 0;
  await page.goto(`http://localhost:4200${route}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(500);
  const title = await page.locator('h1').first().textContent().catch(() => '(no h1)');
  console.log(`${route} -> h1: "${title?.trim()}" | errors: ${errors.length}`);
  if (errors.length) {
    console.log(errors.join('\n'));
  }
}

await browser.close();
