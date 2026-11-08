import { useCallback } from 'react';
import { api } from '../api';
import { effectiveStatus, formatRemaining } from '../holdStatus';
import { isValidStudentId, useNow, usePolling } from '../hooks';
import { ErrorBox, StatusBadge } from './common';

/** Screen: My applications — every application of this student and its status. */
export function MyApplications({ studentId }: { studentId: string }) {
  const valid = isValidStudentId(studentId);
  const loader = useCallback(() => api.myHolds(studentId), [studentId]);
  const { data: holds, error, loading } = usePolling(valid ? loader : null, 4000, studentId);
  const now = useNow();

  return (
    <section className="card">
      <div className="card-header">
        <h2>My applications</h2>
        <span className="muted small">Updates automatically</span>
      </div>

      {!valid && <p className="muted">Enter the student’s 17-digit birth registration number at the top to see applications.</p>}
      <ErrorBox error={error} retrying={!!holds} />
      {valid && loading && !holds && <p className="muted">Loading…</p>}
      {valid && holds && holds.length === 0 && (
        <p className="muted">No applications yet. <a href="#/classes">Browse classes</a> to apply.</p>
      )}

      {valid && holds && holds.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>Status</th>
                <th>Time left</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {holds.map((h) => {
                const status = effectiveStatus(h, now);
                return (
                  <tr key={h.holdId}>
                    <td>{h.className ?? h.classId}</td>
                    <td><StatusBadge status={status} /></td>
                    <td className="mono">{status === 'HELD' ? formatRemaining(h.expiresAt, now) : '—'}</td>
                    <td className="actions">
                      <a className="btn" href={`#/holds/${h.holdId}`}>{status === 'HELD' ? 'Pay / cancel' : 'View'}</a>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
