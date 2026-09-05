import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { MapMarker, MapViewComponent } from '../../../shared/components/map-view/map-view.component';
import { ShipmentResponse, ShipmentStatus } from '../../../core/models';

const TERMINAL_STATUSES = new Set<ShipmentStatus>([ShipmentStatus.Delivered, ShipmentStatus.Cancelled]);

@Component({
  selector: 'app-courier-route-map',
  imports: [MapViewComponent],
  template: `
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-bold text-brand-900">Route Map</h1>
      @if (skippedCount() > 0) {
        <p class="text-sm text-amber-600">{{ skippedCount() }} stop(s) skipped — no coordinates on file.</p>
      }
    </div>

    @if (loading()) {
      <p class="text-slate-500">Loading…</p>
    } @else if (markers().length === 0) {
      <p class="text-slate-500">No active deliveries with mapped addresses right now.</p>
    } @else {
      <div class="h-[32rem]">
        <app-map-view [markers]="markers()" [routePoints]="routePoints()" (markerClick)="openDetail($event.id)" />
      </div>
    }
  `
})
export class CourierRouteMapComponent implements OnInit {
  private readonly driverApi = inject(DriverApiService);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly markers = signal<MapMarker[]>([]);
  protected readonly routePoints = signal<Array<{ lat: number; lng: number }>>([]);
  protected readonly skippedCount = signal(0);

  ngOnInit(): void {
    this.driverApi.getMe().subscribe({
      next: (driver) => this.loadShipments(driver.id),
      error: () => this.loading.set(false)
    });
  }

  private loadShipments(driverId: string): void {
    this.shipmentApi.getPaged(1, 100, undefined, driverId).subscribe({
      next: (page) => {
        const active = page.items
          .filter((s) => !TERMINAL_STATUSES.has(s.status))
          .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

        const mapped = active.filter(
          (s): s is ShipmentResponse & { deliveryAddress: { latitude: number; longitude: number } } =>
            s.deliveryAddress.latitude != null && s.deliveryAddress.longitude != null
        );

        this.skippedCount.set(active.length - mapped.length);

        this.markers.set(
          mapped.map((s) => ({
            id: s.id,
            lat: s.deliveryAddress.latitude,
            lng: s.deliveryAddress.longitude,
            label: `${s.trackingNumber} — ${s.deliveryAddress.street}, ${s.deliveryAddress.city}`,
            iconColor: 'brand' as const
          }))
        );
        this.routePoints.set(mapped.map((s) => ({ lat: s.deliveryAddress.latitude, lng: s.deliveryAddress.longitude })));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  openDetail(shipmentId: string): void {
    this.router.navigate(['/courier/deliveries', shipmentId]);
  }
}
