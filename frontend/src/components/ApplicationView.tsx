import { useCallback, useState } from 'react';
import { api } from '../api';
import { effectiveStatus, formatRemaining } from '../holdStatus';
import { isValidStudentId, useNow, usePolling } from '../hooks';
import { ErrorBox, StatusBadge, SuccessBox } from './common';

/**
 * Screen: Application — status, time left, "Pay admission fee" and "Cancel".
 * The countdown runs every second; when it reaches 0 the status flips to
 * EXPIRED and the pay button is disabled immediately, without a reload.
 */
export function ApplicationView({ holdId, studentId }: { holdId: string; studentId: string }) {
  const loader = useCallback(() => api.getHold(holdId), [holdId]);
  const { data: hold, error, loading, reload } = usePolling(loader, 3000, holdId);
  const now = useNow();
  const [busy, setBusy] = useState<'pay' | 'cancel' | null>(null);
  const [actionError, setActionError] = useState<Error | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  if (loading && !hold) return <section className="card"><p className="muted">Loading application…</p></section>;
  if (!hold) {
    return (
      <section className="card">
        <ErrorBox error={error} />
        <a href="#/my">← Back to my applications</a>
      </section>
    );
  }

  const status = effectiveStatus(hold, now);
  const isHeld = status === 'HELD';
  const studentOk = isValidStudentId(studentId);

  async function run(kind: 'pay' | 'cancel') {
    setActionError(null);
    setSuccess(null);
    if (kind === 'cancel' && !window.confirm('Cancel this application? The seat will be released.')) return;
    setBusy(kind);
    try {
      if (kind === 'pay') {
        await api.confirm(holdId, studentId);
        setSuccess('Payment received — admission confirmed! 🎉');
      } else {
        await api.cancel(holdId, studentId);
        setSuccess('Application cancelled. The seat has been released.');
      }
    } catch (err) {
      setActionError(err as Error);
    } finally {
      setBusy(null);
      reload();
    }
  }

  return (
    <section className="card">
      <a href="#/my" className="small">← My applications</a>
      <h2>{hold.className ?? 'Application'}</h2>

      <dl className="details">
        <dt>Status</dt>
        <dd><StatusBadge status={status} /></dd>
        {isHeld && (
          <>
            <dt>Time left to pay</dt>
            <dd className="countdown" aria-live="polite">{formatRemaining(hold.expiresAt, now)}</dd>
          </>
        )}
        <dt>{status === 'HELD' ? 'Expires at' : 'Hold expiry'}</dt>
        <dd>{new Date(hold.expiresAt).toLocaleString()}</dd>
        <dt>Application ID</dt>
        <dd className="mono small">{hold.holdId}</dd>
      </dl>

      {status === 'EXPIRED' && (
        <div className="alert alert-warn">The time to pay ran out and the seat was released. You can apply again if seats are available.</div>
      )}
      {!studentOk && isHeld && (
        <div className="alert alert-warn">Enter the student’s birth registration number at the top to pay or cancel.</div>
      )}

      <div className="button-row">
        <button className="btn btn-primary" disabled={!isHeld || !studentOk || busy !== null} onClick={() => run('pay')}>
          {busy === 'pay' ? 'Processing…' : 'Pay admission fee'}
        </button>
        <button className="btn btn-danger" disabled={!isHeld || !studentOk || busy !== null} onClick={() => run('cancel')}>
          {busy === 'cancel' ? 'Cancelling…' : 'Cancel application'}
        </button>
      </div>
      <p className="muted small">Payment is simulated — clicking “Pay admission fee” counts as a successful payment.</p>

      <ErrorBox error={actionError} />
      <ErrorBox error={error} retrying />
      <SuccessBox message={success} />
    </section>
  );
}
