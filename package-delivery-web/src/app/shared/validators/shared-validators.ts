import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function matchFieldValidator(controlName: string, matchingControlName: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const control = group.get(controlName);
    const matchingControl = group.get(matchingControlName);
    if (!control || !matchingControl) return null;

    if (matchingControl.errors && !matchingControl.errors['mismatch']) {
      return null;
    }

    if (control.value !== matchingControl.value) {
      matchingControl.setErrors({ ...matchingControl.errors, mismatch: true });
      return { mismatch: true };
    }

    if (matchingControl.errors) {
      const { mismatch, ...rest } = matchingControl.errors;
      matchingControl.setErrors(Object.keys(rest).length ? rest : null);
    }
    return null;
  };
}

export function phoneNumberValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const valid = /^\+?[0-9\s\-()]{7,20}$/.test(control.value);
  return valid ? null : { phoneNumber: true };
}
