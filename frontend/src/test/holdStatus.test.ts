import { describe, expect, it } from 'vitest';
import { effectiveStatus, formatRemaining } from '../holdStatus';

const t0 = Date.parse('2026-11-01T04:00:00Z');
const expiresAt = '2026-11-01T04:10:00Z'; // 10 minutes after t0

describe('effectiveStatus', () => {
  it('keeps HELD while time is left', () => {
    expect(effectiveStatus({ status: 'HELD', expiresAt }, t0)).toBe('HELD');
  });

  it('turns HELD into EXPIRED the moment time is up', () => {
    expect(effectiveStatus({ status: 'HELD', expiresAt }, Date.parse(expiresAt))).toBe('EXPIRED');
  });

  it('never changes CONFIRMED or CANCELLED, even after expiry time', () => {
    const later = Date.parse(expiresAt) + 60_000;
    expect(effectiveStatus({ status: 'CONFIRMED', expiresAt }, later)).toBe('CONFIRMED');
    expect(effectiveStatus({ status: 'CANCELLED', expiresAt }, later)).toBe('CANCELLED');
  });
});

describe('formatRemaining', () => {
  it('shows minutes and seconds', () => {
    expect(formatRemaining(expiresAt, t0)).toBe('10:00');
    expect(formatRemaining(expiresAt, t0 + 65_000)).toBe('8:55');
  });

  it('never goes below 0:00', () => {
    expect(formatRemaining(expiresAt, Date.parse(expiresAt) + 5_000)).toBe('0:00');
  });
});
