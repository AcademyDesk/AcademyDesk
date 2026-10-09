# BUG-UI-0008 — Narrow calendar agenda clips or truncates useful detail

| Field | Value |
| --- | --- |
| Status | OPEN |
| Priority | P2 |
| Severity | Moderate readability |
| Module | SCHEDULE |
| Screen / route | /calendar |
| Environment | Production static export, loopback real API/Identity/disposable SQL |
| Role | Entitled same-tenant AcademyAdmin |
| Device/viewport | Edge emulated 320 × 900; physical devices NOT RUN |
| Discovery | 2026-10-09 live calendar integration screenshot review |
| Root-cause confidence | MEDIUM; constrained layout/nowrap observed, geometry diagnosis pending |
| Related source | apps/web/src/app/globals.css:1949; calendar AgendaItem |

## Observed evidence

`QA/EVIDENCE/calendar-live-browser-1791563279438/UTC-320-light-cancelled.png`
shows useful subject/date text and adjacent make-up title cut short in the narrow
agenda. Cancellation text and location remain readable; the asserted source date
and time are correct in the DOM, but that does not prove their full visual
readability. Existing title/detail rules use nowrap/ellipsis and a two-column
mobile grid. Establish element/ancestor geometry and intended truncation before
assigning the exact CSS cause. An overall document no-horizontal-overflow check
alone can miss clipping inside a card.

## Expected / next retest

At 320/375/390/768/1440 widths, light/dark and long synthetic names/dates/URLs:
useful calendar date, status, subject and teacher content should be readable with
wrapping or an accessible disclosure, without clipped cards/actions. Capture
element scroll/client bounds and screenshots, retain all existing calendar tests,
and verify filters/collapse and disabled cancelled meeting actions still work.
Do not shorten fixture strings or hide useful data to satisfy layout checks.

Next route: Sol Medium for bounded layout diagnosis/repair and exported-DOM
verification; Sol High only if domain/source contracts become involved. No
physical-mobile, full responsive or release acceptance is claimed by discovery.
