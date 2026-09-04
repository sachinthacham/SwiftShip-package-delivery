import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { PackageApiService } from '../../../core/services/api/package-api.service';
import { InvoiceResponse, PackageResponse, PaymentStatus, ShipmentResponse, ShipmentStatus } from '../../../core/models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-admin-shipment-detail',
  imports: [RouterLink, DatePipe, StatusBadgeComponent],
  template: `
    <a routerLink="/admin/shipments" class="text-sm text-accent-600 hover:underline">&larr; Back to shipments</a>

    @if (shipment(); as s) {
      <div class="flex items-center justify-between mt-2 mb-4">
        <h1 class="text-2xl font-bold text-brand-900">{{ s.trackingNumber }}</h1>
        <app-status-badge [status]="s.status" />
      </div>

      <div class="grid md:grid-cols-2 gap-6">
        <section class="rounded-lg border border-slate-200 bg-white p-4">
          <h2 class="font-semibold text-slate-700 mb-3">Shipment</h2>
          <dl class="text-sm space-y-1.5">
            <div class="flex justify-between"><dt class="text-slate-500">Pickup</dt><dd>{{ s.pickupAddress.street }}, {{ s.pickupAddress.city }}</dd></div>
            <div class="flex justify-between"><dt class="text-slate-500">Delivery</dt><dd>{{ s.deliveryAddress.street }}, {{ s.deliveryAddress.city }}</dd></div>
            <div class="flex justify-between"><dt class="text-slate-500">Cost</dt><dd>{{ s.cost.toFixed(2) }} {{ s.currency }}</dd></div>
            <div class="flex justify-between"><dt class="text-slate-500">Created</dt><dd>{{ s.createdAt | date }}</dd></div>
          </dl>

          <div class="mt-4 flex items-center gap-2">
            <label class="text-sm text-slate-600">Override status</label>
            <select class="rounded-md border border-slate-300 px-2 py-1 text-sm" [value]="s.status" (change)="onStatusOverride($event)">
              @for (st of statuses; track st) {
                <option [value]="st">{{ st }}</option>
              }
            </select>
          </div>
        </section>

        <section class="rounded-lg border border-slate-200 bg-white p-4">
          <h2 class="font-semibold text-slate-700 mb-3">Package</h2>
          @if (pkg(); as p) {
            <dl class="text-sm space-y-1.5">
              <div class="flex justify-between"><dt class="text-slate-500">Receiver</dt><dd>{{ p.receiverName }} ({{ p.receiverPhone }})</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Weight</dt><dd>{{ p.weight }} kg</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Dimensions</dt><dd>{{ p.length }}×{{ p.width }}×{{ p.height }} cm</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Declared value</dt><dd>{{ p.declaredValue.toFixed(2) }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Delivery type</dt><dd>{{ p.deliveryType }}</dd></div>
            </dl>
          } @else {
            <p class="text-slate-400 text-sm">Loading package…</p>
          }
        </section>

        <section class="rounded-lg border border-slate-200 bg-white p-4 md:col-span-2">
          <h2 class="font-semibold text-slate-700 mb-3">Invoice</h2>
          @if (invoice(); as inv) {
            <dl class="text-sm space-y-1.5 mb-3">
              <div class="flex justify-between"><dt class="text-slate-500">Invoice #</dt><dd>{{ inv.invoiceNumber }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Amount</dt><dd>{{ inv.amount.toFixed(2) }} {{ inv.currency }}</dd></div>
              <div class="flex justify-between"><dt class="text-slate-500">Payment status</dt><dd>{{ inv.paymentStatus }}</dd></div>
            </dl>
            <div class="flex items-center gap-2">
              <label class="text-sm text-slate-600">Override payment status</label>
              <select class="rounded-md border border-slate-300 px-2 py-1 text-sm" [value]="inv.paymentStatus" (change)="onPaymentOverride($event)">
                @for (ps of paymentStatuses; track ps) {
                  <option [value]="ps">{{ ps }}</option>
                }
              </select>
            </div>
          } @else {
            <p class="text-slate-400 text-sm">No invoice yet.</p>
          }
        </section>
      </div>
    } @else if (loading()) {
      <p class="text-slate-400 mt-4">Loading…</p>
    } @else {
      <p class="text-red-600 mt-4">Shipment not found.</p>
    }
  `
})
export class AdminShipmentDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly packageApi = inject(PackageApiService);
  private readonly toast = inject(ToastService);

  protected readonly shipment = signal<ShipmentResponse | null>(null);
  protected readonly pkg = signal<PackageResponse | null>(null);
  protected readonly invoice = signal<InvoiceResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly statuses = Object.values(ShipmentStatus);
  protected readonly paymentStatuses = Object.values(PaymentStatus);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.shipmentApi.getById(id).subscribe({
      next: (s) => {
        this.shipment.set(s);
        this.loading.set(false);
        this.packageApi.getById(s.packageId).subscribe({ next: (p) => this.pkg.set(p) });
        this.shipmentApi.getInvoice(id).subscribe({ next: (inv) => this.invoice.set(inv), error: () => void 0 });
      },
      error: () => this.loading.set(false)
    });
  }

  onStatusOverride(event: Event): void {
    const status = (event.target as HTMLSelectElement).value as ShipmentStatus;
    const s = this.shipment();
    if (!s) return;
    this.shipmentApi.updateStatus(s.id, { status }).subscribe({
      next: (updated) => {
        this.shipment.set(updated);
        this.toast.success(`Status updated to ${status}.`);
      }
    });
  }

  onPaymentOverride(event: Event): void {
    const status = (event.target as HTMLSelectElement).value as PaymentStatus;
    const s = this.shipment();
    if (!s) return;
    this.shipmentApi.updatePaymentStatus(s.id, { status }).subscribe({
      next: (updated) => {
        this.invoice.set(updated);
        this.toast.success(`Payment status updated to ${status}.`);
      }
    });
  }
}
