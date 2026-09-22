export type AssetStatus = 'Active' | 'UnderMaintenance' | 'Retired';

export function canChangeLocation(status: AssetStatus): boolean {
  return status === 'Active' || status === 'UnderMaintenance';
}

export function canStartMaintenance(status: AssetStatus): boolean {
  return status === 'Active';
}

export function canCompleteMaintenance(status: AssetStatus): boolean {
  return status === 'UnderMaintenance';
}

export function canRetire(status: AssetStatus): boolean {
  return status === 'Active' || status === 'UnderMaintenance';
}

export function canEditDetails(status: AssetStatus): boolean {
  return status !== 'Retired';
}
