import { ValidatorFn, Validators } from '@angular/forms';

/** Shared patient form validators aligned with backend required fields (structure only). */
export function buildPatientFormValidators(): {
  firstName: ValidatorFn[];
  lastName: ValidatorFn[];
  phoneNumber: ValidatorFn[];
  dateOfBirth: ValidatorFn[];
  gender: ValidatorFn[];
} {
  return {
    firstName: [Validators.required, Validators.maxLength(100)],
    lastName: [Validators.required, Validators.maxLength(100)],
    phoneNumber: [Validators.required, Validators.maxLength(32)],
    dateOfBirth: [Validators.required],
    gender: [Validators.required]
  };
}

export function isPatientFormReady(value: {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  dateOfBirth: string;
  gender: number | null;
}): boolean {
  return (
    !!value.firstName?.trim() &&
    !!value.lastName?.trim() &&
    !!value.phoneNumber?.trim() &&
    !!value.dateOfBirth &&
    value.gender !== null &&
    value.gender !== undefined
  );
}
