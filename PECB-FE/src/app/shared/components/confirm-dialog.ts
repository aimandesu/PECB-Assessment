import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Modal confirmation step. The caller keeps ownership of the decision: this
 * component only reports which button was pressed.
 */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (open()) {
      <div class="backdrop" (click)="cancelled.emit()">
        <div
          class="dialog"
          role="alertdialog"
          aria-modal="true"
          [attr.aria-label]="title()"
          (click)="$event.stopPropagation()"
        >
          <h2 class="dialog__title">{{ title() }}</h2>
          <p class="dialog__message">{{ message() }}</p>

          <div class="dialog__actions">
            <button type="button" class="btn btn--ghost" [disabled]="busy()" (click)="cancelled.emit()">
              Cancel
            </button>
            <button type="button" class="btn btn--danger" [disabled]="busy()" (click)="confirmed.emit()">
              {{ busy() ? 'Working...' : confirmLabel() }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    .backdrop {
      position: fixed;
      inset: 0;
      z-index: 60;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1rem;
      background: rgba(15, 23, 42, 0.45);
    }
    .dialog {
      width: min(28rem, 100%);
      background: var(--surface);
      border-radius: 12px;
      border: 1px solid var(--border);
      box-shadow: 0 20px 45px rgba(15, 23, 42, 0.2);
      padding: 1.35rem;
    }
    .dialog__title { margin: 0 0 0.5rem; font-size: 1.05rem; }
    .dialog__message { margin: 0 0 1.25rem; color: var(--muted); font-size: 0.9rem; line-height: 1.5; }
    .dialog__actions { display: flex; justify-content: flex-end; gap: 0.5rem; }
  `,
})
export class ConfirmDialogComponent {
  readonly open = input.required<boolean>();
  readonly title = input('Are you sure?');
  readonly message = input('This action cannot be undone.');
  readonly confirmLabel = input('Confirm');
  readonly busy = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
