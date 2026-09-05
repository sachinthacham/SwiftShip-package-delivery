import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { InvoiceResponse, ShipmentResponse } from '../../../core/models';
import { ColumnDef, DataTableComponent } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

interface InvoiceRow {
  shipment: ShipmentResponse;
  invoice: InvoiceResponse | null;
}

@Component({
  selector: 'app-invoices',
  imports: [DatePipe, DecimalPipe, DataTableComponent, DataTableCellDirective, PaginationComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-6">Invoices</h1>

    <app-data-table [columns]="columns" [rows]="rows()" emptyMessage="No invoices found." (rowClick)="openDetail($event)">
      <ng-template appDataTableCell="createdAt" let-row>
        {{ row.shipment.createdAt | date: 'mediumDate' }}
      </ng-template>
      <ng-template appDataTableCell="amount" let-row>
        @if (row.invoice) {
          {{ row.invoice.amount | number: '1.2-2' }} {{ row.invoice.currency }}
        } @else {
          <span class="text-slate-400">—</span>
        }
      </ng-template>
      <ng-template appDataTableCell="paymentStatus" let-row>
        @if (row.invoice) {
          <span
            class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
            [class.bg-emerald-100]="row.invoice.paymentStatus === 'Paid'"
            [class.text-emerald-700]="row.invoice.paymentStatus === 'Paid'"
            [class.bg-amber-100]="row.invoice.paymentStatus === 'Pending'"
            [class.text-amber-700]="row.invoice.paymentStatus === 'Pending'"
            [class.bg-red-100]="row.invoice.paymentStatus === 'Failed'"
            [class.text-red-700]="row.invoice.paymentStatus === 'Failed'"
          >
            {{ row.invoice.paymentStatus }}
          </span>
        } @else {
          <span class="text-slate-400">Not issued</span>
        }
      </ng-template>
      <ng-template appDataTableCell="actions" let-row>
        <button type="button" class="text-accent-600 hover:underline text-sm" (click)="openDetail(row)">View</button>
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
export class InvoicesComponent implements OnInit {
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly router = inject(Router);

  // Reuses the paged shipments list rather than issuing one getInvoice() call per shipment: the invoice
  // status column is fetched lazily per visible page only, keeping this to at most 20 extra calls/page
  // instead of an unbounded N+1 across the customer's whole history.
  protected readonly rows = signal<InvoiceRow[]>([]);
  protected readonly pageNumber = signal(1);
  protected readonly totalPages = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly hasPreviousPage = signal(false);
  protected readonly hasNextPage = signal(false);

  protected readonly columns: ColumnDef<InvoiceRow>[] = [
    { key: 'trackingNumber', header: 'Shipment', accessor: (r) => r.shipment.trackingNumber },
    { key: 'createdAt', header: 'Date' },
    { key: 'amount', header: 'Amount' },
    { key: 'paymentStatus', header: 'Status' },
    { key: 'actions', header: '' }
  ];

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.shipmentApi.getPaged(page, 20).subscribe({
      next: (result) => {
        this.pageNumber.set(result.pageNumber);
        this.totalPages.set(result.totalPages);
        this.totalCount.set(result.totalCount);
        this.hasPreviousPage.set(result.hasPreviousPage);
        this.hasNextPage.set(result.hasNextPage);

        const rows: InvoiceRow[] = result.items.map((shipment) => ({ shipment, invoice: null }));
        this.rows.set(rows);

        rows.forEach((row, index) => {
          this.shipmentApi.getInvoice(row.shipment.id).subscribe({
            next: (invoice) => this.rows.update((current) => current.map((r, i) => (i === index ? { ...r, invoice } : r))),
            error: () => void 0
          });
        });
      }
    });
  }

  openDetail(row: InvoiceRow): void {
    this.router.navigate(['/customer/shipments', row.shipment.id]);
  }
}
