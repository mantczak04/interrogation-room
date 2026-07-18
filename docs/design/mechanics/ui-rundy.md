# UI Rundy (`RoundPresenter`)

**Status:** 🔶 Kod i assety bazowego UI istnieją; pełne spięcie sceny oraz UI rozszerzenia pozostają do implementacji
**Priorytet:** Must-have (MVP) — krok 4 kolejności implementacji
**Docelowy kod:** `Assets/Scripts/Game/UI/RoundPresenter.cs`

## Cel

Cienka warstwa prezentacji: renderuje otrzymany `PlayerRoundView` i wysyła intencje gracza. **Nie interpretuje reguł i nie przechowuje sekretów innych graczy** — wszystko, co pokazuje, przyszło w jego własnym widoku.

## Zasada działania

### Ekrany / stany UI (sterowane fazą z widoku)

1. **Lobby**: po połączeniu scena `Room` automatycznie pokazuje pełnoekranowe lobby na tle menu głównego. Każdy gracz wybiera jedną z pięciu neutralnych postaci strzałkami i widzi jej podgląd idle; wybór jest publicznie synchronizowany przez należący do gracza `PlayerController`. Host widzi listę Spraw, zaproszenie Steam oraz przycisk „Start Rundy" aktywny przy 3–8 graczach.
2. **Przygotowanie**: karta z rolą gracza, jawnym Przestępstwem i właściwą wersją Alibi (Detektyw: rola + Przestępstwo + instrukcja, bez Alibi). Odliczanie do końca Przygotowania. Po zakończeniu karta znika trwale (ADR-0007).
3. **Runda (HUD)**: pozostały Limit Rundy, własna rola (dyskretnie), jawne Przestępstwo (podręcznie, np. pod klawiszem), prompt interakcji „[E]" oraz prywatny podgląd aktualnego kroku Celu. Detektyw zamiast Celu ma prywatny Rejestr Incydentów.
4. **Wynik**: przyczyna zakończenia, role, Prywatne Cele i ich postęp, właściciel i Cel Wrobienia, działania Planu Ucieczki, zdobyte Tropy do Alibi, prawdziwi autorzy Incydentów oraz indywidualne wyniki. Przycisk powrotu do lobby u hosta.

### Zasady

- UI czyta wyłącznie własny `PlayerRoundView`; zmiana widoku = przerysowanie. Zero lokalnych wniosków o cudzych rolach.
- Intencje (start, koniec Przygotowania, Egzekucja z fallbacku UI) idą do `NetworkRoundCoordinator`; serwer i tak waliduje, UI tylko ukrywa niedozwolone akcje.
- Alibi po Przygotowaniu: komponent niszczy treść (nie `SetActive(false)`).
- Prywatny Cel pozostaje dostępny właścicielowi podczas Rundy, ale klient nie otrzymuje cudzych Celów ani postępu. Nie istnieje funkcja mechanicznego pokazania własnego Celu innemu graczowi.
- Rejestr Incydentów pokazuje Detektywowi skutek, miejsce i czas zgłoszenia lub odkrycia, bez sprawcy, roli, motywu i rzeczywistego czasu cichej akcji.
- Kursor: odblokowany na kartach pełnoekranowych (Lobby, Przygotowanie, Wynik), zablokowany w HUD Rundy — wymaga koordynacji z `PlayerController`.

## Zależności

- `PlayerRoundView` (jedyne źródło danych), `NetworkRoundCoordinator` (intencje), bramka stanu ruchu ([ruch-gracza.md](./ruch-gracza.md)), prompt interakcji ([interakcja-z-obiektami.md](./interakcja-z-obiektami.md)).

## Kryteria akceptacji

- Trzy instancje (Detektyw + 2 Podejrzanych) pokazują różne, poprawne karty Przygotowania.
- Po `EndPreparation` nie da się w żaden sposób wrócić do treści Alibi (także po reconnect).
- Ekran wyników pokazuje różne wyniki indywidualne Niewinnych (gdy jeden zginął).
- Właściciel widzi wyłącznie aktualny krok własnego Prywatnego Celu, a Detektyw widzi wyłącznie dozwolone wpisy Rejestru Incydentów.
- Po Rundzie ujawnienie odtwarza prawdziwy przebieg Celów, Incydentów, Tropów i Ucieczki bez wycieku tych danych wcześniej.

## Otwarte pytania

- Forma Notatek Detektywa (tablica/kartka/panel) — nierozstrzygnięta, poza MVP; slice zakłada notatki poza grą (kartka papieru u gracza) albo najprostszy panel tekstowy dopiero po decyzji.
- Prezentacja Alibi: lista vs narracja ([alibi-i-redagowanie.md](./alibi-i-redagowanie.md)).
- Forma podglądu Prywatnego Celu oraz to, czy wynik ujawnia pełne Alibi ([OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md)).
