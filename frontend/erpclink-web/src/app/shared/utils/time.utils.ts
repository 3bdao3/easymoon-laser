/** Normalizes HTML time input (HH:mm) to ASP.NET TimeOnly JSON (HH:mm:ss). */
export function toTimeOnlyPayload(value: string): string {
  const trimmed = value.trim();
  if (/^\d{2}:\d{2}:\d{2}$/.test(trimmed)) {
    return trimmed;
  }
  if (/^\d{2}:\d{2}$/.test(trimmed)) {
    return `${trimmed}:00`;
  }
  return trimmed;
}

/** Maps API time-only to HTML time input value (HH:mm). */
export function toHtmlTimeValue(value: string | null | undefined): string {
  if (!value) {
    return '';
  }
  const parts = value.split(':');
  if (parts.length >= 2) {
    return `${parts[0]}:${parts[1]}`;
  }
  return value;
}

export const DAY_OF_WEEK_LABELS: readonly string[] = [
  'الأحد',
  'الإثنين',
  'الثلاثاء',
  'الأربعاء',
  'الخميس',
  'الجمعة',
  'السبت'
];
