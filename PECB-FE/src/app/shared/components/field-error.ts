import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl } from '@angular/forms';
import { startWith, switchMap } from 'rxjs/operators';

/**
 * Renders the first validation message for a control, once the user has had a
 * chance to interact with it. Messages produced by the server for this field are
 * shown with the same treatment as client-side ones.
 */
@Component({
  selector: 'app-field-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message(); as text) {
      <p class="field-error" role="alert">{{ text }}</p>
    }
  `,
  styles: `
    .field-error {
      margin: 0.3rem 0 0;
      font-size: 0.8rem;
      color: var(--danger);
    }
  `,
})
export class FieldErrorComponent {
  readonly control = input.required<AbstractControl>();
  readonly label = input('This field');
  /** Messages the backend attributed to this control. */
  readonly serverErrors = input<readonly string[]>([]);

  /**
   * `AbstractControl` is not reactive on its own, so the control's own event stream
   * is what drives re-evaluation of `message()`.
   */
  private readonly controlEvents = toSignal(
    toObservable(this.control).pipe(switchMap((control) => control.events.pipe(startWith(null)))),
    { initialValue: null },
  );

  readonly message = computed<string | null>(() => {
    this.controlEvents();

    const fromServer = this.serverErrors();
    if (fromServer.length) {
      return fromServer[0];
    }

    const control = this.control();
    if (!control.touched && !control.dirty) {
      return null;
    }

    const errors = control.errors;
    if (!errors) {
      return null;
    }

    const label = this.label();
    if (errors['required']) {
      return `${label} is required.`;
    }
    if (errors['email']) {
      return 'Enter a valid email address.';
    }
    if (errors['maxlength']) {
      const { requiredLength, actualLength } = errors['maxlength'];
      return `${label} must be ${requiredLength} characters or fewer (currently ${actualLength}).`;
    }
    if (errors['whitespace']) {
      return `${label} cannot be only whitespace.`;
    }
    return `${label} is not valid.`;
  });
}
