import {
  canApprovePurchaseOrder,
  canCancelPurchaseOrder,
  canSubmitPurchaseOrder,
  previewLineTotal
} from './purchase-order-actions';

describe('purchase order actions', () => {
  it('allows submit only for draft with lines', () => {
    expect(canSubmitPurchaseOrder('Draft', 1)).toBeTrue();
    expect(canSubmitPurchaseOrder('Draft', 0)).toBeFalse();
    expect(canSubmitPurchaseOrder('Submitted', 2)).toBeFalse();
  });

  it('allows approve only when submitted', () => {
    expect(canApprovePurchaseOrder('Submitted')).toBeTrue();
    expect(canApprovePurchaseOrder('Draft')).toBeFalse();
  });

  it('allows cancel for draft submitted approved', () => {
    expect(canCancelPurchaseOrder('Approved')).toBeTrue();
    expect(canCancelPurchaseOrder('Cancelled')).toBeFalse();
  });

  it('previews line total from qty and unit cost', () => {
    expect(previewLineTotal(2, 50, 10)).toBe(90);
  });
});
