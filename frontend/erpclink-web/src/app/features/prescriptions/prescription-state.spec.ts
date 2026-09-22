import {
  canCancelPrescription,
  canIssuePrescription,
  isPrescriptionEditable
} from './prescription-state';

describe('prescription state helpers', () => {
  it('blocks editing issued prescriptions', () => {
    expect(isPrescriptionEditable('Draft')).toBeTrue();
    expect(isPrescriptionEditable('Issued')).toBeFalse();
    expect(isPrescriptionEditable('Cancelled')).toBeFalse();
  });

  it('requires items to issue', () => {
    expect(canIssuePrescription('Draft', 0)).toBeFalse();
    expect(canIssuePrescription('Draft', 1)).toBeTrue();
    expect(canIssuePrescription('Issued', 2)).toBeFalse();
  });

  it('allows cancel for draft and issued', () => {
    expect(canCancelPrescription('Draft')).toBeTrue();
    expect(canCancelPrescription('Issued')).toBeTrue();
    expect(canCancelPrescription('Cancelled')).toBeFalse();
  });
});
