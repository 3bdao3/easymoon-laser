import { Gender } from '../models/patient.models';
import { createPatientForm } from './patient-form.factory';
import { formToRegisterRequest, formToUpdateRequest } from './patient-payload.util';

describe('patient-payload.util', () => {
  it('sends gender as a number even when the form holds a string from <select>', () => {
    const form = createPatientForm();
    form.patchValue({
      firstName: 'حسام',
      lastName: 'خالد',
      dateOfBirth: '2000-02-01',
      phoneNumber: '+201117643308',
      gender: '1' as unknown as Gender
    });

    const body = formToRegisterRequest(form, false);

    expect(body.gender).toBe(Gender.Male);
    expect(typeof body.gender).toBe('number');
  });

  it('maps update payload gender as number', () => {
    const form = createPatientForm();
    form.patchValue({
      firstName: 'A',
      lastName: 'B',
      dateOfBirth: '1990-01-01',
      phoneNumber: '01000000000',
      gender: '2' as unknown as Gender
    });

    expect(formToUpdateRequest(form).gender).toBe(Gender.Female);
  });
});
