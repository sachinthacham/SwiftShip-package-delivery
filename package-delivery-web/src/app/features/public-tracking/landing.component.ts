import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  imports: [RouterLink],
  template: `
    <section class="bg-brand-900 text-white">
      <div class="max-w-6xl mx-auto px-4 py-24 text-center">
        <h1 class="text-4xl md:text-5xl font-extrabold tracking-tight">Delivery, done right.</h1>
        <p class="mt-4 text-lg text-brand-200 max-w-xl mx-auto">
          Create shipments, track them live, and manage your fleet — all from one platform.
        </p>
        <div class="mt-8 flex justify-center gap-3">
          <a routerLink="/track" class="rounded-md bg-accent-500 px-6 py-3 font-medium hover:bg-accent-600 transition-colors">Track a Package</a>
          <a routerLink="/auth/register" class="rounded-md border border-white/30 px-6 py-3 font-medium hover:bg-white/10 transition-colors">Get Started</a>
        </div>
      </div>
    </section>

    <section class="max-w-6xl mx-auto px-4 py-16 grid md:grid-cols-3 gap-8">
      <div class="rounded-lg border border-slate-200 p-6">
        <div class="text-2xl mb-2">📦</div>
        <h3 class="font-semibold text-brand-900 mb-1">Create shipments in seconds</h3>
        <p class="text-sm text-slate-500">Standard, express, or same-day — priced automatically.</p>
      </div>
      <div class="rounded-lg border border-slate-200 p-6">
        <div class="text-2xl mb-2">📍</div>
        <h3 class="font-semibold text-brand-900 mb-1">Live tracking</h3>
        <p class="text-sm text-slate-500">Real-time status updates pushed straight to your dashboard.</p>
      </div>
      <div class="rounded-lg border border-slate-200 p-6">
        <div class="text-2xl mb-2">🚚</div>
        <h3 class="font-semibold text-brand-900 mb-1">Smart dispatch</h3>
        <p class="text-sm text-slate-500">Auto-assign the nearest available courier for every shipment.</p>
      </div>
    </section>
  `
})
export class LandingComponent {}
