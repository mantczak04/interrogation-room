# Role i Skład Rundy

**Status:** ❌ Do zaimplementowania (część `RoundEngine`)
**Priorytet:** Must-have (MVP)
**Docelowy kod:** `Assets/Scripts/Game/Domain/` (wewnątrz `RoundEngine`)

## Cel

Każda Runda potrzebuje poprawnego, tajnego przydziału ról: dokładnie jeden Detektyw, dokładnie jeden Winny, 2–4 Niewinnych, przy 4–6 graczach (balans główny: 5). Przydział jest sercem asymetrii informacyjnej — jego wyciek psuje całą Rundę.

## Zasada działania

### Walidacja Składu Rundy

- `StartRound` odrzuca skład < 4 lub > 6 graczy.
- Role przydziela `RoundEngine` z seeda (deterministycznie testowalne): 1 × Detektyw, 1 × Winny, reszta Niewinni.
- Warianty do rozważenia (decyzja użytkownika, nie MVP): Detektyw wybierany chętnym/rotacją zamiast losowo. MVP: pełna losowość z seeda.

### Tajność ról (ADR-0011)

- Pełna mapa ról istnieje **wyłącznie na hoście** wewnątrz `RoundEngine`.
- Klient dowiaduje się przez `PlayerRoundView` tylko tego, co wolno mu wiedzieć:
  - każdy zna **swoją** rolę;
  - wszyscy wiedzą, **kto jest Detektywem** (jawna funkcja społeczna);
  - nikt (poza hostem-serwerem) nie wie, kto jest Winnym; Niewinni też nie znają ról pozostałych Podejrzanych;
  - właściciel Sekretnego Celu zna rolę swojego Celu (Cel zawsze jest Niewinny — wynika z definicji w CONTEXT.md).
- **Zakaz techniczny:** rola nie może być `SyncVar` ani znajdować się w żadnym globalnie synchronizowanym stanie. Wyłącznie celowana wiadomość/`TargetRpc` z widokiem.

### Sekretne Cele (konfigurowalne, domyślnie wyłączone w MVP)

- Przydzielane wybranym Niewinnym; właściciel wygrywa tylko gdy sam przetrwa **i** jego Cel zostanie wyeliminowany.
- Winny nigdy nie jest Celem. Cel nie wie, że jest czyimś Celem.
- Liczba celów: parametr konfiguracji Rundy `0..N`; wartość domyślna nierozstrzygnięta ([OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md)) — MVP startuje z `0`.

## Zależności

- Mapowanie graczy lobby → `PlayerId` robi `NetworkRoundCoordinator` ([prywatne-widoki-sieciowe.md](./prywatne-widoki-sieciowe.md)); domena operuje wyłącznie na `PlayerId`.
- Redagowanie Alibi dla Winnego: [alibi-i-redagowanie.md](./alibi-i-redagowanie.md).

## Przypadki brzegowe

- Rozłączenie gracza w trakcie Rundy: MVP — Runda trwa dalej; rozłączony Podejrzany nie może zostać poddany Egzekucji? **Decyzja potrzebna**; najprostszy bezpieczny wariant: rozłączenie Winnego lub Detektywa kończy Rundę bez rozstrzygnięcia, rozłączenie Niewinnego nie przerywa Rundy.
- Duplikat `StartRound` podczas trwającej Rundy — odrzucany przez maszynę stanów.

## Kryteria akceptacji / testy

- 1000 losowych seedów × skład 4/5/6 → zawsze dokładnie 1 Detektyw i 1 Winny.
- `ViewFor(Niewinny)` nigdy nie zawiera roli innego Podejrzanego.
- `ViewFor(Detektyw)` nigdy nie zawiera żadnej treści Alibi ani roli Winnego.
- Skład 3 i 7 graczy odrzucony.
