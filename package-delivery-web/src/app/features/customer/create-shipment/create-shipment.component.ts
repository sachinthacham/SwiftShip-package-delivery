import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PackageApiService } from '../../../core/services/api/package-api.service';
import { ShipmentApiService } from '../../../core/services/api/shipment-api.service';
import { IdentityApiService } from '../../../core/services/api/identity-api.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { Address, DeliveryType, SavedAddress } from '../../../core/models';

@Component({
  selector: 'app-create-shipment',
  imports: [ReactiveFormsModule],
  template: `
    <div class="max-w-3xl">
      <h1 class="text-2xl font-bold text-brand-900 mb-1">Create Shipment</h1>
      <p class="text-slate-500 mb-6">Tell us about the package and where it's going.</p>

      @if (!createdPackageId()) {
        <form [formGroup]="packageForm" (ngSubmit)="submitPackage()" class="space-y-6">
          <section class="rounded-lg border border-slate-200 bg-white p-5 space-y-4">
            <h2 class="font-semibold text-brand-900">Package details</h2>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Receiver name</label>
                <input formControlName="receiverName" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Receiver phone</label>
                <input formControlName="receiverPhone" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
            </div>

            <div formGroupName="receiverAddress" class="grid grid-cols-2 gap-4">
              <div class="col-span-2">
                <label class="block text-sm font-medium text-slate-700 mb-1">Street</label>
                <input formControlName="street" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">City</label>
                <input formControlName="city" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">State</label>
                <input formControlName="state" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Postal code</label>
                <input formControlName="postalCode" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Country</label>
                <input formControlName="country" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
            </div>

            <div class="grid grid-cols-4 gap-4">
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Weight (kg)</label>
                <input type="number" step="0.1" formControlName="weight" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Length (cm)</label>
                <input type="number" step="0.1" formControlName="length" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Width (cm)</label>
                <input type="number" step="0.1" formControlName="width" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Height (cm)</label>
                <input type="number" step="0.1" formControlName="height" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Declared value</label>
                <input type="number" step="0.01" formControlName="declaredValue" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Delivery type</label>
                <select formControlName="deliveryType" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400">
                  <option [value]="DeliveryType.Standard">Standard</option>
                  <option [value]="DeliveryType.Express">Express</option>
                  <option [value]="DeliveryType.SameDay">Same day</option>
                </select>
              </div>
            </div>
          </section>

          @if (errorMessage()) {
            <p class="text-sm text-red-600">{{ errorMessage() }}</p>
          }

          <button
            type="submit"
            [disabled]="packageForm.invalid || submitting()"
            class="rounded-md bg-accent-500 text-white font-medium px-6 py-2.5 hover:bg-accent-600 disabled:opacity-50 transition-colors"
          >
            {{ submitting() ? 'Saving…' : 'Continue' }}
          </button>
        </form>
      } @else {
        <form [formGroup]="shipmentForm" (ngSubmit)="submitShipment()" class="space-y-6">
          <section class="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">
            Package created. Now confirm pickup and delivery addresses to schedule the shipment.
          </section>

          <section class="rounded-lg border border-slate-200 bg-white p-5 space-y-4">
            <div class="flex items-center justify-between">
              <h2 class="font-semibold text-brand-900">Pickup address</h2>
              @if (savedAddresses().length > 0) {
                <select (change)="onPickupAddressPicked($event)" class="text-sm rounded-md border border-slate-300 px-2 py-1">
                  <option value="">Choose a saved address…</option>
                  @for (addr of savedAddresses(); track addr.id) {
                    <option [value]="addr.id">{{ addr.label }}</option>
                  }
                </select>
              }
            </div>
            <div formGroupName="pickupAddress" class="grid grid-cols-2 gap-4">
              <div class="col-span-2">
                <label class="block text-sm font-medium text-slate-700 mb-1">Street</label>
                <input formControlName="street" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">City</label>
                <input formControlName="city" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">State</label>
                <input formControlName="state" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Postal code</label>
                <input formControlName="postalCode" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Country</label>
                <input formControlName="country" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
            </div>
          </section>

          <section class="rounded-lg border border-slate-200 bg-white p-5 space-y-4">
            <h2 class="font-semibold text-brand-900">Delivery address</h2>
            <p class="text-xs text-slate-400 -mt-2">Pre-filled from the receiver's address — edit if needed.</p>
            <div formGroupName="deliveryAddress" class="grid grid-cols-2 gap-4">
              <div class="col-span-2">
                <label class="block text-sm font-medium text-slate-700 mb-1">Street</label>
                <input formControlName="street" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">City</label>
                <input formControlName="city" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">State</label>
                <input formControlName="state" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Postal code</label>
                <input formControlName="postalCode" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
              <div>
                <label class="block text-sm font-medium text-slate-700 mb-1">Country</label>
                <input formControlName="country" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
              </div>
            </div>
          </section>

          @if (errorMessage()) {
            <p class="text-sm text-red-600">{{ errorMessage() }}</p>
          }

          <button
            type="submit"
            [disabled]="shipmentForm.invalid || submitting()"
            class="rounded-md bg-accent-500 text-white font-medium px-6 py-2.5 hover:bg-accent-600 disabled:opacity-50 transition-colors"
          >
            {{ submitting() ? 'Scheduling…' : 'Schedule Shipment' }}
          </button>
        </form>
      }
    </div>
  `
})
export class CreateShipmentComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly packageApi = inject(PackageApiService);
  private readonly shipmentApi = inject(ShipmentApiService);
  private readonly identityApi = inject(IdentityApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly DeliveryType = DeliveryType;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly createdPackageId = signal<string | null>(null);
  protected readonly savedAddresses = signal<SavedAddress[]>([]);

  private readonly idempotencyKey = crypto.randomUUID();

  protected readonly packageForm = this.fb.nonNullable.group({
    receiverName: ['', Validators.required],
    receiverPhone: ['', Validators.required],
    receiverAddress: this.fb.nonNullable.group({
      street: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', Validators.required],
      country: ['', Validators.required]
    }),
    weight: [1, [Validators.required, Validators.min(0.01)]],
    length: [10, [Validators.required, Validators.min(0.01)]],
    width: [10, [Validators.required, Validators.min(0.01)]],
    height: [10, [Validators.required, Validators.min(0.01)]],
    declaredValue: [0, [Validators.required, Validators.min(0)]],
    deliveryType: [DeliveryType.Standard, Validators.required]
  });

  protected readonly shipmentForm = this.fb.nonNullable.group({
    pickupAddress: this.fb.nonNullable.group({
      street: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', Validators.required],
      country: ['', Validators.required]
    }),
    deliveryAddress: this.fb.nonNullable.group({
      street: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      postalCode: ['', Validators.required],
      country: ['', Validators.required]
    })
  });

  ngOnInit(): void {
    this.identityApi.listAddresses().subscribe({
      next: (addresses) => this.savedAddresses.set(addresses),
      error: () => void 0
    });
  }

  onPickupAddressPicked(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const address = this.savedAddresses().find((a) => a.id === id);
    if (address) {
      this.shipmentForm.controls.pickupAddress.patchValue(address);
    }
  }

  submitPackage(): void {
    if (this.packageForm.invalid) return;
    this.submitting.set(true);
    this.errorMessage.set(null);

    this.packageApi.create(this.packageForm.getRawValue()).subscribe({
      next: (pkg) => {
        this.submitting.set(false);
        this.createdPackageId.set(pkg.id);
        // Delivery address defaults to the receiver's address; the customer can still edit it before submitting.
        this.shipmentForm.controls.deliveryAddress.patchValue(pkg.receiverAddress as Address);
        const defaultAddress = this.savedAddresses().find((a) => a.isDefault);
        if (defaultAddress) {
          this.shipmentForm.controls.pickupAddress.patchValue(defaultAddress);
        }
      },
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Could not create the package. Please check the details and try again.');
      }
    });
  }

  submitShipment(): void {
    if (this.shipmentForm.invalid || !this.createdPackageId()) return;
    this.submitting.set(true);
    this.errorMessage.set(null);

    const { pickupAddress, deliveryAddress } = this.shipmentForm.getRawValue();
    this.shipmentApi
      .create({ packageId: this.createdPackageId()!, pickupAddress, deliveryAddress }, this.idempotencyKey)
      .subscribe({
        next: (shipment) => {
          this.toast.success('Shipment created successfully.');
          this.router.navigate(['/customer/shipments', shipment.id]);
        },
        error: () => {
          this.submitting.set(false);
          // The package (createdPackageId) already exists server-side; only this half of the form needs retrying.
          this.errorMessage.set('The package was saved, but scheduling the shipment failed. Please review the addresses and try again.');
        }
      });
  }
}
