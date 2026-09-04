import { Routes } from '@angular/router';

export const COURIER_ROUTES: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/courier-dashboard.component').then((m) => m.CourierDashboardComponent)
  },
  {
    path: 'deliveries',
    loadComponent: () => import('./deliveries/courier-deliveries.component').then((m) => m.CourierDeliveriesComponent)
  },
  {
    path: 'deliveries/:id',
    loadComponent: () => import('./delivery-detail/courier-delivery-detail.component').then((m) => m.CourierDeliveryDetailComponent)
  },
  {
    path: 'route-map',
    loadComponent: () => import('./route-map/courier-route-map.component').then((m) => m.CourierRouteMapComponent)
  }
];
