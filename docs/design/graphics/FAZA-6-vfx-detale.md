# Faza 6 — Ruch, VFX i mikrodetale

- **Status:** Open
- **Branch:** `gfx/faza-6-vfx-detale`
- **Zależności:** Faza 2 (`Done`), Faza 3 (`Done`)
- **Szacowany czas:** 1–2 tygodnie pracy agenta

Przeczytaj najpierw: [README.md](./README.md) i `AGENTS.md`.

## Cel / Definition of Done

Statyczna scena „żyje": kurz w snopach światła, animowane detale, deszcz za oknami, drzwi i krzesła reagujące fizycznie i dźwiękowo. Wszystko czysto kliencko-wizualne — zero wpływu na stan gry i sieć (poza już istniejącą synchronizacją drzwi/krzeseł, jeśli jest).

## Kontekst techniczny

- Istniejące VFX do wykorzystania/przeróbki: `Assets/VFX/VFX_Rain.prefab` (za okna), `VFX_Fireflies.prefab` (baza pod kurz), `VFX_Snow.prefab` (raczej nieużywany).
- Silnik cząsteczek: wbudowany **Particle System** (Shuriken). Nie instaluj VFX Graph — niepotrzebny na tę skalę.
- Dźwięki: `Assets/Audio/`. Brakujące SFX — `[CZŁOWIEK]` dostarcza (freesound/CC0) lub zadanie zostaje otwarte z listą potrzebnych plików.
- Skrypty wizualne: `Assets/Scripts/Graphics/` (m.in. `FlickeringLight.cs` z Fazy 2).

## Zadania

### 6.1 Cząsteczki atmosferyczne

- [ ] Kurz w stożku lampy przesłuchań (baza: `VFX_Fireflies`): drobinki, powolny dryf, alpha bardzo niska, Simulation Space = World, emisja ograniczona do objętości stożka; widoczne tylko pod światło.
- [ ] Para znad kubka kawy (automat/biurko): mały, zapętlony system, soft particles.
- [ ] Dym papierosowy w popielniczce pokoju przesłuchań: cienka smużka, turbulencja przez Noise module.

### 6.2 Animowane detale otoczenia

- [ ] Wentylator sufitowy: obrót skryptem `Assets/Scripts/Graphics/SlowRotator.cs` (prędkość w Inspectorze); rzuca ruchomy cień tylko jeśli mieści się w budżecie świateł — inaczej bez cienia.
- [ ] Migotanie świetlówki: podepnij SFX brzęczenia pod zdarzenie `OnFlicker` z `FlickeringLight` (Faza 2); AudioSource spatial (3D, logarithmic rolloff).
- [ ] Deszcz za oknami: `VFX_Rain` ograniczony do widoku zza szyb + delikatny decal/shader mokrych smug na szybach; pętla SFX deszczu przy oknach.
- [ ] Zegar ścienny z chodzącą wskazówką sekundową (skrypt lub animacja) + cichy tik w pobliżu.

### 6.3 Fizyczne reakcje świata

- [ ] Drzwi: dźwięk otwarcia/zamknięcia/skrzypienia podpięty do istniejącego systemu drzwi (sprawdź implementację w `Assets/Scripts/` — nie buduj drugiego systemu; jeśli drzwi nie mają jeszcze interakcji, zgłoś i ogranicz się do SFX zawiasów przy animacji).
- [ ] Krzesła: dźwięk szurania przy przesuwaniu (istniejący system `ChairSeat*`); drobne fizyczne propsy (kubek, długopis) z Rigidbody tylko jeśli projekt już synchronizuje fizykę propsów — inaczej statyczne.
- [ ] Kroki: jeśli istnieje system kroków — dopasuj SFX do materiałów podłóg (wykładzina/beton/płytki przez tagi fizyczne materiałów); jeśli nie istnieje — zadanie otwarte, nie buduj go w tej fazie.

### 6.4 Weryfikacja

- [ ] Wszystkie efekty czysto lokalne/wizualne — brak nowych komponentów sieciowych; test host+client (KCP), brak desync i błędów.
- [ ] Profiler: cząsteczki łącznie < 0.5 ms CPU; Console czyste.
- [ ] Zrzuty/nagranie: `Assets/Screenshots/gfx_faza6_*.png`; sceny zapisane; `git diff --check`; statusy zaktualizowane.

## Poza zakresem

Nowe mechaniki interakcji (fizyczne drzwi od zera, system kroków), okluzja dźwięku przez drzwi (należy do prac nad voice/akustyką, nie do grafiki).
