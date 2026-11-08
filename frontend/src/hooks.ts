import { useCallback, useEffect, useRef, useState } from 'react';
import { serverNow } from './api';

// ---------------------------------------------------------------- student ID
const STUDENT_KEY = 'studentId';
export const isValidStudentId = (id: string) => /^\d{17}$/.test(id);

/** The student's ID, remembered in the browser so a refresh/reopen keeps it. */
export function useStudentId() {
  const [studentId, setStudentId] = useState<string>(() => {
    try {
      return localStorage.getItem(STUDENT_KEY) ?? '';
    } catch {
      return '';
    }
  });

  const update = useCallback((value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, 17);
    setStudentId(digits);
    try {
      localStorage.setItem(STUDENT_KEY, digits);
    } catch {
      /* private mode etc. — still works for this tab */
    }
  }, []);

  return [studentId, update] as const;
}

// ---------------------------------------------------------------- polling
/**
 * Loads data now and again every `intervalMs`, so seat counts and statuses stay
 * fresh without a page reload. Keeps the last good data if a refresh fails.
 */
export function usePolling<T>(loader: (() => Promise<T>) | null, intervalMs: number, key = '') {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [loading, setLoading] = useState(true);
  const loaderRef = useRef(loader);
  loaderRef.current = loader;

  const reload = useCallback(async () => {
    const load = loaderRef.current;
    if (!load) return;
    try {
      setData(await load());
      setError(null);
    } catch (e) {
      setError(e as Error);
    } finally {
      setLoading(false);
    }
  }, []);

  const enabled = loader !== null;
  useEffect(() => {
    if (!enabled) {
      setData(null);
      setLoading(false);
      return;
    }
    // `key` changes (e.g. another student ID or hold) -> start fresh.
    setData(null);
    setError(null);
    setLoading(true);
    reload();
    const timer = setInterval(reload, intervalMs);
    return () => clearInterval(timer);
  }, [enabled, intervalMs, reload, key]);

  return { data, error, loading, reload };
}

// ---------------------------------------------------------------- clock
/** Current (server-corrected) time, updated every second for countdowns. */
export function useNow(intervalMs = 1000) {
  const [now, setNow] = useState(serverNow);
  useEffect(() => {
    const timer = setInterval(() => setNow(serverNow()), intervalMs);
    return () => clearInterval(timer);
  }, [intervalMs]);
  return now;
}

// ---------------------------------------------------------------- hash router
/** Tiny router using the URL hash (#/classes) so refresh keeps the current page. */
export function useHashRoute() {
  const read = () => window.location.hash.replace(/^#/, '') || '/classes';
  const [route, setRoute] = useState(read);
  useEffect(() => {
    const onChange = () => setRoute(read());
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);
  return route;
}

export const navigate = (path: string) => {
  window.location.hash = path;
};
