# Posterunek — Pass 2: kierunek artystyczny i playbook „anty-boks"

Status: **zatwierdzony kierunek** (decyzje użytkownika z 2026-07-11). Ten dokument prowadzi kolejny pass polishu. Układ pomieszczeń po Pass 1 (drzwi 1,5 m, południowe skrzydło do z = −7,6) jest **zamrożony** — Pass 2 nie zmienia obrysu ścian ani wymiarów pokoi.

## Zatwierdzone decyzje

1. **Styl:** doszlifowany obecny mix — meble low-poly Kenney + czyste powierzchnie PBR. Nie wymieniamy mebli, nie idziemy w fotorealizm.
2. **Klimat:** retro posterunek lat 90. — drewno, butelkowa zieleń, kremowe ściany, korkowe tablice, ciepłe jarzeniówki (vibe Twin Peaks / True Detective).
3. **Architektura:** pilastry/wnęki + zróżnicowane sufity — ale patrz playbook: geometria ma być NOŚNIKIEM funkcji (lampy, grzejniki, tablice), nie „boksem na boksie".
4. **Swoboda:** tylko detal. Zero zmian w obrysie ścian, szerokości drzwi, wysokości pomieszczeń bazowych.

## Naprawione bugi (2026-07-11, commit na main) — reguły na przyszłość

1. **Prześwitywanie przez ścianę przy krawędzi ekranu:** róg near-plane kamery (near 0.3, FOV 60) sięgał 0,463 m od oka; przy przytuleniu do ściany oko jest ~0,45 m od lica, a ściana ma 0,2 m. Naprawione: `nearClipPlane = 0.08` w prefabie gracza. **Reguła:** near plane zostaje ≤ 0.1; ściany zewnętrzne obrysu nigdy cieńsze niż 0,2 m.
2. **Migotanie tekstur przy drzwiach (z-fighting):** cztery skrzydła drzwi były zatopione 10 cm w segmentach ścian — koplanarne powierzchnie walczyły o piksel przy ruchu kamery. Naprawione: skrzydła odsunięte o 0,11 m. **Reguła:** ŻADNE dwie powierzchnie nie mogą być koplanarne ani się przecinać; minimalny odstęp równoległych płaszczyzn to 1 cm. Po każdej iteracji uruchom test przecięć bounds (skrypt w sekcji Weryfikacja).

## Playbook „anty-boks" — jak realne produkcje łamią płaskie ściany

Kluczowa uwaga użytkownika: „kolejne boksy nałożone na boks nic nie dadzą". Zgadza się — kolejność poniżej jest wg wpływu na odbiór, i większość NIE jest geometrią. Branżowa praktyka (trim sheets, decale, maski shaderowe, unikanie powtarzalności) — źródła na końcu.

### 1. Materiał przed geometrią: lamperia dwukolorowa (największy efekt, zero boksów)
Klasyka polskich/amerykańskich instytucji lat 90.: dolne ~1,2 m ściany w **butelkowej zieleni półmat** (lamperia olejna), wyżej **kremowa farba mat**, rozdzielone wąską listwą (2–3 cm). To jeden dodatkowy materiał + cienki pas geometrii, a ściana natychmiast przestaje być „ekranem". Zrób to na KAŻDEJ ścianie. Technicznie: albo drugi box 1,2 m wysokości licowany 5 mm PRZED ścianą (nie koplanarnie!), albo drugi materiał na osobnym paśmie geometrii.

### 2. Decale URP (drugi największy efekt)
Włącz **Decal Renderer Feature** w URP Rendererze (PC_RPAsset → Renderer) i użyj `DecalProjector`: przetarcia przy klamkach, smugi butów przy podłodze, zacieki pod sufitem, ślady po zdjętych obrazach, rdza pod grzejnikami, pojedyncze plakaty/ogłoszenia. 10–15 decali na pokój, NIGDY w regularnych odstępach. Tekstury: ambientCG kategorie Decal*/Dirt*, CC0.

