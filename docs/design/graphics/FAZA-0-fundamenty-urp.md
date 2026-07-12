# Faza 0 — Fundamenty techniczne URP

- **Status:** In progress
- **Branch:** `gfx/faza-0-fundamenty-urp`
- **Zależności:** brak (pierwsza faza)
- **Szacowany czas:** 1–2 dni pracy agenta

Przeczytaj najpierw: [README.md](./README.md) (zasady pracy) i `AGENTS.md`.

## Cel / Definition of Done

Pipeline URP jest poprawnie skonfigurowany pod kinowe wnętrza: HDR, poprawne cienie, SSAO na PC, rozsądny podział PC/Mobile. Żadnych zmian artystycznych w scenie — wyłącznie konfiguracja renderowania. Po tej fazie każda dalsza praca świetlno-materiałowa ma poprawny fundament.

## Kontekst techniczny (stan zastany)

- Pipeline assety: `Assets/SourceFiles/Settings/PC_RPAsset.asset` i `Assets/SourceFiles/Settings/Mobile_RPAsset.asset`.
- Renderery: `Assets/SourceFiles/Settings/PC_Renderer.asset`, `Assets/SourceFiles/Settings/Mobile_Renderer.asset`.
- Global settings: `Assets/SourceFiles/Settings/UniversalRenderPipelineGlobalSettings.asset`.
- Color Space jest już **Linear** (`ProjectSettings/ProjectSettings.asset`, `m_ActiveColorSpace: 1`) — tylko zweryfikuj, nie zmieniaj.
- URP w wersji 17.5.0 (Unity 6). Żadne nowe pakiety nie są potrzebne w tej fazie.
- Scena robocza: `Assets/Scenes/Room.unity`.

Wszystkie zmiany pipeline/renderer wykonuj przez Unity MCP (`manage_asset`, `manage_graphics` lub odpowiedni custom tool — sprawdź najpierw `mcpforunity://custom-tools`). Nie edytuj tych `.asset` jako tekstu.

## Zadania

### 0.1 Audyt (bez zmian, wynik zapisz w sekcji „Wynik audytu" na dole tego pliku)

- [x] Odczytaj przez Unity MCP bieżące wartości `PC_RPAsset`: HDR, Render Scale, MSAA, Shadow Distance, Main Light Shadow Resolution, Additional Lights (tryb, limit per object, shadow atlas), Soft Shadows, Cascade Count, LOD Cross Fade.
- [x] To samo dla `Mobile_RPAsset`.
- [x] Odczytaj z `PC_Renderer` i `Mobile_Renderer` Rendering Path oraz listę Renderer Features.
- [x] Sprawdź w Quality Settings (`ProjectSettings` przez Unity MCP), które poziomy jakości wskazują który RP asset. Zanotuj mapowanie.

### 0.2 Konfiguracja PC (`PC_RPAsset` + `PC_Renderer`)

- [x] HDR: włączone, precision domyślna (R11G11B10).
- [x] Rendering Path na `PC_Renderer`: **Forward+** (dużo lokalnych świateł we wnętrzu bez limitu per-object).
- [x] Cienie: Shadow Distance 30 m (gra w całości we wnętrzu), Cascade Count 2, Main Light Shadow Resolution 4096, Additional Lights Shadow Atlas 4096, Soft Shadows ON (jakość High), Conservative Enclosed Space Culling ON jeśli dostępne.
- [x] MSAA: Disabled (antyaliasing zrobi kamera w Fazie 4 — SMAA/TAA; MSAA + SSAO to zbędny koszt).
- [x] Render Scale: 1.0.
- [x] LOD Cross Fade: ON.
- [x] Dodaj Renderer Feature **Screen Space Ambient Occlusion** do `PC_Renderer`: Method = Blue Noise, Intensity = 1.5, Radius = 0.3, Falloff Distance = 50, Downsample = OFF, After Opaque = OFF, Normal Quality = Medium.

### 0.3 Konfiguracja Mobile/Low (`Mobile_RPAsset` + `Mobile_Renderer`)

- [x] HDR: włączone (tonemapping w Fazie 4 musi działać na obu tierach).
- [x] Cienie: Shadow Distance 20 m, Cascade Count 1, Main Light Shadows 2048, Additional Lights Shadow Atlas 1024, Soft Shadows ON (Low).
- [x] Bez SSAO na `Mobile_Renderer`.
- [x] Render Scale: 1.0 (skalowanie w dół to decyzja Fazy 7).

### 0.4 Quality tiers

- [x] Upewnij się, że istnieją co najmniej dwa poziomy Quality: „PC/High” → `PC_RPAsset`, „Low/Mobile” → `Mobile_RPAsset`; domyślny dla Standalone = PC/High. Popraw mapowanie, jeśli jest inne.

### 0.5 Weryfikacja

- [ ] Brak błędów w Unity Console (najpierw `Error`, potem `Warning` z filtrem, mały limit).
- [ ] Otwórz `Room.unity`, wykonaj zrzuty „po" do `Assets/Screenshots/gfx_faza0_room.png` (Game view, ta sama pozycja kamery co istniejące zrzuty `sit_check_*`, jeśli to możliwe).
- [ ] Zapisz zmodyfikowane assety/sceny przez Unity MCP; `git diff --check`.
- [ ] Zaktualizuj statusy: ten plik + tabela w `README.md` → `Review`.

## Poza zakresem tej fazy

Decal Renderer Feature (Faza 3), zmiany świateł w scenie (Faza 2), post-processing (Faza 4), jakiekolwiek zmiany materiałów.

## Wynik audytu

- `PC_RPAsset`: HDR ON (precision 32 Bits / R11G11B10), Render Scale 1.0, MSAA Disabled, Shadow Distance 35 m, Main Light Shadow Resolution 2048, Additional Lights Per Pixel (limit 4 per object, shadows ON, atlas 1024), Soft Shadows ON (High), 4 cascades, LOD Cross Fade ON (Blue Noise).
- `Mobile_RPAsset`: HDR ON (precision 32 Bits / R11G11B10), Render Scale 0.8, MSAA Disabled, Shadow Distance 50 m, Main Light Shadow Resolution 1024, Additional Lights Per Pixel (limit 4 per object, shadows OFF, atlas 2048), Soft Shadows OFF (quality value Medium), 1 cascade, LOD Cross Fade ON (Blue Noise).
- `PC_Renderer`: Forward+; jedna aktywna Renderer Feature — Screen Space Ambient Occlusion (Blue Noise, Intensity 0.4, Radius 0.3, Falloff 100, Downsample OFF, After Opaque OFF, Depth Normals / Normal Quality Medium).
- `Mobile_Renderer`: Forward+; brak Renderer Features.
- Quality Settings: `Mobile` → `Mobile_RPAsset`, `PC` → `PC_RPAsset`; aktywny i domyślny poziom dla bieżącego celu Standalone: `PC`.
- Color Space: Linear (zweryfikowane, bez zmiany).
- Dostępne w tej wersji URP ustawienie `Conservative Enclosing Sphere` na `PC_RPAsset` było już włączone.
