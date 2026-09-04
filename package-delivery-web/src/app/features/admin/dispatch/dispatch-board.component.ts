import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { DriverResponse, ShipmentResponse, ShipmentStatus } from '../../../core/models';
import { DataTableComponent, ColumnDef } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-dispatch-board',
  imports: [DataTableComponent, DataTableCellDirective, StatusBadgeComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-1">Dispatch Board</h1>
    <p class="text-slate-500 mb-6">Shipments awaiting pickup or already picked up, with driver assignment.</p>

    @if (loading()) {
      <p class="text-slate-400">Loading…</p>
    } @else {
      <app-data-table [columns]="columns" [rows]="shipments()" emptyMessage="No shipments need dispatching right now.">
        <ng-template appDataTableCell="status" let-row>
          <app-status-badge [status]="row.status" />
        </ng-template>
        <ng-template appDataTableCell="driver" let-row>
          {{ driverName(row.driverId) }}
        </ng-template>
        <ng-template appDataTableCell="actions" let-row>
          <div class="flex items-center gap-2">
            <select
              class="rounded-md border border-slate-300 px-2 py-1 text-xs"
              [disabled]="assigning() === row.id"
              (change)="assignManually(row, $event)"
            >
              <option value="">Assign to…</option>
              @for (d of availableDrivers(); track d.id) {
                <option [value]="d.id">{{ d.name }}</option>
              }
            </select>
            <button
              type="button"
              class="rounded-md bg-accent-500 text-white text-xs font-medium px-2 py-1 hover:bg-accent-600 disabled:opacity-50"
              [disabled]="assigning() === row.id"
              (click)="autoAssign(row)"
            >
              {{ assigning() === row.id ? 'Assigning…' : 'Auto-assign nearest' }}
            </button>
          </div>
        </ng-template>
      </app-data-table>
    }
  `
})
export class DispatchBoardComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly driverApi = inject(DriverApiService);
  private readonly toast = inject(ToastService);

  protected readonly shipments = signal<ShipmentResponse[]>([]);
  protected readonly availableDrivers = signal<DriverResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly assigning = signal<string | null>(null);

  protected readonly columns: ColumnDef<ShipmentResponse>[] = [
    { key: 'trackingNumber', header: 'Tracking #' },
    { key: 'status', header: 'Status' },
    { key: 'pickupCity', header: 'Pickup City', accessor: (r) => r.pickupAddress?.city ?? '—' },
    { key: 'driver', header: 'Driver' },
    { key: 'actions', header: 'Actions' }
  ];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    // getPaged only accepts a single status filter, so fetch the two dispatch-relevant statuses separately and merge.
    forkJoin({
      created: this.shipmentApi.getPaged(1, 100, ShipmentStatus.Created),
      pickedUp: this.shipmentApi.getPaged(1, 100, ShipmentStatus.PickedUp),
      drivers: this.driverApi.getPaged(1, 100, true)
    }).subscribe({
      next: ({ created, pickedUp, drivers }) => {
        this.shipments.set([...created.items, ...pickedUp.items]);
        this.availableDrivers.set(drivers.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected driverName(driverId: string | null | undefined): string {
    if (!driverId) return 'Unassigned';
    return this.availableDrivers().find((d) => d.id === driverId)?.name ?? driverId;
  }

  protected assignManually(row: ShipmentResponse, event: Event): void {
    const driverId = (event.target as HTMLSelectElement).value;
    if (!driverId) return;
    this.assigning.set(row.id);
    this.shipmentApi.assignDriver(row.id, { driverId }).subscribe({
      next: (updated) => {
        this.replaceRow(updated);
        this.assigning.set(null);
        this.toast.success(`Assigned ${this.driverName(driverId)} to ${row.trackingNumber}.`);
      },
      error: () => this.assigning.set(null)
    });
  }

  protected autoAssign(row: ShipmentResponse): void {
    this.assigning.set(row.id);
    this.shipmentApi.autoAssignDriver(row.id).subscribe({
      next: (updated) => {
        this.replaceRow(updated);
        this.assigning.set(null);
        this.toast.success(`Auto-assigned nearest driver to ${row.trackingNumber}.`);
      },
      error: () => {
        this.assigning.set(null);
        this.toast.error(`No available driver found within range for ${row.trackingNumber}.`);
      }
    });
  }

  private replaceRow(updated: ShipmentResponse): void {
    this.shipments.update((rows) => rows.map((r) => (r.id === updated.id ? updated : r)));
  }
}
