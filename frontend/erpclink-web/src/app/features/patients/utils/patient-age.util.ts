/** Age in full years from a `yyyy-MM-dd` (DateOnly) string. Returns null if invalid. */
export function calculateAgeYears(dateOfBirth: string, asOf: Date = new Date()): number | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateOfBirth.trim());
  if (!match) {
    return null;
  }
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (!year || month < 1 || month > 12 || day < 1 || day > 31) {
    return null;
  }

  let age = asOf.getFullYear() - year;
  const monthNow = asOf.getMonth() + 1;
  const dayNow = asOf.getDate();
  if (monthNow < month || (monthNow === month && dayNow < day)) {
    age -= 1;
  }
  return age < 0 ? null : age;
}

export function isMinor(dateOfBirth: string, asOf: Date = new Date()): boolean {
  const age = calculateAgeYears(dateOfBirth, asOf);
  return age !== null && age < 18;
}

export function formatAgeLabel(dateOfBirth: string, asOf: Date = new Date()): string {
  const age = calculateAgeYears(dateOfBirth, asOf);
  if (age === null) {
    return '—';
  }
  return `${age} سنة`;
}
