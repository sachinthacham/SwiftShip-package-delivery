import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { IdentityApiService } from '../../../core/services/api/identity-api.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { SavedAddress } from '../../../core/models';

@Component({
  selector: 'app-addresses',
  imports: [ReactiveFormsModule],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-6">Saved Addresses</h1>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <section class="rounded-lg border border-slate-200 bg-white">
        @if (addresses().length === 0) {
          <p class="px-5 py-8 text-center text-slate-400 text-sm">No saved addresses yet.</p>
        } @else {
          <ul class="divide-y divide-slate-100">
            @for (address of addresses(); track address.id) {
              <li class="px-5 py-4 flex items-start justify-between">
                <div>
                  <div class="font-medium text-brand-900 flex items-center gap-2">
                    {{ address.label }}
                    @if (address.isDefault) {
                      <span class="text-xs rounded-full bg-brand-100 text-brand-700 px-2 py-0.5">Default</span>
                    }
                  </div>
                  <div class="text-sm text-slate-500">{{ address.street }}, {{ address.city }}, {{ address.state }} {{ address.postalCode }}, {{ address.country }}</div>
                </div>
                <div class="flex gap-3 text-sm shrink-0 ml-4">
                  @if (!address.isDefault) {
                    <button type="button" class="text-accent-600 hover:underline" (click)="setDefault(address)">Set default</button>
                  }
                  <button type="button" class="text-accent-600 hover:underline" (click)="edit(address)">Edit</button>
                  <button type="button" class="text-red-600 hover:underline" (click)="remove(address)">Delete</button>
                </div>
              </li>
            }
          </ul>
        }
      </section>

      <section class="rounded-lg border border-slate-200 bg-white p-5">
        <h2 class="font-semibold text-brand-900 mb-4">{{ editingId() ? 'Edit address' : 'Add a new address' }}</h2>
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-3">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Label</label>
            <input formControlName="label" placeholder="Home, Work…" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Street</label>
            <input formControlName="street" class="w-full rounded-md border border-slate-300 px-3 py-2 focus:outline-none focus:ring-2 focus:ring-accent-400" />
          </div>
          <div class="grid grid-cols-2 gap-3">
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
          <label class="flex items-center gap-2 text-sm text-slate-700">
            <input type="checkbox" formControlName="isDefault" class="rounded border-slate-300" />
            Set as default address
          </label>

          <div class="flex gap-2 pt-2">
            <button
              type="submit"
              [disabled]="form.invalid || submitting()"
              class="rounded-md bg-accent-500 text-white font-medium px-4 py-2 hover:bg-accent-600 disabled:opacity-50 transition-colors"
            >
              {{ submitting() ? 'Saving…' : editingId() ? 'Save changes' : 'Add address' }}
            </button>
            @if (editingId()) {
              <button type="button" class="rounded-md border border-slate-300 px-4 py-2 hover:bg-slate-50" (click)="cancelEdit()">Cancel</button>
            }
          </div>
        </form>
      </section>
    </div>
  `
})
export class AddressesComponent implements OnInit {
  private readonly identityApi = inject(IdentityApiService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);

  protected readonly addresses = signal<SavedAddress[]>([]);
  protected readonly submitting = signal(false);
  protected readonly editingId = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    label: ['', Validators.required],
    street: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    postalCode: ['', Validators.required],
    country: ['', Validators.required],
    isDefault: [false]
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.identityApi.listAddresses().subscribe({ next: (addresses) => this.addresses.set(addresses) });
  }

  edit(address: SavedAddress): void {
    this.editingId.set(address.id);
    this.form.patchValue(address);
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.form.reset({ isDefault: false });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    const value = this.form.getRawValue();
    const editingId = this.editingId();

    const request$ = editingId
      ? this.identityApi.updateAddress(editingId, value)
      : this.identityApi.createAddress(value);

    request$.subscribe({
      next: () => {
        this.submitting.set(false);
        this.toast.success(editingId ? 'Address updated.' : 'Address added.');
        this.cancelEdit();
        this.load();
      },
      error: () => this.submitting.set(false)
    });
  }

  setDefault(address: SavedAddress): void {
    this.identityApi.setDefaultAddress(address.id).subscribe({
      next: () => {
        this.toast.success('Default address updated.');
        this.load();
      }
    });
  }

  remove(address: SavedAddress): void {
    if (!confirm(`Delete "${address.label}"?`)) return;
    this.identityApi.deleteAddress(address.id).subscribe({
      next: () => {
        this.toast.success('Address deleted.');
        this.load();
      }
    });
  }
}
