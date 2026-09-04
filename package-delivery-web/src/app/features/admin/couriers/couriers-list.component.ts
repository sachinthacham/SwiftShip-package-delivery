import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DriverApiService } from '../../../core/services/api/driver-api.service';
import { IdentityApiService } from '../../../core/services/api/identity-api.service';
import { DriverResponse, PaginatedList, UserRole, VehicleType } from '../../../core/models';
import { DataTableComponent, ColumnDef } from '../../../shared/components/data-table/data-table.component';
import { DataTableCellDirective } from '../../../shared/components/data-table/data-table-cell.directive';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-couriers-list',
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, DataTableComponent, DataTableCellDirective, PaginationComponent],
  template: `
    <div class="flex items-center justify-between mb-1">
      <h1 class="text-2xl font-bold text-brand-900">Couriers</h1>
      <button type="button" class="rounded-md bg-accent-500 text-white text-sm font-medium px-3 py-1.5 hover:bg-accent-600" (click)="showForm.set(!showForm())">
        {{ showForm() ? 'Cancel' : '+ Add Courier' }}
      </button>
    </div>
    <p class="text-slate-500 mb-4">
      Driver performance (on-time rate, delivery counts) lives on the
      <a routerLink="/admin/analytics" class="text-accent-600 hover:underline">Analytics</a> page to avoid duplicating it here.
    </p>

    @if (showForm()) {
      <form [formGroup]="form" (ngSubmit)="submit()" class="rounded-lg border border-slate-200 bg-white p-4 mb-6 grid md:grid-cols-3 gap-3">
        <div class="md:col-span-3 font-semibold text-slate-700">
          {{ createdUserId() ? 'Step 2: create driver profile' : 'Step 1: create login account' }}
        </div>

        @if (!createdUserId()) {
          <input formControlName="email" placeholder="Email" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <input formControlName="password" type="password" placeholder="Temporary password" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <input formControlName="phoneNumber" placeholder="Phone (optional)" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <input formControlName="firstName" placeholder="First name" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <input formControlName="lastName" placeholder="Last name" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
        } @else {
          <input formControlName="vehicleNumber" placeholder="Vehicle number" class="rounded-md border border-slate-300 px-3 py-2 text-sm" />
          <select formControlName="vehicleType" class="rounded-md border border-slate-300 px-3 py-2 text-sm">
            @for (v of vehicleTypes; track v) {
              <option [value]="v">{{ v }}</option>
            }
          </select>
        }

        <div class="md:col-span-3">
          <button type="submit" [disabled]="submitting()" class="rounded-md bg-brand-700 text-white text-sm font-medium px-4 py-2 hover:bg-brand-800 disabled:opacity-50">
            {{ createdUserId() ? 'Create driver profile' : 'Create login & continue' }}
          </button>
          @if (createdUserId()) {
            <span class="text-xs text-slate-500 ml-3">Login account already created — retry only creates the driver profile.</span>
          }
        </div>
      </form>
    }

    @if (loading()) {
      <p class="text-slate-400">Loading…</p>
    } @else {
      <app-data-table [columns]="columns" [rows]="page()?.items ?? []" emptyMessage="No couriers yet.">
        <ng-template appDataTableCell="isAvailable" let-row>
          <span [class]="row.isAvailable ? 'text-emerald-600' : 'text-slate-400'">{{ row.isAvailable ? 'Available' : 'Unavailable' }}</span>
        </ng-template>
        <ng-template appDataTableCell="location" let-row>
          {{ row.currentLatitude != null ? (row.currentLatitude | number: '1.3-3') + ', ' + (row.currentLongitude | number: '1.3-3') : 'Unknown' }}
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
export class CouriersListComponent implements OnInit {
  private readonly driverApi = inject(DriverApiService);
  private readonly identityApi = inject(IdentityApiService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  protected readonly page = signal<PaginatedList<DriverResponse> | null>(null);
  protected readonly loading = signal(true);
  protected readonly showForm = signal(false);
  protected readonly submitting = signal(false);
  protected readonly createdUserId = signal<string | null>(null);
  protected readonly vehicleTypes = Object.values(VehicleType);

  protected readonly columns: ColumnDef<DriverResponse>[] = [
    { key: 'name', header: 'Name' },
    { key: 'vehicleType', header: 'Vehicle' },
    { key: 'vehicleNumber', header: 'Vehicle #' },
    { key: 'isAvailable', header: 'Availability' },
    { key: 'location', header: 'Current Location' }
  ];

  protected readonly form = this.fb.nonNullable.group({
    email: [''],
    password: [''],
    firstName: [''],
    lastName: [''],
    phoneNumber: [''],
    vehicleNumber: [''],
    vehicleType: [VehicleType.Car]
  });

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(pageNumber: number): void {
    this.loading.set(true);
    this.driverApi.getPaged(pageNumber, 20).subscribe({
      next: (p) => {
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    this.submitting.set(true);

    if (!this.createdUserId()) {
      const { email, password, firstName, lastName, phoneNumber } = this.form.getRawValue();
      this.identityApi
        .adminCreateUser({ email, password, firstName, lastName, role: UserRole.Courier, phoneNumber: phoneNumber || null })
        .subscribe({
          next: (user) => {
            this.createdUserId.set(user.id);
            this.submitting.set(false);
            this.toast.success('Login account created. Now add the vehicle/driver profile.');
          },
          error: () => this.submitting.set(false)
        });
      return;
    }

    const { vehicleNumber, vehicleType } = this.form.getRawValue();
    this.driverApi.create({ userId: this.createdUserId()!, name: `${this.form.value.firstName} ${this.form.value.lastName}`.trim(), vehicleNumber, vehicleType }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.showForm.set(false);
        this.createdUserId.set(null);
        this.form.reset({ vehicleType: VehicleType.Car });
        this.toast.success('Courier created.');
        this.loadPage(this.page()?.pageNumber ?? 1);
      },
      error: () => {
        this.submitting.set(false);
        this.toast.error('Driver profile creation failed — the login account was already created; retry to finish setup.');
      }
    });
  }
}
