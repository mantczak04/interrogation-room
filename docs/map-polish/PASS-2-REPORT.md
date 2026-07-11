# Posterunek — raport Pass 2

Data: 2026-07-11  
Scena: `Assets/Scenes/Room.unity`  
Branch: `feature/map-pass2`

## Wynik

Pass 2 przebudował odbiór ścian i sufitów bez zmiany zamrożonego obrysu pomieszczeń, położeń i szerokości drzwi ani przestrzeni nawigacji. Architektura ma dwustronną lamperię, listwy rozdzielające, gzymsy, głębokie portale z ościeżami, podwieszany sufit kasetonowy korytarza, belki i obniżenia charakterystyczne dla pomieszczeń oraz funkcjonalne pilastry przy oprawach.

Duże płaszczyzny używają drobnego proceduralnego mikroreliefu oraz niezależnej, lokalnie zmiennej mapy smoothness. Końcowe wartości zostały dostrojone na screenach z wysokości oczu: skala normal mapy `14x`, siła `0,11–0,14`; odpowiedź pozostaje matowa i nie przypomina skały ani plastiku.

Atmosfera pozostaje ciemna, ale czytelna. Dwanaście aktywnych świateł baked rzuca miękkie cienie, a pięć dyskretnych wall-washerów wydobywa profile i fakturę. Sala i Pokój Socjalny są cieplejsze, Archiwum chłodniejsze, a Pokój Przesłuchań zachowuje surowszy charakter.

## Przyczyna i naprawa przecieku światła

Artefakt wynikał ze zbiegu trzech warunków:

1. trzy niebieskie oprawy korytarza były baked, ale miały `shadows=None`;
2. ściany kończyły się dokładnie na spodzie sufitu (`y=3,0`), bez zapasu dla lightmapy;
3. pozostawały trzy stare realtime fill lights korytarza.

Naprawa nie maskuje pasa listwą ani materiałem. Pełne ściany zachodzą teraz 10 cm w bryłę sufitu (`max y=3,10`), stare realtime fill lights są wyłączone, a wszystkie 12 aktywnych świateł baked ma miękkie cienie. Po każdym etapie wykonano świeży bake. Końcowy test pięciu klas przegród, z obu stron na wysokości styku ściana–sufit, przeszedł `5/5`; nie wykryto otwartej drogi przez żadną przegrodę.

## Iteracje

### Iteracja 1 — szczelna bryła i warstwy architektury

- Wprowadzono światłoszczelne połączenia ściana–sufit oraz prawidłowe baked shadows.
- Dodano dwustronną lamperię, listwy, gzymsy i głębokie, profilowane portale.
- Krytyka capture: geometria zaczęła czytać się architektonicznie, ale normal mapa była zbyt mocna i wyglądała jak skała; sufit korytarza pozostawał płaski.
- Commit: `a89e3d2`.

### Iteracja 2 — mikrorelief, roughness i sufity

- Zmniejszono siłę normal mapy i zwiększono jej skalę; dodano lokalnie zmienną mapę smoothness.
- Korytarz otrzymał obniżony sufit kasetonowy z rusztem; Sala, Pokój Przesłuchań i pozostałe przestrzenie dostały odrębne uskoki i belki.
- Krytyka capture: materiał stał się wiarygodnym malowanym tynkiem, lecz faktura potrzebowała kontrolowanego światła bocznego.
- Commit: `02b499d`.

### Iteracja 3 — światłocień i końcowa regresja

- Dodano pięć baked wall-washerów o barwie zależnej od pomieszczenia.
- Przyciemniono sufit, zachowując lokalne plamy światła i czytelne sylwetki.
- Ujednolicono wszystkie aktywne baked lights do miękkich cieni i wykonano końcowy bake.
- Końcowy capture potwierdził czytelną hierarchię lamperii, portali, gzymsów, sufitów i lokalnych akcentów światła.

## Weryfikacja końcowa

- Physics gauntlet: `PASS` — spawny `6/6`, drzwi i trasy `5/5`, strefy `5/5`; kapsuła `r=0,45`, bez wejścia w Play Mode.
- Koplanarność: `PASS` — `0` niedozwolonych przecięć równoległych powierzchni architektury i ścian.
- Light-leak regression: `PASS` — `5/5` klas przegród sprawdzonych z obu stron.
- Baked occlusion: `PASS` — `12/12` aktywnych świateł baked ma cienie.
- Bake: `PASS` — świeży, 1 atlas lightmapy.
- Budżet: `PASS` — 13 158 trójkątów, 49 unikalnych nazw materiałów, 0 decali.
- Hierarchia i zapis sceny przez Unity MCP: `PASS`.
- Console Error po wyczyszczeniu pre-existing błędów inicjalizacji Vivox i 5 s obserwacji: `PASS` — 0 nowych wpisów.
- `git diff --check`: uruchamiany po iteracjach; Unity serializer pozostawia końcowe spacje w polach `m_Name:` sceny. Nie poprawiano ich ręcznie, ponieważ surowa edycja YAML sceny jest zabroniona przez `AGENTS.md`.
- Play Mode: nie uruchamiano, zgodnie z briefem.

## Screeny przed/po

Punkt odniesienia znajduje się w `docs/map-polish/screenshots/baseline/`. Końcowy zestaw z wysokości oczu znajduje się w `docs/map-polish/screenshots/pass-2-final/`:

- Sala Wspólna: `FINAL_Sala_A.png`, `FINAL_Sala_B.png`
- Pokój Przesłuchań: `FINAL_Przesl_A.png`, `FINAL_Przesl_B.png`
- Korytarz: `FINAL_Korytarz_A.png`, `FINAL_Korytarz_B.png`
- Pokój Socjalny: `FINAL_Socjalny_A.png`, `FINAL_Socjalny_B.png`
- Archiwum: `FINAL_Archiwum_A.png`, `FINAL_Archiwum_B.png`
- Widoki przez każde drzwi: cztery pliki `FINAL_Door_*.png`

Materiały diagnostyczne kolejnych etapów pozostają w `pass-2-iteration-01/` i `pass-2-iteration-02/`.
