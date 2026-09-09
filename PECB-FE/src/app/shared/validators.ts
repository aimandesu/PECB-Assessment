import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Rejects values made up entirely of whitespace.
 *
 * The backend uses FluentValidation's `NotEmpty()`, which treats `"   "` as empty,
 * so `Validators.required` alone would let a value through that the server refuses.
 */
export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (typeof value !== 'string' || value.length === 0) {
    return null;
  }
  return value.trim().length === 0 ? { whitespace: true } : null;
};
