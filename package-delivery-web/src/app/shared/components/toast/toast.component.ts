import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-container',
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 w-80 max-w-[90vw]">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="rounded-lg shadow-lg px-4 py-3 text-sm text-white flex items-start justify-between gap-3 animate-[fadeIn_0.15s_ease-out]"
          [class.bg-emerald-600]="toast.kind === 'success'"
          [class.bg-red-600]="toast.kind === 'error'"
          [class.bg-brand-700]="toast.kind === 'info'"
          [class.bg-amber-600]="toast.kind === 'warning'"
        >
          <span>{{ toast.message }}</span>
          <button type="button" class="opacity-80 hover:opacity-100" (click)="toastService.dismiss(toast.id)">✕</button>
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  protected readonly toastService = inject(ToastService);
}
