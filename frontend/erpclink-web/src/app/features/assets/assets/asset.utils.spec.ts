import {
  canChangeLocation,
  canCompleteMaintenance,
  canEditDetails,
  canRetire,
  canStartMaintenance
} from './asset.utils';

describe('asset.utils', () => {
  it('shows maintenance actions only for valid statuses', () => {
    expect(canStartMaintenance('Active')).toBeTrue();
    expect(canStartMaintenance('UnderMaintenance')).toBeFalse();
    expect(canCompleteMaintenance('UnderMaintenance')).toBeTrue();
    expect(canCompleteMaintenance('Active')).toBeFalse();
  });

  it('allows retire and location rules', () => {
    expect(canRetire('Active')).toBeTrue();
    expect(canRetire('UnderMaintenance')).toBeTrue();
    expect(canRetire('Retired')).toBeFalse();
    expect(canChangeLocation('Retired')).toBeFalse();
    expect(canEditDetails('Retired')).toBeFalse();
  });
});
