import { sumJournalTotals } from './journal-balance.util';

describe('sumJournalTotals', () => {
  it('balances when debits equal credits with two lines', () => {
    const result = sumJournalTotals([
      { debit: 100, credit: 0 },
      { debit: 0, credit: 100 }
    ]);
    expect(result.isBalanced).toBeTrue();
    expect(result.difference).toBe(0);
  });

  it('detects unbalanced totals', () => {
    const result = sumJournalTotals([
      { debit: 100, credit: 0 },
      { debit: 0, credit: 50 }
    ]);
    expect(result.isBalanced).toBeFalse();
    expect(result.difference).toBe(50);
  });

  it('requires at least two lines', () => {
    const result = sumJournalTotals([{ debit: 10, credit: 0 }]);
    expect(result.isBalanced).toBeFalse();
  });
});
