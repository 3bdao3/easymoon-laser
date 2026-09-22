import { FormControl, FormGroup, Validators } from '@angular/forms';
import { DoctorDto } from '../models/doctor.models';
import { trimToNull } from '../../../shared/utils/form.util';
import { RegisterDoctorRequest, UpdateDoctorRequest } from '../models/doctor.models';

export type DoctorFormGroup = FormGroup<{
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  displayName: FormControl<string>;
  specialtyId: FormControl<string>;
  licenseNumber: FormControl<string>;
  phoneNumber: FormControl<string>;
  email: FormControl<string>;
  userId: FormControl<string>;
}>;

export function createDoctorForm(): DoctorFormGroup {
  return new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    displayName: new FormControl('', { nonNullable: true }),
    specialtyId: new FormControl('', { nonNullable: true }),
    licenseNumber: new FormControl('', { nonNullable: true }),
    phoneNumber: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true }),
    userId: new FormControl('', { nonNullable: true })
  });
}

export function patchDoctorForm(form: DoctorFormGroup, doctor: DoctorDto): void {
  form.patchValue({
    firstName: doctor.firstName,
    lastName: doctor.lastName,
    displayName: doctor.displayName ?? '',
    specialtyId: doctor.specialtyId ?? '',
    licenseNumber: doctor.licenseNumber ?? '',
    phoneNumber: doctor.phoneNumber ?? '',
    email: doctor.email ?? '',
    userId: doctor.userId ?? ''
  });
}

function formPayload(form: DoctorFormGroup): RegisterDoctorRequest {
  const v = form.getRawValue();
  return {
    firstName: v.firstName.trim(),
    lastName: v.lastName.trim(),
    displayName: trimToNull(v.displayName),
    specialtyId: trimToNull(v.specialtyId),
    licenseNumber: trimToNull(v.licenseNumber),
    phoneNumber: trimToNull(v.phoneNumber),
    email: trimToNull(v.email),
    userId: trimToNull(v.userId)
  };
}

export function formToRegisterRequest(form: DoctorFormGroup): RegisterDoctorRequest {
  return formPayload(form);
}

export function formToUpdateRequest(form: DoctorFormGroup): UpdateDoctorRequest {
  return formPayload(form);
}
