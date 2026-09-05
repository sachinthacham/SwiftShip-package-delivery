import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastContainerComponent } from '../../shared/components/toast/toast.component';

@Component({
  selector: 'app-public-layout',
  imports: [RouterOutlet, RouterLink, ToastContainerComponent],
  template: `
    <div class="min-h-screen flex flex-col bg-slate-50">
      <header class="bg-brand-900 text-white">
        <div class="max-w-6xl mx-auto px-4 py-4 flex items-center justify-between">
          <a routerLink="/" class="text-lg font-bold tracking-tight">📦 Package Delivery</a>
          <nav class="flex items-center gap-4 text-sm">
            <a routerLink="/track" class="hover:text-accent-400">Track a Package</a>
            @if (authService.isAuthenticated()) {
              <a routerLink="/customer" class="hover:text-accent-400">My Dashboard</a>
            } @else {
              <a routerLink="/auth/login" class="hover:text-accent-400">Sign In</a>
              <a
                routerLink="/auth/register"
                class="rounded-md bg-accent-500 px-3 py-1.5 font-medium hover:bg-accent-600 transition-colors"
              >
                Get Started
              </a>
            }
          </nav>
        </div>
      </header>

      <main class="flex-1">
        <router-outlet />
      </main>

      <footer class="bg-brand-950 text-slate-400 text-sm">
        <div class="max-w-6xl mx-auto px-4 py-6">© {{ year }} Package Delivery System</div>
      </footer>
    </div>
    <app-toast-container />
  `
})
export class PublicLayoutComponent {
  protected readonly authService = inject(AuthService);
  protected readonly year = new Date().getFullYear();
}
