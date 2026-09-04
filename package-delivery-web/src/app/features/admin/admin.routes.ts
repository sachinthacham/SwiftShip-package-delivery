import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent)
  },
  {
    path: 'dispatch',
    loadComponent: () => import('./dispatch/dispatch-board.component').then((m) => m.DispatchBoardComponent)
  },
  {
    path: 'shipments',
    loadComponent: () => import('./shipments/shipments-list.component').then((m) => m.AdminShipmentsListComponent)
  },
  {
    path: 'shipments/:id',
    loadComponent: () => import('./shipments/shipment-detail.component').then((m) => m.AdminShipmentDetailComponent)
  },
  {
    path: 'couriers',
    loadComponent: () => import('./couriers/couriers-list.component').then((m) => m.CouriersListComponent)
  },
  {
    path: 'customers',
    loadComponent: () => import('./customers/customers-list.component').then((m) => m.CustomersListComponent)
  },
  {
    path: 'analytics',
    loadComponent: () => import('./analytics/analytics.component').then((m) => m.AnalyticsComponent)
  }
];
