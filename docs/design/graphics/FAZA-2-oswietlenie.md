# Faza 2 — Oświetlenie

- **Status:** In progress
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

- [ ] Audyt hierarchii `Room.unity`: zidentyfikuj geometrię statyczną (ściany, podłogi, sufity, meble nieprzesuwalne) vs dynamiczną (postacie, drzwi, krzesła, propsy interaktywne).
- [ ] Statycznej geometrii ustaw flagi: `ContributeGI` + `StaticBatching` (przez Unity MCP, `batch_execute` dla wielu obiektów).
- [ ] Lighting Settings asset dla sceny: utwórz `Assets/Settings/Room_LightingSettings.asset`; Lightmapper = Progressive GPU, Baked GI = ON, Realtime GI = OFF, Lightmap Resolution = 20 texels/unit (drafty: 10), Max Lightmap Size = 2048, Directional Mode = Directional, Ambient Occlusion (baked) = ON, Denoiser = domyślny dostępny.
- [ ] Environment: Skybox nocny lub kolor ambientu ciemnogranatowy `#1A2130`; Intensity niskie (~0.2–0.4). Wnętrze ma być ciemne między światłami.

### 2.2 Mixed lighting — układ świateł

Tryb: **Shadowmask** (Lighting window → Mixed Lighting). Docelowy układ:

- [ ] **Pokój przesłuchań:** jeden Spot Light nad stołem — Mode = Mixed, kolor `#FFB868` (~3000K), Intensity dobrana tak, by stół był mocno oświetlony, a kąty pokoju ciemne; Shadows = Soft; Range/kąt tak, by stożek obejmował stół + krzesła.
- [ ] **Korytarz:** świetlówki jako Area Lights (Mode = Baked, kolor `#C8E0C0`) wzdłuż sufitu + materiał emissive na kloszach (patrz 2.4). Jedna świetlówka „usterka” — patrz 2.5.
- [ ] **Pozostałe pomieszczenia** (archiwum itd.): po 1 świetle Baked, celowo słabszym; kąty i zakamarki bez światła = strefy prywatności.
- [ ] **Za oknami:** jeden Directional Light — Mode = Mixed, kolor `#3A4A6B`, bardzo niska intensywność (~0.3), kąt niski (noc, poświata księżyca/latarni).
- [ ] Zweryfikuj limit: w żadnym kadrze więcej niż 4 światła realtime/mixed z cieniami.

### 2.3 Probes

- [ ] Light Probe Group pokrywająca wszystkie pomieszczenia i korytarze (siatka ~1.5 m, gęściej przy granicach światło/cień) — bez probe'ów wypieczone GI nie oświetli postaci.
- [ ] Po jednym Reflection Probe na pomieszczenie: Type = Baked, Box Projection = ON, dopasowany do wymiarów pokoju, rozdzielczość 128.

### 2.4 Materiały emissive

- [ ] Klosze świetlówek i lampa nad stołem: materiały z Emission (HDR, intensywność ~2–4), Global Illumination = Baked. Emission ma się zgadzać kolorem ze światłem, które „udaje”.

### 2.5 Skrypt migotania

- [ ] Utwórz `Assets/Scripts/Graphics/FlickeringLight.cs`: komponent sterujący `Light.intensity` + emission materiału klosza; wzorzec migotania deterministyczny per instancja (seed z pozycji), parametry w Inspectorze (min/max intensity, częstotliwość, procent czasu w stanie „zgaszona”). Bez zależności sieciowych — czysto wizualny, lokalny.
- [ ] Podepnij do jednej świetlówki na korytarzu (światło tej jednej: Mode = Realtime, bez cieni, mały Range).
- [ ] Zdarzenie publiczne `OnFlicker` (UnityEvent) — Faza 6 podepnie pod nie dźwięk.

### 2.6 Fake volumetrics

- [ ] Stożek światła nad stołem przesłuchań: mesh stożka + materiał Shader Graph (URP Unlit, Transparent Additive, gradient przezroczystości od źródła do podstawy, fade przy przecięciu geometrii przez depth — soft particles/depth fade node). Zapisz shader jako `Assets/VFX/LightConeAdditive.shadergraph`, materiał `Assets/VFX/Mat_LightCone.mat`.
- [ ] Intensywność bardzo subtelna (widoczna, dopiero gdy się jej szuka). Bez cząsteczek kurzu — to Faza 6.

### 2.7 Bake i strojenie

- [ ] Wypiek draft (10 texels/unit) → ocena → korekty intensywności → wypiek finalny (20 texels/unit).
- [ ] Sprawdź artefakty: przecieki światła przez ściany (popraw grubość geometrii lub `Cast Shadows = Two Sided`), plamy na lightmapach (podnieś rozdzielczość obiektu), zbyt czarne cienie (delikatnie podnieś ambient).

### 2.8 Weryfikacja

- [ ] Unity Console bez błędów; kompilacja `FlickeringLight.cs` czysta.
- [ ] Zrzuty „po” per pomieszczenie: `Assets/Screenshots/gfx_faza2_<pomieszczenie>.png`.
- [ ] Postać testowa (dowolny prefab z `Assets/Characters/`) postawiona w 3 miejscach — odbiera światło z probe'ów i rzuca cień pod lampą.
- [ ] Scena zapisana przez Unity MCP; `git diff --check`; statusy zaktualizowane.

## Poza zakresem

Cząsteczki kurzu i synchronizacja dźwięku (Faza 6), wymiana materiałów ścian/podłóg (Faza 3), zmiany post-processingu (Faza 4 — jeśli światło wymaga korekty ekspozycji, zmień intensywności świateł, nie Volume).
