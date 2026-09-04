import { ShipmentStatus } from '../../../core/models';

/** Solid fill hexes for bar charts — same hue families as shared/pipes/status-label.pipe.ts's pastel badge colors, at bar-fill saturation. */
export const STATUS_BAR_COLORS: Record<string, string> = {
  [ShipmentStatus.Created]: '#94a3b8',
  [ShipmentStatus.PickedUp]: '#0ea5e9',
  [ShipmentStatus.InTransit]: '#3b82f6',
  [ShipmentStatus.OutForDelivery]: '#6366f1',
  [ShipmentStatus.Delivered]: '#10b981',
  [ShipmentStatus.FailedDelivery]: '#ef4444',
  [ShipmentStatus.Returned]: '#f59e0b',
  [ShipmentStatus.Cancelled]: '#cbd5e1'
};

export function statusBarColor(status: string): string {
  return STATUS_BAR_COLORS[status] ?? '#94a3b8';
}
