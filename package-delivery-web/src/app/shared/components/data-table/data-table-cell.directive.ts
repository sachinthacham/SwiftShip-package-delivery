import { Directive, Input, TemplateRef } from '@angular/core';

/**
 * Marks an <ng-template> as the custom cell renderer for a given column key, e.g.:
 * <ng-template appDataTableCell="status" let-row>
 *   <app-status-badge [status]="row.status" />
 * </ng-template>
 */
@Directive({
  selector: '[appDataTableCell]'
})
export class DataTableCellDirective {
  @Input('appDataTableCell') columnKey!: string;

  constructor(public readonly templateRef: TemplateRef<{ $implicit: unknown; row: unknown }>) {}
}
