/** Digits only for tel / WhatsApp deep links. */
export function phoneDigits(phone: string | null | undefined): string {
  if (!phone) {
    return '';
  }
  return phone.replace(/\D/g, '');
}

/**
 * Normalize Egyptian / international phone to WhatsApp `wa.me` id (country code, no +).
 * Returns empty string when not usable.
 */
export function toWhatsAppNumber(phone: string | null | undefined): string {
  let digits = phoneDigits(phone);
  if (!digits) {
    return '';
  }
  if (digits.startsWith('00')) {
    digits = digits.slice(2);
  }
  if (digits.startsWith('0') && digits.length >= 10) {
    digits = `20${digits.slice(1)}`;
  }
  if (digits.length < 10) {
    return '';
  }
  return digits;
}

export function telHref(phone: string | null | undefined): string | null {
  const digits = phoneDigits(phone);
  return digits ? `tel:${digits}` : null;
}

export function whatsAppHref(phone: string | null | undefined): string | null {
  const id = toWhatsAppNumber(phone);
  return id ? `https://wa.me/${id}` : null;
}

export function patientInitials(fullName: string): string {
  const parts = fullName
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return `${parts[0][0] ?? ''}${parts[parts.length - 1][0] ?? ''}`.toUpperCase();
}
