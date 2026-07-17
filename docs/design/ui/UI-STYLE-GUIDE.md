# Przewodnik stylu UI — „Przesłuchanie"

- **Status:** Obowiązujący — źródło prawdy dla redesignu UI z `FABLE-PLAYTEST-IMPROVEMENTS.md`, sekcja 1.
- **Nadrzędne dokumenty:** [ART-DIRECTION.md](../graphics/ART-DIRECTION.md) (paleta świata, zakazy), [CONTEXT.md](../../../CONTEXT.md) (terminy domenowe).
- **Stack:** Unity UI Toolkit (UXML/USS) dla UI Rundy; uGUI dla menu głównego. Oba stosują te same wartości.

## Metafora przewodnia: akta sprawy

Cały UI udaje policyjną papierologię lat 80–90: teczki, formularze, maszynopis, stemple, przypinane notatki. Gracz nie ogląda „okien gry", tylko dokumenty na posterunku. Research:

- **Papers, Please** — dokument jako interfejs: stempel zamiast przycisku „OK", papier niesie stan gry. Stąd stemple na kartach ról i ekranie wyniku.
- **Return of the Obra Dinn** — skrajnie oszczędna, niemal duotonowa paleta i typografia robią całą robotę; czytelność rośnie, gdy ekran ma jeden materiał tła. Stąd zasada: jeden dokument = jedna płaszczyzna papieru, bez pięter półprzezroczystych paneli.
- **The Case of the Golden Idol** — fakty jako krótkie, wymienne żetony słów. Stąd 6 punktów Alibi jako oddzielne, numerowane wiersze formularza, nie akapit prozy.
- **Her Story / Disco Elysium** — jedno diegetyczne „urządzenie" na ekran; tekst na ciemnym tle wymaga większej interlinii i mniejszych bloków. Stąd HUD na grafitowych panelach z krótkimi wierszami.
- **Among Us / Town of Salem 2** — karta roli pokazywana na starcie musi być zrozumiała w 3 sekundy: rola > co robić > czego nie wiem. Stąd hierarchia karty w Przygotowaniu.
- **Project Winter** — HUD w rogach, środek ekranu pusty; prywatne informacje w zwijanych panelach. Stąd reguła wolnego centrum podczas Rundy.

## 1. Paleta

Wartości pochodzą z zatwierdzonego `ART-DIRECTION.md` i z niego nie wolno wychodzić. Zmienne deklarujemy w selektorze `:root` arkusza motywu (TSS/USS podpięty do korzenia panelu); uGUI używa tych samych heksów w materiałach i kolorach komponentów.

### Papier i tusz (dokumenty)

