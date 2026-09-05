import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { ShipmentResponse } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-customer-dashboard',
  imports: [RouterLink, DatePipe, StatusBadgeComponent],
  template: `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-brand-900 mb-1">Dashboard</h1>
        <p class="text-slate-500">Overview of your shipments and quick actions.</p>
      </div>
      <a
        routerLink="/customer/create-shipment"
        class="rounded-md bg-accent-500 text-white font-medium px-5 py-2.5 hover:bg-accent-600 transition-colors"
      >
        + Create Shipment
      </a>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
      <div class="rounded-lg border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">Total shipments</div>
        <div class="text-2xl font-bold text-brand-900 mt-1">{{ totalCount() }}</div>
      </div>
      <div class="rounded-lg border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">In progress</div>
        <div class="text-2xl font-bold text-brand-900 mt-1">{{ inProgressCount() }}</div>
      </div>
      <div class="rounded-lg border border-slate-200 bg-white p-5">
        <div class="text-sm text-slate-500">Delivered</div>
        <div class="text-2xl font-bold text-brand-900 mt-1">{{ deliveredCount() }}</div>
      </div>
    </div>

    <div class="rounded-lg border border-slate-200 bg-white">
      <div class="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
        <h2 class="font-semibold text-brand-900">Recent shipments</h2>
        <a routerLink="/customer/shipments" class="text-sm text-accent-600 hover:underline">View all</a>
      </div>

      @if (loading()) {
        <p class="px-5 py-8 text-center text-slate-400 text-sm">Loading…</p>
      } @else if (recentShipments().length === 0) {
        <p class="px-5 py-8 text-center text-slate-400 text-sm">No shipments yet. Create your first one to get started.</p>
      } @else {
        <ul class="divide-y divide-slate-100">
          @for (shipment of recentShipments(); track shipment.id) {
            <li>
              <a
                [routerLink]="['/customer/shipments', shipment.id]"
                class="flex items-center justify-between px-5 py-3 hover:bg-slate-50"
              >
                <div>
                  <div class="text-sm font-medium text-brand-900">{{ shipment.trackingNumber }}</div>
                  <div class="text-xs text-slate-400">{{ shipment.createdAt | date: 'medium' }}</div>
                </div>
                <app-status-badge [status]="shipment.status" />
              </a>
            </li>
          }
        </ul>
      }
    </div>
  `
})
export class CustomerDashboardComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);

  protected readonly loading = signal(true);
  protected readonly recentShipments = signal<ShipmentResponse[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly inProgressCount = signal(0);
  protected readonly deliveredCount = signal(0);

  ngOnInit(): void {
    // pageSize=100 keeps the stat tiles accurate for typical customer volumes without a dedicated summary
    // endpoint; the visible list below is still trimmed to the 5 most recent.
    this.shipmentApi.getPaged(1, 100).subscribe({
      next: (page) => {
        this.loading.set(false);
        this.recentShipments.set(page.items.slice(0, 5));
        this.totalCount.set(page.totalCount);
        this.deliveredCount.set(page.items.filter((s) => s.status === 'Delivered').length);
        this.inProgressCount.set(
          page.items.filter((s) => !['Delivered', 'Cancelled', 'Returned'].includes(s.status)).length
        );
      },
      error: () => this.loading.set(false)
    });
  }
}
