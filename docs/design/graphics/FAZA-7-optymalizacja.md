# Faza 7 — Optymalizacja i skalowalność

- **Status:** Open
- **Branch:** `gfx/faza-7-optymalizacja`
- **Zależności:** finalny pass po Fazach 3, 5, 6; ale sekcja „Budżety" obowiązuje wszystkie fazy od początku.
- **Szacowany czas:** 1–2 tygodnie na końcu + kontrole ciągłe

Przeczytaj najpierw: [README.md](./README.md) i `AGENTS.md`.

## Budżety (obowiązują wszystkie fazy)

| Metryka | Cel PC (High) | Cel Low/Mobile |
|---|---|---|
| Klatka | ≤ 16.6 ms (60 FPS) | ≤ 33 ms (30 FPS) |
| Realtime światła z cieniami w kadrze | ≤ 4 | ≤ 2 |
| Trisy w kadrze | ≤ 500 tys. | ≤ 200 tys. |
| SetPass calls | ≤ 150 | ≤ 80 |
| Pamięć tekstur | ≤ 2 GB | ≤ 1 GB |
| Postać | ≤ 40 tys. trisów, ≤ 2 materiały | LOD1 ≤ 15 tys. |

## Cel / Definition of Done

Gra trzyma budżety na sprzęcie referencyjnym w scenie pełnej (5 graczy, pełny art). Tier Low działa i wygląda akceptowalnie. Wyniki profilowania udokumentowane w tym pliku.

## Kontekst techniczny

- Narzędzia: Unity Profiler przez Unity MCP (`manage_profiler`), Frame Debugger, statystyki Game view. RenderDoc — `[CZŁOWIEK]`, jeśli potrzebna głębsza analiza GPU.
- Sprzęt referencyjny: `[CZŁOWIEK]` wskazuje docelowy minimalny GPU (propozycja: GTX 1060 / RX 580 dla 60 FPS w 1080p). Bez tej decyzji profilowanie na maszynie deweloperskiej + zapas 40%.
- Test wieloosobowy: ParrelSync (KCP), scena z 4–5 postaciami.

## Zadania

### 7.1 Pomiar bazowy

- [ ] Profil CPU/GPU w 5 kadrach referencyjnych (pokój przesłuchań przy stole, korytarz wzdłuż, archiwum, widok przez lustro weneckie, kadr z 5 postaciami). Zapisz wyniki w sekcji „Wyniki" poniżej.
- [ ] Frame Debugger: policz SetPass calls i zidentyfikuj 5 najdroższych draw calls.

### 7.2 Redukcje

- [ ] LOD Groups na propsach > 5 tys. trisów (LOD1 = 40%, cull 2–3% wysokości ekranu); postacie: LOD1 ≤ 15 tys. trisów.
- [ ] Static batching zweryfikowany na całej geometrii kitu; sprawdź, czy decale nie łamią batchy nadmiernie.
- [ ] Occlusion Culling: bake occlusion (pomieszczenia + drzwi jako portale) — wnętrza to idealny przypadek; zweryfikuj w widoku Occlusion, że sąsiednie pokoje są cullowane.
- [ ] Tekstury: audyt rozmiarów (Max Size adekwatny do texel density; propsy tła 1K), kompresja BC7/DXT5, mipmapy ON.
- [ ] Światła: zweryfikuj limit realtime cieni; nadmiarowe światła → Baked.
- [ ] Cząsteczki: overdraw check (Scene view Overdraw mode); zmniejsz rozmiary/liczbę, jeśli > 0.5 ms.

### 7.3 Tier Low

- [ ] Przejdź grę na Quality „Low/Mobile": render scale 0.85 jeśli potrzeba, cienie 2048/1024, bez SSAO, bez grain/CA (zgodnie z Fazą 4). Zweryfikuj czytelność (ciemne strefy nadal ciemne, ale gra grywalna).

### 7.4 Weryfikacja końcowa

- [ ] 5 kadrów referencyjnych w budżecie na obu tierach; wyniki wpisane poniżej.
- [ ] Test 2× klient (ParrelSync): brak spadków przy 5 postaciach animowanych z lip-sync.
- [ ] Console czyste; `git diff --check`; statusy zaktualizowane.

## Poza zakresem

Optymalizacja sieci/CPU gameplayu (nie-graficzna), build pipeline, streaming assetów.

## Wyniki

_(agent wypełnia: tabela kadr × ms CPU/GPU × SetPass × trisy, przed i po redukcjach)_
