import { Component, Input } from '@angular/core';

/** Figure contract: label (sentence case) + value (auto-compact). No delta/trend — backend has no historical comparison endpoint yet. */
@Component({
  selector: 'app-admin-stat-tile',
  template: `
    <div class="rounded-lg border border-slate-200 bg-white px-4 py-3">
      <div class="text-sm text-slate-500">{{ label }}</div>
      <div class="text-3xl font-semibold text-brand-900 mt-1">{{ value }}</div>
    </div>
  `
})
export class StatTileComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
}