### 3. Props naścienne — ściana przestaje być ścianą, gdy coś na niej „mieszka"
Retro posterunek: **grzejniki żeberkowe** pod pilastrami, **zegar ścienny**, **gaśnica na haku**, **skrzynka elektryczna z kablem natynkowym**, **wieszaki naścienne**, **korkowe tablice z kartkami** (już jest jedna — potrzeba 2–3 więcej), **żaluzje/rolety** przy oknach jeśli będą, ramki ze „zdjęciami" (proste quady z CC0 teksturami). Źródła modeli: poly.pizza (filtr CC0), Kenney, Quaternius. Jeśli brakuje modelu — prosty złożony z 3–5 prymitywów z fazowanymi krawędziami czyta się lepiej niż nic.

### 4. Pilastry i wnęki — ale z funkcją
Pilaster co ~3,5–4 m na długich ścianach (Sala, Korytarz), głębokość 8–12 cm, ale KAŻDY pilaster ma powód: niesie kinkiet, dzieli lamperię, stoi za nim grzejnik. Wnęka = płytkie cofnięcie 10 cm z tablicą ogłoszeń albo ławką w środku. Pilaster bez funkcji to właśnie „boks na boksie" — nie rób.

### 5. Sufit podwieszany zróżnicowany
Korytarz: obniż do 2,6 m osobną płaszczyzną z **kasetonami** (siatka 0,6 m z paneli, co któryś panel to oprawa świetlna, jeden przesunięty/przebarwiony). Sala: sufit zostaje na 3 m, ale dostaje **belki/skrzynki instalacyjne** przy ścianach i kratki wentylacyjne. Pokój Przesłuchań: goły ciemny sufit — kontrast z resztą jest zamierzony.

### 6. Fazowanie i futryny (ProBuilder)
Unity ma ProBuilder (MCP: `manage_probuilder`) — użyj do: fazowania zewnętrznych narożników ścian (bevel 2–3 cm), profili listew przypodłogowych i futryn drzwi z prawdziwym profilem (nie prostopadłościan). To usuwa „ostrza" charakterystyczne dla boksów.

### 7. Światło łamie płaskość za darmo
Kinkiety co nieregularne odstępy (już częściowo jest), wall-washery robiące plamy na lamperii, jedna migocząca jarzeniówka w Archiwum (animowana intensywność — jedyne światło realtime poza spotem przesłuchań, bez cienia). Po zmianach ponowny bake.

## Paleta retro lat 90. (trzymaj się jej)

| Element | Kolor | Hex orientacyjny |
| --- | --- | --- |
| Lamperia (dół ścian) | butelkowa zieleń półmat | `#2F4F3E` |
| Ściany (góra) | krem/ecru mat | `#E8E0CC` |
| Listwy, futryny, drzwi | ciemniejsze drewno | `#6B4A2E` |
| Podłoga Sala/Korytarz | linoleum oliwkowo-brązowe, drobna szachownica | `#8A7B5C` / `#5C5140` |
| Pokój Przesłuchań | zimna szarość, goły beton — wyjątek od palety | `#5A5D63` |
| Akcenty | korkowe tablice, mosiądz klamek, zieleń roślin | — |

Meble Kenney zostają w swoich kolorach — paleta ścian ma je „osadzić", nie konkurować.

## Źródła CC0 (tylko te, z licencją commitowaną do repo)

- Tekstury PBR i decale: **ambientCG.com** (CC0) — Paint*, PaintedPlaster*, Linoleum/Tiles, Decal*, Dirt*.
- Modele: **Kenney** (kenney.nl — Building Kit ma ściany/okna/drzwi), **Quaternius** (quaternius.com), **poly.pizza** (agregator, filtruj CC0), **KayKit**.
- Każdy nowy zestaw: `Assets/ThirdParty/<Autor>/<Kit>/` + License/SOURCE.md, jak dotychczas.

