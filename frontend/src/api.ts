import { API_URL } from './config';

// ---------------------------------------------------------------- types (API contract)
export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export interface SchoolClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface Hold {
  holdId: string;
  classId: string;
  className?: string;
  studentId?: string;
  status: HoldStatus;
  expiresAt: string;
}

// ---------------------------------------------------------------- errors
/** An error that is always safe to show to the user as-is. */
export class ApiError extends Error {
  status: number;
  code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

// ---------------------------------------------------------------- server clock
/**
 * Difference between the server clock and this computer's clock (ms).
 * Countdowns use server time so a wrong PC clock does not show a wrong timer.
 */
let serverOffsetMs = 0;
export const serverNow = () => Date.now() + serverOffsetMs;

// ---------------------------------------------------------------- request helper
async function request<T>(method: string, path: string, opts: { studentId?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = {};
  if (opts.body !== undefined) headers['Content-Type'] = 'application/json';
  if (opts.studentId) headers['X-Student-Id'] = opts.studentId;

  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, {
      method,
      headers,
      body: opts.body !== undefined ? JSON.stringify(opts.body) : undefined,
    });
  } catch {
    // fetch only throws when the server cannot be reached at all.
    throw new ApiError(0, 'NETWORK_ERROR',
      'Cannot reach the admission server. Please check your connection or try again shortly.');
  }

  // The Date header only has whole seconds, so ignore differences under 2s
  // (normal rounding) and only correct a genuinely wrong computer clock.
  const date = response.headers.get('Date');
  if (date) {
    const offset = new Date(date).getTime() - Date.now();
    serverOffsetMs = Math.abs(offset) > 2000 ? offset : 0;
  }

  if (response.status === 204) return undefined as T;

  let data: unknown = null;
  try {
    data = await response.json();
  } catch {
    /* empty or non-JSON body */
  }

  if (!response.ok) {
    const err = data as { code?: string; message?: string } | null;
    throw new ApiError(
      response.status,
      err?.code ?? 'UNKNOWN',
      err?.message ?? (response.status >= 500
        ? 'The server had a problem. Please try again in a moment.'
        : 'The request could not be completed.'),
    );
  }
  return data as T;
}

// ---------------------------------------------------------------- endpoints
export const api = {
  createClass: (name: string, totalSeats: number) =>
    request<SchoolClass>('POST', '/classes', { body: { name, totalSeats } }),
  listClasses: () => request<SchoolClass[]>('GET', '/classes'),
  getClass: (classId: string) => request<SchoolClass>('GET', `/classes/${encodeURIComponent(classId)}`),
  apply: (classId: string, studentId: string) =>
    request<Hold>('POST', `/classes/${encodeURIComponent(classId)}/holds`, { studentId }),
  getHold: (holdId: string) => request<Hold>('GET', `/holds/${encodeURIComponent(holdId)}`),
  confirm: (holdId: string, studentId: string) =>
    request<{ holdId: string; status: HoldStatus }>('POST', `/holds/${encodeURIComponent(holdId)}/confirm`, { studentId }),
  cancel: (holdId: string, studentId: string) =>
    request<void>('DELETE', `/holds/${encodeURIComponent(holdId)}`, { studentId }),
  myHolds: (studentId: string) => request<Hold[]>('GET', '/me/holds', { studentId }),
};
