---
name: mirror-round-networking
description: Design, implement, or review Mirror networking for a Runda in this project. Use when work touches NetworkRoundCoordinator, connection ownership, targeted PlayerRoundView delivery, host authority, secrets, KCP, ParrelSync, or FizzySteamworks.
---

# Mirror Runda networking

## Establish the contract

Read [AGENTS.md](../../../AGENTS.md), `CONTEXT.md`, `docs/architecture/MVP-ARCHITECTURE.md`, and [ADR-0011](../../../docs/adr/0011-server-owns-secrets-and-exposes-private-views.md). Read other ADRs only for affected rules.

Map the change across these boundaries before editing:

- `RoundEngine`: pure rules and recipient-filtered `ViewFor`; no networking concerns.
- `NetworkRoundCoordinator`: sole Mirror adapter for a Runda and sole connection-to-`PlayerId` mapper.
- Transport/messages: serialization and targeted delivery, never rule resolution.
- UI: render its received view and send intentions.

## Protect private information

For each changed outbound field, check source, recipient, and permitted phase. The host owns authoritative secrets; each client receives only its phase-filtered `ViewFor(recipientPlayerId)` through a targeted message or `TargetRpc`. Public synchronization contains only public state.

Treat client input as an intention. Resolve sender identity from the authenticated connection, validate authority and phase on the host, then pass a domain command to `RoundEngine`. Never accept a client-supplied `PlayerId` as authority.

## Verify in order

For implementation, use `$unity-change-verification`. For design/review, assess evidence without treating investigation as a code change.

- Test changed Runda rules and private-view filters in Edit Mode.
- For networking changes, test KCP host/client, recipient privacy, and a rejected unauthorized or wrong-phase intention. Use ParrelSync for multiple clients.
- Test FizzySteamworks on two machines/accounts only when Steam/lobby/transport behavior is affected, after KCP succeeds.

Complete when payloads respect recipient/phase permissions, authority is host-derived, and applicable checks pass. Report unavailable checks as gaps, not success.
