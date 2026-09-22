import { Validators } from '@angular/forms';

export function buildSupplierFormValidators() {
  return {
    name: [Validators.required, Validators.maxLength(200)],
    email: [Validators.email, Validators.maxLength(256)]
  };
}

export function isSupplierFormReady(values: { name: string }): boolean {
  return values.name.trim().length > 0;
}
