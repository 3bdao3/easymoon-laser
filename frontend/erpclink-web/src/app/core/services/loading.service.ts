import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly counter = signal(0);

  readonly isLoading = computed(() => this.counter() > 0);

  show(): void {
    this.counter.update((n) => n + 1);
  }

  hide(): void {
    this.counter.update((n) => (n > 0 ? n - 1 : 0));
  }
}
