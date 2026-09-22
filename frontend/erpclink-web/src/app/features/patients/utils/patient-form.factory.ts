import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Gender, PatientDto } from '../models/patient.models';

export type PatientFormGroup = FormGroup<{
  firstName: FormControl<string>;
  middleName: FormControl<string>;
  lastName: FormControl<string>;
  dateOfBirth: FormControl<string>;
  gender: FormControl<Gender>;
  nationalId: FormControl<string>;
  phoneNumber: FormControl<string>;
  email: FormControl<string>;
  addressLine1: FormControl<string>;
  addressLine2: FormControl<string>;
  city: FormControl<string>;
  emergencyContactName: FormControl<string>;
  emergencyContactPhone: FormControl<string>;
}>;

export function createPatientForm(): PatientFormGroup {
  return new FormGroup({
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    middleName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    dateOfBirth: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    gender: new FormControl(Gender.Unknown, { nonNullable: true }),
    nationalId: new FormControl('', { nonNullable: true }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true }),
    addressLine1: new FormControl('', { nonNullable: true }),
    addressLine2: new FormControl('', { nonNullable: true }),
    city: new FormControl('', { nonNullable: true }),
    emergencyContactName: new FormControl('', { nonNullable: true }),
    emergencyContactPhone: new FormControl('', { nonNullable: true })
  }) as PatientFormGroup;
}

export function patchPatientForm(form: PatientFormGroup, patient: PatientDto): void {
  form.patchValue({
    firstName: patient.firstName,
    middleName: patient.middleName ?? '',
    lastName: patient.lastName,
    dateOfBirth: patient.dateOfBirth,
    gender: patient.gender,
    nationalId: patient.nationalId ?? '',
    phoneNumber: patient.phoneNumber,
    email: patient.email ?? '',
    addressLine1: patient.addressLine1 ?? '',
    addressLine2: patient.addressLine2 ?? '',
    city: patient.city ?? '',
    emergencyContactName: patient.emergencyContactName ?? '',
    emergencyContactPhone: patient.emergencyContactPhone ?? ''
  });
}
