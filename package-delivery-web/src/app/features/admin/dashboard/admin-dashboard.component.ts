import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { ShipmentAnalyticsSummary } from '../../../core/models';
import { StatTileComponent } from '../shared/stat-tile.component';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, StatTileComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-1">Admin Dashboard</h1>
    <p class="text-slate-500 mb-6">Operational snapshot across all shipments.</p>

    @if (summary(); as s) {
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <app-admin-stat-tile label="Total shipments" [value]="s.totalShipments" />
        <app-admin-stat-tile label="Delivered today" [value]="s.deliveredToday" />
        <app-admin-stat-tile label="Failed attempts today" [value]="s.failedAttemptsToday" />
        <app-admin-stat-tile label="SLA breaches today" [value]="s.slaBreachedToday" />
      </div>
    } @else if (loading()) {
      <p class="text-slate-400 mb-6">Loading summary…</p>
    } @else {
      <p class="text-slate-400 mb-6">Summary unavailable.</p>
    }

    <div class="flex gap-3">
      <a routerLink="/admin/dispatch" class="rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 transition-colors">Dispatch Board</a>
      <a routerLink="/admin/analytics" class="rounded-md border border-slate-300 px-4 py-2 hover:bg-slate-50 transition-colors">Full Analytics</a>
    </div>
  `
})
export class AdminDashboardComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);

  protected readonly summary = signal<ShipmentAnalyticsSummary | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.shipmentApi.getAnalyticsSummary().subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }
}
