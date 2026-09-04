import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models';
import { phoneNumberValidator } from '../../../shared/validators/shared-validators';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="max-w-md mx-auto px-4 py-16">
      <h1 class="text-2xl font-bold text-brand-900 mb-1">Create an account</h1>
      <p class="text-slate-500 mb-6">Ship packages or deliver them — pick your role below.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-4">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">First name</label>
            <input formControlName="firstName" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Last name</label>
            <input formControlName="lastName" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Email</label>
          <input type="email" formControlName="email" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Phone number (for SMS updates)</label>
          <input formControlName="phoneNumber" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          @if (form.controls.phoneNumber.touched && form.controls.phoneNumber.invalid) {
            <p class="text-xs text-red-600 mt-1">Enter a valid phone number.</p>
          }
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Password</label>
          <input type="password" formControlName="password" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          @if (form.controls.password.touched && form.controls.password.invalid) {
            <p class="text-xs text-red-600 mt-1">Password must be at least 8 characters.</p>
          }
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">I want to</label>
          <div class="grid grid-cols-2 gap-3">
            <label class="flex items-center gap-2 rounded-md border border-slate-300 px-3 py-2 cursor-pointer has-[:checked]:border-accent-500 has-[:checked]:bg-accent-50">
              <input type="radio" formControlName="role" [value]="UserRole.Customer" />
              Ship packages
            </label>
            <label class="flex items-center gap-2 rounded-md border border-slate-300 px-3 py-2 cursor-pointer has-[:checked]:border-accent-500 has-[:checked]:bg-accent-50">
              <input type="radio" formControlName="role" [value]="UserRole.Courier" />
              Deliver packages
            </label>
          </div>
        </div>

        @if (errorMessage()) {
          <p class="text-sm text-red-600">{{ errorMessage() }}</p>
        }
        @if (successMessage()) {
          <p class="text-sm text-emerald-600">{{ successMessage() }}</p>
        }

        <button
          type="submit"
          [disabled]="form.invalid || submitting()"
          class="w-full rounded-md bg-accent-500 text-white font-medium py-2.5 hover:bg-accent-600 disabled:opacity-50 transition-colors"
        >
          {{ submitting() ? 'Creating account…' : 'Create account' }}
        </button>
      </form>

      <p class="text-sm text-slate-500 mt-4">
        Already have an account? <a routerLink="/auth/login" class="text-accent-600 font-medium hover:underline">Sign in</a>
      </p>
    </div>
  `
})
export class RegisterComponent {
  protected readonly UserRole = UserRole;

  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', phoneNumberValidator],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: [UserRole.Customer, Validators.required]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();
    this.authService
      .register({ ...raw, phoneNumber: raw.phoneNumber || null })
      .subscribe({
        next: () => {
          this.successMessage.set('Account created! Redirecting to sign in…');
          setTimeout(() => this.router.navigate(['/auth/login']), 1200);
        },
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(err?.error?.detail ?? err?.error?.title ?? 'Could not create account. Try a different email.');
        }
      });
  }
}
