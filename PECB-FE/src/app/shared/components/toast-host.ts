import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite" aria-atomic="false">
      @for (toast of notifications.toasts(); track toast.id) {
        <div class="toast" [class.toast--error]="toast.kind === 'error'">
          <span>{{ toast.text }}</span>
          <button type="button" class="toast__close" aria-label="Dismiss" (click)="notifications.dismiss(toast.id)">
            &times;
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed;
      right: 1rem;
      bottom: 1rem;
      z-index: 80;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      max-width: min(24rem, calc(100vw - 2rem));
    }
    .toast {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 0.7rem 0.9rem;
      border-radius: 8px;
      border: 1px solid #a7f3d0;
      background: #ecfdf5;
      color: #065f46;
      font-size: 0.86rem;
      box-shadow: 0 10px 24px rgba(15, 23, 42, 0.12);
    }
    .toast--error { border-color: #fecaca; background: #fef2f2; color: #7f1d1d; }
    .toast__close {
      border: 0;
      background: none;
      color: inherit;
      font-size: 1.1rem;
      line-height: 1;
      cursor: pointer;
      padding: 0;
    }
  `,
})
export class ToastHostComponent {
  protected readonly notifications = inject(NotificationService);
}
