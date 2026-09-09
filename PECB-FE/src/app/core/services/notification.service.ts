import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  kind: 'success' | 'error';
  text: string;
}

/** Small transient-message bus shared by the feature pages. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 0;
  private readonly items = signal<Toast[]>([]);

  readonly toasts = this.items.asReadonly();

  success(text: string): void {
    this.push('success', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((toast) => toast.id !== id));
  }

  private push(kind: Toast['kind'], text: string): void {
    const id = this.nextId++;
    this.items.update((list) => [...list, { id, kind, text }]);
    setTimeout(() => this.dismiss(id), kind === 'error' ? 7000 : 4000);
  }
}
