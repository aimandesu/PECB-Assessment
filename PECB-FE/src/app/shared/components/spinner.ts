import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="spinner" role="status" aria-live="polite">
      <span class="spinner__ring" aria-hidden="true"></span>
      <span class="spinner__text">{{ label() }}</span>
    </div>
  `,
  styles: `
    .spinner {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.6rem;
      padding: 3rem 1rem;
      color: var(--muted);
      font-size: 0.9rem;
    }
    .spinner__ring {
      width: 1.1rem;
      height: 1.1rem;
      border: 2px solid var(--border);
      border-top-color: var(--accent);
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    @media (prefers-reduced-motion: reduce) {
      .spinner__ring { animation-duration: 2s; }
    }
  `,
})
export class SpinnerComponent {
  readonly label = input('Loading...');
}
