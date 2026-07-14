# Przegląd mechanik gry

Aktualna mapa mechanik „Przesłuchania”: wdrożony fundament, prace potrzebne do spięcia pierwszego grywalnego vertical slice oraz zatwierdzone rozszerzenie rozgrywki. Źródłami prawdy są [CONTEXT.md](../../CONTEXT.md), [MVP-ARCHITECTURE.md](../architecture/MVP-ARCHITECTURE.md), [ADR-y](../adr/) i specyfikacje w [`docs/design/mechanics/`](./mechanics/).

## Tabela zbiorcza

| # | Mechanika | Status | Następny krok | Specyfikacja |
|---|---|---|---|---|
| 1 | Ruch gracza (FPP) | ✅ Zaimplementowany, ma znane luki | Weryfikacja w pełnej Rundzie | [ruch-gracza.md](./mechanics/ruch-gracza.md) |
| 2 | Lobby i sieć (Steam + KCP) | ✅ Zaimplementowane, ma znane luki | Test pełnej Rundy host + klienci | [lobby-i-siec.md](./mechanics/lobby-i-siec.md) |
| 3 | Interakcja z obiektami („E”) | ✅ Fundament zaimplementowany | Rozszerzyć o działania trwające i przerywanie | [interakcja-z-obiektami.md](./mechanics/interakcja-z-obiektami.md) |
| 4 | Podnoszenie przedmiotów | ✅ Fundament pistoletu | Zapewnić pistolet Detektywowi od początku Rundy | [podnoszenie-przedmiotow.md](./mechanics/podnoszenie-przedmiotow.md) |
| 5 | Strzelanie i hitboxy | ✅ Fundament zaimplementowany | Spiąć pierwsze trafienie z Egzekucją | [strzelanie-i-hitboxy.md](./mechanics/strzelanie-i-hitboxy.md) |
| 6 | Silnik Rundy (`RoundEngine`) | ✅ Bazowy slice zaimplementowany | Rozszerzyć o Cele i Ucieczkę | [silnik-rundy.md](./mechanics/silnik-rundy.md) |
| 7 | Role i Skład Rundy | ✅ Bazowe role zaimplementowane | Dodać Prywatne Cele | [role-i-sklad-rundy.md](./mechanics/role-i-sklad-rundy.md) |
| 8 | Alibi i redagowanie | ✅ Bazowe reguły zaimplementowane | Dodać Tropy do Alibi | [alibi-i-redagowanie.md](./mechanics/alibi-i-redagowanie.md) |
| 9 | Content Sprawy (`CaseAsset`) | ✅ Bazowy model i content istnieją | Rozszerzyć Sprawy o Tropy | [content-sprawy.md](./mechanics/content-sprawy.md) |
| 10 | Prywatne widoki sieciowe | ✅ Bazowy koordynator istnieje | Spiąć i zweryfikować end-to-end | [prywatne-widoki-sieciowe.md](./mechanics/prywatne-widoki-sieciowe.md) |
| 11 | Limit Rundy | ✅ Reguła domenowa istnieje | Spiąć zegar runtime i UI | [limit-rundy.md](./mechanics/limit-rundy.md) |
| 12 | Egzekucja | 🔶 Reguła i fundament broni istnieją | Spiąć rolę, trafienie i koniec Rundy | [egzekucja.md](./mechanics/egzekucja.md) |
| 13 | UI Rundy (`RoundPresenter`) | 🔶 Kod i assety istnieją | Dokończyć wiring sceny i przepływ Rundy | [ui-rundy.md](./mechanics/ui-rundy.md) |
| 14 | Głos Przestrzenny | 🔶 Istnieje wcześniejszy spike Vivox; docelowy kierunek wymaga spike'a | Dissonance + akustyka drzwi | [glos-przestrzenny.md](./mechanics/glos-przestrzenny.md) |
| 15 | Drzwi i pomieszczenia | ❌ Brak docelowej implementacji | Zbudować podstawę przestrzennej prywatności | [drzwi-i-pomieszczenia.md](./mechanics/drzwi-i-pomieszczenia.md) |
| 16 | Prywatne Przesłuchanie | ◇ Mechanika emergentna | Wynika z przestrzeni, drzwi i Głosu | opis niżej |
| 17 | Prywatne Cele, Incydenty i Ucieczka | ✅ Koncepcja zatwierdzona, kodu brak | Implementować po spięciu bazowego slice | [prywatne-cele-incydenty-i-ucieczka.md](./mechanics/prywatne-cele-incydenty-i-ucieczka.md) |
| 18 | Notatki Detektywa | 🔶 Zasada zatwierdzona, forma UI otwarta | Decyzja po prototypie UI | [OPEN-QUESTIONS.md](./OPEN-QUESTIONS.md) |
| 19 | Ujawnienie po Rundzie | 🔶 Zakres informacji zatwierdzony | Zaprojektować prezentację | [ui-rundy.md](./mechanics/ui-rundy.md) |
| 20 | Bunt | ◇ Mechanika emergentna | Bez osobnego systemu i tasku implementacyjnego | [ADR-0013](../adr/0013-private-goals-and-emergent-rebellion.md) |

Legenda: ✅ działa albo decyzja jest zatwierdzona; 🔶 część istnieje lub wymaga spięcia; ❌ brak implementacji; ◇ zachowanie emergentne bez osobnego systemu.

## Co już mamy

Repozytorium zawiera fundament fizyczno-sieciowy oraz bazową domenę Rundy:

