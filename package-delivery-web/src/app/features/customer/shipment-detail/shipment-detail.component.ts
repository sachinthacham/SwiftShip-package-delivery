import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { PackageApiService } from '../../../core/services/api/package-api.service';
import { TrackingApiService } from '../../../core/services/api/tracking-api.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { InvoiceResponse, PackageResponse, RatingResponse, ShipmentResponse, TrackingEvent } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { MapMarker, MapViewComponent } from '../../../shared/components/map-view/map-view.component';

@Component({
  selector: 'app-shipment-detail',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, StatusBadgeComponent, MapViewComponent],
  template: `
    @if (shipment(); as shipment) {
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-brand-900">{{ shipment.trackingNumber }}</h1>
          <p class="text-slate-500 text-sm">Created {{ shipment.createdAt | date: 'medium' }}</p>
        </div>
        <app-status-badge [status]="shipment.status" />
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <section class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="font-semibold text-brand-900 mb-3">Tracking timeline</h2>
          @if (liveConnected()) {
            <span class="text-xs text-emerald-600 flex items-center gap-1 mb-3">
              <span class="w-2 h-2 rounded-full bg-emerald-500 inline-block"></span> Live updates
            </span>
          }
          @if (events().length === 0) {
            <p class="text-sm text-slate-400">No tracking events yet.</p>
          } @else {
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
          }
        </section>

        <section class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="font-semibold text-brand-900 mb-3">Route</h2>
          <div class="h-64">
            <app-map-view [markers]="mapMarkers()" />
          </div>
        </section>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <section class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="font-semibold text-brand-900 mb-3">Package</h2>
          @if (pkg(); as pkg) {
            <dl class="text-sm space-y-1.5">
              <div class="flex justify-between"><dt class="text-slate-500">Receiver</dt><dd>{{ pkg.receiverName }} ({{ pkg.receiverPhone }})</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Weight</dt><dd>{{ pkg.weight }} kg</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Dimensions</dt><dd>{{ pkg.length }}×{{ pkg.width }}×{{ pkg.height }} cm</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Declared value</dt><dd>{{ pkg.declaredValue | number: '1.2-2' }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Delivery type</dt><dd>{{ pkg.deliveryType }}</dd></div>
            </dl>
          }
        </section>

        <section class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="font-semibold text-brand-900 mb-3">Invoice</h2>
          @if (invoiceLoading()) {
            <p class="text-sm text-slate-400">Loading…</p>
          } @else if (invoice() === null) {
            <p class="text-sm text-slate-400">No invoice available yet.</p>
          } @else {
            @let inv = invoice()!;
            <dl class="text-sm space-y-1.5 mb-4">
              <div class="flex justify-between"><dt class="text-slate-500">Invoice #</dt><dd>{{ inv.invoiceNumber }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Amount</dt><dd>{{ inv.amount | number: '1.2-2' }} {{ inv.currency }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Status</dt><dd>{{ inv.paymentStatus }}</dd></div>
            </dl>
            @if (inv.paymentStatus === 'Pending' || inv.paymentStatus === 'Failed') {
              <button
                type="button"
                (click)="payNow()"
                [disabled]="payingNow()"
                class="rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 disabled:opacity-50 transition-colors"
              >
                {{ payingNow() ? 'Redirecting…' : 'Pay Now' }}
              </button>
            }
          }
        </section>
      </div>

      @if (shipment.status === 'Delivered') {
        <section class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="font-semibold text-brand-900 mb-3">Your rating</h2>
          @if (rating(); as rating) {
            <div class="text-sm">
              <span class="text-amber-500">{{ '★'.repeat(rating.stars) }}{{ '☆'.repeat(5 - rating.stars) }}</span>
              @if (rating.comment) {
                <p class="text-slate-600 mt-1">{{ rating.comment }}</p>
              }
            </div>
          } @else {
            <form [formGroup]="ratingForm" (ngSubmit)="submitRating()" class="space-y-3">
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Stars (1-5)</label>
                <select formControlName="stars" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
                  @for (n of [1, 2, 3, 4, 5]; track n) {
                    <option [ngValue]="n">{{ n }}</option>
                  }
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Comment (optional)</label>
                <textarea formControlName="comment" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400"></textarea>
              </div>
              <button
                type="submit"
                [disabled]="ratingForm.invalid || submittingRating()"
                class="rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 disabled:opacity-50 transition-colors"
              >
                {{ submittingRating() ? 'Submitting…' : 'Submit Rating' }}
              </button>
            </form>
          }
        </section>
      }
    } @else if (loading()) {
      <p class="text-slate-400 text-sm">Loading shipment…</p>
    } @else {
      <p class="text-red-600 text-sm">Shipment not found.</p>
    }
  `
})
export class ShipmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly packageApi = inject(PackageApiService);
  private readonly trackingApi = inject(TrackingApiService);
  private readonly signalR = inject(SignalRService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(true);
  protected readonly shipment = signal<ShipmentResponse | null>(null);
  protected readonly pkg = signal<PackageResponse | null>(null);
  protected readonly events = signal<TrackingEvent[]>([]);
  protected readonly liveConnected = signal(false);
  protected readonly invoice = signal<InvoiceResponse | null>(null);
  protected readonly invoiceLoading = signal(true);
  protected readonly payingNow = signal(false);
  protected readonly rating = signal<RatingResponse | null>(null);
  protected readonly submittingRating = signal(false);

  protected readonly ratingForm = this.fb.nonNullable.group({
    stars: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['']
  });

  constructor() {
    effect(() => {
      const update = this.signalR.lastUpdate();
      const currentShipment = this.shipment();
      if (update && currentShipment && update.trackingNumber === currentShipment.trackingNumber) {
        this.events.update((events) => [...events, update]);
      }
    });
  }

  protected mapMarkers(): MapMarker[] {
    const shipment = this.shipment();
    if (!shipment) return [];
    const markers: MapMarker[] = [];
    if (shipment.pickupAddress.latitude != null && shipment.pickupAddress.longitude != null) {
      markers.push({ id: 'pickup', lat: shipment.pickupAddress.latitude, lng: shipment.pickupAddress.longitude, label: 'Pickup', iconColor: 'brand' });
    }
    if (shipment.deliveryAddress.latitude != null && shipment.deliveryAddress.longitude != null) {
      markers.push({ id: 'delivery', lat: shipment.deliveryAddress.latitude, lng: shipment.deliveryAddress.longitude, label: 'Delivery', iconColor: 'accent' });
    }
    return markers;
  }

  protected sortedEvents(): TrackingEvent[] {
    return [...this.events()].sort((a, b) => new Date(a.timestampUtc).getTime() - new Date(b.timestampUtc).getTime());
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.shipmentApi.getById(id).subscribe({
      next: (shipment) => {
        this.loading.set(false);
        this.shipment.set(shipment);
        this.loadPackage(shipment.packageId);
        this.loadTracking(shipment.packageId, shipment.trackingNumber);
        this.loadInvoice(id);
        if (shipment.status === 'Delivered') {
          this.loadRating(id);
        }
      },
      error: () => this.loading.set(false)
    });
  }

  private loadPackage(packageId: string): void {
    this.packageApi.getById(packageId).subscribe({ next: (pkg) => this.pkg.set(pkg) });
  }

  private loadTracking(packageId: string, trackingNumber: string): void {
    this.trackingApi.getByPackageId(packageId).subscribe({
      next: (events) => this.events.set(events),
      error: () => void 0
    });

    this.signalR
      .subscribeToTracking(trackingNumber)
      .then(() => this.liveConnected.set(true))
      .catch(() => this.liveConnected.set(false));

    this.destroyRef.onDestroy(() => this.signalR.unsubscribeFromTracking(trackingNumber));
  }

  private loadInvoice(shipmentId: string): void {
    this.shipmentApi.getInvoice(shipmentId).subscribe({
      next: (invoice) => {
        this.invoiceLoading.set(false);
        this.invoice.set(invoice);
      },
      error: () => this.invoiceLoading.set(false)
    });
  }

  private loadRating(shipmentId: string): void {
    this.shipmentApi.getRating(shipmentId).subscribe({
      next: (rating) => this.rating.set(rating),
      error: () => this.rating.set(null)
    });
  }

  payNow(): void {
    const shipment = this.shipment();
    if (!shipment) return;
    this.payingNow.set(true);
    this.shipmentApi
      .createCheckoutSession(shipment.id, { successUrl: window.location.href, cancelUrl: window.location.href })
      .subscribe({
        next: (session) => {
          window.location.href = session.checkoutUrl;
        },
        error: () => {
          this.payingNow.set(false);
          this.toast.error('Online payment is not available right now. Please contact support.');
        }
      });
  }

  submitRating(): void {
    const shipment = this.shipment();
    if (!shipment || this.ratingForm.invalid) return;
    this.submittingRating.set(true);
    this.shipmentApi.addRating(shipment.id, this.ratingForm.getRawValue()).subscribe({
      next: (rating) => {
        this.submittingRating.set(false);
        this.rating.set(rating);
        this.toast.success('Thanks for your feedback!');
      },
      error: () => {
        this.submittingRating.set(false);
        this.toast.error('Could not submit your rating.');
      }
    });
  }
}
