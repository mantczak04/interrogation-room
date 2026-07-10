# Przegląd mechanik gry

Kompletna mapa mechanik „Przesłuchania": co już działa, co jest niezbędne do pierwszego grywalnego slice'a, co jest świadomie odłożone, a co czeka na decyzję projektową. Każda mechanika must-have ma osobną specyfikację w [`docs/design/mechanics/`](./mechanics/), pisaną tak, żeby dało się ją wręczyć agentowi do implementacji.

Źródła prawdy: [CONTEXT.md](../../CONTEXT.md) (słownik i reguły), [MVP-ARCHITECTURE.md](../architecture/MVP-ARCHITECTURE.md) (moduły i kolejność), [docs/adr](../adr/) (decyzje), stan kodu w `Assets/Scripts/` na branchu `codex/shooting-mechanic`.

## Tabela zbiorcza

| # | Mechanika | Status | Priorytet | Specyfikacja |
|---|-----------|--------|-----------|--------------|
| 1 | Ruch gracza (FPP) | ✅ Zaimplementowana (luki) | Must-have | [ruch-gracza.md](./mechanics/ruch-gracza.md) |
| 2 | Lobby i sieć (Steam + KCP) | ✅ Zaimplementowana (luki) | Must-have | [lobby-i-siec.md](./mechanics/lobby-i-siec.md) |
| 3 | Interakcja z obiektami („E") | ✅ Zaimplementowana | Must-have | [interakcja-z-obiektami.md](./mechanics/interakcja-z-obiektami.md) |
| 4 | Podnoszenie przedmiotów do ręki | ✅ Zaimplementowana (pistolet) | Must-have | [podnoszenie-przedmiotow.md](./mechanics/podnoszenie-przedmiotow.md) |
| 5 | Strzelanie i hitboxy pocisków | ✅ Zaimplementowana (bez skutku w regułach) | Must-have | [strzelanie-i-hitboxy.md](./mechanics/strzelanie-i-hitboxy.md) |
| 6 | Silnik Rundy (`RoundEngine`) | ❌ Brak | **Must-have, krytyczna** | [silnik-rundy.md](./mechanics/silnik-rundy.md) |
| 7 | Role i Skład Rundy | ❌ Brak | Must-have | [role-i-sklad-rundy.md](./mechanics/role-i-sklad-rundy.md) |
| 8 | Alibi i redagowanie faktów | ❌ Brak | Must-have | [alibi-i-redagowanie.md](./mechanics/alibi-i-redagowanie.md) |
| 9 | Content sprawy (`CaseAsset`) | ❌ Brak | Must-have | [content-sprawy.md](./mechanics/content-sprawy.md) |
| 10 | Prywatne widoki sieciowe (koordynator) | ❌ Brak | Must-have | [prywatne-widoki-sieciowe.md](./mechanics/prywatne-widoki-sieciowe.md) |
| 11 | Limit Rundy (timer) | ❌ Brak | Must-have | [limit-rundy.md](./mechanics/limit-rundy.md) |
| 12 | Egzekucja i stan po eliminacji | ❌ Brak (fundament: pistolet) | Must-have | [egzekucja.md](./mechanics/egzekucja.md) |
| 13 | UI Rundy (`RoundPresenter`) | ❌ Brak | Must-have | [ui-rundy.md](./mechanics/ui-rundy.md) |
| 14 | Głos Przestrzenny (Dissonance) | ❌ Brak (research gotowy) | Must-have | [glos-przestrzenny.md](./mechanics/glos-przestrzenny.md) |
| 15 | Drzwi i pomieszczenia | ❌ Brak | Must-have | [drzwi-i-pomieszczenia.md](./mechanics/drzwi-i-pomieszczenia.md) |
| 16 | Prywatne Przesłuchanie | ❌ Emergentna (15 + 14) | Must-have | opis niżej |
| 17 | Sekretne Cele | ❌ Brak | Should-have (po slice) | konfiguracja w [silnik-rundy.md](./mechanics/silnik-rundy.md) |
| 18 | Notatki Detektywa | ❌ Brak | Should-have (forma nierozstrzygnięta) | opis niżej |
| 19 | Dopracowana broń / prezentacja wyników | 🔶 Podstawy | Nice-to-have | — |
| 20 | Bunt i Sygnał Buntu | ❌ Brak | **Niezatwierdzona** — wymaga decyzji | [OPEN-QUESTIONS.md](./OPEN-QUESTIONS.md) |

## Co już mamy (fundament fizyczno-sieciowy)

Zaimplementowana jest cała warstwa „ciała" gry — bez żadnych reguł Rundy:

- **Ruch FPP** z synchronizacją Mirror (klient-autorytatywny) — [ruch-gracza.md](./mechanics/ruch-gracza.md).
- **Steam multiplayer**: lobby friends-only, invite przez nakładkę, automatyczny fallback na KCP do testów lokalnych — [lobby-i-siec.md](./mechanics/lobby-i-siec.md).
- **Framework interakcji** server-authoritative (zasięg + line-of-sight walidowane na serwerze) — [interakcja-z-obiektami.md](./mechanics/interakcja-z-obiektami.md).
- **Podnoszenie pistoletu do ręki** z poprawną synchronizacją i ochroną przed wyścigami — [podnoszenie-przedmiotow.md](./mechanics/podnoszenie-przedmiotow.md).
- **Strzelanie hitscan** rozstrzygane serwerowo, z hitboxami (`ShotHitbox` + event serwerowy), tracerami, impactami i dźwiękiem u wszystkich — [strzelanie-i-hitboxy.md](./mechanics/strzelanie-i-hitboxy.md).

Wspólny wniosek z audytu: wzorce sieciowe są zdrowe (serwer decyduje, klient wysyła intencje), ale **nic nie jest jeszcze podpięte do reguł gry**, bo reguły nie istnieją.

## Czego brakuje do grywalnego slice'a (must-have)

Brakuje całej warstwy „umysłu" gry — dokładnie tej opisanej w MVP-ARCHITECTURE:

1. **`RoundEngine`** — czysta domena: fazy (Lobby → Przygotowanie → Runda → Zakończona), niezmienniki ról, jedna Egzekucja, rozliczenie wyników. Wszystkie pozostałe mechaniki wpinają się w niego. [silnik-rundy.md](./mechanics/silnik-rundy.md)
2. **Role i Skład Rundy** — losowy, tajny przydział 1 Detektyw / 1 Winny / 2–4 Niewinnych. [role-i-sklad-rundy.md](./mechanics/role-i-sklad-rundy.md)
3. **Alibi** — trzy poziomy dostępu (pełne / zredagowane / brak) i zniknięcie po Przygotowaniu. [alibi-i-redagowanie.md](./mechanics/alibi-i-redagowanie.md)
4. **Content sprawy** — `CaseAsset` do ręcznego authoringu + niezmienny `CaseDefinition`. [content-sprawy.md](./mechanics/content-sprawy.md)
5. **Koordynator sieciowy** — celowane `PlayerRoundView` per gracz, zero sekretów w stanie globalnym. [prywatne-widoki-sieciowe.md](./mechanics/prywatne-widoki-sieciowe.md)
6. **Limit Rundy** — serwerowy zegar + rozstrzygnięcie w domenie. [limit-rundy.md](./mechanics/limit-rundy.md)
7. **Egzekucja** — rekomendacja: diegetycznie, strzałem z istniejącego pistoletu (trafienie gracza przez Detektywa → `Execute`); fallback: wybór z UI. [egzekucja.md](./mechanics/egzekucja.md)
8. **UI Rundy** — lobby ze startem, karta Przygotowania, HUD z timerem, ekran wyników. [ui-rundy.md](./mechanics/ui-rundy.md)
9. **Głos Przestrzenny** — Dissonance + własna akustyka portalowa (drzwi/pokoje). [glos-przestrzenny.md](./mechanics/glos-przestrzenny.md)
10. **Drzwi i pomieszczenia** — nowy `INetworkInteractable`, jawny stan `SyncVar`, portale dla akustyki. [drzwi-i-pomieszczenia.md](./mechanics/drzwi-i-pomieszczenia.md)

### Prywatne Przesłuchanie (mechanika emergentna)

Celowo **nie ma własnego systemu**: Prywatne Przesłuchanie to Detektyw + Podejrzany w małym pokoju za zamkniętymi drzwiami (ADR-0005/0009). Wymaga wyłącznie działających drzwi, pokoi i akustyki. Nie implementować żadnych „tur przesłuchań", zaproszeń ani teleportów do pokoju — jeśli kiedyś okaże się to potrzebne, najpierw decyzja użytkownika.

## Po slice (should-have, zatwierdzone co do zasady)

- **Sekretne Cele** — zatwierdzona reguła (właściciel wygrywa, gdy przetrwa i jego Cel zginie), ale liczba domyślna nierozstrzygnięta. Silnik ma przyjmować konfigurację `0..N`; MVP gra z `0`.
- **Notatki Detektywa** — zatwierdzone jako ręczny tekst bez treści Alibi (ADR-0008), forma UI (tablica / kartka / panel) nierozstrzygnięta. Do czasu decyzji Detektyw może notować poza grą.
- **FizzySteamworks jako domyślny transport w produkcji**, dalszy content spraw, dopracowana broń i prezentacja wyników — jawnie poza pierwszym slice'em (MVP-ARCHITECTURE).

## Nierozstrzygnięte (wymagają decyzji użytkownika — nie implementować bez niej)

Z [OPEN-QUESTIONS.md](./OPEN-QUESTIONS.md) oraz z tego audytu:

1. **Bunt i Sygnał Buntu** — cały mechanizm niezatwierdzony.
2. **Prezentacja Alibi** — lista faktów vs narracja (model danych rekomendowany: lista).
3. **Liczba Sekretnych Celów** — default i zależność od liczby graczy.
4. **Forma Notatek Detektywa.**
5. Nowe pytania z audytu: czy tylko Detektyw może podnieść pistolet; czy pudło konsumuje Egzekucję; co się dzieje przy rozłączeniu Detektywa/Winnego w trakcie Rundy; czy ekran wyników ujawnia pełne Alibi.

## Rekomendowana kolejność implementacji

Zgodna z MVP-ARCHITECTURE, uzupełniona o istniejący stan kodu:

1. `Domain` (`RoundEngine` + role + Alibi + Limit + Egzekucja jako reguły) z testami Edit Mode — czysty C#, zero ryzyka scenowego.
2. `CaseAsset` + jedna testowa Sprawa.
3. `NetworkRoundCoordinator` na KCP + mapowanie `PlayerId` + start Rundy z lobby.
4. `RoundPresenter` (karta Przygotowania, HUD, ekran wyników) + bramka stanu ruchu/kursoru.
5. Integracja Egzekucji z pistoletem (`ShotHitbox.HitReceivedServer` → `Execute`) + ograniczenie pickupu do Detektywa.
6. Test 2–3 klientów przez ParrelSync; potem test Steam na dwóch kontach.
7. Drzwi + pomieszczenia (poziom posterunku: przestrzeń wspólna + 2–3 pokoje przesłuchań).
8. Spike Dissonance na KCP → akustyka portalowa → test przez FizzySteamworks.
9. Po udanym slice: Sekretne Cele, Notatki Detektywa, content, polish.

## Uwagi dla agenta implementującego

- Reguły domenowe wyłącznie w `RoundEngine` (asmdef bez Unity/Mirror); sekrety nigdy w `SyncVar` — tylko celowane widoki (ADR-0011).
- Zmiany scen/prefabów przez Unity MCP, nie przez edycję YAML (AGENTS.md).
- Po zmianach C#: kompilacja + konsola; po zmianach sieciowych: test host+klient na KCP przed Steamem.
- Polskie terminy kanoniczne z CONTEXT.md (`Runda`, `Detektyw`, `Winny`, `Niewinny`, `Alibi`, `Egzekucja`, `Przygotowanie`, `Limit Rundy`, `Sekretny Cel`) obowiązują we wszystkich dokumentach i nazwach domenowych.
