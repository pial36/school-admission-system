declare global {
  interface Window {
    __APP_CONFIG__?: { apiUrl?: string };
  }
}

/**
 * Backend base URL, in priority order:
 *  1. window.__APP_CONFIG__.apiUrl — written into /config.js by the Docker container
 *     from the BACKEND_URL env var, so it can change WITHOUT rebuilding the app.
 *  2. VITE_API_URL — for `npm run dev`.
 *  3. http://localhost:8080 — the default from the brief.
 */
export const API_URL = (
  window.__APP_CONFIG__?.apiUrl ||
  import.meta.env.VITE_API_URL ||
  'http://localhost:8080'
).replace(/\/+$/, '');
