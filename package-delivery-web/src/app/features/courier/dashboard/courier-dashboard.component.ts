import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DriverResponse, ShipmentStatus } from '../../../core/models';

const TERMINAL_STATUSES = new Set<ShipmentStatus>([ShipmentStatus.Delivered, ShipmentStatus.Cancelled]);

@Component({
  selector: 'app-courier-dashboard',
  imports: [RouterLink, DecimalPipe],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-6">Courier Dashboard</h1>

    @if (loading()) {
      <p class="text-slate-500">Loading…</p>
    } @else if (driver()) {
      @let d = driver()!;
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <div class="rounded-lg border border-slate-200 bg-white p-5">
          <p class="text-sm text-slate-500 mb-1">Active deliveries</p>
          <p class="text-3xl font-bold text-brand-900">{{ activeCount() }}</p>
        </div>

        <div class="rounded-lg border border-slate-200 bg-white p-5">
          <p class="text-sm text-slate-500 mb-2">Availability</p>
          <button
            type="button"
            (click)="toggleAvailability(d)"
            [disabled]="togglingAvailability()"
            class="w-full rounded-md py-2 font-medium transition-colors disabled:opacity-50"
            [class]="d.isAvailable ? 'bg-emerald-100 text-emerald-700 hover:bg-emerald-200' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
          >
            {{ d.isAvailable ? 'Available' : 'Unavailable' }} — tap to toggle
          </button>
        </div>

        <div class="rounded-lg border border-slate-200 bg-white p-5">
          <p class="text-sm text-slate-500 mb-2">Current location</p>
          @if (d.currentLatitude != null && d.currentLongitude != null) {
            <p class="text-sm text-slate-700 mb-2">{{ d.currentLatitude | number: '1.4-4' }}, {{ d.currentLongitude | number: '1.4-4' }}</p>
          } @else {
            <p class="text-sm text-slate-400 mb-2">Not set</p>
          }
          <button
            type="button"
            (click)="updateLocation()"
            [disabled]="updatingLocation()"
            class="w-full rounded-md border border-slate-300 py-2 font-medium hover:bg-slate-50 disabled:opacity-50"
          >
            {{ updatingLocation() ? 'Updating…' : 'Update my location' }}
          </button>
        </div>
      </div>

      <a routerLink="/courier/deliveries" class="inline-block rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 transition-colors">
        View assigned deliveries
      </a>
    } @else {
      <p class="text-red-600">Could not load your driver profile. Contact a dispatcher if this persists.</p>
    }
  `,
  standalone: true
})
export class CourierDashboardComponent implements OnInit {
  private readonly driverApi = inject(DriverApiService);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(true);
  protected readonly driver = signal<DriverResponse | null>(null);
  protected readonly activeCount = signal(0);
  protected readonly togglingAvailability = signal(false);
  protected readonly updatingLocation = signal(false);

  ngOnInit(): void {
    this.driverApi.getMe().subscribe({
      next: (driver) => {
        this.driver.set(driver);
        this.loading.set(false);
        this.loadActiveCount(driver.id);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadActiveCount(driverId: string): void {
    this.shipmentApi.getPaged(1, 100, undefined, driverId).subscribe({
      next: (page) => {
        const active = page.items.filter((s) => !TERMINAL_STATUSES.has(s.status));
        this.activeCount.set(active.length);
      },
      error: () => void 0
    });
  }

  toggleAvailability(driver: DriverResponse): void {
    this.togglingAvailability.set(true);
    const next = !driver.isAvailable;
    this.driverApi.setAvailability(driver.id, { isAvailable: next }).subscribe({
      next: () => {
        this.driver.set({ ...driver, isAvailable: next });
        this.togglingAvailability.set(false);
        this.toast.success(next ? 'You are now available.' : 'You are now unavailable.');
      },
      error: () => {
        this.togglingAvailability.set(false);
        this.toast.error('Could not update availability.');
      }
    });
  }

  updateLocation(): void {
    if (!navigator.geolocation) {
      this.toast.error('Geolocation is not supported by this browser.');
      return;
    }

    this.updatingLocation.set(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        const { latitude, longitude } = position.coords;
        this.driverApi.updateMyLocation({ latitude, longitude }).subscribe({
          next: () => {
            const current = this.driver();
            if (current) this.driver.set({ ...current, currentLatitude: latitude, currentLongitude: longitude });
            this.updatingLocation.set(false);
            this.toast.success('Location updated.');
          },
          error: () => {
            this.updatingLocation.set(false);
            this.toast.error('Could not update location.');
          }
        });
      },
      () => {
        this.updatingLocation.set(false);
        this.toast.error('Location permission denied or unavailable.');
      }
    );
  }
}
