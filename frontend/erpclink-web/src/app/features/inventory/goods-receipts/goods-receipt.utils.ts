export function lineRemaining(ordered: number, received: number): number {
  return Math.max(0, ordered - received);
}

export function requiresBatch(trackExpiry: boolean, batchNumber?: string | null, expiryDate?: string | null): boolean {
  if (!trackExpiry) return true;
  return !!batchNumber?.trim() && !!expiryDate?.trim();
}
