# Faza 2 — Oświetlenie

- **Status:** Review — poprawki po przeglądzie użytkownika 2026-07-12 wykonane (sekcja na dole dokumentu); czeka na ocenę w grze (P1 czytelność, P4 test migotania w ruchu)
- **Branch roboczy pakietu:** `gfx/graphics-overhaul`
- **Branch historyczny fazy:** `gfx/faza-2-oswietlenie`
- **Zależności:** Faza 0 (`Approved` lub `Done`), Faza 4 (`Review`, `Approved` lub `Done` na tym samym branchu integracyjnym — tonemapping ACES musi działać przed strojeniem świateł)
- **Szacowany czas:** 3–7 dni pracy agenta + wypieki lightmap

Przeczytaj najpierw: [README.md](./README.md), [AUTONOMOUS-GRAPHICS-RUN.md](./AUTONOMOUS-GRAPHICS-RUN.md) i `AGENTS.md`. Faza 1 jest zatwierdzona, więc użyj motywów świetlnych z `ART-DIRECTION.md`.

## Cel / Definition of Done

Scena `Room.unity` ma wypieczone GI, motywy świetlne per pomieszczenie, Light Probes i Reflection Probes. Postacie rzucają cienie realtime. Ciemne strefy dają realną prywatność (ADR-0009). Budżet: ≤ 4 realtime światła z cieniami w kadrze.

## Kontekst techniczny

- Scena: `Assets/Scenes/Room.unity`. Lightmapper: **Progressive GPU** (wbudowany, bez nowych pakietów).
- Wszystkie operacje na scenie przez Unity MCP (`manage_scene`, `manage_gameobject`, `manage_components`, `batch_execute`). Hierarchię pobieraj stronicowaną (≤ 50 obiektów).
- Nowe skrypty w `Assets/Scripts/Graphics/` (utwórz katalog). Skrypty nie mogą zależeć od Mirror ani `RoundEngine`.

## Zadania

### 2.1 Przygotowanie sceny do bake'u

- [x] Audyt hierarchii `Room.unity`: zidentyfikuj geometrię statyczną (ściany, podłogi, sufity, meble nieprzesuwalne) vs dynamiczną (postacie, drzwi, krzesła, propsy interaktywne).
- [x] Statycznej geometrii ustaw flagi: `ContributeGI` + `StaticBatching` (przez Unity MCP, `batch_execute` dla wielu obiektów).
- [x] Lighting Settings asset dla sceny: utwórz `Assets/Settings/Room_LightingSettings.asset`; Lightmapper = Progressive GPU, Baked GI = ON, Realtime GI = OFF, Lightmap Resolution = 20 texels/unit (drafty: 10), Max Lightmap Size = 2048, Directional Mode = Directional, Ambient Occlusion (baked) = ON, Denoiser = domyślny dostępny.
- [x] Environment: Skybox nocny lub kolor ambientu ciemnogranatowy `#1A2130`; Intensity niskie (~0.2–0.4). Wnętrze ma być ciemne między światłami.

### 2.2 Mixed lighting — układ świateł

Tryb: **Shadowmask** (Lighting window → Mixed Lighting). Docelowy układ:

- [x] **Pokój przesłuchań:** jeden Spot Light nad stołem — Mode = Mixed, kolor `#FFB868` (~3000K), Intensity dobrana tak, by stół był mocno oświetlony, a kąty pokoju ciemne; Shadows = Soft; Range/kąt tak, by stożek obejmował stół + krzesła.
- [x] **Korytarz:** świetlówki jako Area Lights (Mode = Baked, kolor `#C8E0C0`) wzdłuż sufitu + materiał emissive na kloszach (patrz 2.4). Jedna świetlówka „usterka” — patrz 2.5.
- [x] **Pozostałe pomieszczenia** (archiwum itd.): po 1 świetle Baked, celowo słabszym; kąty i zakamarki bez światła = strefy prywatności.
- [x] **Za oknami:** jeden Directional Light — Mode = Mixed, kolor `#3A4A6B`, bardzo niska intensywność (~0.3), kąt niski (noc, poświata księżyca/latarni).
- [x] Zweryfikuj limit: w żadnym kadrze więcej niż 4 światła realtime/mixed z cieniami.

### 2.3 Probes

- [x] Light Probe Group pokrywająca wszystkie pomieszczenia i korytarze (siatka ~1.5 m, gęściej przy granicach światło/cień) — bez probe'ów wypieczone GI nie oświetli postaci.
- [x] Po jednym Reflection Probe na pomieszczenie: Type = Baked, Box Projection = ON, dopasowany do wymiarów pokoju, rozdzielczość 128.

### 2.4 Materiały emissive

- [x] Klosze świetlówek i lampa nad stołem: materiały z Emission (HDR, intensywność ~2–4), Global Illumination = Baked. Emission ma się zgadzać kolorem ze światłem, które „udaje”.

### 2.5 Skrypt migotania

