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

For every outbound field, record its source, recipient, permitted phase, and reason. Construct each payload from `ViewFor(recipientPlayerId)`. Keep roles, complete Alibi, hidden facts, and Sekretne Cele only on the host. Use targeted messages or `TargetRpc`; public synchronization may contain only explicitly public state.

Treat client input as an intention. Resolve sender identity from the authenticated connection, validate authority and phase on the host, then pass a domain command to `RoundEngine`. Never accept a client-supplied `PlayerId` as authority.

## Verify in order

1. Edit Mode tests for every changed Runda rule or private-view filter.
2. Unity compilation and bounded Console Errors.
3. Local host/client over KCP, including one unauthorized or wrong-phase intention and recipient privacy checks.
4. ParrelSync multi-client test after KCP succeeds.
5. FizzySteamworks only after local behavior is correct; use two machines/accounts.

Use `$unity-change-verification` and report levels not run. Complete when each secret field has one intended recipient path, authority is host-derived, and the highest applicable reachable integration level passes.
