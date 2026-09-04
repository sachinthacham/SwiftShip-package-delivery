import { Component, Input } from '@angular/core';

/**
 * A single horizontal-bar row for a magnitude/part-to-whole comparison: label left, thin
 * capped bar (max 24px thick, 4px rounded data-end, square at baseline), value at the tip.
 * Text stays in text tokens; only the bar fill carries color, per dataviz mark specs.
 */
@Component({
  selector: 'app-admin-bar-row',
  template: `
    <div class="flex items-center gap-3 text-sm">
      <div class="w-36 shrink-0 truncate text-slate-600">{{ label }}</div>
      <div class="flex-1 h-4 rounded-sm bg-slate-100 overflow-hidden">
        <div class="h-full rounded-r-[4px]" [style.width.%]="widthPercent" [style.backgroundColor]="color"></div>
      </div>
      <div class="w-14 shrink-0 text-right font-medium text-slate-700 tabular-nums">{{ valueLabel }}</div>
    </div>
  `
})
export class BarRowComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) widthPercent!: number;
  @Input({ required: true }) valueLabel!: string;
  @Input() color = '#4b5e97';
}
