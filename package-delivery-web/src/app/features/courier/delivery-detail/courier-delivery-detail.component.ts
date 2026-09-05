import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DeliveryAttemptFailureReason, DeliveryAttemptResponse, ShipmentResponse, ShipmentStatus } from '../../../core/models';

/** Forward-only transitions a courier is expected to drive; the backend remains the source of truth for validity. */
const NEXT_STATUS: Partial<Record<ShipmentStatus, ShipmentStatus>> = {
  [ShipmentStatus.Created]: ShipmentStatus.PickedUp,
  [ShipmentStatus.PickedUp]: ShipmentStatus.InTransit,
  [ShipmentStatus.InTransit]: ShipmentStatus.OutForDelivery
};

@Component({
  selector: 'app-courier-delivery-detail',
  imports: [RouterLink, ReactiveFormsModule, StatusBadgeComponent, DatePipe],
  template: `
    <a routerLink="/courier/deliveries" class="text-sm text-accent-600 hover:underline mb-4 inline-block">&larr; Back to deliveries</a>

    @if (loading()) {
      <p class="text-slate-500">Loading…</p>
    } @else if (shipment()) {
      @let s = shipment()!;
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-brand-900">{{ s.trackingNumber }}</h1>
        <app-status-badge [status]="s.status" />
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
        <div class="rounded-lg border border-slate-200 bg-white p-4">
          <p class="text-sm font-semibold text-slate-600 mb-1">Pickup</p>
          <p class="text-sm text-slate-700">{{ s.pickupAddress.street }}, {{ s.pickupAddress.city }}, {{ s.pickupAddress.state }} {{ s.pickupAddress.postalCode }}</p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-4">
          <p class="text-sm font-semibold text-slate-600 mb-1">Delivery</p>
          <p class="text-sm text-slate-700">{{ s.deliveryAddress.street }}, {{ s.deliveryAddress.city }}, {{ s.deliveryAddress.state }} {{ s.deliveryAddress.postalCode }}</p>
        </div>
      </div>

      @if (nextStatus(s.status); as next) {
        <div class="mb-8">
          <button
            type="button"
            (click)="advanceStatus(s, next)"
            [disabled]="updatingStatus()"
            class="rounded-md bg-brand-700 text-white font-medium px-4 py-2 hover:bg-brand-800 disabled:opacity-50"
          >
            {{ updatingStatus() ? 'Updating…' : 'Mark as ' + next }}
          </button>
        </div>
      }

      <div class="rounded-lg border border-slate-200 bg-white p-5 mb-6">
        <h2 class="text-lg font-semibold text-brand-900 mb-4">Log Delivery Attempt</h2>

        <form [formGroup]="attemptForm" (ngSubmit)="submitAttempt(s)" class="space-y-4">
          <div class="flex gap-4">
            <label class="flex items-center gap-2 text-sm">
              <input type="radio" [value]="true" formControlName="successful" /> Successful
            </label>
            <label class="flex items-center gap-2 text-sm">
              <input type="radio" [value]="false" formControlName="successful" /> Failed
            </label>
          </div>

          @if (attemptForm.controls.successful.value === false) {
            <div>
              <label class="block text-sm font-medium text-slate-700 mb-1">Failure reason</label>
              <select formControlName="failureReason" class="w-full rounded-md border border-slate-300 px-3 py-2">
                @for (reason of failureReasons; track reason) {
                  <option [value]="reason">{{ reason }}</option>
                }
              </select>
            </div>
          }

          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Notes</label>
            <textarea formControlName="notes" rows="2" class="w-full rounded-md border border-slate-300 px-3 py-2"></textarea>
          </div>

          <button
            type="submit"
            [disabled]="submittingAttempt()"
            class="rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 disabled:opacity-50"
          >
            {{ submittingAttempt() ? 'Saving…' : 'Log Attempt' }}
          </button>
        </form>
      </div>

      @if (lastAttempt(); as attempt) {
        <div class="rounded-lg border border-slate-200 bg-white p-5">
          <h2 class="text-lg font-semibold text-brand-900 mb-3">Most Recent Attempt</h2>
          <p class="text-sm text-slate-700 mb-1">
            {{ attempt.successful ? 'Successful' : 'Failed (' + (attempt.failureReason ?? 'Other') + ')' }}
            — {{ attempt.attemptedAt | date: 'medium' }}
          </p>
          @if (attempt.notes) {
            <p class="text-sm text-slate-500 mb-3">{{ attempt.notes }}</p>
          }

          @if (attempt.successful) {
            @if (attempt.proofOfDeliveryUrl) {
              <p class="text-sm text-emerald-700">Proof of delivery uploaded.</p>
            } @else {
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Proof of delivery photo</label>
                <input type="file" accept="image/*" capture="environment" (change)="onProofFileSelected($event, s.id, attempt.id)" />
                @if (uploadingProof()) {
                  <p class="text-sm text-slate-500 mt-1">Uploading…</p>
                }
              </div>
            }
          }
        </div>
      }
    } @else {
      <p class="text-red-600">Could not load this shipment.</p>
    }
  `
})
export class CourierDeliveryDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly failureReasons = Object.values(DeliveryAttemptFailureReason);

  protected readonly loading = signal(true);
  protected readonly shipment = signal<ShipmentResponse | null>(null);
  protected readonly updatingStatus = signal(false);
  protected readonly submittingAttempt = signal(false);
  protected readonly uploadingProof = signal(false);
  protected readonly lastAttempt = signal<DeliveryAttemptResponse | null>(null);

  protected readonly attemptForm = this.fb.nonNullable.group({
    successful: [true, Validators.required],
    failureReason: [DeliveryAttemptFailureReason.RecipientAbsent],
    notes: ['']
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.shipmentApi.getById(id).subscribe({
      next: (shipment) => {
        this.shipment.set(shipment);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  nextStatus(current: ShipmentStatus): ShipmentStatus | null {
    return NEXT_STATUS[current] ?? null;
  }

  advanceStatus(shipment: ShipmentResponse, next: ShipmentStatus): void {
    this.updatingStatus.set(true);
    this.shipmentApi.updateStatus(shipment.id, { status: next }).subscribe({
      next: (updated) => {
        this.shipment.set(updated);
        this.updatingStatus.set(false);
        this.toast.success(`Status updated to ${next}.`);
      },
      error: () => {
        this.updatingStatus.set(false);
        this.toast.error('Could not update status.');
      }
    });
  }

  submitAttempt(shipment: ShipmentResponse): void {
    const { successful, failureReason, notes } = this.attemptForm.getRawValue();
    this.submittingAttempt.set(true);

    this.shipmentApi
      .logDeliveryAttempt(shipment.id, {
        successful,
        failureReason: successful ? null : failureReason,
        notes: notes || null
      })
      .subscribe({
        next: (attempt) => {
          this.lastAttempt.set(attempt);
          this.submittingAttempt.set(false);
          this.toast.success('Delivery attempt logged.');
          // Refresh the shipment so its status badge reflects any backend-driven transition (e.g. auto-Delivered).
          this.shipmentApi.getById(shipment.id).subscribe((updated) => this.shipment.set(updated));
        },
        error: () => {
          this.submittingAttempt.set(false);
          this.toast.error('Could not log delivery attempt.');
        }
      });
  }

  onProofFileSelected(event: Event, shipmentId: string, attemptId: string): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingProof.set(true);
    this.shipmentApi.uploadProofOfDelivery(shipmentId, attemptId, file).subscribe({
      next: (attempt) => {
        this.lastAttempt.set(attempt);
        this.uploadingProof.set(false);
        this.toast.success('Proof of delivery uploaded.');
      },
      error: () => {
        this.uploadingProof.set(false);
        this.toast.error('Could not upload proof of delivery.');
      }
    });
  }
}
