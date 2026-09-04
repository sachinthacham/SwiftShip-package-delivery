import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { ColumnDef, DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ShipmentResponse, ShipmentStatus } from '../../../core/models';

@Component({
  selector: 'app-courier-deliveries',
  imports: [DataTableComponent, DataTableCellDirective, StatusBadgeComponent, PaginationComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-4">Assigned Deliveries</h1>

    <div class="mb-4">
      <label class="text-sm font-medium text-slate-700 mr-2">Status</label>
      <select
        class="rounded-md border border-slate-300 px-3 py-1.5 text-sm"
        [value]="statusFilter() ?? ''"
        (change)="onStatusChange($event)"
      >
        <option value="">All</option>
        @for (status of statuses; track status) {
          <option [value]="status">{{ status }}</option>
        }
      </select>
    </div>

    @if (driverId(); as id) {
      <app-data-table
        [columns]="columns"
        [rows]="rows()"
        emptyMessage="No deliveries assigned yet."
        (rowClick)="openDetail($event)"
      >
        <ng-template appDataTableCell="status" let-row>
          <app-status-badge [status]="row.status" />
        </ng-template>
      </app-data-table>

      <app-pagination
        [pageNumber]="pageNumber()"
        [totalPages]="totalPages()"
        [totalCount]="totalCount()"
        [hasPreviousPage]="hasPreviousPage()"
        [hasNextPage]="hasNextPage()"
        (pageChange)="loadPage($event)"
      />
    } @else if (loadError()) {
      <p class="text-red-600">Could not load your driver profile. Contact a dispatcher if this persists.</p>
    }
  `
})
export class CourierDeliveriesComponent implements OnInit {
  private readonly driverApi = inject(DriverApiService);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly router = inject(Router);

  protected readonly statuses = Object.values(ShipmentStatus);
  protected readonly statusFilter = signal<ShipmentStatus | null>(null);
  protected readonly driverId = signal<string | null>(null);
  protected readonly loadError = signal(false);

  protected readonly rows = signal<ShipmentResponse[]>([]);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly hasPreviousPage = signal(false);
  protected readonly hasNextPage = signal(false);

  protected readonly columns: ColumnDef<ShipmentResponse>[] = [
    { key: 'trackingNumber', header: 'Tracking #' },
    { key: 'status', header: 'Status' },
    { key: 'deliveryCity', header: 'Deliver To', accessor: (row) => `${row.deliveryAddress.street}, ${row.deliveryAddress.city}` },
    { key: 'cost', header: 'Cost', accessor: (row) => `${row.cost.toFixed(2)} ${row.currency}` }
  ];

  ngOnInit(): void {
    this.driverApi.getMe().subscribe({
      next: (driver) => {
        this.driverId.set(driver.id);
        this.loadPage(1);
      },
      error: () => this.loadError.set(true)
    });
  }

  onStatusChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.statusFilter.set(value ? (value as ShipmentStatus) : null);
    this.loadPage(1);
  }

  loadPage(page: number): void {
    const driverId = this.driverId();
    if (!driverId) return;

    this.shipmentApi.getPaged(page, 20, this.statusFilter() ?? undefined, driverId).subscribe({
      next: (result) => {
        this.rows.set(result.items);
        this.pageNumber.set(result.pageNumber);
        this.totalPages.set(result.totalPages);
        this.totalCount.set(result.totalCount);
        this.hasPreviousPage.set(result.hasPreviousPage);
        this.hasNextPage.set(result.hasNextPage);
      },
      error: () => void 0
    });
  }

  openDetail(shipment: ShipmentResponse): void {
    this.router.navigate(['/courier/deliveries', shipment.id]);
  }
}
