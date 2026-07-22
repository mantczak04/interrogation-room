# Regresja zadań i sandboxa — 2026-07-21

## Zakres

- role `Niewinny` i `Winny`;
- wszystkie fizyczne etapy Osobistej Sprawy, Sekretnego Celu, Tropu do Alibi i Planu Ucieczki;
- minigierki Przeszukiwanie akt, Terminal kartoteki i Zamek szyfrowy;
- ukończenie, przerwanie, reset, następne zadanie i świeża druga Runda;
- prywatne widoki i filtrowanie sekretów przez hosta;
- widoczność kotwic zadań oraz oznaczenia pomieszczeń w scenie `Room`.

## Znalezione i naprawione problemy

### Sandbox wybierał tylko całe scenariusze

**Problem:** panel nie pozwalał rozpocząć konkretnego etapu ani pojedynczej minigierki. Test końcowego kroku wymagał ręcznego powtarzania całego łańcucha.

**Poprawka:** dodano wybór roli i jedenastu etapów. Sandbox autorytatywnie przygotowuje wcześniejsze kroki na hoście i rozpoczyna wybrany etap bez zmiany reguł Rundy.

**Retest:** host uruchomił każdy etap. Bieżący krok i przygotowane wyjście odpowiadały wyborowi.

### Brak resetu i przejścia do następnego zadania

**Problem:** reset świata był dostępny dopiero po zakończeniu Rundy.

**Poprawka:** dodano `Reset zadania` i `Następne zadanie`. Reset zachowuje rolę i etap; następne zadanie przechodzi cyklicznie w obrębie roli.

**Retest:** reset Osobistej Sprawy przywrócił krok `osobista-sprawa-przygotuj`. Kolejne zadania Niewinnego i Winnego przygotowały oczekiwane kroki bez pozostałości poprzedniego stanu.

### Pominięty krok nie przygotowywał fizycznego przedmiotu

**Problem:** wybór końcowego etapu Osobistej Sprawy albo Wrobienia przesuwał stan domeny, ale pozostawiał wymagany przedmiot w miejscu startowym. Slot docelowy nie był od razu testowalny.

**Poprawka:** host po przygotowaniu etapu zleca `RoundPhysicalActionBinder` przekazanie kontrolowanemu graczowi odpowiednio `personal-document` albo `suspicious-token` przez istniejący serwerowy system noszenia.

**Retest:** działający host rozpoczyna oba etapy z właściwym bieżącym krokiem i wymaganym przedmiotem niesionym przez kontrolowanego gracza. Reset nadal zwraca przedmiot do stanu początkowego.

### Brak powrotu do wyboru drugiej roli

**Problem:** po rozpoczęciu zadania nie dało się wybrać drugiej roli bez sztucznego zakończenia Rundy.

**Poprawka:** dodano `Wybór roli i zadania`, który resetuje domenę i fizyczny świat do lobby.

**Retest:** aktywny plan i wybrane zadanie zostały wyczyszczone, a host wrócił do listy.

### Regały zasłaniały kotwice w Archiwum

**Problem:** cztery dekoracyjne regały były osadzone w tylnej ścianie Archiwum i wizualnie nakładały się na strefę interaktywnych obiektów.

**Poprawka:** usunięto wyłącznie `Archiwum_Regal1`, `Archiwum_Regal2`, `Archiwum_Regal3` i `Archiwum_RegalW`. Zachowano wszystkie 16 obiektów `RoundPhysicalIntegration`.

**Retest:** hierarchia nie zawiera regałów, a wszystkie kotwice B4/B5 nadal istnieją i są aktywne.

### Tabliczki nie zawierały nazw pomieszczeń

**Problem:** cztery istniejące tabliczki miały tylko abstrakcyjne piktogramy.

**Poprawka:** wpisano `Sala Wspólna`, `Pokój Przesłuchań`, `Pokój Socjalny` i `Archiwum`. Piąta tabliczka dla Korytarza nie istnieje, więc zgodnie z zakresem nie została dodana.

**Retest:** tekst został sprawdzony z obu stron korytarza w podglądzie kamery. Polskie znaki są renderowane poprawnie.

## Wyniki przejścia

| Obszar | Wynik |
| --- | --- |
| Osobista Sprawa: przygotowanie i zakończenie | zaliczone |
| Sekretny Cel: zabranie i podłożenie | zaliczone |
| Trop do Alibi | zaliczone |
| Plan Ucieczki: dwa kroki wspólne | zaliczone |
| Przygotowanie i finał Service Vent | zaliczone |
| Przygotowanie i finał Loading Gate Exit | zaliczone |
| Przeszukiwanie akt | zaliczone |
| Terminal kartoteki | zaliczone |
| Zamek szyfrowy | zaliczone |
| Przerwanie i niezależność wyjść | zaliczone |
| Reset i świeża druga Runda | zaliczone |
| Prywatne payloady roli, Celu i Planu | zaliczone |

## Dowody

- Edit Mode: 141 testów domeny, planera sandboxa, prywatnych payloadów i minigierek przeszło.
- Play Mode: 11 testów fizycznych interakcji, przygotowania niesionych przedmiotów, przerwania, resetu i bramki czasu minigierki przeszło.
- Działający lokalny host: reset, pełny cykl czterech zadań Niewinnego, pełny cykl siedmiu zadań Winnego i powrót do wyboru przeszły.
- Scena `Room`: zapisana przez Unity MCP; cztery etykiety istnieją, cztery regały nie istnieją, wszystkie kotwice B4/B5 pozostały aktywne.

Rotacje instancji prefabów B4/B5 sprawdzono względem ścian i dostępnego wnętrza pomieszczeń. Nie znaleziono odwróconej, przechylonej ani skierowanej w ścianę kotwicy wymagającej zmiany.
