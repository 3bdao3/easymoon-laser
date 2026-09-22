import { lineRemaining, requiresBatch } from './goods-receipt.utils';

describe('goods-receipt utils', () => {
  it('computes remaining quantity', () => {
    expect(lineRemaining(10, 3)).toBe(7);
    expect(lineRemaining(5, 5)).toBe(0);
  });

  it('validates batch when track expiry', () => {
    expect(requiresBatch(false)).toBeTrue();
    expect(requiresBatch(true, 'B1', '2026-12-01')).toBeTrue();
    expect(requiresBatch(true, '', '2026-12-01')).toBeFalse();
  });
});
