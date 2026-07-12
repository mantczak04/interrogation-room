# Roadmapa graficzna — poziom „premium"

Status: propozycja (2026-07-12). Wykonawcze rozbicie na fazy, branche i tracker statusów: [docs/design/graphics/README.md](./graphics/README.md) — tam odhaczamy postęp; ten dokument pozostaje przeglądem wysokopoziomowym. Cel: gra ma wyglądać jak droga produkcja klasy Phasmophobia/Inside po polishu, nie fotorealistyczne AAA (nieosiągalne solo). Gra dzieje się w jednym wnętrzu (`Posterunek`), co pozwala dopieścić każdy kadr.

Decyzja strategiczna: pozostajemy na **URP** (multiplayer, szeroki sprzęt graczy). Nie migrujemy na HDRP. URP w Unity 6 zapewnia Render Graph, SSAO, decale i Volume system.

Stan wyjściowy: renderery `PC_Renderer`/`Mobile_Renderer` w `Assets/SourceFiles/Settings/`, pierwszy post-processing `Assets/Settings/PosterunekPostFX.asset`, materiały graybox + początki PBR (`P2_*`), scena `Assets/Scenes/Room.unity`.

## Faza 0 — Fundamenty techniczne (1–2 tyg.)

- Audyt i korekta ustawień URP: HDR włączone, Color Space Linear, SSAO na rendererze PC, cienie 2048–4096 z soft shadows.
- Tonemapping **ACES** w Volume.
- Decyzja o texel density (propozycja: 512 px/m) — przed produkcją assetów, żeby nie robić ich dwa razy.
- Zdefiniowanie quality tiers: co konkretnie różni High/Medium/Low między `PC_Renderer` a `Mobile_Renderer`.

## Faza 1 — Art direction (1–2 tyg.) — decyduje o wszystkim

- Moodboard noir/przesłuchanie: L.A. Noire, Interrogation, Twelve Minutes, Twin Peaks.
- Kierunek: **poważne, kinowe światło + absurdalne postacie** (Jak, Małpa, Wieprz, Karton) — kontrast jako tożsamość wizualna.
- Stylizowany realizm (rekomendowany), nie fotorealizm.
- Paleta sceny: 2–3 dominanty (np. zgniła zieleń świetlówek + ciepły wolfram lampy przesłuchań + zimna noc za oknami).

## Faza 2 — Oświetlenie (2–4 tyg.) — największy zwrot z inwestycji

- **Baked GI** (lightmapy) dla całego posterunku; Light Probes dla postaci; Reflection Probes per pomieszczenie.
- Mixed lighting: baked GI + realtime cienie głównych lamp (postacie rzucają cień).
- Motyw świetlny per pomieszczenie: migocząca świetlówka na korytarzu, jedna mocna lampa nad stołem przesłuchań, ciemne kąty.
- Światło wspiera gameplay: prywatność = ciemne strefy (spójne z ADR-0009 — prywatność z przestrzeni).
- Fake volumetrics: stożki światła jako mesh z shaderem additive (URP nie ma natywnych volumetrics).

## Faza 3 — Materiały i modularny kit środowiska (4–8 tyg.)

- **Trim sheets + modularny kit**: ściany, podłogi, listwy, drzwi, okna z 2–3 wspólnych atlasów.
- Pełny PBR: albedo + normal + mask (metallic/AO/smoothness); rozwinięcie podejścia z `P2_PlasterNormal`/`P2_RoughnessVariation` na wszystkie powierzchnie.
- **Decale (URP Decal Projector)**: zacieki, brud, plakaty, zadrapania.
- Props i clutter: biurka z papierami, akta, korkowa tablica, kubki. Puste pokoje zdradzają amatorską produkcję. Dozwolone paczki z Asset Store ujednolicane teksturami (nie Synty — zbyt rozpoznawalne).

## Faza 4 — Post-processing i kamera (1–2 tyg.) — duży efekt, tanio

- Volume stack: ACES, color grading (LUT noir), subtelny bloom, vignette, chromatic aberration ~0.05, film grain, lift-gamma-gain.
- SSAO dostrojone do skali wnętrz.
- Motion blur per-object wyłączony (multiplayer); depth of field tylko w menu/zbliżeniach.

## Faza 5 — Postacie (4–8 tyg.) — największe ryzyko

- Postacie oglądane z bliska podczas przesłuchań: modele 15–40k trisów, PBR.
- Mimika: świadoma stylizacja bez riggu twarzy (maski/sztywne pyski — pasuje do absurdu, 10× tańsze) albo pełny rig.
- Animacje: idle z wariacjami, gesty, chód — paczki mocap + blend trees.
- Lip-sync z amplitudy głosu (np. uLipSync) — gra opiera się na voice chacie, usta ruszające się do mowy to duży efekt za małą cenę.

## Faza 6 — Ruch, VFX i mikrodetale (2–4 tyg.)

- Cząsteczki: kurz w snopach światła, para z kubka, dym.
- Animowane detale: wentylator sufitowy, migotanie świetlówki zsynchronizowane z dźwiękiem, deszcz za oknem.
- Reakcje świata: drzwi z fizyką i dźwiękiem, przesuwane krzesła.

## Faza 7 — Optymalizacja i skalowalność (ciągłe + 2 tyg. na końcu)

- Profilowanie na słabym sprzęcie (GPU Profiler, RenderDoc); budżet 16.6 ms/klatkę (60 FPS).
- LOD-y na propsy, static batching środowiska, limit ~4 realtime świateł z cieniami na widok.

## Harmonogram i priorytety

| Faza | Czas (solo) | Efekt wizualny |
|---|---|---|
| 0. Fundamenty URP | 1–2 tyg. | mały, odblokowuje resztę |
| 1. Art direction | 1–2 tyg. | decyduje o wszystkim |
| 2. Oświetlenie | 2–4 tyg. | ogromny |
| 3. Materiały + kit | 4–8 tyg. | ogromny |
| 4. Post-processing | 1–2 tyg. | duży, tani |
| 5. Postacie | 4–8 tyg. | duży, drogi |
| 6. VFX/detale | 2–4 tyg. | średni, „premium feel" |
| 7. Optymalizacja | ciągłe | warunek grywalności |

Razem realistycznie: **4–7 miesięcy pracy solo**.

## Zakres „teraz" vs „po MVP"

Projekt jest na etapie MVP mechaniki (`docs/architecture/MVP-ARCHITECTURE.md`). Teraz wykonujemy tylko **pakiet startowy**: Faza 0 + Faza 4 + lekki pass Fazy 2 (kilka dni, natychmiastowa poprawa wyglądu). Pełny art pass (Fazy 1, 3, 5, 6) — po zweryfikowaniu pętli rozgrywki.