- [x] Utwórz `Assets/Scripts/Graphics/FlickeringLight.cs`: komponent sterujący `Light.intensity` + emission materiału klosza; wzorzec migotania deterministyczny per instancja (seed z pozycji), parametry w Inspectorze (min/max intensity, częstotliwość, procent czasu w stanie „zgaszona”). Bez zależności sieciowych — czysto wizualny, lokalny.
- [x] Podepnij do jednej świetlówki na korytarzu (światło tej jednej: Mode = Realtime, bez cieni, mały Range).
- [x] Zdarzenie publiczne `OnFlicker` (UnityEvent) — Faza 6 podepnie pod nie dźwięk.

### 2.6 Fake volumetrics

- [x] Stożek światła nad stołem przesłuchań: mesh stożka + materiał Shader Graph (URP Unlit, Transparent Additive, gradient przezroczystości od źródła do podstawy, fade przy przecięciu geometrii przez depth — soft particles/depth fade node). Zapisz shader jako `Assets/VFX/LightConeAdditive.shadergraph`, materiał `Assets/VFX/Mat_LightCone.mat`.
- [x] Intensywność bardzo subtelna (widoczna, dopiero gdy się jej szuka). Bez cząsteczek kurzu — to Faza 6.

### 2.7 Bake i strojenie

- [x] Wypiek draft (10 texels/unit) → ocena → korekty intensywności → wypiek finalny (20 texels/unit).
- [x] Sprawdź artefakty: przecieki światła przez ściany (popraw grubość geometrii lub `Cast Shadows = Two Sided`), plamy na lightmapach (podnieś rozdzielczość obiektu), zbyt czarne cienie (delikatnie podnieś ambient).

### 2.8 Weryfikacja

- [x] Unity Console bez błędów; kompilacja `FlickeringLight.cs` czysta.
- [x] Zrzuty „po” per pomieszczenie: `Assets/Screenshots/gfx_faza2_<pomieszczenie>.png`.
- [x] Postać testowa (dowolny prefab z `Assets/Characters/`) postawiona w 3 miejscach — odbiera światło z probe'ów i rzuca cień pod lampą.
- [x] Scena zapisana przez Unity MCP; `git diff --check`; statusy zaktualizowane.

## Poza zakresem

Cząsteczki kurzu i synchronizacja dźwięku (Faza 6), wymiana materiałów ścian/podłóg (Faza 3), zmiany post-processingu (Faza 4 — jeśli światło wymaga korekty ekspozycji, zmień intensywności świateł, nie Volume).

## Przegląd użytkownika 2026-07-12 — poprawki wymagane przed ponownym `Review`

Ocena użytkownika: stożek światła i migotanie świetlówki — bardzo dobre, zostają bez zmian. Cztery problemy do naprawy (diagnozy potwierdzone inspekcją sceny przez Unity MCP):

### P1. Zbyt ciemno, słaba widoczność (krytyczne)

Przyczyna (skorygowana po pełnym odczycie przez `execute_code`): lampy pomieszczeń to **Point lights o zbyt małej intensywności i zasięgu** (`Swiatlo_Sala`…`Sala4`: int 1.6, range 5.5; `Socjalny` 1.8/6; `Archiwum` 1.5/6), wszystkie **bez cieni w bake'u**, a ambient to Flat `#1A2130` (w trybie Flat pole Intensity nie działa — decyduje sam kolor). Spadek kwadratowy przy range 5.5 zostawia ściany niemal czarne.

- [x] Podnieś intensywność i zasięg lamp: Sala 1.6→2.4/8 m, Socjalny 2.2/8, Archiwum 1.8/8 (celowo najsłabsze), świetlówki korytarza (Rectangle) range→8. Wykonane 2026-07-12.
- [x] Ambient Flat rozjaśniony: `#1A2130` → `(0.15, 0.18, 0.25)`. Wykonane 2026-07-12.
- [ ] Zasada akceptacji: „ciemno ≠ czarno" — w każdym punkcie mapy sylwetka postaci ma być rozróżnialna na tle ściany; strefy prywatności zostają wyraźnie ciemniejsze, ale nie nieczytelne. Ocena użytkownika po re-bake'u.
- [x] Po zmianach pełny re-bake i nowe zrzuty per pomieszczenie.
- [x] Korytarz — dwa dodatkowe błędy znalezione testem „intensity 20": światła Rectangle korytarza (a) miały rotację (0,0,0), czyli emitowały poziomo w ścianę zamiast w dół, oraz (b) wisiały na y 2.6–2.7, czyli WEWNĄTRZ/NAD kratownicą T-bar sufitu (y 2.56–2.62), która pochłaniała cały wypiek (fizyczny raycast przechodził, bo kratownica nie ma collidera — mylący trop). Poprawka: rotacja X=90°, pozycja y=2.45 (pod kratownicą), intensywność finalnie 4.5. Wykonane 2026-07-12.

### P2. „Odstająca warstwa" telewizora w sali wspólnej

