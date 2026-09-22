import { FormControl, FormGroup, Validators } from '@angular/forms';
import { trimToNull } from '../../../shared/utils/form.util';
import {
  ClinicDto,
  CreateClinicRequest,
  UpdateClinicRequest
} from '../models/clinic.models';

export type ClinicFormGroup = FormGroup<{
  code: FormControl<string>;
  name: FormControl<string>;
  description: FormControl<string>;
  location: FormControl<string>;
}>;

export function createClinicForm(): ClinicFormGroup {
  return new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    location: new FormControl('', { nonNullable: true })
  });
}

export function createClinicEditForm(): FormGroup<{
  name: FormControl<string>;
  description: FormControl<string>;
  location: FormControl<string>;
}> {
  return new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    location: new FormControl('', { nonNullable: true })
  });
}

export function patchClinicEditForm(
  form: FormGroup<{ name: FormControl<string>; description: FormControl<string>; location: FormControl<string> }>,
  clinic: ClinicDto
): void {
  form.patchValue({
    name: clinic.name,
    description: clinic.description ?? '',
    location: clinic.location ?? ''
  });
}

export function formToCreateRequest(form: ClinicFormGroup): CreateClinicRequest {
  const v = form.getRawValue();
  return {
    code: v.code.trim(),
    name: v.name.trim(),
    description: trimToNull(v.description),
    location: trimToNull(v.location)
  };
}

export function formToUpdateRequest(
  form: FormGroup<{ name: FormControl<string>; description: FormControl<string>; location: FormControl<string> }>
): UpdateClinicRequest {
  const v = form.getRawValue();
  return {
    name: v.name.trim(),
    description: trimToNull(v.description),
    location: trimToNull(v.location)
  };
}
