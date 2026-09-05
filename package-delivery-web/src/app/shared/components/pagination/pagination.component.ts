import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  template: `
    <div class="flex items-center justify-between py-3 text-sm text-slate-600">
      <span>Page {{ pageNumber }} of {{ totalPages || 1 }} ({{ totalCount }} total)</span>
      <div class="flex gap-2">
        <button
          type="button"
          class="px-3 py-1.5 rounded-md border border-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50"
          [disabled]="!hasPreviousPage"
          (click)="pageChange.emit(pageNumber - 1)"
        >
          Previous
        </button>
        <button
          type="button"
          class="px-3 py-1.5 rounded-md border border-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50"
          [disabled]="!hasNextPage"
          (click)="pageChange.emit(pageNumber + 1)"
        >
          Next
        </button>
      </div>
    </div>
  `
})
export class PaginationComponent {
  @Input() pageNumber = 1;
  @Input() totalPages = 1;
  @Input() totalCount = 0;
  @Input() hasPreviousPage = false;
  @Input() hasNextPage = false;

  @Output() pageChange = new EventEmitter<number>();
}
