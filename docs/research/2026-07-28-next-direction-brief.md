# Repository direction brief — 2026-07-28

Companion synthesis to [the complete repository audit](./2026-07-28-repository-direction-audit.md).

## Scope

Reviewed all **68 first-party project documents—5,339 lines**—and cross-checked important status claims against current code and tests. Vendor documentation, generated folders, licenses, and asset provenance were excluded from the product-direction analysis.

Unity was not opened and tests were not run during this research pass. The full audit records the evidence and verification boundaries.

## 1. Overall assessment

The game is considerably further along than the documentation suggests.

The project already has an advanced vertical slice:

- the complete `RoundEngine` model;
- private role views and host-owned secrets;
- `Prywatne Cele`, Incydenty, Tropy, and Ucieczka;
- physical Egzekucja using the pistol;
- carryable objects and timed/minigame interactions;
- 18 Spraw and 15 Osobistych Spraw in source;
- lobby, preparation, results, and settings UI;
- Steam/KCP transport integration;
- substantial Vivox spatial voice implementation;
- broad Edit Mode and Play Mode test coverage.

The problem is no longer “what major system should we invent?” It is:

> **We do not have one trustworthy description of what is implemented, what has been genuinely validated, and what still blocks a real multiplayer playtest.**

Several documents describe completed systems as future work, while other documents report completion without enough end-to-end evidence. See `docs/research/2026-07-28-repository-direction-audit.md:13-36`.

---

## 2. What should be closed or updated now

A dedicated **documentation rebaseline** should happen before another design phase.

### Close or archive

#### `REFACTOR-PLAN.md`

Most of its assembly and voice work is already completed. Presenting it as an active plan could cause someone to repeat old migrations.

Keep only the genuine remaining debt:

- legacy MainMenu path;
- IMGUI developer panel;
- stable multiplayer identity;
- eventual decomposition of `NetworkRoundCoordinator`.

See `docs/research/2026-07-28-repository-direction-audit.md:129-155`.

#### Old map-polish execution reports

Archive as historical evidence:

- `docs/map-polish/ITERATION-01.md`
- `docs/map-polish/ITERATION-02.md`
- `docs/map-polish/ITERATION-03.md`
- `docs/map-polish/FINAL-REPORT.md`
- the chronological parts of `docs/map-polish/PASS-2-ART-DIRECTION.md`

`PASS-2-ART-DIRECTION.md` has become a long troubleshooting diary containing obsolete instructions, later corrections, and unchecked acceptance criteria. Preserve its useful lessons, but do not treat it as the current visual specification.

#### Old graphics phase tracker

The graphics tracker does not represent `main`. It still labels substantial completed work as `Open` or `Review`. Replace it with:

1. current environment baseline;
2. next presentation work;
3. remaining validation;
4. historical reports.

See `docs/research/2026-07-28-repository-direction-audit.md:308-333`.

#### B4/B5 “handoffs”

These are no longer pending handoffs. Convert them into current integration contracts or mark them historical.

B4 particularly needs attention: production carry/item-slot behavior has diverged from the source prefabs and old harness. Scene-only component overrides are now carrying important gameplay behavior.

#### Historical voice research

`docs/research/proximity-voice-tools.md` correctly warns that Vivox superseded the old Dissonance recommendation, but the body still prominently instructs the reader to buy and implement Dissonance. Move that material under an unmistakable historical section.

### Rewrite as current sources

#### `MECHANICS-OVERVIEW.md`

This should become the current status ledger. Its present implementation roadmap is substantially stale.

For each system, track separate columns:

- product decision;
- domain implemented;
- runtime adapter implemented;
- scene/UI wired;
- automated evidence;
- real-client evidence;
- last verification;
- next gate.

“Class exists,” “test passes,” and “three real clients completed a Runda” are not the same status.

#### `MVP-ARCHITECTURE.md`

Either:

- label it as the historical first-slice architecture plan; or
- rewrite it to describe current module boundaries without the obsolete implementation sequence.

The architectural seams remain good; its status and scope sections are what have expired.

#### `MAP-MVP.md`

Ratify the current map footprint as the playtest contract rather than leaving it as an unapproved proposal. It also still contains obsolete Egzekucja language that conflicts with the physical pistol contract.

Do **not** expand it yet. The existing documents themselves recommend gathering playtest evidence first. See `docs/research/2026-07-28-repository-direction-audit.md:157-178`.

#### Fable brief

Give `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md` an explicit status:

- completed current profile;
- active permanent product contract; or
- completed experiment.

