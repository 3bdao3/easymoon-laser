import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'info' | 'success' | 'warning' | 'error';

export interface ToastMessage {
  id: number;
  message: string;
  variant: ToastVariant;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  readonly toasts = signal<ToastMessage[]>([]);

  show(message: string, variant: ToastVariant = 'info', durationMs = 5000): void {
    const id = ++this.seq;
    const toast: ToastMessage = { id, message, variant };
    this.toasts.update((list) => [...list, toast]);

    window.setTimeout(() => this.dismiss(id), durationMs);
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error', 7000);
  }

  warning(message: string): void {
    this.show(message, 'warning');
  }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
