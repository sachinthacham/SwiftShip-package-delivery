import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { UserRole } from './core/models';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./layouts/public-layout/public-layout.component').then((m) => m.PublicLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./features/public-tracking/landing.component').then((m) => m.LandingComponent) },
      {
        path: 'track/:trackingNumber',
        loadComponent: () => import('./features/public-tracking/public-tracking.component').then((m) => m.PublicTrackingComponent)
      },
      {
        path: 'track',
        loadComponent: () => import('./features/public-tracking/public-tracking.component').then((m) => m.PublicTrackingComponent)
      },
      {
        path: 'auth/login',
        loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
      },
      {
        path: 'auth/register',
        loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent)
      }
    ]
  },
  {
    path: 'customer',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Customer] },
    loadComponent: () => import('./layouts/customer-layout/customer-layout.component').then((m) => m.CustomerLayoutComponent),
    loadChildren: () => import('./features/customer/customer.routes').then((m) => m.CUSTOMER_ROUTES)
  },
  {
    path: 'courier',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Courier] },
    loadComponent: () => import('./layouts/courier-layout/courier-layout.component').then((m) => m.CourierLayoutComponent),
    loadChildren: () => import('./features/courier/courier.routes').then((m) => m.COURIER_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.Dispatcher, UserRole.Admin] },
    loadComponent: () => import('./layouts/admin-layout/admin-layout.component').then((m) => m.AdminLayoutComponent),
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES)
  },
  { path: '**', redirectTo: '' }
];
