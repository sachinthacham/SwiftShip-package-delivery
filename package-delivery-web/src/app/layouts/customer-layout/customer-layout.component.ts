import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastContainerComponent } from '../../shared/components/toast/toast.component';

@Component({
  selector: 'app-customer-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainerComponent],
  template: `
    <div class="min-h-screen flex bg-slate-50">
      <aside class="w-60 shrink-0 bg-brand-900 text-slate-200 flex flex-col">
        <div class="px-4 py-5 text-lg font-bold text-white">📦 Customer</div>
        <nav class="flex-1 px-2 space-y-1 text-sm">
          <a routerLink="/customer/dashboard" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Dashboard</a>
          <a routerLink="/customer/shipments" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">My Shipments</a>
          <a routerLink="/customer/create-shipment" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Create Shipment</a>
          <a routerLink="/customer/addresses" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Saved Addresses</a>
          <a routerLink="/customer/invoices" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Invoices</a>
        </nav>
        <div class="px-4 py-4 border-t border-brand-800 text-sm">
          <div class="truncate mb-2">{{ authService.currentUser()?.email }}</div>
          <button type="button" class="text-accent-400 hover:text-accent-300" (click)="logout()">Sign out</button>
        </div>
      </aside>
      <main class="flex-1 p-6">
        <router-outlet />
      </main>
    </div>
    <app-toast-container />
  `
})
export class CustomerLayoutComponent {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