- ruch FPP, lobby Steam z fallbackiem KCP i serwerowo walidowane interakcje;
- pistolet, serwerowy hitscan i hitboxy;
- `RoundEngine`, role, redagowanie Alibi, Limit Rundy i bazowe rozliczenie Egzekucji;
- `CaseAsset` → niezmienny `CaseDefinition` oraz prywatne `PlayerRoundView`;
- `NetworkRoundCoordinator` i `RoundPresenter` jako bazowe adaptery sieci i UI;
- testy Edit Mode dla domeny, contentu, serializacji widoków i prezentera.

To nie oznacza jeszcze gotowej pętli gry w scenie. Najważniejsze braki bazowego slice to spięcie istniejących modułów end-to-end, Egzekucja przez pistolet, runtime timera, pełny przepływ UI oraz weryfikacja hosta z klientami.

## Pierwszy grywalny vertical slice

Zakres i kolejność bazowego slice pozostają opisane w [MVP-ARCHITECTURE.md](../architecture/MVP-ARCHITECTURE.md). Najbliższy cel to jedna kompletna Runda:

1. host uruchamia Rundę dla 4–6 graczy;
2. każdy dostaje wyłącznie własną rolę i wersję Alibi;
3. po Przygotowaniu Alibi znika, gracze poruszają się swobodnie podczas jednego Limitu Rundy;
4. Detektyw ma pistolet, a pierwsze trafienie żywego Podejrzanego jest jedyną Egzekucją;
5. wszyscy otrzymują spójny wynik po Egzekucji albo upływie czasu;
6. całość przechodzi lokalny test host + klienci na KCP przed testem Steam.

### Prywatne Przesłuchanie

Prywatne Przesłuchanie celowo nie ma własnego trybu ani blokady drzwi. Detektyw i Podejrzany rozmawiają w wybranym pokoju, a reszta graczy pozostaje aktywna. Podejrzany może opuścić rozmowę w dowolnym momencie. Prywatność tworzą dystans, pomieszczenia, drzwi i Głos Przestrzenny.

## Następny filar: Prywatne Cele, Incydenty i Ucieczka

Po spięciu bazowego slice wdrażamy [zatwierdzoną specyfikację](./mechanics/prywatne-cele-incydenty-i-ucieczka.md):

- każdy Niewinny potrzebuje dokładnie jednego Prywatnego Celu i Przetrwania;
- Osobista Sprawa jest wariantem domyślnym, a Sekretny Cel ją zastępuje;
- przy 4 graczach Sekretny Cel jest wyłączony, przy 5–6 domyślnie występuje jeden, a host może ustawić zero;
- działania są konkretne i obserwowalne, ale nie potwierdzają motywu ani roli;
- Hałaśliwe Incydenty zgłaszają się od razu, Ciche wymagają osobistego odkrycia przez Detektywa;
- Winny może mieszać zdobywanie Tropów do Alibi z przygotowaniem Ucieczki;
- skuteczna Ucieczka kończy Rundę, a przerwana próba wymaga dodatkowego kroku albo innego wyjścia;
- indywidualne wyniki tworzą emergentny Bunt bez hasła, przycisku, fazy i dedykowanej akcji.

## Otwarte decyzje i strojenie

Nie implementować bez dalszej decyzji użytkownika:

1. forma Notatek Detektywa;
2. prezentacja Alibi podczas Przygotowania;
3. fizyczna forma podglądu Prywatnego Celu;
4. ujawnienie pełnego Alibi po Rundzie;
5. zachowanie przy rozłączeniu Detektywa albo Winnego.

Do playtestu, bez zmiany zatwierdzonego rdzenia: czasy działań i Ucieczki, liczba punktów ukrycia i wyjść, trudność Tropów, ewentualne dwa Sekretne Cele przy sześciu graczach oraz potrzeba powiększenia mapy. Pełna lista znajduje się w [OPEN-QUESTIONS.md](./OPEN-QUESTIONS.md).

## Rekomendowana kolejność prac

1. Dokończyć bazowy vertical slice: wiring koordynatora, prezentera, timera i startu Rundy.
2. Spiąć pistolet Detektywa z Egzekucją i zweryfikować zakończenie Rundy.
3. Przetestować host + klienci na KCP, potem Steam.
4. Zbudować drzwi, pomieszczenia i spike Głosu Przestrzennego.
5. Wdrożyć Prywatne Cele i ich prywatne widoki w domenie oraz sieci.
6. Dodać trwające interakcje, trwałe zmiany świata i Incydenty.
7. Dodać Tropy do Alibi, Plan Ucieczki i finałową Ucieczkę.
8. Dodać pełne ujawnienie po Rundzie, przeprowadzić playtesty i dopiero wtedy stroić mapę oraz parametry.

## Uwagi dla implementacji

- Reguły domenowe należą wyłącznie do `RoundEngine`; sekrety nigdy nie trafiają do globalnych `SyncVar`.
- `NetworkRoundCoordinator` pozostaje jedynym adapterem mapującym połączenia na `PlayerId` i dostarczającym prywatne widoki.
- Interakcje scenowe zgłaszają intencje i renderują animacje, ale nie rozstrzygają Celów ani zwycięstwa.
- Zmiany scen i prefabów wykonujemy przez Unity MCP, nie przez ręczną edycję YAML.
- Obowiązują kanoniczne polskie terminy z [CONTEXT.md](../../CONTEXT.md).
