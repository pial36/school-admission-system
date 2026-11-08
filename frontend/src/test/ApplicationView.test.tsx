import { act, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api';
import { ApplicationView } from '../components/ApplicationView';

const STUDENT = '20152691234567890';
const T0 = new Date('2026-11-01T04:00:00Z');

/** Let pending promises (mocked API calls) resolve under fake timers. */
const flush = () => act(async () => { await vi.advanceTimersByTimeAsync(0); });

describe('ApplicationView', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(T0);
    // The server keeps answering HELD — the UI must still switch to EXPIRED by itself.
    vi.spyOn(api, 'getHold').mockResolvedValue({
      holdId: 'h1',
      classId: 'c1',
      className: 'Class 6 - Morning Shift',
      status: 'HELD',
      expiresAt: '2026-11-01T04:00:05Z', // 5 seconds after T0
    });
  });

  it('counts down and shows EXPIRED with the pay button disabled — no reload', async () => {
    render(<ApplicationView holdId="h1" studentId={STUDENT} />);
    await flush();

    expect(screen.getByText('Seat held — fee pending')).toBeInTheDocument();
    expect(screen.getByText('0:05')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pay admission fee' })).toBeEnabled();

    // 6 seconds pass.
    await act(async () => { await vi.advanceTimersByTimeAsync(6000); });

    expect(screen.getByText('Expired')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pay admission fee' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Cancel application' })).toBeDisabled();
  });

  it('shows a success message after paying', async () => {
    const confirm = vi.spyOn(api, 'confirm').mockResolvedValue({ holdId: 'h1', status: 'CONFIRMED' });
    render(<ApplicationView holdId="h1" studentId={STUDENT} />);
    await flush();

    await act(async () => {
      screen.getByRole('button', { name: 'Pay admission fee' }).click();
      await vi.advanceTimersByTimeAsync(0);
    });

    expect(confirm).toHaveBeenCalledWith('h1', STUDENT);
    expect(screen.getByText(/admission confirmed!/i)).toBeInTheDocument();
  });
});
