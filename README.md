# School Admission System

First-come-first-served online admission for a school.
A parent applies for a seat in a class → the seat is **held** for a limited time →
paying the admission fee **confirms** it. If the fee is not paid in time the seat is
released automatically and someone else can take it.

Every shift is its own class with its own seats (e.g. *Class 6 – Morning Shift* and
*Class 6 – Day Shift*).

**Stack:** .NET 8 (ASP.NET Core, EF Core) · PostgreSQL 16 · React + TypeScript (Vite) · Docker

---

## Run it

Only **Docker** is needed.

```bash
docker compose up --build
```

| What | URL |
|---|---|
| **Frontend (UI)** | **http://localhost:3000** |
| Backend API | http://localhost:8080 |
| Health check | http://localhost:8080/health |

Stop with `docker compose down` — **data is kept** (PostgreSQL uses a named volume).
`docker compose down -v` also deletes the data.

### Quick tour of the UI
1. Type a 17-digit birth registration number in the box at the top (e.g. `20152691234567890`).
2. **Create class** tab → add a class with a number of seats (school admin).
3. **Classes** tab → seat counts refresh every few seconds → **Apply**.
4. The application page shows a live countdown → **Pay admission fee** (payment is simulated) or **Cancel**.
5. **My applications** tab → every application of this student and its status.

To watch a seat expire quickly, start with a short hold time:

```bash
HOLD_DURATION_SECONDS=30 docker compose up --build
```

(PowerShell: `$env:HOLD_DURATION_SECONDS=30; docker compose up --build`)

---

## Run the tests

### With Docker only (no .NET / Node needed)

```bash
docker compose --profile test run --rm backend-tests
docker compose --profile test run --rm frontend-tests
```

The backend tests create a throw-away database on the compose PostgreSQL and drop it afterwards.

### Locally (optional)

```bash
cd backend && dotnet test        # needs .NET 8 SDK + Docker running (Testcontainers starts PostgreSQL)
cd frontend && npm ci && npm test # needs Node 20+
```

### What is tested

**Backend — 17 integration tests** against a real PostgreSQL (`backend/tests`):

| Test | Proves |
|---|---|
| 50 students apply at once for 10 seats | exactly 10 succeed, 40 get `409` — **never oversold** |
| One student applies 20× at once | exactly 1 active hold |
| Two API instances on one database (bonus) | still never oversold |
| Confirm the same hold 6× (5 in parallel) | `confirmedSeats` grows by 1 only |
| Hold expires | seat freed without any API call, confirm → `409 HOLD_EXPIRED` |
| Owner checks, cancel, contract fields, invalid input | `403`, `204`, all contract fields present, `400`/`404` |

The concurrency tests were checked to **fail** (40 holds for 10 seats) when the row lock is removed.

**Frontend — 12 tests** (Vitest + Testing Library, `frontend/src/test`): countdown and
EXPIRED switch without reload, Pay/Cancel disabled after expiry, student-ID validation and
persistence, seat counts, friendly error messages.

---

## Configuration

All settings are environment variables (defaults in `docker-compose.yml`):

| Variable | Service | Default | Meaning |
|---|---|---|---|
| `ConnectionStrings__Default` | backend | `Host=db;Port=5432;Database=school_admission;Username=postgres;Password=postgres` | PostgreSQL connection |
| `HOLD_DURATION_SECONDS` | backend | `600` | How long a seat stays held |
| `BACKEND_URL` | frontend | `http://localhost:8080` | Backend URL **as seen by the browser**. Applied when the container starts — no rebuild needed |

For `npm run dev` the frontend reads `VITE_API_URL` (default `http://localhost:8080`).

---

## API

Base URL `http://localhost:8080`, JSON in and out. The student is identified by the
`X-Student-Id` header (17-digit birth registration number). IDs are GUIDs.

