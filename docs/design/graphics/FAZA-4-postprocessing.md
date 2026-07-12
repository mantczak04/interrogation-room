# Faza 4 — Post-processing i kamera

- **Status:** Review
- **Branch roboczy pakietu:** `gfx/graphics-overhaul`
- **Branch historyczny fazy:** `gfx/faza-4-postprocessing`
- **Zależności:** Faza 0 (`Approved` na `gfx/graphics-overhaul` lub `Done` na `main`)
- **Szacowany czas:** 1–2 dni pracy agenta

Przeczytaj najpierw: [README.md](./README.md), [AUTONOMOUS-GRAPHICS-RUN.md](./AUTONOMOUS-GRAPHICS-RUN.md) i `AGENTS.md`.

## Cel / Definition of Done

Globalny Volume w `Room.unity` daje kinowy, noirowy obraz: ACES, grading, subtelny bloom, vignette, grain. Kamera gracza ma poprawny antyaliasing. Efekt działa na obu tierach jakości (na Mobile okrojony).

## Kontekst techniczny

- Istniejący profil: `Assets/Settings/PosterunekPostFX.asset` — **modyfikuj ten profil**, nie twórz nowego. Sprawdź najpierw przez Unity MCP, jakie overridy już zawiera i który obiekt Volume w `Room.unity` go używa (jeśli żaden — utwórz globalny Volume `PostFX_Global`).
- Pozostałe profile (`Postprocessing Profile.asset`, `SampleSceneProfile.asset`, `DefaultVolumeProfile.asset` w `Assets/SourceFiles/Settings/`) — nie ruszaj, to spuścizna szablonu.
- Kamera gracza: zlokalizuj w prefabie gracza (`Assets/Prefabs/Player.prefab` / `PlayerRobot.prefab`) lub w scenie; projekt używa Cinemachine 3.1.6 — jeśli kamerą steruje CinemachineCamera, ustawienia post/AA są na `Camera` z `UniversalAdditionalCameraData`, nie na wirtualnej kamerze.
- Bez nowych pakietów.

## Zadania

### 4.1 Volume — overridy profilu `PosterunekPostFX`

Wartości startowe (strojenie w 4.3):

- [x] **Tonemapping:** Mode = ACES.
- [x] **Color Adjustments:** Post Exposure = 0 (ekspozycję reguluje światło, nie post!), Contrast = +15, Saturation = −8, Color Filter = biały (bez filtra; klimat robi White Balance).
- [x] **White Balance:** Temperature = −5 (chłodniej), Tint = +3 (delikatnie ku zieleni świetlówek).
- [x] **Lift Gamma Gain:** Lift lekko uniesiony ku teal (ciemności nigdy czysto czarne), Gain lekko ciepły — wartości subtelne (|offset| ≤ 0.05).
- [x] **Bloom:** Threshold = 1.1, Intensity = 0.25, Scatter = 0.6 — łapie tylko emissive i lampę.
- [x] **Vignette:** Intensity = 0.3, Smoothness = 0.4.
- [x] **Film Grain:** Type = Thin 1, Intensity = 0.2, Response = 0.8.
- [x] **Chromatic Aberration:** Intensity = 0.05.
- [x] **Motion Blur:** brak override'u / wyłączony (multiplayer, czytelność).
- [x] **Depth of Field:** wyłączony w gameplayu (dozwolony w przyszłości w menu — poza zakresem).
- [x] Volume `PostFX_Global` w `Room.unity`: Mode = Global, Priority = 0, Profile = `PosterunekPostFX`.

### 4.2 Kamera

- [x] Na kamerze gracza: Post Processing = ON, Anti-aliasing = **SMAA (High)** (TAA dopiero po testach ghostingu w multiplayerze — zanotuj jako decyzję odroczoną), HDR = Use Pipeline Settings, Stop NaN = OFF, Dithering = ON.
- [x] Zweryfikuj FOV (Cinemachine): wartość 55–65; jeśli poza zakresem — zaproponuj zmianę użytkownikowi zamiast zmieniać samodzielnie (FOV wpływa na gameplay).

  `Player/Main Camera` ma FOV 60. `PlayerRobot/PlayerFollowCamera` i `RobotCamera` mają FOV 40; pozostawiono je bez zmian jako decyzję gameplayową do zatwierdzenia. TAA pozostaje odroczone do testów ghostingu w multiplayerze.

### 4.3 Strojenie i tier Mobile

- [x] Porównaj obraz przed/po na tych samych kadrach; skoryguj Contrast/Bloom/Vignette, jeśli obraz jest przepalony lub zbyt ciemny do gry.
- [x] Sprawdź scenę na Quality tier Mobile: jeśli koszt za wysoki, na Mobile wyłącz Film Grain i Chromatic Aberration (osobny, odchudzony profil `PosterunekPostFX_Low.asset` + przełączanie tieru — tylko jeśli potrzebne; nie komplikuj na zapas).

  Ten sam kadr na tierze Mobile zachował czytelność i efekt profilu; nie wykazano potrzeby tworzenia `PosterunekPostFX_Low.asset`.

### 4.4 Weryfikacja

- [x] Console czyste; zrzuty przed/po: `Assets/Screenshots/gfx_faza4_before.png` / `gfx_faza4_after.png` (ten sam kadr!).
- [x] Scena i profil zapisane przez Unity MCP; `git diff --check`; statusy zaktualizowane (ten plik + `README.md`).

## Poza zakresem

Zmiany świateł w scenie (Faza 2), LUT autorski (wymaga człowieka i DCC — osobna decyzja po Fazie 1), UI/menu.
