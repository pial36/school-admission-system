# AI usage

## Tool

**Claude Code** (Anthropic, model Claude Opus 5.5), used in the Claude desktop app with
access to the project folder, terminal, Docker and a built-in browser.

## How I used it

I worked step by step and reviewed each step before moving on:

| Part | What the AI did | What I did |
|---|---|---|
| Reading the brief | Summarised requirements and proposed a 10-step plan | Checked the plan, approved each step |
| Backend (.NET 8, EF Core) | Wrote the data model, migration, 8 endpoints, error handling | Reviewed design choices (row lock, computed seat counts, error codes) |
| Concurrency | Implemented the `SELECT … FOR UPDATE` class-row lock | Asked for proof it works (see below) |
| Tests | Wrote backend integration tests and frontend Vitest tests | Ran them locally and in Docker |
| Docker | Dockerfiles, `docker-compose.yml`, test profiles | Ran `docker compose up` from a clean clone |
| Frontend (React + TS) | Wrote the screens, polling, countdown, runtime config | Tested every screen in the browser |
| Docs | Drafted README, ASSUMPTIONS and this file | Reviewed and edited them |

Every commit was made after the step was built **and** verified (build, tests, or a manual check).

## Where the AI was wrong, and how it was caught

1. **Migration lock broke on a brand-new database.**
   To stop two backends migrating at once, the AI wrapped migrations in a PostgreSQL advisory
   lock — but taking the lock needs a connection to the database *before* it exists.
   Normal `docker compose up` hid this (PostgreSQL creates the main database itself).
   **Caught:** running the backend tests inside Docker, where each run uses a new database
   name — all 17 tests failed with `database "sas_test_…" does not exist`.
   **Fix:** create the database first if missing, then take the lock.

2. **Countdown showed `0:31` for a 30-second hold.**
   The UI corrected its clock with the server's `Date` header, which only has whole seconds,
   so the countdown could be up to 1 s too long.
   **Caught:** testing in the browser with `HOLD_DURATION_SECONDS=30`.
   **Fix:** only correct the clock if the difference is more than 2 s (a really wrong PC clock).

3. **Error message said “retrying” twice** (“…we will keep retrying. (retrying automatically…)”).
   **Caught:** stopping the backend while the UI was open. **Fix:** reworded the message.

4. **A CORS error that was not a CORS problem.**
   After restarting the backend, the browser reported “No Access-Control-Allow-Origin header”,
   although `curl` showed the header. Comparing ports (8080 failed, 8081 worked with the same
   image) and IPv4 vs IPv6 showed that another Windows service on my PC
   (*NI Application Web Server*) had taken port 8080 on IPv4. Not a code bug; I stopped the service.

## How I made sure AI-written code is correct

- **Tests that are proven to fail without the fix:** the AI temporarily removed the
  `FOR UPDATE` lock and the concurrency test failed (40 holds for 10 seats); it also removed
  the expiry check and two frontend tests failed. Both were then restored.
- **Real PostgreSQL in tests**, not an in-memory fake — the lock and the partial unique index
  only exist in a real database.
- **Manual end-to-end checks** with `curl` and in the browser (expiry, double confirm,
  wrong owner, server down, data surviving `docker compose down`/`up`).
- **Clean-clone check:** cloned the repo into an empty folder and ran
  `docker compose up --build` and the test suite, the same way the reviewers will.
