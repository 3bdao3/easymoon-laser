export interface JournalLineAmounts {
  debit: number;
  credit: number;
}

/** UX helper — server remains authoritative for posting rules. */
export function sumJournalTotals(lines: JournalLineAmounts[]): {
  totalDebit: number;
  totalCredit: number;
  difference: number;
  isBalanced: boolean;
} {
  const totalDebit = round2(lines.reduce((s, l) => s + (l.debit ?? 0), 0));
  const totalCredit = round2(lines.reduce((s, l) => s + (l.credit ?? 0), 0));
  const difference = round2(totalDebit - totalCredit);
  return {
    totalDebit,
    totalCredit,
    difference,
    isBalanced: difference === 0 && lines.length >= 2
  };
}

function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}
