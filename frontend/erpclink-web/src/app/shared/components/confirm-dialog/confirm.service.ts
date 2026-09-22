import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: 'default' | 'danger';
}

interface ConfirmState extends ConfirmOptions {
  confirmLabel: string;
  cancelLabel: string;
  variant: 'default' | 'danger';
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly state = signal<ConfirmState | null>(null);

  private resolver: ((confirmed: boolean) => void) | null = null;

  confirm(options: ConfirmOptions): Promise<boolean> {
    if (this.resolver) {
      this.resolver(false);
    }

    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
      this.state.set({
        title: options.title,
        message: options.message,
        confirmLabel: options.confirmLabel ?? 'تأكيد',
        cancelLabel: options.cancelLabel ?? 'إلغاء',
        variant: options.variant ?? 'default'
      });
    });
  }

  resolve(confirmed: boolean): void {
    this.resolver?.(confirmed);
    this.resolver = null;
    this.state.set(null);
  }
}
