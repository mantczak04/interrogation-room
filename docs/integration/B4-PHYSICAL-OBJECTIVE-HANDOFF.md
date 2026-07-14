# B4 — przekazanie fizycznych Celów i Incydentów do Area A

## Granica odpowiedzialności

Komponenty B4 wykonują wyłącznie serwerowo rozstrzygniętą zmianę świata i emitują neutralny rezultat fizyczny. Nie odczytują roli, nie znają przypisania Prywatnego Celu i nie przyznają postępu. `NetworkRoundCoordinator` albo późniejszy binder Area A pozostaje jedynym miejscem mapowania aktora na `PlayerId` i wysłania intencji do `RoundEngine`.

Każdy prefab korzysta z `NetworkTimedInteractable`. Anulowanie przed końcem nie zmienia `WorldRevision` i nie emituje rezultatu. Ukończenie obcego gracza zmienia świat, ale nie zużywa możliwości innego gracza: `NetworkObjectiveWorldAction` dopuszcza po jednym ukończeniu na aktora.

## Prefaby i stabilne ID

| Prefab | Anchor ID | Action ID | Payload / krok A1 |
| --- | --- | --- | --- |
| `B4_PersonalMatter_Prepare_RecordsCabinet` | `records-cabinet-a` | `search-confiscated-records` | `osobista-sprawa-przygotuj` |
| `B4_PersonalMatter_Prepare_EvidenceShelf` | `evidence-shelf-b` | `search-evidence-shelf` | `osobista-sprawa-przygotuj` |
| `B4_PersonalMatter_Finish_Locker` | `locker-a` | `hide-personal-document` | `osobista-sprawa-zakoncz` |
| `B4_PersonalMatter_Finish_ArchiveSlot` | `archive-slot-b` | `store-personal-document` | `osobista-sprawa-zakoncz` |
| `B4_Framing_Acquire_SuspiciousItem` | `evidence-tray` | `take-suspicious-item` | `wrobienie-przygotuj` |
| `B4_Framing_Plant_QuietIncident` | `target-locker` | `plant-suspicious-item` | `wrobienie-podloz` |
| `B4_LoudIncident_ArchiveAlarm` | `archive-alarm-panel` | `trigger-archive-alarm` | brak kroku Celu |

Prefab testowy `Assets/Prefabs/Testing/B4PhysicalObjectiveHarness.prefab` zawiera wszystkie warianty oraz techniczną zasłonę do sprawdzenia line-of-sight Cichego Incydentu. Nie jest częścią głównej sceny.

## Mapowanie rezultatów

### Zwykły krok Prywatnego Celu

Subskrybuj `INetworkTimedInteractable.CompletedServer`. Dla aktora pobierz z host-owned stanu aktualny `PrivateObjectiveView`. Wyślij `AdvancePrivateObjective` tylko wtedy, gdy `completion.PayloadId` jest równy bieżącemu `CurrentStep.Id`. `ObjectiveId` musi pochodzić z przypisania gracza (`osobista-sprawa:<PlayerId>` albo `sekretny-cel:<PlayerId>`), nigdy ze statycznej nazwy prefabu.

Odrzucona lub obca intencja nie cofa publicznej zmiany świata. To zamierzone: akcja pozostaje wiarygodnym blefem, a reguły postępu rozstrzyga domena.

### Incydent

Subskrybuj `IPhysicalIncidentSource.IncidentRaisedServer` i mapuj:

- `Actor` przez istniejącą mapę połączenie → `PlayerId`;
- `PhysicalIncidentKind.Loud/Quiet` na `IncidentKind.Loud/Quiet`;
- `IncidentId`, `EffectId` i `LocationId` bez zmiany;
- czas wyłącznie z zegara serwera Rundy.

Identyfikator pojedynczego efektu ma postać `<incidentIdPrefix>-<revision D3>`, np. `archive-alarm-001`. Dzięki temu każda przyjęta zmiana świata jest jednoznaczna, a jeden aktor nie może powtarzać tego samego prefabu.

`B4_Framing_Plant_QuietIncident` ma `ObjectiveStepId = wrobienie-podloz`. Dla tego prefabu postęp należy łączyć z `RegisterIncident` przez `PrivateObjectiveStepReference`; nie wysyłaj równolegle osobnego `AdvancePrivateObjective`, bo spowodowałoby to podwójną próbę postępu.

Jeżeli rezultat nie przesunął bieżącego kroku (w tym gdy krok 2 wykonano przed krokiem 1), binder musi wywołać `NetworkObjectiveWorldAction.ReleaseActorCompletionServer(actor)`. Publiczna zmiana świata i już zarejestrowany Incydent pozostają, lecz ten aktor może później ponowić fizyczną akcję. Po zaakceptowanym postępie nie zwalniaj ukończenia; chroni to przed spamem tego samego anchora.

### Odkrycie Cichego Incydentu

`QuietIncidentDiscoveryProbe` automatycznie sprawdza po stronie serwera zasięg i bezpośredni line-of-sight. Subskrybuj `DiscoveryCandidateServer` i wyślij `DiscoverQuietIncident` z `candidate.IncidentId`. Probe celowo nie zna roli; tylko domena A2 akceptuje Detektywa, więc kandydatura innego gracza nie ujawnia sprawcy ani nie tworzy wpisu Rejestru.

## Referencje Inspector

Każdy prefab wymaga `NetworkIdentity`, zwykłego collidra dla raycastu B2, `InteractionPoint`, stabilnego `anchorId`, `VisualRoot` i opcjonalnego `EffectVisual`. `EffectVisual` jest publiczną prezentacją zmiany świata i synchronizuje się przez `WorldRevision`. Prefab Cichego Incydentu dodatkowo wymaga `QuietIncidentDiscoveryProbe.incidentSource` oraz `discoveryPoint` na zmienionym obiekcie.

`VisualRoot` jest jedynym miejscem przeznaczonym do późniejszej podmiany placeholdera przez Area C. Podmiana nie może zmieniać komponentu gameplayowego, collidra, `InteractionPoint`, anchor ID, action ID ani payloadu.
