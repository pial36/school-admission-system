import type { Hold, HoldStatus } from './api';

/**
 * Status as of `now`. A HELD hold whose time is up is shown as EXPIRED right
 * away — the UI does not wait for the next server poll or a page reload.
 */
export function effectiveStatus(hold: Pick<Hold, 'status' | 'expiresAt'>, now: number): HoldStatus {
  if (hold.status === 'HELD' && new Date(hold.expiresAt).getTime() <= now) return 'EXPIRED';
  return hold.status;
}

/** "9:05" style remaining time; "0:00" when over. */
export function formatRemaining(expiresAt: string, now: number): string {
  const totalSeconds = Math.max(0, Math.ceil((new Date(expiresAt).getTime() - now) / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export const STATUS_LABEL: Record<HoldStatus, string> = {
  HELD: 'Seat held — fee pending',
  CONFIRMED: 'Admission confirmed',
  EXPIRED: 'Expired',
  CANCELLED: 'Cancelled',
};
