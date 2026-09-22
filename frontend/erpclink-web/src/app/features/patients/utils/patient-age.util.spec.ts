import { calculateAgeYears, formatAgeLabel, isMinor } from './patient-age.util';

describe('patient-age.util', () => {
  const asOf = new Date(2026, 8, 15); // 15 Sep 2026

  it('calculates age from DateOnly string', () => {
    expect(calculateAgeYears('1990-09-15', asOf)).toBe(36);
    expect(calculateAgeYears('1990-09-16', asOf)).toBe(35);
  });

  it('detects minors', () => {
    expect(isMinor('2015-01-01', asOf)).toBeTrue();
    expect(isMinor('2000-01-01', asOf)).toBeFalse();
  });

  it('formats Arabic age label', () => {
    expect(formatAgeLabel('2000-09-15', asOf)).toBe('26 سنة');
    expect(formatAgeLabel('bad', asOf)).toBe('—');
  });
});
