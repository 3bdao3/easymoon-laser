export function trimToNull(value: string | null | undefined): string | null {
  if (value == null) {
    return null;
  }
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

export function optionalTrim(value: string | null | undefined): string | undefined {
  const n = trimToNull(value ?? '');
  return n ?? undefined;
}