Przyczyna: `Map_Graybox/Meble/Sala_TV/TV_Ekran` (quad ekranu) ma `localPosition.z = -0.145`, a korpus TV jest głęboki na 0.2568 (ściana frontowa na z = -0.1284) — **ekran lewituje ~1.7 cm przed telewizorem** i łapie własny cień/AO jako oddzielna płyta.

- [x] Przesuń `TV_Ekran` na `localPosition.z = -0.131` (2–3 mm przed frontem korpusu), przez Unity MCP. Wykonane 2026-07-12.

### P3. Dziwne cienie

Przyczyny złożone: (a) wszystkie baked lampy pomieszczeń mają `shadows = None`, więc bake przepuszcza światło przez meble — brak naturalnych cieni kontaktowych, płaskie plamy; (b) lightmapa 20 texels/unit daje rozmyte, „blobowate" cienie; (c) SSAO intensity 1.5 w ciemnej scenie dodaje brudne obwódki.

- [x] Włącz Soft Shadows na wszystkich lampach Baked (koszt tylko w bake'u, nie w runtime). Wykonane 2026-07-12.
- [x] Zmniejsz SSAO Intensity na `PC_Renderer` do ~0.8 przy obecnej ciemności sceny. Wykonane 2026-07-12 (1.5 → 0.8).
- [ ] Rozważ podniesienie rozdzielczości lightmap do 30 texels/unit, jeśli czas bake'u pozwoli. Decyzja: zostaje 20 — najpierw ocena użytkownika po obecnych poprawkach.

### P4. „Migotanie" światła w sali wspólnej podczas chodzenia (stojąc — brak)

`FlickeringLight` jest tylko jeden (korytarz) i działa niezależnie od ruchu gracza — to NIE on. Główne hipotezy: (a) **shimmer SSAO Blue Noise** — wzór szumu jest ekranowy, przy nieruchomej kamerze stoi, przy ruchu „gotuje się" w ciemnych, zokludowanych partiach obrazu; bez TAA nic go nie uspokaja; (b) światło migoczącej świetlówki z korytarza **nie ma cieni**, więc przebija przez ścianę do sali jako słaba pulsująca poświata.

- [ ] Test A/B: wyłącz SSAO feature → przejdź się po sali → jeśli migotanie znikło, winne (a); zredukuj Intensity/Radius lub przełącz Method i porównaj. Częściowo zaadresowane redukcją Intensity 1.5→0.8; test w ruchu wykonuje użytkownik.
- [x] Ogranicz przeciek świetlówki: Range `Faza2_Flicker_Korytarz` 3.2 → 3.0 (cieni na tym świetle nadal nie włączamy — koszt). Wykonane 2026-07-12.
- [ ] Decyzja o TAA zamiast SMAA — odroczona do testów ghostingu (notatka w Fazie 4).

### Przegląd użytkownika #2 (2026-07-12, po poprawkach P1–P4)

Ocena: wszystkie pokoje OK, ale sala wspólna (pokój startowy) zbyt zielona względem ciepłych pozostałych pomieszczeń; dodatkowo bałagan propsów.

- [x] Kolor świateł sali `Swiatlo_Sala`…`Sala4`: zielony `(0.784, 0.878, 0.753)` → ciepły `(1.0, 0.722, 0.408)` (ten sam co `Swiatlo_Socjalny`; panele opraw sali już używały `Mat_Lamp_Warm`, więc kolor świateł wreszcie zgadza się z kloszami). Zielony zostaje tożsamością korytarza.
- [x] Propsy sali: oba stoły (`Sala_StolW/E`) były zapadnięte 0.54 m w podłogę (min.y = −0.52) — podniesione na 0.56, blat na ~0.75 m; krzesła stały wewnątrz obrysów stołów — rozstawione logicznie wokół stołów (W: 3 krzesła, E: 2), tyłem od stołu wg konwencji z pokoju przesłuchań; stolik kawowy był wbity w kanapę — przesunięty między kanapę a szafkę TV; kącik TV wyrównany do osi telewizora (z = 3.0); drugi stolik kawowy dosunięty przed `Sala_SofaE`.
- [x] Kanapy (`Sala_Sofa`, `Sala_SofaE`): dodany `NetworkChairSeat` + `NetworkIdentity` + child `SeatPoint` (konfiguracja wg krzeseł; seatSurfaceHeight 0.35, backrestOffset 0.15) — na kanapach można siedzieć.

### Kwestia projektowa (poza Fazą 2): latarka na F

Pomysł użytkownika: skoro ciemno, może dać graczom latarkę (toggle F). To decyzja gameplayowa, nie graficzna — latarka pozwala prześwietlać ciemne strefy prywatności (ADR-0009): wzmacnia `Detektywa`, osłabia szeptanie po kątach; zmienia też czytelność sylwetek. Rekomendacja: najpierw naprawić P1 („ciemno ≠ czarno"), a latarkę wpisać do `docs/design/OPEN-QUESTIONS.md` jako odroczoną decyzję — nie implementować w ramach grafiki.
