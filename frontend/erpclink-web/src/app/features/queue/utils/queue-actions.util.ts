import { QueueStatus } from '../models/queue.models';

export interface QueueActionFlags {
  canCall: boolean;
  canStartService: boolean;
  canComplete: boolean;
  canSkip: boolean;
  canCancel: boolean;
}

export function queueActionFlags(status: QueueStatus): QueueActionFlags {
  const terminal = status === 'Completed' || status === 'Skipped' || status === 'Cancelled';
  if (terminal) {
    return {
      canCall: false,
      canStartService: false,
      canComplete: false,
      canSkip: false,
      canCancel: false
    };
  }
  return {
    canCall: status === 'Waiting',
    canStartService: status === 'Called',
    canComplete: status === 'InService',
    canSkip: status === 'Waiting' || status === 'Called',
    canCancel: true
  };
}

export const QUEUE_STATUS_OPTIONS: readonly { value: QueueStatus | ''; label: string }[] = [
  { value: '', label: '— الكل —' },
  { value: 'Waiting', label: 'انتظار' },
  { value: 'Called', label: 'تم النداء' },
  { value: 'InService', label: 'قيد الخدمة' },
  { value: 'Completed', label: 'مكتمل' },
  { value: 'Skipped', label: 'تم التخطي' },
  { value: 'Cancelled', label: 'ملغى' }
];
