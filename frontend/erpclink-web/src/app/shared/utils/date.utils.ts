const dateOnlyPattern = /^\d{4}-\d{2}-\d{2}$/;
const timeOnlyPattern = /^\d{2}:\d{2}(:\d{2})?$/;

/** Formats ISO date-only (yyyy-MM-dd) for display. */
export function formatDateOnly(value: string | Date | null | undefined, locale = 'ar-EG'): string {
  if (value == null || value === '') {
    return '—';
  }

  const date =
    typeof value === 'string' && dateOnlyPattern.test(value)
      ? parseDateOnly(value)
      : value instanceof Date
        ? value
        : new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(date);
}

/** Formats time-only (HH:mm or HH:mm:ss) for display. */
export function formatTimeOnly(value: string | null | undefined, locale = 'ar-EG'): string {
  if (value == null || value === '') {
    return '—';
  }

  if (timeOnlyPattern.test(value)) {
    const parts = value.split(':').map(Number);
    const date = new Date();
    date.setHours(parts[0] ?? 0, parts[1] ?? 0, parts[2] ?? 0, 0);
    return new Intl.DateTimeFormat(locale, {
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(locale, {
    hour: '2-digit',
    minute: '2-digit'
  }).format(parsed);
}

/** Formats UTC ISO datetime in local timezone. */
export function formatDateTimeUtc(isoUtc: string | null | undefined, locale = 'ar-EG'): string {
  if (isoUtc == null || isoUtc === '') {
    return '—';
  }

  const date = new Date(isoUtc);
  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

function parseDateOnly(value: string): Date {
  const [y, m, d] = value.split('-').map(Number);
  return new Date(y, (m ?? 1) - 1, d ?? 1);
}
