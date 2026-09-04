import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { IdentityApiService } from '../../../core/services/api/identity-api.service';
import { UserRole, UserSummary } from '../../../core/models';
import { DataTableComponent, ColumnDef } from '../../../shared/components/data-table/data-table.component';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

const PAGE_SIZE = 20;

/**
 * adminListUsers has no server-side role filter, only pagination — filtering a server-paged
 * response to Customer role client-side would make some pages show fewer than PAGE_SIZE rows
 * (or even appear empty while later pages have customers). Instead this fetches one large batch
 * (up to 200 users — comfortably above what this demo-scale system expects) and paginates the
 * filtered Customer list entirely client-side, so pagination is always consistent.
 */
@Component({
  selector: 'app-customers-list',
  imports: [DataTableComponent, PaginationComponent],
  template: `
    <h1 class="text-2xl font-bold text-brand-900 mb-1">Customers</h1>
    <p class="text-slate-500 mb-4">{{ allCustomers().length }} registered customers.</p>

    @if (loading()) {
      <p class="text-slate-400">Loading…</p>
    } @else {
      <app-data-table [columns]="columns" [rows]="pageRows()" emptyMessage="No customers found." />
      <app-pagination
        [pageNumber]="pageNumber()"
        [totalPages]="totalPages()"
        [totalCount]="allCustomers().length"
        [hasPreviousPage]="pageNumber() > 1"
        [hasNextPage]="pageNumber() < totalPages()"
        (pageChange)="pageNumber.set($event)"
      />
    }
  `
})
export class CustomersListComponent implements OnInit {
  private readonly identityApi = inject(IdentityApiService);

  protected readonly allCustomers = signal<UserSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly pageNumber = signal(1);

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.allCustomers().length / PAGE_SIZE)));
  protected readonly pageRows = computed(() => {
    const start = (this.pageNumber() - 1) * PAGE_SIZE;
    return this.allCustomers().slice(start, start + PAGE_SIZE);
  });

  protected readonly columns: ColumnDef<UserSummary>[] = [
    { key: 'name', header: 'Name', accessor: (r) => `${r.firstName} ${r.lastName}` },
    { key: 'email', header: 'Email' },
    { key: 'phoneNumber', header: 'Phone', accessor: (r) => r.phoneNumber ?? '—' },
    { key: 'createdAt', header: 'Joined', accessor: (r) => new Date(r.createdAt).toLocaleDateString() }
  ];

  ngOnInit(): void {
    this.identityApi.adminListUsers(1, 200).subscribe({
      next: (page) => {
        this.allCustomers.set(page.items.filter((u) => u.role === UserRole.Customer));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
