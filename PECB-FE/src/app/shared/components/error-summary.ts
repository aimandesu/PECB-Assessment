import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ApiError } from '../../core/http/api-error';

/**
 * Presents a backend failure in full: every validation message and every domain
 * rule violation the server reported, rather than a single generic sentence.
 */
@Component({
  selector: 'app-error-summary',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (error(); as failure) {
      <div class="alert" role="alert">
        <p class="alert__heading">{{ heading() }}</p>
        <ul class="alert__list">
          @for (message of failure.messages; track $index) {
            <li>{{ message }}</li>
          }
        </ul>
        <p class="alert__code">{{ failure.code }} &middot; HTTP {{ failure.status }}</p>
      </div>
    }
  `,
  styles: `
    .alert {
      border: 1px solid #fecaca;
      background: #fef2f2;
      color: #7f1d1d;
      border-radius: 8px;
      padding: 0.8rem 0.95rem;
      margin-bottom: 1rem;
    }
    .alert__heading { margin: 0 0 0.35rem; font-weight: 600; font-size: 0.9rem; }
    .alert__list { margin: 0; padding-left: 1.1rem; font-size: 0.85rem; line-height: 1.55; }
    .alert__list:empty { display: none; }
    .alert__code { margin: 0.5rem 0 0; font-size: 0.72rem; opacity: 0.7; font-family: var(--mono); }
  `,
})
export class ErrorSummaryComponent {
  readonly error = input.required<ApiError | null>();

  readonly heading = computed(() => {
    const failure = this.error();
    if (!failure) {
      return '';
    }
    if (failure.isRuleViolation) {
      return 'That action is not allowed right now';
    }
    if (failure.status === 400) {
      return 'The server rejected this request';
    }
    if (failure.status === 404) {
      return 'Not found';
    }
    return 'Something went wrong';
  });
}
