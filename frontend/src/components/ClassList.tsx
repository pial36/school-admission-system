import { useState } from 'react';
import { api, ApiError } from '../api';
import { isValidStudentId, navigate, usePolling } from '../hooks';
import { ErrorBox } from './common';

/** Screen: Class list — seat counts refresh every 3 seconds; "Apply" button. */
export function ClassList({ studentId }: { studentId: string }) {
  const { data: classes, error, loading, reload } = usePolling(api.listClasses, 3000);
  const [applying, setApplying] = useState<string | null>(null);
  const [applyError, setApplyError] = useState<Error | null>(null);
  const studentOk = isValidStudentId(studentId);

  async function apply(classId: string) {
    setApplyError(null);
    if (!studentOk) {
      setApplyError(new Error('Please enter the student’s 17-digit birth registration number at the top first.'));
      return;
    }
    setApplying(classId);
    try {
      const hold = await api.apply(classId, studentId);
      navigate(`/holds/${hold.holdId}`);
    } catch (err) {
      setApplyError(err as Error);
      reload(); // counts probably changed
    } finally {
      setApplying(null);
    }
  }

  const alreadyApplied = applyError instanceof ApiError &&
    (applyError.code === 'ALREADY_APPLIED' || applyError.code === 'ALREADY_CONFIRMED');

  return (
    <section className="card">
      <div className="card-header">
        <h2>Classes</h2>
        <span className="muted small">Seat counts update automatically</span>
      </div>

      {applyError && (
        <div className="alert alert-error" role="alert">
          {applyError.message}
          {alreadyApplied && <> <a href="#/my">See my applications</a></>}
        </div>
      )}
      <ErrorBox error={error} retrying={!!classes} />

      {loading && !classes && <p className="muted">Loading classes…</p>}
      {classes && classes.length === 0 && (
        <p className="muted">No classes yet. A school admin can create one under “Create class”.</p>
      )}

      {classes && classes.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th className="num">Total</th>
                <th className="num">On hold</th>
                <th className="num">Confirmed</th>
                <th className="num">Available</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {classes.map((c) => (
                <tr key={c.id}>
                  <td>{c.name}</td>
                  <td className="num">{c.totalSeats}</td>
                  <td className="num">{c.heldSeats}</td>
                  <td className="num">{c.confirmedSeats}</td>
                  <td className={`num ${c.availableSeats === 0 ? 'full' : 'free'}`}>{c.availableSeats}</td>
                  <td className="actions">
                    <button className="btn btn-primary" disabled={c.availableSeats === 0 || applying !== null}
                            onClick={() => apply(c.id)}>
                      {applying === c.id ? 'Applying…' : c.availableSeats === 0 ? 'Full' : 'Apply'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
