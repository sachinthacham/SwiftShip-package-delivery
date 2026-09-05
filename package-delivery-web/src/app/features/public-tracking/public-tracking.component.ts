import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TrackingApiService } from '../../core/services/api/tracking-api.service';
import { SignalRService } from '../../core/services/signalr.service';
import { TrackingEvent } from '../../core/models';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-public-tracking',
  imports: [FormsModule, DatePipe, StatusBadgeComponent],
  template: `
    <div class="max-w-2xl mx-auto px-4 py-16">
      <h1 class="text-3xl font-bold text-brand-900 mb-2">Track your package</h1>
      <p class="text-slate-500 mb-6">Enter your tracking number — no account needed.</p>

      <form class="flex gap-2 mb-8" (ngSubmit)="search()">
        <input
          [(ngModel)]="trackingNumberInput"
          name="trackingNumber"
          placeholder="e.g. SHIP-2026-000123"
          class="flex-1 rounded-md border border-slate-300 px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-accent-400"
        />
        <button type="submit" class="rounded-md bg-accent-500 text-white font-medium px-5 hover:bg-accent-600 transition-colors" [disabled]="loading()">
          {{ loading() ? 'Searching…' : 'Track' }}
        </button>
      </form>

      @if (notFound()) {
        <p class="text-sm text-red-600">No shipment found for that tracking number.</p>
      }

      @if (events().length > 0) {
        <div class="rounded-lg border border-slate-200 bg-white p-5">
          <div class="flex items-center justify-between mb-4">
            <h2 class="font-semibold text-brand-900">Tracking number: {{ trackingNumberInput }}</h2>
            @if (liveConnected()) {
              <span class="text-xs text-emerald-600 flex items-center gap-1">
                <span class="w-2 h-2 rounded-full bg-emerald-500 inline-block"></span> Live
              </span>
            }
          </div>

          <ol class="relative border-s border-slate-200 ms-2">
            @for (event of sortedEvents(); track event.id) {
              <li class="mb-6 ms-4">
                <div class="absolute w-2.5 h-2.5 bg-accent-500 rounded-full mt-1.5 -start-1.25 border border-white"></div>
                <time class="text-xs text-slate-400">{{ event.timestampUtc | date: 'medium' }}</time>
                <div class="flex items-center gap-2 mt-0.5">
                  <app-status-badge [status]="event.status" />
                  <span class="text-sm text-slate-700">{{ event.location }}</span>
                </div>
              </li>
            }
          </ol>
        </div>
      }
    </div>
  `
})
export class PublicTrackingComponent implements OnInit {
  private readonly trackingApi = inject(TrackingApiService);
  private readonly signalR = inject(SignalRService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected trackingNumberInput = '';
  protected readonly events = signal<TrackingEvent[]>([]);
  protected readonly loading = signal(false);
  protected readonly notFound = signal(false);
  protected readonly liveConnected = signal(false);

  private currentSubscription: string | null = null;

  constructor() {
    effect(() => {
      const update = this.signalR.lastUpdate();
      if (update && update.trackingNumber === this.trackingNumberInput) {
        this.events.update((events) => [...events, update]);
      }
    });
  }

  ngOnInit(): void {
    const routeParam = this.route.snapshot.paramMap.get('trackingNumber');
    if (routeParam) {
      this.trackingNumberInput = routeParam;
      this.search();
    }

    this.destroyRef.onDestroy(() => {
      if (this.currentSubscription) {
        this.signalR.unsubscribeFromTracking(this.currentSubscription);
      }
    });
  }

  protected sortedEvents(): TrackingEvent[] {
    return [...this.events()].sort((a, b) => new Date(a.timestampUtc).getTime() - new Date(b.timestampUtc).getTime());
  }

  search(): void {
    const trackingNumber = this.trackingNumberInput.trim();
    if (!trackingNumber) return;

    this.loading.set(true);
    this.notFound.set(false);
    this.router.navigate(['/track', trackingNumber]);

    this.trackingApi.getPublicByTrackingNumber(trackingNumber).subscribe({
      next: (events) => {
        this.loading.set(false);
        this.events.set(events);
        this.subscribeLive(trackingNumber);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
        this.events.set([]);
      }
    });
  }

  private subscribeLive(trackingNumber: string): void {
    if (this.currentSubscription) {
      this.signalR.unsubscribeFromTracking(this.currentSubscription);
    }
    this.currentSubscription = trackingNumber;
    this.signalR
      .subscribeToTracking(trackingNumber)
      .then(() => this.liveConnected.set(true))
      .catch(() => this.liveConnected.set(false));
  }
}
