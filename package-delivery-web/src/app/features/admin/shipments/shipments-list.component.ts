import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { PaginatedList, ShipmentResponse, ShipmentStatus } from '../../../core/models';
import { DataTableComponent, ColumnDef } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-admin-shipments-list',
  imports: [DataTableComponent, DataTableCellDirective, StatusBadgeComponent, PaginationComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-1">Shipments</h1>
    <p class="text-slate-500 mb-4">All shipments across every customer and driver.</p>

    <div class="mb-4 flex items-center gap-2">
      <label class="text-sm text-slate-600">Status</label>
      <select class="rounded-md border border-slate-300 px-2 py-1 text-sm" [value]="statusFilter() ?? ''" (change)="onStatusChange($event)">
        <option value="">All</option>
        @for (s of statuses; track s) {
          <option [value]="s">{{ s }}</option>
        }
      </select>
    </div>

    @if (loading()) {
      <p class="text-slate-400">Loading…</p>
    } @else {
      <app-data-table [columns]="columns" [rows]="page()?.items ?? []" emptyMessage="No shipments found." (rowClick)="openDetail($event)">
        <ng-template appDataTableCell="status" let-row>
          <app-status-badge [status]="row.status" />
        </ng-template>
      </app-data-table>

      @if (page(); as p) {
        <app-pagination
          [pageNumber]="p.pageNumber"
          [totalPages]="p.totalPages"
          [totalCount]="p.totalCount"
          [hasPreviousPage]="p.hasPreviousPage"
          [hasNextPage]="p.hasNextPage"
          (pageChange)="loadPage($event)"
        />
      }
    }
  `
})
export class AdminShipmentsListComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly router = inject(Router);

  protected readonly page = signal<PaginatedList<ShipmentResponse> | null>(null);
  protected readonly loading = signal(true);
  protected readonly statusFilter = signal<ShipmentStatus | null>(null);
  protected readonly statuses = Object.values(ShipmentStatus);

  protected readonly columns: ColumnDef<ShipmentResponse>[] = [
    { key: 'trackingNumber', header: 'Tracking #' },
    { key: 'status', header: 'Status' },
    { key: 'pickupCity', header: 'Pickup', accessor: (r) => r.pickupAddress?.city ?? '—' },
    { key: 'deliveryCity', header: 'Delivery', accessor: (r) => r.deliveryAddress?.city ?? '—' },
    { key: 'cost', header: 'Cost', accessor: (r) => `${r.cost.toFixed(2)} ${r.currency}` },
    { key: 'createdAt', header: 'Created', accessor: (r) => new Date(r.createdAt).toLocaleDateString() }
  ];

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(pageNumber: number): void {
    this.loading.set(true);
    this.shipmentApi.getPaged(pageNumber, 20, this.statusFilter() ?? undefined).subscribe({
      next: (p) => {
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onStatusChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.statusFilter.set((value as ShipmentStatus) || null);
    this.loadPage(1);
  }

  openDetail(row: ShipmentResponse): void {
    this.router.navigate(['/admin/shipments', row.id]);
  }
}
