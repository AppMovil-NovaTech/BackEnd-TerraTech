# Sprint 2 — IAM and integration

## Scope and contracts

Epic02 belongs to Rubens in the October 23 group agreement. This increment covers US06/US07 email verification, US08 password recovery and US09 registration date, and publishes the previously integrated Monitoring endpoints. It preserves the existing team's authorship. Flutter, group videos and a real satellite-image provider are outside this increment.

- `POST /api/v1/authentication/sign-up`: 201, `emailVerificationRequired: true`, null token/expiry. New accounts verify their address before login.
- `POST /api/v1/authentication/sign-in`: 200 with an eight-hour JWT, 401 for invalid credentials, 403 `EMAIL_NOT_VERIFIED` for pending verification.
- `POST /api/v1/authentication/email-verifications`: `{emailAddress}`, 202 generic acknowledgement.
- `POST /api/v1/authentication/email-verifications/confirm`: `{token}`, 204 on success.
- `POST /api/v1/authentication/password-reset-requests`: `{emailAddress}`, 202 generic acknowledgement.
- `POST /api/v1/authentication/password-resets`: `{token,password,confirmPassword}`, 204 on success.
- Expired, replaced, wrong-purpose or consumed tokens return 400 `TOKEN_INVALID_OR_EXPIRED`. Incorrect password confirmation does not consume a valid token.
- Verification expires after 24 hours and recovery after one hour, including the exact expiry boundary. Only SHA-256 token hashes are persisted. Repeated requests within a minute retain the existing link; endpoints also apply an IP request limit (default 20/minute).
- Password reset increments the account token version; previous sessions are rejected. Historical versionless JWTs remain compatible while their account token version is zero.
- The additive migration exempts only accounts existing when the migration runs from verification; it does not invent verification dates. New registration sets the exemption to false.
- `/api/v1/profiles/me` adds nullable `registeredAt` from the existing audit timestamp. No historical dates are fabricated.

## Resend

The adapter uses HTTPS to Resend and two explicit outbound operations. Missing configuration or provider failure returns 503 `EMAIL_UNAVAILABLE`, never a false delivery confirmation. Request failures can leave an unverified account after creation; the user can retry through resend verification.

Configure `Email__ApiKey`, `Email__From`, and `Email__PublicBaseUrl` outside Git. `.env.example` contains placeholders; `.env.resend` is ignored and excluded from Docker/Cloud Build contexts. For local Debug runs, supply a Development database and use `python3 scripts/run-local-with-email.py` with .NET 10 on PATH. Automated tests replace the provider with an in-memory sender and isolated MySQL.

The current sender is temporarily `TerraTech <onboarding@resend.dev>`, as requested. Resend restricts this sender to its account owner's email; this is a test configuration, not delivery to arbitrary users. Replace the sender with `TerraTech <no-reply@auth.lucemz.com>` after domain verification. The supplied key has send-only permissions, so domain inspection/DNS records cannot be retrieved with it. The actual DKIM/SPF values must come from the Resend domain dashboard; no records are invented.

Email links use HTTPS `/account/verify-email` and `/account/reset-password`, tokens in fragments, and explicit POST confirmation. Pages remove fragments from browser history, disable caching/referrers and use a restrictive CSP. Opening a link with GET does not consume it.

## Validation and contribution evidence

The pre-change backend baseline passed 87 tests. See `docs/sprint2-validation.json` for actual post-change results and timestamps. Git author/committer dates were explicitly assigned October 29 at the user's request; they are not evidence of test execution dates.

Tasks: implement and test verification/recovery; preserve historical accounts; connect Android pending-account/recovery flows; integrate the team's recommendations with a Room upgrade; maintain endpoint documentation; validate and deploy the existing Cloud Run service. No unrecorded hours, physical-device tests, interviews or inbox receipt are claimed.

The public deployment needs matching source, successful readiness and the four IAM operations plus the seven Monitoring operations previously missing from Swagger. A private production SQL backup is kept outside Git before the additive migration. Secrets remain outside artifacts and evidence. Publication details are recorded separately after rollout.

## Published result

Final active revision: `backend-terratech-git-00016-rer`, project `terratech-510620`, region `us-east1`, existing HTTPS domain. The four IAM operations and all seven previously absent Monitoring operations are present in public Swagger. Health, both pages and their versioned CSS/JS respond 200; invalid verification tokens return 400. See [public HTTP checks](sprint2-public-verification.json).

The public check found an existing account for the authorized test address, so its password and profile were preserved. The recipient confirmed receipt of the recovery email. The recipient also confirmed receipt of the verification email. Historical accounts may verify voluntarily while keeping their login exemption. User completion of the links is separate from receipt; no existing password was changed by these public checks.

The published-page smoke test caught CSS/JS responses that were unavailable through static middleware. Explicit anonymous resource routes and versioned asset URLs fix deployment behavior and the previous cached 404. The regression test now checks all four page/resource routes.

Validation: 96 backend Debug tests passed, compared with 87 at baseline, including historical-data migration, optional verification, recovery, session revocation, tokens and pages. Android passed 80 unit tests, 28 emulator instrumented tests and lint with zero errors. Both code workflows passed remotely, including the final optional-verification backend run: https://github.com/AppMovil-NovaTech/BackEnd-TerraTech/actions/runs/41168749311 . No physical phone test is claimed.

For a later custom sender, verify auth.lucemz.com in Resend/Cloudflare and update Email__From; no application rewrite is needed. Domain DNS changes were deliberately deferred at the user's request.
