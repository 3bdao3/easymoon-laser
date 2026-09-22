import { QueueStatus } from './models/queue.models';
import { queueActionFlags } from './utils/queue-actions.util';

export type { QueueStatus };

export function canCallQueue(status: string): boolean {
  return queueActionFlags(status as QueueStatus).canCall;
}

export function canStartQueueService(status: string): boolean {
  return queueActionFlags(status as QueueStatus).canStartService;
}

export function canCompleteQueue(status: string): boolean {
  return queueActionFlags(status as QueueStatus).canComplete;
}

export function canSkipQueue(status: string): boolean {
  return queueActionFlags(status as QueueStatus).canSkip;
}

export function canCancelQueue(status: string): boolean {
  return queueActionFlags(status as QueueStatus).canCancel;
}
