import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      <p class="empty__title">{{ title() }}</p>
      @if (hint()) {
        <p class="empty__hint">{{ hint() }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: `
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.45rem;
      padding: 3.5rem 1.5rem;
      text-align: center;
    }
    .empty__title { margin: 0; font-weight: 600; color: var(--text); }
    .empty__hint { margin: 0; color: var(--muted); font-size: 0.875rem; max-width: 34rem; }
  `,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly hint = input<string>('');
}