| Zmienna USS | HEX | Rola |
|---|---|---|
| `--col-paper` | `#D5CCB6` | baza dokumentu (zatwierdzona „pożółkła kość słoniowa") |
| `--col-paper-bright` | `#E3DBC6` | podświetlony wiersz, hover na papierze |
| `--col-paper-dark` | `#C2B79D` | nagłówkowy pasek formularza, zakładka teczki |
| `--col-paper-shadow` | `#A89D82` | linie formularza, separatory, pola stempli |
| `--col-ink` | `#26221C` | tekst maszynopisu na papierze (ciepła czerń) |
| `--col-ink-faded` | `#5A5344` | tekst drugorzędny na papierze, opisy pól |

### Grafit (HUD, panele na świecie)

| Zmienna USS | HEX | Rola |
|---|---|---|
| `--col-graphite-900` | `#14181B` | tło paneli HUD (z alfa 0.85 podczas Rundy) |
| `--col-graphite-700` | `#2B333A` | przycisk neutralny, wiersz listy |
| `--col-graphite-500` | `#465059` | obramowania, nieaktywne kontrolki (zatwierdzony „metal") |
| `--col-graphite-300` | `#8B959D` | tekst drugorzędny na ciemnym |
| `--col-text-hi` | `#E8E3D5` | tekst główny na ciemnym (nigdy czysta biel) |

### Kolory funkcyjne ze stanami

| Zmienna USS | HEX | Rola |
|---|---|---|
| `--col-green` | `#415B4C` | **jedyny kolor akcji podstawowej** (zgaszona zieleń) |
| `--col-green-hover` | `#4E6E5B` | hover akcji |
| `--col-green-active` | `#33473C` | wciśnięcie akcji |
| `--col-red` | `#C22E28` | **sygnał krytyczny — patrz budżet czerwieni** (zatwierdzony) |
| `--col-red-hover` | `#D23A33` | hover przycisku destrukcyjnego |
| `--col-red-active` | `#9E2521` | wciśnięcie destrukcyjne |
| `--col-amber` | `#E0B568` | akcent: nazwy ról, aktywna zakładka, obrys `:focus` |
| `--col-amber-dim` | `#B18F55` | akcent wyciszony, metadane |
| `--col-brass` | `#806948` | mosiężne ramki teczek i kart pełnoekranowych |

Stan `disabled` dla każdej kontrolki: tło `--col-graphite-700`, tekst `--col-graphite-300`, obramowanie `--col-graphite-500`, `opacity: 0.6`. Wyłączony przycisk destrukcyjny również szarzeje — czerwień nigdy nie występuje w stanie disabled.

### Budżet czerwieni

`#C22E28` wolno użyć **wyłącznie** w tych miejscach; wszystko inne jest błędem review:

1. cyfry timera Rundy, gdy zostało mniej niż 60 sekund;
2. jeden przycisk destrukcyjny potwierdzający nieodwracalną akcję (np. „Opuść Rundę");
3. stempel przegranej na ekranie wyniku (np. `EGZEKUCJA NIEWINNEGO`);
4. toast krytyczny: utrata połączenia/hosta.

Zasada: w jednym kadrze co najwyżej jeden czerwony element. Ostrzeżenia niekrytyczne używają `--col-amber`.

## 2. Typografia

Wszystkie fonty są darmowe (OFL/Apache) i mają `latin-ext` — pełne polskie znaki `ąćęłńóśźż` plus „" i —. **Special Elite** (klasyczna maszyna do pisania) został odrzucony: brak `latin-ext`, więc brak polskich diakrytyków.

| Font | Licencja | Rola | Pobranie (raw GitHub google/fonts) |
|---|---|---|---|
| **Courier Prime** (Regular, Bold, Italic) | OFL | maszynopis: treść Alibi, dokumenty, Rejestr Incydentów, **cyfry timera** (monospace = brak drgania) | `https://raw.githubusercontent.com/google/fonts/main/ofl/courierprime/CourierPrime-Regular.ttf` (analogicznie `-Bold`, `-Italic`, `-BoldItalic`) |
| **Staatliches** (Regular) | OFL | stemple, tytuły ekranów, nazwy ról, nagłówki teczek (font wyłącznie wersalikowy, plakatowo-urzędowy) | `https://raw.githubusercontent.com/google/fonts/main/ofl/staatliches/Staatliches-Regular.ttf` |
| **Lato** (Regular, Bold, Italic) | OFL | neutralne UI: przyciski, ustawienia, toggle, pomoc, toasty (projekt Łukasza Dziedzica — natywnie polski) | `https://raw.githubusercontent.com/google/fonts/main/ofl/lato/Lato-Regular.ttf` (analogicznie `-Bold`, `-Italic`) |

### Import do Unity

1. TTF do `Assets/UI/Fonts/`.
2. Dla UI Toolkit: utworzyć Font Asset (Create → Text Core → Font Asset), atlas statyczny 1024–2048, custom character set zawierający ASCII + zakres `0x0100–0x017F` + `„”—…§`. Przypisać w USS przez `-unity-font-definition` w klasach `.font-doc`, `.font-stamp`, `.font-ui` (nie przez zmienne — referencje assetów w `var()` bywają zawodne).
3. Dla uGUI (menu główne): osobny TMP Font Asset z tym samym character setem.
4. **Fallback:** w każdym Font Asset ustawić fallback na domyślny font Unity. Layout nie może zależeć od szerokości glifów — jedyny wyjątek (timer) dostaje sztywny kontener na 5 znaków `MM:SS`.

### Skala rozmiarów (px przy referencji 1920×1080)

| Rola | Font | Rozmiar | Interlinia docelowa |
|---|---|---:|---|
| metadane, podpisy pól | Lato | 13 | 18 |
| tekst pomocniczy, wiersze ustawień | Lato | 15 | 22 |
| treść dokumentu, punkty Alibi | Courier Prime | 18 | 27 (~1.5) |
| podtytuł sekcji dokumentu | Courier Prime Bold | 20 | 28 |
| nazwa postaci, rola w HUD | Staatliches | 26 | 30 |
| timer | Courier Prime Bold | 32 | — |
| tytuł ekranu | Staatliches | 38 | 44 |
| stempel wyniku / roli | Staatliches | 56 | 60 |

USS nie ma właściwości `line-height` — interlinię kontrolują metryki Font Asset i `-unity-paragraph-spacing`; wartości z tabeli są celem wizualnym do strojenia w Font Asset. Tekst na ciemnym tle (HUD) dostaje o ~10% większą interlinię niż na papierze. `letter-spacing: 3px` tylko dla Staatliches ≥ 26 px; maszynopis nigdy nie jest spacjowany.

## 3. Odstępy, siatka, krawędzie

- **Skala odstępów:** 4 / 8 / 12 / 16 / 24 / 32 / 48 px (`--space-1` … `--space-7`). Żadnych wartości spoza skali; procenty tylko do pozycjonowania kolumn ekranów pełnych.
- **Padding paneli:** dokument pełnoekranowy 32; karta/teczka 24; panel HUD 16; wiersz listy 12 pion / 16 poziom.
- **Promienie:** papier i stemple `border-radius: 0` (papier ma ostre rogi); przyciski i panele HUD 2; zakładka teczki 4 (tylko górne rogi); toast 4. Zakaz promieni > 4 — zaokrąglone „appowe" karty łamią styl.
- **Krawędzie:** dokument papierowy ma 1 px obrys `--col-paper-shadow` oraz 3 px pasek cienia po prawej i dole (osobny element — USS nie wspiera `box-shadow`); teczka/karta pełnoekranowa ma 2 px ramkę `--col-brass`; wiersz Rejestru ma 3 px lewy pasek akcentu. Linie formularza: `border-bottom: 1px --col-paper-shadow`.

## 4. Komponenty (przepisy USS)

Wspólne dla interaktywnych: `transition-property: background-color, border-color, color; transition-duration: 0.1s; transition-timing-function: ease-out;`. Stan `:focus`: obrys 2 px `--col-amber` (nawigacja klawiaturą w menu i ustawieniach).

### Przycisk

```uss
.btn { height: 44px; padding: 0 24px; border-radius: 2px; border-width: 1px;
       font-size: 15px; color: var(--col-text-hi); letter-spacing: 1px; }
.btn-primary   { background-color: var(--col-green); border-color: var(--col-green-hover); }
.btn-primary:hover  { background-color: var(--col-green-hover); }
.btn-primary:active { background-color: var(--col-green-active); }
.btn-secondary { background-color: var(--col-graphite-700); border-color: var(--col-graphite-500); }
.btn-secondary:hover { background-color: var(--col-graphite-500); }
.btn-danger    { background-color: var(--col-red); border-color: var(--col-red-hover); }
.btn-danger:hover   { background-color: var(--col-red-hover); }
.btn-danger:active  { background-color: var(--col-red-active); }
.btn:disabled  { background-color: var(--col-graphite-700); color: var(--col-graphite-300);
                 border-color: var(--col-graphite-500); opacity: 0.6; }
```

Na papierze przycisk wygląda jak pole formularza: tło `--col-paper-bright`, tekst `--col-ink`, obrys 1 px `--col-ink-faded`; hover przyciemnia obrys do `--col-ink`.

- **Dokument papierowy:** tło `--col-paper`, tekst `--col-ink`, padding 32, nagłówkowy pasek `--col-paper-dark` z tytułem Staatliches i sygnaturą sprawy (Lato 13, `--col-ink-faded`). Treść w Courier Prime. Maksymalna szerokość tekstu 640 px (~70 znaków maszynopisu).
- **Teczka / dossier:** ciemna rama `--col-graphite-900` z 2 px `--col-brass`, wewnątrz dokument papierowy; zakładka 4 px radius u góry z etykietą Staatliches 15. Zakładki nieaktywne `--col-paper-dark`, aktywna `--col-paper`.
- **Panel HUD:** `background-color: rgba(20, 24, 27, 0.85)`, obrys 1 px `--col-graphite-500`, radius 2, padding 16; tytuł Staatliches 15 w `--col-amber`, treść Lato/Courier 13–15 w `--col-text-hi`.
- **Wiersz Rejestru Incydentów:** trzy linie — czas zgłoszenia (Courier 13, `--col-amber-dim`), miejsce (Lato Bold 15), skutek (Lato 15, `--col-graphite-300`); lewy pasek 3 px `--col-amber`; nowy wpis wjeżdża fade+slide 220 ms i przez 5 s ma tło rozjaśnione o krok, po czym gaśnie (transition 600 ms). Rejestr nigdy nie pokazuje sprawcy ani roli — pola „sprawca" nie ma w ogóle, nawet pustego.
- **Toggle:** kwadrat 20 px, obrys 1 px; zaznaczenie to stempelkowy znak „X" w `--col-ink` (na papierze) lub `--col-amber` (na grafitach), nie „ptaszek" w kółku. Przejście 100 ms.
- **Slider (ustawienia):** tor 4 px `--col-graphite-500`, wypełnienie `--col-green-hover`, uchwyt 16×16 radius 2; wartość liczbowo po prawej (Courier 15).
- **Toast:** panel HUD u góry ekranu, wchodzi slide-down 220 ms, znika po 4 s fade 300 ms; wariant krytyczny z lewym paskiem `--col-red` (budżet czerwieni, poz. 4), zwykły z paskiem `--col-amber`.
- **Timer:** kontener stały na `MM:SS`, Courier Prime Bold 32, tło panel HUD; poniżej 60 s kolor cyfr zmienia się na `--col-red` (jedno przejście koloru 300 ms) — **cyfry nigdy nie są animowane** (bez pulsowania, skalowania, migania).
- **Karta roli (Przygotowanie):** dokument papierowy z ukośnym stemplem roli (Staatliches 56, obrót −4°, `opacity` obniżone jak tusz stempla: rola `--col-ink`, dla Winnego nadal `--col-ink` — kolor stempla **nie koduje roli** dla oka obserwatora zza pleców; treść karty jest jedynym nośnikiem sekretu).
- **Przycisk `Gotowy`:** primary; po kliknięciu nieodwracalnie przechodzi w stan „przybity": tło `--col-graphite-700`, tekst `GOTOWY` w `--col-amber`, obok licznik gotowych graczy (np. `3/5`). Zmiana stanu 100 ms, bez fanfar.
- **Wskaźnik niesionego przedmiotu:** mały panel HUD przy dolnej krawędzi, ikona przedmiotu + nazwa (Lato 15) + podpowiedź klawisza odłożenia (Lato 13, `--col-graphite-300`). Pojawia się fade 220 ms przy podniesieniu.

## 5. Ikony

Proste, geometryczne, stemplowo-szablonowe: jednolity kolor, grubość kreski 2 px na siatce 24×24, bez gradientów, cieni i drugiego koloru. Ikona tylko tam, gdzie tekst nie wystarcza: mute mikrofonu, niesiony przedmiot, typ Incydentu (alarm / brak / uszkodzenie), klawisz interakcji, suwaki ustawień. Nazwy ról i akcje gry pozostają tekstem — słowo `WINNY` jest mocniejsze niż jakakolwiek ikona. Kolor ikon: `--col-text-hi` na grafitach, `--col-ink` na papierze; nigdy czerwony (budżet).

## 6. Animacje

| Zdarzenie | Czas | Easing |
|---|---:|---|
| hover/active kontrolek | 100 ms | `ease-out` |
| wjazd panelu HUD, wiersza listy, toastu | 220 ms | `ease-out` |
| fade przejścia ekranów (lobby → Przygotowanie → Runda → wynik) | 300 ms | `ease-in-out` |
| przybicie stempla (rola, wynik): scale 1.15 → 1.0 + fade | 250 ms | `ease-out-cubic` |
| wygaszenie podświetlenia nowego wpisu | 600 ms | `ease-out` |

Zasady: żadnych animacji w pętli poza jednym wyjątkiem — pasek obrysu toastu krytycznego może pulsować maks. 1 Hz. **Nigdy nie animujemy:** cyfr timera, treści Alibi podczas czytania, celownika/promptu interakcji, pozycji paneli HUD podczas Rundy (panel może się zwinąć na żądanie gracza, nie sam). Animacja nie może opóźniać akcji: klik działa natychmiast, animacja jest tylko odpowiedzią.

## 7. Hierarchia informacji per ekran

Reguła nadrzędna (kryterium akceptacji playtestu): gracz nieznający projektu w 3 sekundy wskazuje najważniejszą informację i następną dostępną akcję.

- **Menu główne (uGUI):** najważniejsze — tytuł gry i pionowa lista maks. 5 akcji (Graj / Ustawienia / Wyjście) na tle posterunku przyciemnionym `rgba(5,7,10,0.6)`; następna akcja — `Graj`. Lista wyrównana do lewej kolumny, Staatliches; zero elementów developerskich.
- **Lobby:** najważniejsze — podgląd 3D wybranej postaci (prawa, większa kolumna); następna akcja — strzałki karuzeli postaci, a dla hosta `Rozpocznij` (primary, dół lewej kolumny). Lewa kolumna to teczka: lista graczy z gotowością, przełączniki hosta (np. `Sekretny Cel`) z jednozdaniowym opisem skutku. Losowania Sprawy nie pokazujemy — host jej nie wybiera.
- **Przygotowanie (30 s):** najważniejsze — treść: dla Podejrzanego 6 numerowanych punktów Alibi na pełnoekranowym dokumencie, dla Detektywa karta roli z jawnym Przestępstwem i 3-punktową instrukcją; następna akcja — `Gotowy` (stale widoczny pod dokumentem, nie zasłania treści). Timer Przygotowania w rogu dokumentu, nie nad treścią. Po starcie Rundy dokument znika bezpowrotnie (fade 300 ms) — bez przycisku powrotu.
- **HUD Rundy:** najważniejsze — świat gry: centralne 50% × 50% ekranu jest zawsze wolne (wyjątek: prompt interakcji tuż pod środkiem). Timer u góry na środku; karta rola/Przestępstwo prawy dolny róg; panel Prywatnego Celu lewy dolny róg, zwijany klawiszem do samej etykiety; wskaźnik przedmiotu dół-środek; margines paneli od krawędzi 24 px, szerokość panelu maks. 24% ekranu. Następna akcja — zawsze bieżący krok Celu w panelu (jedno zdanie: co i w jakiej okolicy).
- **Rejestr Incydentów (Detektyw):** najważniejsze — najnowszy wpis (lista od najnowszych, wjazd wpisu przyciąga wzrok raz); następna akcja — rozwinięcie Rejestru klawiszem z panelu HUD do wąskiej kolumny po prawej (nie pełny ekran — Runda trwa, świat musi być widoczny).
- **Pauza/ustawienia (`Esc`):** najważniejsze — lista ustawień z czułością myszy na górze (suwak z wartością, działa natychmiast); następna akcja — `Wróć do gry` (primary, góra listy akcji). Menu nie zatrzymuje Rundy — timer pozostaje widoczny w rogu. `Opuść Rundę` jako jedyny danger, oddzielony odstępem 32. Opcje techniczne/dev: nieobecne w buildzie gracza.
- **Ekran wyniku:** najważniejsze — stempel werdyktu (wygrana `--col-ink` na papierze, przegrana `--col-red` — budżet, poz. 3) i jedno zdanie „dlaczego"; niżej lista graczy z indywidualnymi wynikami (Niewinni mają własne, nie drużynowe); następna akcja — `Nowa Runda` / `Wróć do lobby`. Pełnego Alibi nie ujawniamy (kwestia otwarta w `OPEN-QUESTIONS.md`).
- **Panele minigier:** najważniejsze — pole interakcji minigry (dokument/zamek/terminal jako rekwizyt papierowo-sprzętowy w stylu sekcji 4); następna akcja — wynika z samej minigry, plus stały `Przerwij` (secondary, róg panelu). Panel maks. 60% szerokości ekranu, świat pozostaje widoczny wokół — obserwowalność działań to zasada projektu. Minigra nie pokazuje, któremu Celowi służy.

## 8. Zasady redakcyjne i techniczne

- **Kontrolki developerskie:** nigdy w UI gracza. Panele debug za flagą kompilacji/klawiszem dostępnym tylko w Editorze; żaden ekran produkcyjny nie zawiera surowych pól tekstowych, ID ani przycisków testowych.
- **Polska typografia:** cudzysłowy „takie", myślnik — (nie dywiz), wielokropek …; w tekstach authorowanych nietłamane spacje po jednoliterowych spójnikach (`i tak`, `w archiwum`) — dotyczy treści Spraw i opisów Celów; UI nie łamie słów w środku. Wersaliki tylko w Staatliches (stemple, tytuły); treść zdaniowa nigdy caps-lockiem.
- **Terminy domenowe** w UI zawsze w kanonicznej formie z `CONTEXT.md`: Runda, Przygotowanie, Detektyw, Winny, Niewinny, Alibi, Egzekucja, Prywatny Cel, Rejestr Incydentów.
- **Skalowanie:** PanelSettings — Scale Mode `Scale With Screen Size`, Reference Resolution `1920×1080`, Match `1.0` (wysokość): przy 4K UI skaluje się 2×, przy ultrawide nie rośnie. uGUI Canvas Scaler w menu identycznie (`Match = 1`). Wszystkie wymiary w tym dokumencie to px przy referencji 1080p. Minimalny margines od krawędzi ekranu 16 px; brak założeń o overscanie (desktop).
- **Ograniczenia USS, które przepisy respektują:** brak `box-shadow` (cień = dodatkowy element lub border), brak `line-height` (metryki Font Asset + `-unity-paragraph-spacing`), brak zagnieżdżania `var()` w funkcjach i matematyki na zmiennych — pochodne wartości zapisujemy jako osobne zmienne. Stany przez pseudoklasy `:hover`, `:active`, `:focus`, `:disabled`, `:checked`.
- **Prywatność:** UI renderuje wyłącznie własny `PlayerRoundView`. Żaden styl, kolor, ikona ani animacja nie może kodować cudzej roli, Celu, postępu ani autorstwa Incydentu — także „niewinnie" (np. inny kolor stempla dla Winnego jest zakazany).
- **Kontrast:** pary tekst/tło z tego dokumentu (`--col-ink`/`--col-paper`, `--col-text-hi`/`--col-graphite-900`) spełniają ~WCAG AA dla rozmiarów ≥ 15 px; nie wprowadzać nowych par bez sprawdzenia kontrastu.

## Weryfikacja redesignu

1. Zrzuty wszystkich ekranów i stanów w działającej grze (1080p i 4K) — porównanie z sekcją 7.
2. Przejście review budżetu czerwieni: `rg -n "C22E28|--col-red" Assets/UI` i ręczna kontrola każdego użycia.
3. Test polskich diakrytyków i „cudzysłowów" na każdym foncie po imporcie Font Assetów.
4. Test skalowania: 1280×720, 1920×1080, 2560×1440, 3840×2160, 21:9 — centrum wolne, panele w marginesach.
