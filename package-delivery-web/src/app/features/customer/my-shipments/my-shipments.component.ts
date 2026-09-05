import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { ShipmentResponse, ShipmentStatus } from '../../../core/models';
import { ColumnDef, DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-my-shipments',
  imports: [FormsModule, DatePipe, DecimalPipe, DataTableComponent, DataTableCellDirective, StatusBadgeComponent, PaginationComponent],
  template: `
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold text-brand-900">My Shipments</h1>
      <select [(ngModel)]="statusFilter" (ngModelChange)="onFilterChange()" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
        <option [ngValue]="undefined">All statuses</option>
        @for (status of statuses; track status) {
          <option [ngValue]="status">{{ status }}</option>
        }
      </select>
    </div>

    <app-data-table
      [columns]="columns"
      [rows]="shipments()"
      emptyMessage="No shipments found."
      (rowClick)="openDetail($event)"
    >
      <ng-template appDataTableCell="status" let-row>
        <app-status-badge [status]="row.status" />
      </ng-template>
      <ng-template appDataTableCell="createdAt" let-row>
        {{ row.createdAt | date: 'mediumDate' }}
      </ng-template>
      <ng-template appDataTableCell="cost" let-row>
        {{ row.cost | number: '1.2-2' }} {{ row.currency }}
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
  `
})
export class MyShipmentsComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly router = inject(Router);

  protected readonly statuses = Object.values(ShipmentStatus);
  protected statusFilter: ShipmentStatus | undefined = undefined;

  protected readonly shipments = signal<ShipmentResponse[]>([]);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly hasPreviousPage = signal(false);
  protected readonly hasNextPage = signal(false);

  protected readonly columns: ColumnDef<ShipmentResponse>[] = [
    { key: 'trackingNumber', header: 'Tracking #' },
    { key: 'status', header: 'Status' },
    { key: 'cost', header: 'Cost' },
    { key: 'createdAt', header: 'Created' }
  ];

  ngOnInit(): void {
    this.loadPage(1);
  }

  onFilterChange(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.shipmentApi.getPaged(page, 20, this.statusFilter).subscribe({
      next: (result) => {
        this.shipments.set(result.items);
        this.pageNumber.set(result.pageNumber);
        this.totalPages.set(result.totalPages);
        this.totalCount.set(result.totalCount);
        this.hasPreviousPage.set(result.hasPreviousPage);
        this.hasNextPage.set(result.hasNextPage);
      }
    });
  }

  openDetail(shipment: ShipmentResponse): void {
    this.router.navigate(['/customer/shipments', shipment.id]);
  }
}