## Twarde zasady techniczne (wnioski z Pass 1)

1. Unity MCP dla wszystkich operacji edytora; przy braku możliwości — STOP i handoff (AGENTS.md).
2. `component_properties` przy tworzeniu obiektów bywa ignorowane — ustawiaj właściwości OSOBNYM krokiem i CZYTAJ JE Z POWROTEM.
3. Kolory w MCP jako obiekt `{r,g,b,a}`, nie tablica.
4. Pivoty Kenney/FBX są przesunięte — pozycjonuj po zmierzonych bounds rendererów.
5. Zero edycji w Play Mode (zmiany przepadają). Zero wejścia w Play Mode w ogóle.
6. Po każdej iteracji: bake świateł jeśli zmieniono geometrię/światła, save sceny przez MCP, commit.

## Weryfikacja po każdej iteracji (wszystko musi być zielone)

1. Physics gauntlet z Pass 1: 6 spawnów OverlapCapsule czyste, 4 drzwi + korytarz + trasy przez pokoje CapsuleCast czyste (r = 0,45), strefy `Strefa_*` — isTrigger i rozmiar pokoju.
2. **Nowy test koplanarności:** dla każdej pary rendererów w `Map_Graybox` sprawdź przecięcia bounds; dopuszczalne tylko celowe zagłębienia (listwa w ścianie ≤ 5 mm licowania jest ZABRONIONE — zawsze 5 mm PRZED licem). Raportuj każdą parę z przecięciem > 0.
3. Screeny z wysokości oczu (y ≈ 1,7): 2 na pokój + korytarz w obu kierunkach + przez każde drzwi; porównanie z poprzednią iteracją.
4. Konsola: zero błędów (szum Vivox „Callback dispatcher is not initialized" ignoruj — pre-existing).
5. Budżet: scena ≤ 150 tys. trójkątów łącznie, ≤ 60 materiałów, 3–4 atlasy lightmap; decal projectors ≤ 60.

## Kryteria akceptacji Pass 2

- [ ] Lamperia dwukolorowa z listwą na 100% ścian; zero widocznych „gołych" płaskich ścian wyższych niż 2 m bez podziału.
- [ ] ≥ 10 decali na pomieszczenie, nieregularnie; zero powtarzalnych wzorów widocznych z pozycji gracza.
- [ ] ≥ 4 funkcjonalne props naścienne na pomieszczenie (grzejnik/zegar/gaśnica/tablica/skrzynka).
- [ ] Korytarz z obniżonym sufitem kasetonowym; Sala z belkami/wentylacją; pilastry tylko tam, gdzie niosą funkcję.
- [ ] Narożniki zewnętrzne ścian fazowane; wszystkie futryny mają profil.
- [ ] Paleta zgodna z tabelą; Pokój Przesłuchań celowo wyłamany (zimny).
- [ ] Test koplanarności czysty, gauntlet czysty, bake świeży, konsola czysta, sceny zapisane, commity wypchnięte.
- [ ] Raport końcowy w `docs/map-polish/PASS-2-REPORT.md` ze screenami przed/po.

## Źródła researchu

- polycount — Modular Environment Techniques: https://polycount.com/discussion/209426/modular-environment-techniques
- The Level Design Book — Environment Art: https://book.leveldesignbook.com/process/env-art
- World of Level Design — Modular Environment Design 101: https://www.worldofleveldesign.com/categories/game_environments_design/modular-environment-design-101.php
- Beyond Extent — Balancing modularity and uniqueness: https://www.beyondextent.com/articles/balancing-modularity-and-uniqueness-in-environment-art
- Kenney Building Kit (CC0): https://kenney.nl/assets/building-kit • Quaternius: https://quaternius.com • ambientCG (CC0 PBR/decale): https://ambientcg.com
