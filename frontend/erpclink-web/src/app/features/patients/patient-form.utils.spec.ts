import { FormControl, FormGroup } from '@angular/forms';
import { buildPatientFormValidators, isPatientFormReady } from './patient-form.utils';

describe('patient form utils', () => {
  it('marks incomplete forms as not ready', () => {
    expect(
      isPatientFormReady({
        firstName: '',
        lastName: 'Ali',
        phoneNumber: '010',
        dateOfBirth: '1990-01-01',
        gender: 1
      })
    ).toBeFalse();
  });

  it('accepts complete minimal payload', () => {
    expect(
      isPatientFormReady({
        firstName: 'Omar',
        lastName: 'Ali',
        phoneNumber: '01000000000',
        dateOfBirth: '1990-01-01',
        gender: 1
      })
    ).toBeTrue();
  });

  it('applies required validators on FormGroup', () => {
    const v = buildPatientFormValidators();
    const form = new FormGroup({
      firstName: new FormControl('', { nonNullable: true, validators: v.firstName }),
      lastName: new FormControl('x', { nonNullable: true, validators: v.lastName }),
      phoneNumber: new FormControl('1', { nonNullable: true, validators: v.phoneNumber }),
      dateOfBirth: new FormControl('1990-01-01', { nonNullable: true, validators: v.dateOfBirth }),
      gender: new FormControl(1, { nonNullable: true, validators: v.gender })
    });
    expect(form.valid).toBeFalse();
    form.controls.firstName.setValue('Omar');
    expect(form.valid).toBeTrue();
  });
});
