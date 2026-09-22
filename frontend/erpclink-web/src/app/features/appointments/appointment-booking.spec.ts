import { canLoadAvailability, canSubmitAppointment } from './appointment-booking';

describe('appointment booking flow', () => {
  it('requires doctor and date before loading availability', () => {
    expect(
      canLoadAvailability({
        patientId: 'p',
        doctorId: null,
        clinicId: 'c',
        date: '2026-09-14',
        startTime: null
      })
    ).toBeFalse();

    expect(
      canLoadAvailability({
        patientId: 'p',
        doctorId: 'd',
        clinicId: 'c',
        date: '2026-09-14',
        startTime: null
      })
    ).toBeTrue();
  });

  it('requires full selection to submit', () => {
    expect(
      canSubmitAppointment({
        patientId: 'p',
        doctorId: 'd',
        clinicId: 'c',
        date: '2026-09-14',
        startTime: null
      })
    ).toBeFalse();

    expect(
      canSubmitAppointment({
        patientId: 'p',
        doctorId: 'd',
        clinicId: 'c',
        date: '2026-09-14',
        startTime: '09:00:00'
      })
    ).toBeTrue();
  });
});
