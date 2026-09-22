import { trimToNull } from '../../../shared/utils/form.util';
import {
  Gender,
  RegisterPatientRequest,
  UpdatePatientRequest
} from '../models/patient.models';
import { PatientFormGroup } from './patient-form.factory';

function toGender(value: Gender | string | number): Gender {
  const n = typeof value === 'number' ? value : Number(value);
  return (Number.isFinite(n) ? n : Gender.Unknown) as Gender;
}

export function formToUpdateRequest(form: PatientFormGroup): UpdatePatientRequest {
  const v = form.getRawValue();
  return {
    firstName: v.firstName.trim(),
    middleName: trimToNull(v.middleName),
    lastName: v.lastName.trim(),
    dateOfBirth: v.dateOfBirth,
    gender: toGender(v.gender),
    nationalId: trimToNull(v.nationalId),
    phoneNumber: v.phoneNumber.trim(),
    email: trimToNull(v.email),
    addressLine1: trimToNull(v.addressLine1),
    addressLine2: trimToNull(v.addressLine2),
    city: trimToNull(v.city),
    emergencyContactName: trimToNull(v.emergencyContactName),
    emergencyContactPhone: trimToNull(v.emergencyContactPhone)
  };
}

export function formToRegisterRequest(
  form: PatientFormGroup,
  allowPossibleDuplicate: boolean
): RegisterPatientRequest {
  return {
    ...formToUpdateRequest(form),
    allowPossibleDuplicate
  };
}
