# Faza 3 — Materiały i modularny kit środowiska

- **Status:** Open
- **Branch:** `gfx/faza-3-materialy-kit`
- **Zależności:** Faza 2 (`Done`), Faza 1 (`Done` — zasady materiałowe z `ART-DIRECTION.md` są wiążące)
- **Szacowany czas:** 2–4 tygodnie, z udziałem użytkownika (import paczek tekstur/propsów)

Przeczytaj najpierw: [README.md](./README.md), `AGENTS.md`, `ART-DIRECTION.md` (sekcje Materiały i Paleta).

## Cel / Definition of Done

Graybox zastąpiony docelowymi materiałami PBR i modularnym kitem. Decale (brud, zacieki, plakaty) łamią powtarzalność. Pomieszczenia mają props i clutter. Wszystko w texel density 512 px/m i zgodne z paletą.

## Kontekst techniczny

- Istniejące materiały-szablony: `Assets/Characters/Mat_Template_*.mat` (Baseboard, Ceiling, Concrete, Oak, Plaster, Rubber) oraz `Assets/Materials/P2_*` z pierwszymi mapami (`P2_PlasterNormal.png`, `P2_RoughnessVariation.png`). Rozwijaj ten zestaw, nie twórz równoległego.
- Shader bazowy: **URP Lit** (workflow Metallic). Mapy: BaseMap + NormalMap + MetallicGlossMap (metallic w R, smoothness w A) + OcclusionMap.
- Docelowa struktura: `Assets/Materials/Posterunek/` (materiały), `Assets/Textures/Posterunek/` (tekstury — utwórz), `Assets/Prefabs/Kit/` (moduły — utwórz).
- Pakiet do zainstalowania: **com.unity.probuilder** (modelowanie modułów w edytorze). Instalacja przez Unity MCP `manage_packages`; nie edytuj `manifest.json` ręcznie, a `packages-lock.json` nigdy.

## Źródła tekstur i propsów (decyzja wykonawcza)

- Tekstury bazowe: **CC0** z ambientCG / PolyHaven (beton, tynk, PCV, drewno biurowe, metal malowany). `[CZŁOWIEK]` pobiera i wrzuca do `Assets/Textures/Posterunek/`; agent konfiguruje importy i materiały.
- Propsy: paczki z Asset Store o realistycznej stylistyce biurowej/policyjnej. **Nie Synty / low-poly flat-shaded** — zbyt rozpoznawalne i niezgodne z art direction. `[CZŁOWIEK]` kupuje/importuje; agent integruje.
- Zakaz generowania tekstur fabularnych (dokumenty, akta z treścią) przez AI w runtime — treści spraw są ręcznie autorskie (ADR-0010). Dekoracyjne tekstury tła (plamy, zacieki) mogą być generowane w edytorze.

## Zadania

### 3.1 Infrastruktura

- [ ] [AGENT] Zainstaluj `com.unity.probuilder` przez Unity MCP; zweryfikuj kompilację.
- [ ] [AGENT] Dodaj Renderer Feature **Decal** do `PC_Renderer` (Technique = Screen Space, Max Draw Distance = 50) i do `Mobile_Renderer` (lub świadomie pomiń na mobile — zanotuj decyzję).
- [ ] [AGENT] Utwórz katalogi `Assets/Textures/Posterunek/`, `Assets/Prefabs/Kit/`, `Assets/Prefabs/Props/`.
- [ ] [CZŁOWIEK] Import tekstur CC0 (lista minimalna: tynk malowany ×2 kolory, beton, wykładzina/lastryko, drewno biurko, metal malowany, płytki). Format 2K, z mapami normal/roughness.

### 3.2 Materiały bazowe (agent, po imporcie tekstur)

- [ ] Ustaw importy tekstur: sRGB tylko dla BaseMap, normal mapy jako Normal Map, kompresja BC7 (PC), Max Size 2048.
- [ ] Zbuduj/uzupełnij komplet materiałów w `Assets/Materials/Posterunek/`: ściana tynk (2 kolory), lamperia (dolna połowa ścian — klasyka posterunku), beton, podłoga, sufit, drewno, metal, szkło weneckie (przyciemniane, lekko refleksyjne).
- [ ] Każdy materiał: roughness z wariacją (mapa, nie stała), zgodność z zakazami palety (bez czystej bieli/czerni).
- [ ] Podmień materiały graybox na docelowe na istniejącej geometrii `Room.unity` (Unity MCP, `batch_execute`).

### 3.3 Modularny kit

- [ ] [AGENT] Zdefiniuj siatkę modułów: 0.5 m grid, wysokość ścian wg istniejącej sceny. Moduły: ściana pełna, ściana z drzwiami, ściana z oknem, ściana z lustrem weneckim, narożnik, listwa przypodłogowa, sufit kasetonowy 0.6×0.6.
- [ ] [AGENT] Wykonaj moduły ProBuilderem (lub uporządkuj istniejącą geometrię do prefabów), zapisz jako prefaby w `Assets/Prefabs/Kit/`, poprawne UV pod 512 px/m.
- [ ] [AGENT] Przebuduj `Room.unity` z kitu tam, gdzie geometria graybox na to pozwala; zachowaj wymiary pomieszczeń (gameplay/akustyka bez zmian).

### 3.4 Decale

- [ ] Utwórz 6–10 materiałów decal (URP Decal shader): zacieki pod oknami, przetarcia przy klamkach, rysy na lamperii, plamy na wykładzinie, żółknięcie sufitu przy świetlówkach.
- [ ] Rozmieść URP Decal Projectors w scenie — asymetrycznie, gęściej w miejscach „używanych" (przy drzwiach, przy biurkach).

### 3.5 Props i clutter

- [ ] [CZŁOWIEK] Import wybranych paczek propsów.
- [ ] [AGENT] Prefabizacja i rozmieszczenie: pokój przesłuchań (stół, 3 krzesła, popielniczka, lampa, lustro weneckie), korytarz (ławka, tablica korkowa, gaśnica, automat z kawą), archiwum (regały, segregatory, kartony), biurka (papiery, kubki, telefony, maszyna do pisania/komputer CRT).
- [ ] [AGENT] Ujednolicenie: materiały propsów przepięte na URP Lit, kolory dopasowane do palety, statyczne propsy z flagą `ContributeGI`.

### 3.6 Re-bake i weryfikacja

- [ ] Ponowny bake lightmap (ustawienia z Fazy 2) — nowe materiały zmieniają GI (albedo bounce).
- [ ] Zrzuty per pomieszczenie: `Assets/Screenshots/gfx_faza3_<pomieszczenie>.png`.
- [ ] Budżet: pojedynczy pokój ≤ 150 tys. trisów — sprawdź statystyki przez Unity MCP/profiler.
- [ ] Console czyste, sceny zapisane, `git diff --check`, statusy zaktualizowane.

## Poza zakresem

Materiały postaci (Faza 5), cząsteczki i animowane detale (Faza 6), LOD-y (Faza 7).
