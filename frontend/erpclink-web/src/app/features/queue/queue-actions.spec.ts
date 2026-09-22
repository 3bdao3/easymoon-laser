import {
  canCallQueue,
  canCancelQueue,
  canCompleteQueue,
  canSkipQueue,
  canStartQueueService
} from './queue-actions';

describe('queue action visibility', () => {
  it('allows call only for Waiting', () => {
    expect(canCallQueue('Waiting')).toBeTrue();
    expect(canCallQueue('Called')).toBeFalse();
  });

  it('allows start only for Called', () => {
    expect(canStartQueueService('Called')).toBeTrue();
    expect(canStartQueueService('Waiting')).toBeFalse();
  });

  it('allows complete only for InService', () => {
    expect(canCompleteQueue('InService')).toBeTrue();
    expect(canCompleteQueue('Called')).toBeFalse();
  });

  it('allows skip for Waiting/Called', () => {
    expect(canSkipQueue('Waiting')).toBeTrue();
    expect(canSkipQueue('Called')).toBeTrue();
    expect(canSkipQueue('InService')).toBeFalse();
  });

  it('blocks cancel for terminal states', () => {
    expect(canCancelQueue('Waiting')).toBeTrue();
    expect(canCancelQueue('Completed')).toBeFalse();
    expect(canCancelQueue('Cancelled')).toBeFalse();
  });
});
