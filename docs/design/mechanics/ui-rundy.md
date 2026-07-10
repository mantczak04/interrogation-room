# UI Rundy (`RoundPresenter`)

**Status:** ❌ Do zaimplementowania
**Priorytet:** Must-have (MVP) — krok 4 kolejności implementacji
**Docelowy kod:** `Assets/Scripts/Game/UI/RoundPresenter.cs`

## Cel

Cienka warstwa prezentacji: renderuje otrzymany `PlayerRoundView` i wysyła intencje gracza. **Nie interpretuje reguł i nie przechowuje sekretów innych graczy** — wszystko, co pokazuje, przyszło w jego własnym widoku.

## Zasada działania

### Ekrany / stany UI (sterowane fazą z widoku)

1. **Lobby**: lista graczy, przycisk „Start Rundy" u hosta (aktywny przy 4–6 graczach). Dziś istnieje tylko `CenteredNetworkManagerHUD` (połączenie) — start Rundy trzeba dodać.
2. **Przygotowanie**: karta z rolą gracza, jawnym Przestępstwem i właściwą wersją Alibi (Detektyw: rola + Przestępstwo + instrukcja, bez Alibi). Odliczanie do końca Przygotowania. Po zakończeniu karta znika trwale (ADR-0007).
3. **Runda (HUD)**: pozostały Limit Rundy, własna rola (dyskretnie), jawne Przestępstwo (podręcznie, np. pod klawiszem), prompt interakcji „[E]", ewentualny Sekretny Cel właściciela.
4. **Wynik**: kto został poddany Egzekucji, rola ofiary, wynik Detektywa, indywidualny wynik lokalnego gracza (Przetrwanie / Sekretny Cel). Przycisk powrotu do lobby u hosta.

### Zasady

- UI czyta wyłącznie własny `PlayerRoundView`; zmiana widoku = przerysowanie. Zero lokalnych wniosków o cudzych rolach.
- Intencje (start, koniec Przygotowania, Egzekucja z fallbacku UI) idą do `NetworkRoundCoordinator`; serwer i tak waliduje, UI tylko ukrywa niedozwolone akcje.
- Alibi po Przygotowaniu: komponent niszczy treść (nie `SetActive(false)`).
- Kursor: odblokowany na kartach pełnoekranowych (Lobby, Przygotowanie, Wynik), zablokowany w HUD Rundy — wymaga koordynacji z `PlayerController`.

## Zależności

- `PlayerRoundView` (jedyne źródło danych), `NetworkRoundCoordinator` (intencje), bramka stanu ruchu ([ruch-gracza.md](./ruch-gracza.md)), prompt interakcji ([interakcja-z-obiektami.md](./interakcja-z-obiektami.md)).

## Kryteria akceptacji

- Trzy instancje (Detektyw + 2 Podejrzanych) pokazują różne, poprawne karty Przygotowania.
- Po `EndPreparation` nie da się w żaden sposób wrócić do treści Alibi (także po reconnect).
- Ekran wyników pokazuje różne wyniki indywidualne Niewinnych (gdy jeden zginął).

## Otwarte pytania

- Forma Notatek Detektywa (tablica/kartka/panel) — nierozstrzygnięta, poza MVP; slice zakłada notatki poza grą (kartka papieru u gracza) albo najprostszy panel tekstowy dopiero po decyzji.
- Prezentacja Alibi: lista vs narracja ([alibi-i-redagowanie.md](./alibi-i-redagowanie.md)).
