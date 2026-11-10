# Assumptions

Decisions I made where the brief did not say exactly what to do, and why.

## Students and identity

1. **Student ID = exactly 17 digits.** The brief says the birth registration number is 17 digits, so `X-Student-Id` must match `^\d{17}$`. Anything else → `400 INVALID_STUDENT_ID`.No checksum or real-registry check — there is no source for that.
2. **No login, no admin role.** The brief says no login is needed, so the “Create class” screen and `POST /classes` are open to anyone. In a real system this would be behind
   admin authentication.
3. **`GET /holds/{holdId}` does not need `X-Student-Id`**, because the contract lists no header for it. Hold IDs are random GUIDs, so they cannot be guessed. Changing anything
   (confirm, cancel) does require the header and must be the owner (`403 NOT_YOUR_HOLD` otherwise).

## Seats and applications

4. **“One active application per student per class”** means at most one `HELD` or `CONFIRMED` application per (student, class). A student **may** hold seats in different
   classes at the same time (e.g. Morning and Day shift) — the brief only limits it per class.
5. **After an application expires or is cancelled, the student may apply again** to the same class (if a seat is free). After it is confirmed they cannot apply again to that class
   (`409 ALREADY_CONFIRMED`).
6. **A confirmed admission cannot be cancelled** through the API (`409 ALREADY_CONFIRMED`).
   The fee is already paid; a refund/withdrawal is an office process, not part of this brief.
7. **Cancelling something that is already cancelled or expired returns `204`** (nothing to do,
   the seat is free anyway). This makes cancel safe to retry.
8. **Confirming an already confirmed application returns `200` again** with
   `"status": "CONFIRMED"` (idempotent, as the brief requires `confirmedSeats` to grow only once).
9. **Expiry boundary:** an application is expired when `now >= expiresAt`. Confirming at or
   after that moment is refused with `409 HOLD_EXPIRED`.
10. **`expiresAt` is rounded down to whole seconds** so the stored value equals what the
    API returns (`"2026-11-01T04:10:00Z"` format like the brief).
11. **Class names may repeat** (two classes can have the same name); every class has its own ID.
    Name: 1–200 characters. Seats: 1–10,000.
12. **Classes cannot be edited or deleted** — the brief has no endpoint for it.
13. **Status codes I chose** where the brief only says “409 if no seat”: `409` is used for all
    “valid request but conflicts with current state” cases (seats full, already applied,
    expired, cancelled), `403` for someone else's application, `404` for unknown IDs
    (also for malformed IDs, since they can never exist), `400` for bad input.

## Time and expiry

14. **Expiry needs no API call:** seat counts and statuses are calculated from `expiresAt`
    on every read, so the seat is free the moment time runs out. A background job also
    marks such rows `EXPIRED` every 15 s, only to keep stored data tidy.
15. **The backend's clock (UTC) is the source of truth.** If several backend processes run,
    their clocks are assumed to be in sync (normal NTP). The UI corrects its countdown with
    the server's `Date` header if the user's computer clock is off by more than 2 s.

## Concurrency (incl. bonus)

16. **Overbooking is prevented inside PostgreSQL**, not in application memory: applying for a
    seat locks that class's row (`SELECT … FOR UPDATE`) for the length of the transaction.
    Therefore it holds with **two or more backend processes** on one database (tested).
    Only requests for the *same* class wait for each other; other classes are not blocked.
    I chose this over optimistic retries because it is simple, always correct, and the
    lock is held only for a few milliseconds.
17. **Seat counts are never stored as counters** — always counted from the holds — so they
    cannot drift out of sync with the real applications.
18. **Database migrations run on startup** inside a PostgreSQL advisory lock, so two backends
    starting together do not migrate at the same time.

## UI

19. **Live updates use polling** (class list every 3 s, application every 3 s, my applications
    every 4 s) instead of WebSockets: simpler, works everywhere, and “updates within a few
    seconds” is what the brief asks for.
20. **The countdown is calculated in the browser every second**, so an application turns
    `EXPIRED` and the pay button is disabled at 0 without waiting for the next poll.
21. **The student ID is remembered in the browser (`localStorage`)**, so after a refresh or
    reopening the browser the student's applications are still shown.
22. **“Pay admission fee” = payment succeeded** (the brief says no real payment gateway).
23. **The frontend runs on port 3000.** The brief fixes only the backend port (8080).
24. **The backend URL is set when the frontend container starts** (`BACKEND_URL` →
    `/config.js`), so it can be changed without rebuilding the image.
25. **CORS allows any origin**, because the UI and API are on different ports and the API uses
    no cookies or credentials.

## Infrastructure

26. **Database credentials in `docker-compose.yml` are local-development defaults**
    (`postgres`/`postgres`). In production they would come from secrets.
27. **IDs are GUIDs** (the brief allows any format).