The implementation already follows much of it—30-second `Przygotowanie`, three-second all-ready shortening, six Alibi facts, random case selection—but the canonical instructions still describe timings and content volume as unresolved tuning. See `docs/research/2026-07-28-repository-direction-audit.md:71-97`.

---

## 3. Immediate dangerous documentation issue

`AGENTS.md` currently lists `Tools/Setup Main Menu Scene` as the canonical builder.

That builder creates the obsolete uGUI menu and overwrites the scene, while the current menu uses UI Toolkit. Following the repository instruction could therefore destroy the active MainMenu setup.

This should be the **first cleanup change**:

- remove the builder from the approved-tool list;
- mark the menu item legacy or disable it;
- eventually rewrite it for the UI Toolkit contract;
- decide whether `MainMenuManager` can be removed.

See `docs/research/2026-07-28-repository-direction-audit.md:180-199`.

---

## 4. Next biggest engineering changes

### Priority 1 — Enforce the eight-player boundary

This is a concrete multiplayer bug.

The product supports 3–8 players, but the `NetworkManager` currently permits 100 connections:

`Assets/Scenes/Room.unity:42778-42783`

Steam uses that value directly when creating its lobby:

`Assets/Scripts/Steam/SteamLobby.cs:125-135`

Consequently, a ninth player can potentially enter, after which the host cannot start the Runda because the player count is invalid.

Required work:

1. enforce a maximum of eight at admission;
2. advertise eight as the Steam lobby capacity;
3. reject player nine with a visible reason;
4. test joins 7, 8, and 9;
5. correct documentation that currently says the seventh player should be rejected.

This is small compared with some other work, but it is a release blocker.

### Priority 2 — Decide disconnect policy and implement stable identity

This is the largest remaining multiplayer correctness issue.

Current `PlayerId` mapping is derived from Mirror `connectionId`, not authenticated Steam identity. A reconnecting Steam player therefore cannot reliably reclaim their role or private view.

The product also has no settled rule for losing the `Detektyw` or `Winny` during a Runda.

Decide one of:

- immediate no-contest;
- reconnect grace period;
- permanent continuation with an explicit fallback.

Recommended first-public-version policy:

> **A short reconnect grace period, followed by a no-contest termination if the missing player is the `Detektyw` or `Winny`.**

That avoids silently awarding a distorted result while still tolerating brief network interruptions.

Then implement:

- SteamID-derived stable identity;
- a development identity strategy for KCP;
- private-view restoration;
- disconnect during `Przygotowanie`, active Ucieczka, and Runda;
- explicit outcome/reason reporting.

See `docs/research/2026-07-28-repository-direction-audit.md:287-306`.

### Priority 3 — Create and pass a real vertical-slice exit gate

Before adding map area or more content, prove the existing game works as a complete session.

The gate should use KCP first with a host and at least two real clients:

1. join lobby;
2. validate public roster and private identities;
3. start `Przygotowanie`;
4. verify each role receives only its own information;
5. complete physical objectives;
6. test Incydenty and Tropy;
7. finish by physical Egzekucja;
8. repeat with Ucieczka;
9. return to lobby;
10. start a clean second Runda;
11. test one disconnect case;
12. verify voice through rooms and doors.

Only after KCP succeeds should the same essential flow be tested through Steam on two machines/accounts.

The repository has good automated coverage, but the sandbox explicitly does not replace this test. See `docs/research/2026-07-28-repository-direction-audit.md:244-266`.

### Priority 4 — Complete Vivox operational acceptance

Vivox is not missing as an implementation. It is missing as **proven operational behavior**.

Still needed:

- confirm production/dashboard credentials;
- verify real microphones across processes;
- test Lobby global voice;
- test Runda spatial voice;
- same room at different distances;
- corridor;
- full wall;
- open and closed doors;
- eavesdropping next to a closed door;
- participant mute/volume;
- reconnect/channel switching.

The documented microphone hot-unplug case also appears unsupported: device availability is checked during setup/recovery, not continuously after reaching `Ready`.

See `docs/research/2026-07-28-repository-direction-audit.md:268-285`.

### Priority 5 — Validate existing content instead of adding more

The code already contains more content than several roadmaps expected:

- 18 Spraw;
- 15 Osobistych Spraw;
- four `Wrobienie` variants;
- three Ucieczka narratives.

Do not author another large content batch yet.

Create a content matrix:

| Content | Definition | Asset synced | Coordinator configured | Physical anchors | Automated test | Real-client tested |
|---|---:|---:|---:|---:|---:|---:|

