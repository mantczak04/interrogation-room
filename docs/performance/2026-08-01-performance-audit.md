# Audyt wydajności — 2026-08-01

## Środowisko

- Unity 6000.5.3f1, Direct3D 12, Windows 11 (10.0.26200).
- AMD Ryzen 7 6800H, 16 logicznych rdzeni.
- NVIDIA GeForce RTX 3050 Laptop GPU, 3962 MB VRAM.
- 28 357 MB RAM.
- Scena `Assets/Scenes/Room.unity`, lokalny host KCP, jeden lokalny klient i gracz FPP.
- Vivox osiągnął stan `Ready` i dołączył do globalnego kanału lobby.

## Metoda

Ręczny przebieg obejmował około 30 sekund ruchu po głównej sali, obracanie kamery, interakcje i tańce. Posłużył do odkrycia problemu, ale ze względu na małą liczbę próbek nie jest podstawą końcowych procentów.

Końcowy kontrolowany A/B izolował wyłącznie `MapOverviewCamera`. Oba warianty miały:

- tę samą scenę `Room`, transport `KcpTransport`, aktywny host i klient oraz jedno połączenie;
- tego samego lokalnego gracza w pozycji `(1.000, 0.000, 7.280)`;
- identyczny stan wejścia i UI, bez ruchu użytkownika;
- 120 unikalnych klatek rozgrzewki i 180 unikalnych klatek pomiaru na wariant;
- te same aktywne kamery pomocnicze; jedyną zmienną było `MapOverviewCamera.enabled`.

Pierwszy przebieg na SteamSockets bez lokalnego `PlayerController` został odrzucony jako niereprezentatywny.

## Wyniki kontrolowanego A/B

| Metryka | Kamera przeglądowa włączona | Kamera przeglądowa wyłączona | Zmiana |
|---|---:|---:|---:|
| Frame time p50 | 29,57 ms | 16,50 ms | -44,2% |
| Frame time p95 | 31,02 ms | 17,06 ms | -45,0% |
| CPU frame time p50 | 29,35 ms | 16,46 ms | -43,9% |
| CPU frame time p95 | 31,40 ms | 17,34 ms | -44,8% |
| GPU frame time p50 | 29,48 ms | 16,43 ms | -44,3% |
| GPU frame time p95 | 30,85 ms | 16,97 ms | -45,0% |
| SetPass p50 | 945 | 435 | -54,0% |
| Trójkąty p50 | 1 794 052 | 814 214 | -54,6% |
| Wierzchołki p50 | 2 376 451 | 1 073 917 | -54,8% |
| GC Allocated In Frame p50 | 11 547 B | 11 547 B | bez zmiany |
| GC Allocated In Frame p95 | 12 247 B | 12 159 B | w granicy szumu |
| Total Used Memory p50 | 3 066 874 599 B | 3 063 054 919 B | -0,1% |

Każdy wariant zawiera 180 próbek. Poprawa czasu klatki jest znacznie większa niż rozrzut między medianą i p95, a liczniki renderingu spadają równocześnie, co potwierdza przyczynę.

## Największe koszty

1. **Równoczesne renderowanie `MapOverviewCamera` i `Main Camera`.** Frame Debugger pokazał 729 zdarzeń: 521 dla kamery przeglądowej i 208 dla kamery gracza. Lokalny gracz renderował większość sceny drugi raz. Problem został naprawiony.
2. **Cienie i geometria.** 531 z 729 zdarzeń Frame Debuggera było związanych z cieniami. Usunięcie zbędnego drugiego widoku obniżyło medianę geometrii o około 55%. Nie obniżano jakości cieni ani obrazu.
3. **Alokacje zarządzane.** Kontrolowany pomiar wykazał identyczne GC p50 przed i po. Kamera nie była źródłem alokacji, więc nie wprowadzono spekulacyjnych zmian w GC.

Audio nie było dominującym kosztem w dostępnym liczniku wstępnym (około 0,17 ms wątku audio). Nie wykonano pełnego pomiaru rozmowy dwóch rzeczywistych uczestników Vivox.

## Zmiana

`PlayerController.OnStartLocalPlayer` wyłącza `MapOverviewCamera`, gdy aktywuje się lokalna kamera gracza. `OnStopLocalPlayer` przywraca kamerę przeglądową. Kamera startowa pozostaje dostępna przed spawnem gracza, a jakość obrazu z `Main Camera` nie została zmieniona.

## Weryfikacja po aktualizacji maina

- `main` jest równy `origin/main` przed lokalnym commitem.
- Kompilacja C#: przeszła, Console bez błędów.
- Edit Mode: 554/554 testów przeszło, 0 pominiętych i 0 nieudanych.
- Lokalny host KCP: lokalny `PlayerController` i `Main Camera` utworzone; normalny kod ustawia `MapOverviewCamera.enabled == false`, `Main Camera.enabled == true`.
- `git diff --check`: przeszedł.
- Zasad Rundy ani ustawień jakości wizualnej nie zmieniono.

## Niewykonane i dalsze propozycje

- Development Build został anulowany po około 12 minutach przy 284/3072 wariantów URP/Lit. Raport nie przedstawia danych z builda jako wykonanych.
- Pełny pomiar host + klient KCP w dwóch procesach oraz rozmowa dwóch uczestników Vivox pozostają do wykonania po przygotowaniu cache'u shaderów albo dedykowanego profilu builda QA.
- Dalsze prace nad cieniami, LOD, materiałami lub obniżeniem jakości obrazu powinny być osobnym zadaniem i wymagać zgody wizualnej.
