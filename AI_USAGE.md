# How I used AI on this project

## The tool

I used **Claude** (by Anthropic) in the Claude desktop app, mostly the Claude Haiku 4.5 and Haiku 5.5 models.

## How I worked with it

- **Understanding the brief:** I summarised the requirements and suggested a 10-step plan. I went through the plan and approved each step before it started.
- **Backend (.NET 8, EF Core):** Claude wrote the, the migration. I reviewed the main design decisions: the row lock, and the error codes.
- **Tests:** Claude wrote the backend integration tests and the frontend Vitest tests. I ran them myself, both locally and in Docker.
- **Frontend (React + TypeScript):** Claude built the screens, polling. I clicked through every screen in the browser.
- **Docs:** Claude drafted the README, ASSUMPTIONS and this file. I reviewed and edited all of them.

I committed steps once it was built **and** checked, whether by a build, the tests, or trying it by hand.

## Where the AI got it wrong (and how I found out)

**1. The migration lock failed on a brand-new database.**
To stop two backends from running migrations at the same time, Claude wrapped them in a PostgreSQL advisory lock. The problem: taking that lock needs a connection to the database, and on a fresh setup the database doesn't exist yet. A normal `docker compose up` hid this because PostgreSQL creates the main database on its own. I only caught it when I ran the backend tests inside Docker, where each run uses a new database name. All 17 tests failed with `database "sas_test_…" does not exist`. The fix was to create the database first if it's missing, and only then take the lock.

**2. A 30-second hold showed `0:31` on the countdown.**
The UI was adjusting its clock using the server's `Date` header, but that header only has whole seconds, so the countdown could run up to a second long. I noticed this while testing in the browser with `HOLD_DURATION_SECONDS=30`. Now the UI only corrects the clock if it's off by more than 2 seconds, which is the case it was really meant for (a PC with a badly wrong clock).

**3. The error message said "retrying" twice.**
It read something like "…we will keep retrying. (retrying automatically…)". I spotted it by stopping the backend while the UI was open, and reworded the message.

**4. A "CORS error" that had nothing to do with CORS.**
After restarting the backend, the browser complained that there was no `Access-Control-Allow-Origin` header, even though `curl` clearly showed it. This one took some digging. The same image worked on port 8081 but not on 8080, and IPv4 behaved differently from IPv6. It turned out another Windows service on my machine (*NI Application Web Server*) had grabbed port 8080 on IPv4. It wasn't a bug in the code. I stopped the service and it went away.

## How I checked the AI-written code

- **I made sure the tests can actually fail.** We temporarily removed the `FOR UPDATE` lock, and the concurrency test failed: 40 holds went through for only 10 seats. We also removed the expiry check, and two frontend tests failed. After that, both were put back. A test that never fails doesn't prove much, so this mattered to me.
- **The tests use a real PostgreSQL database**, not an in-memory fake. The row lock and the partial unique index only exist in a real database, so a fake would have given false confidence.
- **I tested end to end by hand** with `curl` and in the browser: holds expiring, confirming twice, the wrong user trying to confirm, the server going down, and data surviving a `docker compose down` / `up`.
- **I tested from a clean clone.** I cloned the repo into an empty folder and ran `docker compose up --build` plus the test suite, the same way a reviewer would.