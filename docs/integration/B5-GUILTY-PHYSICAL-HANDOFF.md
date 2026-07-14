# B5 — przekazanie fizycznego Tropu i Planu Ucieczki do Area A

## Granica odpowiedzialności

Prefaby B5 są publiczną warstwą fizyczną. Nie znają roli, pełnej mapy Planu, ukrytego faktu Alibi ani pozostałego czasu Rundy. Emitują serwerowe rezultaty z ID A3; `NetworkRoundCoordinator` mapuje aktora na `PlayerId`, dodaje czas serwera i dopiero wtedy wysyła intencję do domeny.

Ruch, anulowanie i progress wielosekundowej akcji zapewnia B2. Finał ma konfigurowalne `6.5 s`, blokuje ruch, ale nie wyłącza look ani voice. Żaden Podejrzany nie dostaje osobnego przycisku pomocy lub anulowania Ucieczki.

## Prefaby i anchory

| Prefab | Anchor ID | Action ID | ID A3 / payload |
| --- | --- | --- | --- |
| `B5_AlibiClue_CrumpledReceipt` | `confiscated-receipt-tray` | `search-receipt-tray` | clue `paragon-cztery-kompoty` |
| `B5_EscapePrepare_FindTool` | `maintenance-cabinet` | `search-maintenance-cabinet` | `escape-find-tool` |
| `B5_EscapePrepare_OpenRoute` | `service-panel` | `inspect-service-route` | `escape-open-route` |
| `B5_EscapePrepare_ExitA` | `vent-control` | `loosen-vent-cover` | `escape-prepare-exit-a` |
| `B5_EscapePrepare_ExitB` | `loading-gate-control` | `unlatch-loading-gate` | `escape-prepare-exit-b` |
| `B5_EscapeFinal_ExitA` | `service-vent-exit` | `force-service-vent` | plan `escape-prototype`, exit `escape-exit-a` |
| `B5_EscapeFinal_ExitB` | `loading-gate-exit` | `force-loading-gate` | plan `escape-prototype`, exit `escape-exit-b` |

`Assets/Prefabs/Testing/B5GuiltyTracerHarness.prefab` zawiera wszystkie elementy oraz zagnieżdżony harness dwóch pomieszczeń B3. Nie jest częścią głównej sceny.

Wspólne i wyjściowe przygotowania przedstawiają zwykłe manipulacje szafką techniczną, panelem, wentylacją i bramą. Te same publiczne czynności mogą później zostać użyte przez Osobistą Sprawę Niewinnego; prefab nie nazywa motywu ani roli.

## Trop do Alibi

Subskrybuj `NetworkAlibiClueAction.ClueAcquiredServer` i mapuj sygnał na `AcquireAlibiClue`:

- `ClueId = paragon-cztery-kompoty`;
- Incydent `Quiet`, efekt `searched-confiscated-property`, lokacja `evidence-room`;
- autora z `signal.Actor`, czas wyłącznie z zegara serwera Rundy.

Prefab przechowuje tylko publiczny wygląd „Crumpled receipt”. Nie zawiera `linkedFactId` ani prywatnej treści Tropu. Interpretowalny tekst „Zmięty paragon z czterema kompotami dopisanymi innym charakterem pisma” przychodzi Winnemu dopiero w celowanym `PlayerRoundView` A3. Nigdy nie kopiuj do prefabu zdania faktu `bledny-rachunek`.

Jeżeli domena nie zaakceptuje Tropu dla aktora, wywołaj `ReleaseActorCompletionServer(actor)`. Publiczne przeszukanie pozostaje widoczne, ale właściwy Winny nie zostaje zablokowany przez cudzy blef.

## Przygotowanie Planu

Dla czterech prefabów przygotowawczych subskrybuj `CompletedServer`. `PayloadId` jest `EscapeStepId`; `PlanId` to `escape-prototype`. Wyślij `PrepareEscape`. Po odrzuceniu zwolnij aktora przez `ReleaseActorCompletionServer(actor)`, tak samo jak w B4.

Po zaakceptowaniu ponownego `escape-prepare-exit-a` albo `escape-prepare-exit-b` wywołaj `AuthorizeRetryServer()` na odpowiadającym finalnym prefabie. Nie odblokowuj wyjścia na podstawie timera ani lokalnego cooldownu.

## Finał Ucieczki

1. `EscapeAttemptStartedServer` emituje plan, exit, unikalny `IncidentId`, lokację i aktora serwerowego. Wyślij `BeginEscape`.
2. Po akceptacji natychmiast wywołaj `ConfirmBeginServer(actor)`. Po odrzuceniu wywołaj `RejectBeginServer(actor)`; nie tworzy to domenowego `InterruptEscape` ani blokady retry.
3. `EscapeAttemptInterruptedServer` mapuj na `InterruptEscape`. Potwierdzone przerwanie ustawia publiczne `RetryLocked` i zeruje progress B2. Ten sam exit pozostaje zablokowany do dodatkowego zaakceptowanego przygotowania; drugi exit jest niezależny.
4. `EscapeAttemptCompletedServer` mapuj na `CompleteEscape`. Prefab sam nie kończy Rundy.

Automatyczny Hałaśliwy Incydent powstaje z `BeginEscape` i raportuje wyłącznie `LocationId`. `Actor` istnieje w sygnale tylko do serwerowej autentykacji. Obserwator rozpoznaje wykonawcę fizycznie przez aktywną postać i `ActivePerformerNetId`; nie dodawaj nazwiska do Rejestru.

## Egzekucja podczas finału

Nie wyłączaj `ShotHitbox`, collidera gracza ani portów B1 podczas Ucieczki. Gdy istniejący seam trafienia doprowadzi do zaakceptowanej Egzekucji wykonawcy, wywołaj `TryInterruptPerformerServer(targetIdentity)` na aktywnym wyjściu. To emituje zwykłe przerwanie serwerowe; nie jest akcją dostępną dla Niewinnych. Wynik Rundy nadal rozstrzyga wyłącznie A3/B1.

## Referencje Inspector i placeholdery

Każdy prefab ma `NetworkIdentity`, collider, `InteractionPoint`, stabilne ID oraz wymienny `VisualRoot`. Finały dodatkowo wymagają `AttemptVisual` i `CompletedVisual`. Podmiana Area C może zmienić wyłącznie zawartość wizualną; nie może zmienić collidera, czasu gameplayowego, action ID, plan/step/exit ID ani lokacji Incydentu.
