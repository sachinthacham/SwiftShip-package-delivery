import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { DriverPerformance, ShipmentAnalyticsSummary } from '../../../core/models';
import { StatTileComponent } from '../shared/stat-tile.component';
import { BarRowComponent } from '../shared/bar-row.component';
import { statusBarColor } from '../shared/status-colors';

interface StatusBar {
  status: string;
  count: number;
  widthPercent: number;
  color: string;
}

interface DriverBar {
  driverId: string;
  name: string;
  onTimeRate: number;
  delivered: number;
  totalAssigned: number;
}

@Component({
  selector: 'app-admin-analytics',
  imports: [StatTileComponent, BarRowComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-1">Analytics</h1>
    <p class="text-slate-500 mb-6">Fleet-wide shipment and driver performance.</p>

    @if (loading()) {
      <p class="text-slate-400">Loading…</p>
    } @else {
      @if (summary(); as s) {
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
          <app-admin-stat-tile label="Total shipments" [value]="s.totalShipments" />
          <app-admin-stat-tile label="Delivered today" [value]="s.deliveredToday" />
          <app-admin-stat-tile label="SLA breaches (all time)" [value]="s.slaBreachedTotal" />
          <app-admin-stat-tile label="SLA breaches today" [value]="s.slaBreachedToday" />
        </div>
      }

      <section class="rounded-lg border border-slate-200 bg-white p-4 mb-8">
        <h2 class="font-semibold text-slate-700 mb-4">Shipments by status</h2>
        <div class="space-y-2.5">
          @for (bar of statusBars(); track bar.status) {
            <app-admin-bar-row [label]="bar.status" [widthPercent]="bar.widthPercent" [valueLabel]="bar.count.toString()" [color]="bar.color" />
          } @empty {
            <p class="text-slate-400 text-sm">No shipment data yet.</p>
          }
        </div>
      </section>

      <section class="rounded-lg border border-slate-200 bg-white p-4">
        <h2 class="font-semibold text-slate-700 mb-1">Driver on-time rate</h2>
        <p class="text-xs text-slate-400 mb-4">Share of delivered shipments completed before their SLA threshold.</p>
        <div class="space-y-2.5">
          @for (bar of driverBars(); track bar.driverId) {
            <app-admin-bar-row [label]="bar.name" [widthPercent]="bar.onTimeRate * 100" [valueLabel]="(bar.onTimeRate * 100).toFixed(0) + '%'" color="#4b5e97" />
          } @empty {
            <p class="text-slate-400 text-sm">No driver performance data yet.</p>
          }
        </div>
      </section>
    }
  `
})
export class AnalyticsComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly driverApi = inject(DriverApiService);

  protected readonly summary = signal<ShipmentAnalyticsSummary | null>(null);
  protected readonly statusBars = signal<StatusBar[]>([]);
  protected readonly driverBars = signal<DriverBar[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    forkJoin({
      summary: this.shipmentApi.getAnalyticsSummary(),
      performance: this.shipmentApi.getDriverPerformance(),
      drivers: this.driverApi.getPaged(1, 200)
    }).subscribe({
      next: ({ summary, performance, drivers }) => {
        this.summary.set(summary);
        this.buildStatusBars(summary.countsByStatus);
        this.buildDriverBars(performance, new Map(drivers.items.map((d) => [d.id, d.name])));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private buildStatusBars(countsByStatus: Record<string, number>): void {
    const max = Math.max(1, ...Object.values(countsByStatus));
    this.statusBars.set(
      Object.entries(countsByStatus)
        .filter(([, count]) => count > 0)
        .map(([status, count]) => ({
          status,
          count,
          widthPercent: (count / max) * 100,
          color: statusBarColor(status)
        }))
    );
  }

  private buildDriverBars(performance: DriverPerformance[], nameById: Map<string, string>): void {
    this.driverBars.set(
      performance
        .map((p) => ({
          driverId: p.driverId,
          name: nameById.get(p.driverId) ?? p.driverId,
          onTimeRate: p.onTimeRate,
          delivered: p.delivered,
          totalAssigned: p.totalAssigned
        }))
        .sort((a, b) => b.onTimeRate - a.onTimeRate)
    );
  }
}
