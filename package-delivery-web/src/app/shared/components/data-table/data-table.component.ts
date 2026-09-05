import { AfterContentInit, Component, ContentChildren, EventEmitter, Input, Output, QueryList, TemplateRef } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { DataTableCellDirective } from './data-table-cell.directive';

export interface ColumnDef<T> {
  key: string;
  header: string;
  sortable?: boolean;
  accessor?: (row: T) => unknown;
}

export interface SortState {
  key: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-data-table',
  imports: [NgTemplateOutlet],
  template: `
    <div class="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table class="min-w-full divide-y divide-slate-200 text-sm">
        <thead class="bg-slate-50">
          <tr>
            @for (col of columns; track col.key) {
              <th
                class="px-4 py-3 text-left font-semibold text-slate-600 whitespace-nowrap"
                [class.cursor-pointer]="col.sortable"
                (click)="col.sortable && onSort(col.key)"
              >
                {{ col.header }}
                @if (col.sortable && sort?.key === col.key) {
                  <span>{{ sort?.direction === 'asc' ? '▲' : '▼' }}</span>
                }
              </th>
            }
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100">
          @for (row of rows; track trackBy(row)) {
            <tr class="hover:bg-slate-50 cursor-pointer" (click)="rowClick.emit(row)">
              @for (col of columns; track col.key) {
                <td class="px-4 py-3 whitespace-nowrap text-slate-700">
                  @if (cellTemplate(col.key); as tpl) {
                    <ng-container *ngTemplateOutlet="tpl; context: { $implicit: row, row: row }" />
                  } @else {
                    {{ cellValue(row, col) }}
                  }
                </td>
              }
            </tr>
          } @empty {
            <tr>
              <td [attr.colspan]="columns.length" class="px-4 py-8 text-center text-slate-400">{{ emptyMessage }}</td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class DataTableComponent<T> implements AfterContentInit {
  @Input({ required: true }) columns: ColumnDef<T>[] = [];
  @Input({ required: true }) rows: T[] = [];
  @Input() sort: SortState | null = null;
  @Input() emptyMessage = 'No records found.';
  @Input() trackByFn: ((row: T) => unknown) | null = null;

  @Output() rowClick = new EventEmitter<T>();
  @Output() sortChange = new EventEmitter<SortState>();

  /** Optional per-column custom cell templates, e.g. <ng-template appDataTableCell="status" let-row>...</ng-template> */
  @ContentChildren(DataTableCellDirective) private cellTemplates!: QueryList<DataTableCellDirective>;
  private cellTemplateMap = new Map<string, TemplateRef<unknown>>();

  ngAfterContentInit(): void {
    this.syncTemplates();
    this.cellTemplates.changes.subscribe(() => this.syncTemplates());
  }

  private syncTemplates(): void {
    this.cellTemplateMap = new Map(this.cellTemplates.map((t) => [t.columnKey, t.templateRef]));
  }

  cellTemplate(key: string): TemplateRef<unknown> | undefined {
    return this.cellTemplateMap.get(key);
  }

  cellValue(row: T, col: ColumnDef<T>): unknown {
    return col.accessor ? col.accessor(row) : (row as Record<string, unknown>)[col.key];
  }

  trackBy(row: T): unknown {
    return this.trackByFn ? this.trackByFn(row) : row;
  }

  onSort(key: string): void {
    const direction = this.sort?.key === key && this.sort.direction === 'asc' ? 'desc' : 'asc';
    this.sortChange.emit({ key, direction });
  }
}
