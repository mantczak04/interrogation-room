# Role i Skład Rundy

**Status:** ✅ Bazowe role zaimplementowane; przydział Prywatnych Celów zatwierdzony do rozszerzenia
**Priorytet:** Must-have (MVP)
**Docelowy kod:** `Assets/Scripts/Game/Domain/` (wewnątrz `RoundEngine`)

## Cel

Każda Runda potrzebuje poprawnego, tajnego przydziału ról: dokładnie jeden Detektyw, dokładnie jeden Winny, 1–6 Niewinnych, przy 3–8 graczach (balans główny: 5). Przydział jest sercem asymetrii informacyjnej — jego wyciek psuje całą Rundę.

## Zasada działania

### Walidacja Składu Rundy

- `StartRound` odrzuca skład < 3 lub > 6 graczy.
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

### Prywatne Cele Niewinnych

- Każdy Niewinny zawsze otrzymuje dokładnie jeden Prywatny Cel i wygrywa wyłącznie po jego ukończeniu oraz osiągnięciu Przetrwania.
- Podstawowym wariantem jest Osobista Sprawa. Sekretny Cel zastępuje Osobistą Sprawę, zamiast być dodatkowym zadaniem.
- Sekretny Cel wymaga ukończenia dwukrokowego Wrobienia, Egzekucji wskazanego Celu i Przetrwania właściciela.
- Winny nigdy nie jest Celem. Cel nie wie, że jest czyimś Celem; właściciel wie, że wskazana osoba jest Niewinna.
- Przy 3–4 graczach konfiguracja wymusza `0` Sekretnych Celów. Przy 5–6 domyślnie przydzielany jest `1`, a host może wyłączyć go w lobby. Ewentualne `2` przy 6 graczach pozostaje parametrem do przyszłego playtestu.
- Przypisanie, postęp i ukończenie zna wyłącznie właściciel oraz host. Pełne reguły opisuje [specyfikacja Prywatnych Celów](./prywatne-cele-incydenty-i-ucieczka.md).

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
- Przy 3–4 graczach próba ustawienia Sekretnego Celu jest normalizowana albo odrzucana zgodnie z kontraktem konfiguracji, ale nigdy nie prowadzi do przydziału.
- Przy 5–6 graczach konfiguracja domyślna przydziela dokładnie jeden Sekretny Cel, a ustawienie hosta pozwala uruchomić Rundę z zerem.