This will reveal whether the real shortage is authored content or scene compatibility.

Also resolve the current content-contract disagreement:

- exactly six facts in code;
- exactly eight in the playtest catalog;
- 6–10 in an older mechanics document.

The engine currently accepts exactly six. See `docs/research/2026-07-28-repository-direction-audit.md:205-223`.

### Priority 6 — Repair production prefab ownership

Move important production behavior out of fragile scene-only overrides.

In particular:

- update B4 source prefabs to use the actual carry/item-slot flow;
- update the B4 harness to match production;
- verify foreign item placement cannot permanently block another player’s objective;
- establish whether source prefab or scene instance owns each physical contract.

This cleanup should happen before map expansion multiplies the number of anchors and overrides.

---

## 5. Presentation work after the reliability gate

Once the current session is proven, the best non-map presentation investments are:

### Production UI cleanup

- remove or editor-gate the visible `TEST DEWELOPERSKI` action;
- retire legacy MainMenu code;
- reconcile `UI-STYLE-GUIDE.md` with `Theme.uss`;
- choose the real `PanelSettings` match factor;
- capture 720p, 1080p, 1440p, 4K, and ultrawide evidence;
- verify long Polish labels and glyphs;
- ensure developer controls cannot appear in production builds.

### Character presentation

This is probably the largest remaining visual weakness because interrogation places players close together for long periods.

Focus on:

- silhouette and material readability in current lighting;
- dependable locomotion and sitting;
- several asynchronous idle variants;
- a small conversational gesture layer;
- simple Vivox-energy jaw/mask motion;
- host/client synchronization checks.

Do not start with new character models or a sophisticated facial rig. `docs/design/graphics/FAZA-5-postacie.md:27-64` already outlines a reasonable bounded pass.

### Readable gameplay props

Prioritize props that communicate gameplay state:

- evidence cabinet/drawer;
- keys and key rack;
- document/envelope;
- personal item;
- hiding slot;
- planting slot;
- Ucieczka components.

Replace decorative furniture only when it is a close-up hero prop or visibly damages the art direction.

### Small VFX and audio

After characters and UI:

- fluorescent buzz tied to flicker;
- rain/window ambience;
- coffee steam;
- dust in the interrogation beam;
- clock sound;
- door and chair foley.

These will produce more perceived polish than another room.

---

## 6. What not to focus on yet

Explicitly defer:

- map expansion;
- additional Spraw or Osobiste Sprawy;
- a broad furniture replacement campaign;
- another lighting redesign;
- final `Notatki Detektywa`;
- another large UI redesign;
- a major rewrite of `RoundEngine`;
- premature optimization without current measurements.

The final form of Detective Notes, Alibi presentation, and post-Runda reveal remain legitimate open questions, but they do not currently block networking, voice, or the full playtest. See `docs/research/2026-07-28-repository-direction-audit.md:395-405`.

---

## 7. Recommended order

### Phase A — Documentation and guardrails

1. Block the legacy MainMenu builder.
2. Fix all 3–8 and `Sekretny Cel` 5–8 contradictions.
3. Decide the status of the Fable brief.
4. Rebuild `MECHANICS-OVERVIEW.md` as a current-state ledger.
5. Archive completed refactor, graphics, and map execution plans.
6. Ratify the current map without expanding it.

### Phase B — Multiplayer blockers

1. Enforce eight-player admission.
2. Decide disconnect semantics.
3. Introduce stable Steam identity.
4. Correct reconnect/private-view restoration.
5. Repair production prefab/harness drift.

### Phase C — Proof of the current game

1. Run the complete KCP real-client gate.
2. Run the real Vivox acoustic matrix.
3. Validate the content wiring matrix.
4. Verify a clean second Runda.
5. Run Steam on two machines/accounts.
6. Produce one dated release/playtest evidence report.

### Phase D — Focused polish

1. Production UI acceptance.
2. Character presentation.
3. Gameplay-readable props.
4. Small VFX/audio.
5. Performance and Low-quality validation.
6. Only then reconsider map expansion.

---

## Bottom line

The project does not need another broad feature push. It needs to **consolidate, prove, and harden what is already there**.

The most valuable next milestone is:

> **A documented, repeatable, three-client Runda that passes from lobby to a clean second Runda—with private information, physical Egzekucja/Ucieczka, disconnect handling, doors, and real Vivox voice all working together.**

Once that passes, the evidence will show whether the next investment should be map expansion, balance, presentation, or content. Right now, more implementation without that evidence would mostly add uncertainty rather than progress.
