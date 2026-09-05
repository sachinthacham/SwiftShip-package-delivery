import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastContainerComponent } from '../../shared/components/toast/toast.component';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainerComponent],
  template: `
    <div class="min-h-screen flex bg-slate-50">
      <aside class="w-60 shrink-0 bg-brand-900 text-slate-200 flex flex-col">
        <div class="px-4 py-5 text-lg font-bold text-white">🛠️ Admin</div>
        <nav class="flex-1 px-2 space-y-1 text-sm">
          <a routerLink="/admin/dashboard" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Dashboard</a>
          <a routerLink="/admin/dispatch" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Dispatch Board</a>
          <a routerLink="/admin/shipments" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Shipments</a>
          <a routerLink="/admin/couriers" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Couriers</a>
          <a routerLink="/admin/customers" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Customers</a>
          <a routerLink="/admin/analytics" routerLinkActive="bg-brand-700 text-white" class="block rounded-md px-3 py-2 hover:bg-brand-800">Analytics</a>
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
export class AdminLayoutComponent {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
