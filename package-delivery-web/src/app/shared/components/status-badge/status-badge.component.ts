import { Component, Input } from '@angular/core';
import { StatusLabelPipe, statusColorClasses } from '../../pipes/status-label.pipe';

@Component({
  selector: 'app-status-badge',
  imports: [StatusLabelPipe],
  template: `
    <span class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium" [class]="colorClasses">
      {{ status | statusLabel }}
    </span>
  `
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: string;

  get colorClasses(): string {
    return statusColorClasses(this.status);
  }
}
