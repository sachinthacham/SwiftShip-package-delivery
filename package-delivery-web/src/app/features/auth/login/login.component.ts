import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TokenService } from '../../../core/services/token.service';
import { UserRole } from '../../../core/models';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="max-w-md mx-auto px-4 py-16">
      <h1 class="text-2xl font-bold text-brand-900 mb-1">Sign in</h1>
      <p class="text-slate-500 mb-6">Welcome back. Enter your details to continue.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Email</label>
          <input type="email" formControlName="email" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          @if (form.controls.email.touched && form.controls.email.invalid) {
            <p class="text-xs text-red-600 mt-1">A valid email is required.</p>
          }
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Password</label>
          <input type="password" formControlName="password" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          @if (form.controls.password.touched && form.controls.password.invalid) {
            <p class="text-xs text-red-600 mt-1">Password is required.</p>
          }
        </div>

        @if (errorMessage()) {
          <p class="text-sm text-red-600">{{ errorMessage() }}</p>
        }

        <button
          type="submit"
          [disabled]="form.invalid || submitting()"
          class="w-full rounded-md bg-accent-500 text-white font-medium py-2.5 hover:bg-accent-600 disabled:opacity-50 transition-colors"
        >
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>

      <p class="text-sm text-slate-500 mt-4">
        Don't have an account? <a routerLink="/auth/register" class="text-accent-600 font-medium hover:underline">Create one</a>
      </p>
    </div>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly tokenService = inject(TokenService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.authService.loadCurrentUser().subscribe();
        this.router.navigate([this.landingRouteForRole()]);
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Invalid email or password.');
      }
    });
  }

  private landingRouteForRole(): string {
    const role = this.tokenService.getRoleFromToken();
    switch (role) {
      case UserRole.Courier:
        return '/courier/dashboard';
      case UserRole.Dispatcher:
      case UserRole.Admin:
        return '/admin/dashboard';
      default:
        return '/customer/dashboard';
    }
  }
}
