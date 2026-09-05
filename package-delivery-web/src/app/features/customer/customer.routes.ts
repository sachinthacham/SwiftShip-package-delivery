import { Routes } from '@angular/router';

export const CUSTOMER_ROUTES: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/customer-dashboard.component').then((m) => m.CustomerDashboardComponent)
  },
  {
    path: 'shipments',
    loadComponent: () => import('./my-shipments/my-shipments.component').then((m) => m.MyShipmentsComponent)
  },
  {
    path: 'shipments/:id',
    loadComponent: () => import('./shipment-detail/shipment-detail.component').then((m) => m.ShipmentDetailComponent)
  },
  {
    path: 'create-shipment',
    loadComponent: () => import('./create-shipment/create-shipment.component').then((m) => m.CreateShipmentComponent)
  },
  {
    path: 'addresses',
    loadComponent: () => import('./addresses/addresses.component').then((m) => m.AddressesComponent)
  },
  {
    path: 'invoices',
    loadComponent: () => import('./invoices/invoices.component').then((m) => m.InvoicesComponent)
  }
];
