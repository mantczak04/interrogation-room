# Faza 5 — Postacie

- **Status:** Open
- **Branch:** `gfx/faza-5-postacie`
- **Zależności:** Faza 1 (`Done` — zasady postaci z `ART-DIRECTION.md`), Faza 2 (`Done` — probes muszą istnieć, żeby oceniać postacie w docelowym świetle)
- **Szacowany czas:** 2–4 tygodnie, częściowo `[CZŁOWIEK]`

Przeczytaj najpierw: [README.md](./README.md), `AGENTS.md`, `ART-DIRECTION.md` (sekcja Postacie).

## Cel / Definition of Done

Czwórka postaci (Jak, Karton, Małpa, Wieprz) ma docelowe materiały PBR, komplet animacji lokomocji i gestów przez blend trees oraz lip-sync z głosu. Sylwetki czytelne w półmroku. Brak riggu mimiki — celowa, sztywna absurdalność.

## Kontekst techniczny

- Postacie: `Assets/Characters/{Jak,Karton,Malpa,Wieprz}/` + `Assets/Characters/Animations/`, instancje w `Assets/Characters/Instances/`, generowane elementy w `Assets/Characters/Generated/`.
- Prefaby gracza: `Assets/Prefabs/Player.prefab`, `Assets/Prefabs/PlayerRobot.prefab` — sprawdź, który jest sieciowym prefabem Mirror i jak podpinany jest model postaci.
- **Voice = Vivox** (`com.unity.services.vivox` 16.11.0 w projekcie). Lip-sync musi czerpać amplitudę z Vivox (`IParticipant.AudioEnergy` / tap na `VivoxParticipant`), nie z `Microphone` bezpośrednio.
- Budżet per postać: ≤ 40 tys. trisów, 1 materiał (max 2), tekstury 2K.
- Animator per postać wspólny (jeden `AnimatorController`, cztery avatary), chyba że proporcje szkieletów to uniemożliwiają.

## Pakiety / zasoby do pozyskania

- [CZŁOWIEK] Paczka animacji mocap humanoidalnych (idle z wariacjami, walk/run 8-kierunkowe, siadanie/wstawanie, 6–10 gestów rozmowy, wskazanie palcem, wzruszenie ramion). Wymóg: rig Humanoid, licencja komercyjna.
- [AGENT] **uLipSync** (MIT): instalacja przez Unity MCP `manage_packages` z git URL `https://github.com/hecomi/uLipSync.git#upm`. Jeśli analiza fonemów okaże się zbędna, dopuszczalny fallback: własny skrypt amplitudowy (jaw-flap) — prostszy i pewniejszy przy sztywnych pyskach.

## Zadania

### 5.1 Audyt i rig

- [ ] [AGENT] Audyt czwórki modeli: liczba trisów, rig (Humanoid/Generic), materiały, tekstury. Wynik zapisz w sekcji „Wynik audytu" poniżej.
- [ ] [AGENT] Ujednolić import: Rig = Humanoid (jeśli szkielety pozwalają), poprawne avatary, Optimize Game Objects = OFF (dopóki debugujemy).

### 5.2 Materiały PBR postaci

- [ ] [AGENT] Materiały URP Lit per postać: normal mapy z detalem (sierść Małpy/Wieprza jako normal+roughness, nie geometria; tektura Kartonu z widocznym flutingiem na krawędziach), roughness z wariacją.
- [ ] [AGENT] Test czytelności: każda postać w 3 warunkach (lampa przesłuchań, korytarz świetlówki, ciemny kąt) — sylwetka rozpoznawalna; zrzuty `gfx_faza5_<postac>_<warunek>.png`.

### 5.3 Animacje

- [ ] [CZŁOWIEK] Import paczki mocap.
- [ ] [AGENT] `AnimatorController` w `Assets/Characters/Animations/`: warstwa Locomotion (blend tree 2D — prędkość × kierunek), warstwa Gesture (override, gesty wyzwalane triggerami), warstwa Sitting (przesłuchanie — stany siedzenia zgodne z istniejącym systemem `ChairSeat*`).
- [ ] [AGENT] Idle z 2–3 wariacjami (losowy dobór, żeby postacie w kadrze nie oddychały synchronicznie).
- [ ] [AGENT] Podpięcie parametrów Animatora pod istniejący kontroler ruchu gracza (prędkość z `CharacterController`/rigidbody — sprawdź implementację w `Assets/Scripts/`). Synchronizacja przez Mirror: użyj `NetworkAnimator` lub istniejącego wzorca projektu — nie wymyślaj własnej synchronizacji.

### 5.4 Lip-sync

- [ ] [AGENT] Spike: pobierz energię audio mówiącego gracza z Vivox (lokalnie `AudioEnergy` uczestnika). Jeśli API nie daje energii zdalnych uczestników w użytecznej formie — status `Blocked` z opisem i propozycją (np. tap na AudioSource uczestnika).
- [ ] [AGENT] Komponent `Assets/Scripts/Graphics/JawFlap.cs`: mapuje energię głosu na kość żuchwy/blendshape „open" z wygładzaniem (attack szybki, release ~150 ms). Parametry w Inspectorze.
- [ ] [AGENT] Fallback bez blendshape'ów (Karton): skalowanie/rotacja segmentu „klapy" kartonu — ten sam komponent, inny target.
- [ ] [AGENT] Test dwuosobowy (ParrelSync, KCP): usta ruszają się u zdalnego gracza zgodnie z jego mową.

### 5.5 Weryfikacja

- [ ] Console czyste, kompilacja czysta; test host+client lokalnie (KCP) — postacie animują się i lip-sync działa po sieci.
- [ ] Budżety trisów dotrzymane; `git diff --check`; statusy zaktualizowane.

## Poza zakresem

Rig mimiki twarzy (celowo brak), ragdoll `Egzekucji` (osobna decyzja gameplayowa), nowe postacie ponad istniejącą czwórkę.

## Wynik audytu

_(agent wypełnia po zadaniu 5.1)_