| # | Method & path | Purpose | Success |
|---|---|---|---|
| 1 | `POST /classes` | Create a class `{ "name", "totalSeats" }` | `201` |
| 2 | `GET /classes` | All classes with seat counts | `200` |
| 3 | `GET /classes/{classId}` | One class with seat counts | `200` |
| 4 | `POST /classes/{classId}/holds` | Apply (hold a seat) — needs `X-Student-Id` | `201` |
| 5 | `GET /holds/{holdId}` | Current state of an application | `200` |
| 6 | `POST /holds/{holdId}/confirm` | Pay fee, confirm — needs `X-Student-Id` | `200` |
| 7 | `DELETE /holds/{holdId}` | Cancel — needs `X-Student-Id` | `204` |
| 8 | `GET /me/holds` | All applications of the student — needs `X-Student-Id` | `200` |

Hold `status` is one of `HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED`.
Response field names follow the brief exactly; a few extra fields are added
(e.g. `className` on `GET /holds/{id}`).

### Errors

Every error has the same body so the UI can show `message` directly:

```json
{ "code": "SEATS_FULL", "message": "Sorry, all seats in this class are taken right now. Please check again later." }
```

| HTTP | `code` | When |
|---|---|---|
| 400 | `INVALID_INPUT` | Bad class name / seat count / malformed JSON |
| 400 | `INVALID_STUDENT_ID` | `X-Student-Id` missing or not exactly 17 digits |
| 403 | `NOT_YOUR_HOLD` | Confirming/cancelling another student's application |
| 404 | `CLASS_NOT_FOUND`, `HOLD_NOT_FOUND`, `NOT_FOUND` | Unknown ID or URL |
| 409 | `SEATS_FULL` | No seat available |
| 409 | `ALREADY_APPLIED` / `ALREADY_CONFIRMED` | Student already has an active / confirmed application in this class; cancelling a confirmed admission |
| 409 | `HOLD_EXPIRED` / `HOLD_CANCELLED` | Confirming an application that expired / was cancelled |
| 503 | `SERVICE_UNAVAILABLE` | Database unreachable |
| 500 | `INTERNAL_ERROR` | Unexpected error |

---

## How the important rules are enforced

| Rule | How |
|---|---|
| `held + confirmed` never exceeds `totalSeats`, even under heavy concurrency | Applying runs in a transaction that first locks the class row (`SELECT … FOR UPDATE`). Requests for the same class queue up, so “count seats → insert hold” happens one at a time. The lock is inside PostgreSQL, so it also works with several backend processes. |
| Seat comes back automatically when time runs out | Seat counts are calculated from the `holds` table on every request and ignore `HELD` rows whose `expiresAt` has passed. No API call or background job is needed. A background job (every 15 s) additionally writes `EXPIRED` into those rows for tidiness. |
| One active application per student per class | Checked in the locked transaction **and** guaranteed by a partial unique index on `(class_id, student_id) WHERE status IN ('HELD','CONFIRMED')`. |
| Only the owner's own, unexpired `HELD` application can be confirmed | One atomic `UPDATE … WHERE id = @id AND student_id = @me AND status = 'HELD' AND expires_at > now`. |
| Confirming twice counts once | Confirm only changes a `HELD` row; a second call finds it `CONFIRMED` and returns `200` without changes. `confirmedSeats` is a count of rows, so it cannot be incremented twice. |
| No data loss on restart | PostgreSQL data lives in the `pgdata` named volume. Tables are created/updated by EF Core migrations on startup. |

---

## Project structure

```
backend/
  src/SchoolAdmission.Api/
    Controllers/      HTTP endpoints (thin)
    Services/         Business rules: ClassService, HoldService, ExpiredHoldSweeper
    Domain/           SchoolClass, Hold, HoldStatus
    Data/             AppDbContext + EF Core migrations
    Contracts/        Request/response shapes (the API contract)
    Infrastructure/   Error handling, student-ID header, JSON date format, settings
  tests/SchoolAdmission.Api.Tests/   Integration tests (xUnit, Testcontainers)
  Dockerfile
frontend/
  src/
    api.ts            API client + friendly errors
    hooks.ts          Polling, countdown clock, student ID, tiny hash router
    holdStatus.ts     Expiry / countdown logic
    components/       The screens
    test/             Vitest tests
  Dockerfile, nginx.conf, docker-entrypoint.sh
docker-compose.yml
ASSUMPTIONS.md       Decisions where the brief was not specific
AI_USAGE.md          How AI tools were used
```
