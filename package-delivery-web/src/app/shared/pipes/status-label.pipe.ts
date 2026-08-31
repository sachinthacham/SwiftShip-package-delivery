import { Pipe, PipeTransform } from '@angular/core';

/** Splits PascalCase enum values into readable labels: "OutForDelivery" -> "Out For Delivery". */
@Pipe({ name: 'statusLabel' })
export class StatusLabelPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    return value.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}

const STATUS_COLORS: Record<string, string> = {
  Created: 'bg-slate-100 text-slate-700',
  PickedUp: 'bg-sky-100 text-sky-700',
  InTransit: 'bg-blue-100 text-blue-700',
  OutForDelivery: 'bg-indigo-100 text-indigo-700',
  Delivered: 'bg-emerald-100 text-emerald-700',
  FailedDelivery: 'bg-red-100 text-red-700',
  Returned: 'bg-amber-100 text-amber-700',
  Cancelled: 'bg-slate-200 text-slate-500',
  Pending: 'bg-amber-100 text-amber-700',
  Paid: 'bg-emerald-100 text-emerald-700',
  Failed: 'bg-red-100 text-red-700',
  Refunded: 'bg-slate-200 text-slate-500'
};

export function statusColorClasses(status: string | null | undefined): string {
  if (!status) return 'bg-slate-100 text-slate-700';
  return STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-700';
}
