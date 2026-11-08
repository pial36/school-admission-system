import type { HoldStatus } from '../api';
import { STATUS_LABEL } from '../holdStatus';

export function StatusBadge({ status }: { status: HoldStatus }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{STATUS_LABEL[status]}</span>;
}

/** Red box for errors. `retrying` adds a note that live updates continue. */
export function ErrorBox({ error, retrying }: { error: Error | null; retrying?: boolean }) {
  if (!error) return null;
  return (
    <div className="alert alert-error" role="alert">
      {error.message}
      {retrying && <span className="muted"> (retrying automatically…)</span>}
    </div>
  );
}

export function SuccessBox({ message }: { message: string | null }) {
  if (!message) return null;
  return <div className="alert alert-success" role="status">{message}</div>;
}
