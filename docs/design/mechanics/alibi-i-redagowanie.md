# Alibi i redagowanie faktów

**Status:** ❌ Do zaimplementowania (część `RoundEngine` + prezentacja)
**Priorytet:** Must-have (MVP)
**Docelowy kod:** `Assets/Scripts/Game/Domain/` (dane i filtrowanie), `Assets/Scripts/Game/UI/` (prezentacja)

## Cel

Alibi to wspólna, prawdziwa wersja wydarzeń — punkt odniesienia dla wszystkich zeznań. Cała gra informacyjna opiera się na trzech poziomach dostępu (ADR-0006, ADR-0008):

- **Niewinny** widzi pełne Alibi,
- **Winny** widzi tę samą treść z ukrytymi wybranymi faktami (wie, **że** czegoś nie wie, ale nie wie czego),
- **Detektyw** nie widzi Alibi w żadnej postaci i rekonstruuje je wyłącznie z zeznań.

## Zasada działania

### Model danych

- Alibi jest częścią `CaseDefinition` ([content-sprawy.md](./content-sprawy.md)) jako **uporządkowana lista faktów** (rekomendacja robocza — patrz „Otwarte pytania").
- Każdy fakt ma stabilny identyfikator oraz flagę `możliwyDoUkrycia` ustawianą przez autora sprawy (nie każdy fakt nadaje się do ukrycia).
- Sprawa definiuje, ile faktów ukrywa się Winnemu (stała lub zakres losowany z seeda Rundy).

### Redagowanie dla Winnego

- Wybór ukrytych faktów wykonuje `RoundEngine` przy `StartRound` z seeda, wyłącznie spośród faktów `możliwyDoUkrycia`.
- Widok Winnego zawiera fakty jawne + **markery ukrycia** w miejscach faktów ukrytych (Winny widzi strukturalną „dziurę", np. „[fragment niedostępny]") — to część napięcia: musi improwizować tam, gdzie nie zna prawdy.
- Zestaw ukrytych faktów nigdy nie opuszcza hosta inaczej niż jako już zredagowany widok Winnego.

### Cykl życia dostępu (ADR-0007)

1. **Przygotowanie**: `ViewFor(Podejrzany)` zawiera odpowiednią wersję Alibi. UI pokazuje kartę Alibi.
2. **Koniec Przygotowania**: widoki przestają zawierać Alibi. UI ma obowiązek zniszczyć lokalną kopię (nie tylko schować) — a przede wszystkim serwer po prostu nigdy więcej jej nie wysyła, więc reconnect/late-join też nie odzyska treści.
3. **Runda i po Rundzie**: żaden Podejrzany nie może ponownie otworzyć Alibi. (Czy pokazywać pełne Alibi na ekranie wyników — decyzja użytkownika; domyślnie nie, dopóki nie zapadnie.)

### Przestępstwo (jawne)

- Treść Przestępstwa jest jawna dla wszystkich przez cały czas — wchodzi do publicznej części każdego widoku i może wisieć w HUD.

## Zależności

- `CaseDefinition` (dane), `RoundEngine` (filtrowanie i losowanie), `PlayerRoundView` (transport), `RoundPresenter` (karta Alibi w Przygotowaniu).

## Kryteria akceptacji / testy

- Winny widzi dokładnie skonfigurowaną liczbę braków i tylko wśród faktów `możliwyDoUkrycia`.
- Suma: fakty widoczne dla Winnego + ukryte = pełne Alibi Niewinnego (ta sama treść bazowa, ADR-0006).
- Po `EndPreparation` żadne wywołanie `ViewFor` nie zwraca treści Alibi.
- Widok Detektywa w żadnej fazie nie zawiera ani faktów, ani markerów ukrycia, ani liczby faktów.

## Otwarte pytania

- **Forma prezentacji** (lista faktów vs akapit narracyjny) — nierozstrzygnięta ([OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md)). Model „lista faktów" jest rekomendowany technicznie (łatwe ukrywanie, modularność), ale ostateczna forma UI wymaga playtestu. Model danych listowy nie blokuje narracyjnego renderowania.
- Długość Przygotowania i czy kończy ją host ręcznie, timer, czy „gotowość" wszystkich — do decyzji przy [ui-rundy.md](./ui-rundy.md); MVP: przycisk hosta + timer bezpieczeństwa.
