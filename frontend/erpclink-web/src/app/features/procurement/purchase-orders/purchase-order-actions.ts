export function canSubmitPurchaseOrder(status: string, lineCount: number): boolean {
  return status === 'Draft' && lineCount > 0;
}

export function canApprovePurchaseOrder(status: string): boolean {
  return status === 'Submitted';
}

export function canCancelPurchaseOrder(status: string): boolean {
  return status === 'Draft' || status === 'Submitted' || status === 'Approved';
}

export function previewLineTotal(quantity: number, unitCost: number, lineDiscount = 0): number {
  const subtotal = Math.round(quantity * unitCost * 100) / 100;
  return Math.round((subtotal - lineDiscount) * 100) / 100;
}
