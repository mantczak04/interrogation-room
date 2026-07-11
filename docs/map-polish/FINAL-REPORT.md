# Posterunek — raport końcowy polishu mapy

## Wynik

`Assets/Scenes/Room.unity` zachowuje pięć wymaganych przestrzeni, obrys `17,6 × 17,3 m`, wypukłe i zamknięte pokoje oraz drzwi jako jedyne portale dźwięku. Trzy iteracje zostały ukończone i zapisane w osobnych commitach; po ostatniej iteracji wykonano dodatkowy pełny bake, gauntlet oraz zapis sceny.

## Najważniejsze zmiany

- Powiększono Pokój Socjalny i Archiwum do `4,0 × 4,8 m`, przesuwając południową ścianę do `z = -7,6`.
- Poszerzono wszystkie cztery otwory drzwiowe do `1,5 m` i przebudowano segmenty ścian, nadproża, skrzydła oraz progi.
- Przestawiono meble według zmierzonych granic rendererów; dodano drugi klaster wypoczynkowy w Sali Wspólnej.
- Powierzchnie konstrukcyjne otrzymały CC0 PBR: tynk, podłogi, sufit i drewno, z gęstością `250–530 px/m`, listwami i progami.
- Dodano tablicę ogłoszeń, fizyczne tabliczki z emissive piktogramami i osiem widocznych opraw światła.
- Ustawiono siedem świateł `Baked` i jeden reflektor `Mixed` w Pokoju Przesłuchań; wygenerowano trzy atlasy, Light Probes i pięć Reflection Probes.
- `PosterunekPostFX.asset` zawiera cztery trwałe sub-assets: ACES, Bloom, Vignette i Color Adjustments.
- Poprawiono sześć colliderów do granic rendererów oraz odsunięto kosz Archiwum od podejścia do drzwi.

## Kryteria akceptacji

- [x] Pełny physics gauntlet jest zielony po finalnym bake: 6 spawnów, 4 drzwi, korytarz, 7 odcinków tras, 2 pola rozmowy i 5 stref.
- [x] Kapsuła `r = 0,45`, `h = 2,0` przechodzi wszędzie; dodatkowe casty dla efektywnego `r = 0,70` potwierdzają `0,25 m` zapasu. Przy wschodnim końcu korytarza trasa łagodnie omija wieszak.
- [x] 29/29 powierzchni konstrukcyjnych i 18/18 elementów trim ma komplet map PBR; 139/139 aktywnych rendererów ma prawidłowy materiał, bez `Default-Material`, nulli i error shaderów.
- [x] Brak widocznych szwów i rozciągnięć w 14 finalnych ujęciach z wysokości gracza, w tym przez wszystkie drzwi.
- [x] Finalny bake: 3 atlasy, 124/124 renderery z prawidłowym indeksem lightmapy, 178 pozycji / 172 baked Light Probes oraz 5/5 baked Reflection Probes.
- [x] Osiem aktywnych świateł ma widoczne emissive oprawy; siedem jest `Baked`, jedno `Mixed`, zero świateł `Realtime` rzuca cień.
- [ ] Stabilne 60+ FPS w Game View — **NOT RUN**. Polecenie zabrania wejścia w Play Mode, a Edit Mode nie daje wiarygodnej próbki runtime FPS.
- [x] Pięć dodanych zestawów PBR jest CC0; każdy ma `SOURCE.md`, pełną licencję i checksumy. Zweryfikowano 20 zachowanych plików: `20/20` zgodnych, `0` błędnych.
- [x] Po czasowym ukryciu `Meble`: 514 próbek obwodu bez dodatkowych otworów, 45/45 próbek sufitu i podłogi, jeden portal Pokoju Przesłuchań, brak szczelin do NE/SE voidów. `Meble` ponownie włączono.
- [x] Scena zapisana przez Unity MCP; Console Errors: `0`.
- [x] Nie zmieniono `RoundEngine`, `NetworkRoundCoordinator`, `SteamLobby`, Mirror ani katalogów vendorowych.

## Przed / po

| Przestrzeń | Przed | Po finalnym bake |
| --- | --- | --- |
| Sala Wspólna | [ujęcie 1](screenshots/baseline/baseline_sala_01.png), [ujęcie 2](screenshots/baseline/baseline_sala_02.png) | [ujęcie A](screenshots/final/FINAL_SalaWspolna_A.png), [ujęcie B](screenshots/final/FINAL_SalaWspolna_B.png) |
| Pokój Przesłuchań | [ujęcie 1](screenshots/baseline/baseline_przesluchania_01.png), [ujęcie 2](screenshots/baseline/baseline_przesluchania_02.png) | [ujęcie A](screenshots/final/FINAL_PokojPrzesluchan_A.png), [ujęcie B](screenshots/final/FINAL_PokojPrzesluchan_B.png) |
| Pokój Socjalny | [ujęcie 1](screenshots/baseline/baseline_socjalny_01.png), [ujęcie 2](screenshots/baseline/baseline_socjalny_02.png) | [ujęcie A](screenshots/final/FINAL_PokojSocjalny_A.png), [ujęcie B](screenshots/final/FINAL_PokojSocjalny_B.png) |
| Archiwum | [ujęcie 1](screenshots/baseline/baseline_archiwum_01.png), [ujęcie 2](screenshots/baseline/baseline_archiwum_02.png) | [ujęcie A](screenshots/final/FINAL_Archiwum_A.png), [ujęcie B](screenshots/final/FINAL_Archiwum_B.png) |
| Korytarz | [ujęcie 1](screenshots/baseline/baseline_korytarz_01.png), [ujęcie 2](screenshots/baseline/baseline_korytarz_02.png) | [A → B](screenshots/final/FINAL_Korytarz_AtoB.png), [B → A](screenshots/final/FINAL_Korytarz_BtoA.png) |

Finalny katalog zawiera również cztery ujęcia przez drzwi: `docs/map-polish/screenshots/final/`.

## Otwarte decyzje dla zespołu

1. Zatwierdzić szerokość drzwi `1,5 m` zamiast pierwotnych `1,2 m`.
2. Zatwierdzić powiększenie południowego skrzydła do `z = -7,6` i pokoje `4,0 × 4,8 m`.
3. Zdecydować, czy piktogramy na tabliczkach są docelowe, czy później dodać tekst z osobnym, zatwierdzonym fontem CC0.
4. Jeśli zespół wymaga idealnie prostej trasy o efektywnym `r = 0,70` przez cały korytarz, przesunąć wieszak; obecna drożna trasa ma niewielkie ominięcie przy wschodniej ścianie.

## Operacja niewykonana w tej sesji

Nie wykonano tylko runtime pomiaru 60+ FPS. Wszystkie pozostałe operacje Unity Editor zostały wykonane przez Unity MCP.

```text
Use the `computer-use:computer-use` skill to perform the following operation in Unity Editor.

Project: C:\Users\Piotr\Documents\Unity projects\interrogation-room
Scene/object/asset: Assets/Scenes/Room.unity, Map_Graybox, Game View
Goal: Verify that the final Posterunek map sustains at least 60 FPS in Game View at the project's target resolution and quality settings.

Unity MCP cannot provide a meaningful runtime FPS sample while the originating task prohibits Play Mode. First obtain explicit user authorization to enter Play Mode for this performance-only check. Make no scene, prefab, material, lighting, package, or script changes. Do not edit raw YAML, do not modify unrelated files, and do not revert existing user changes.

After the check:
1. exit Play Mode without saving runtime state,
2. check Console for Errors,
3. visually verify all five spaces render correctly and report resolution, quality level, sampling duration, average FPS, and minimum observed FPS,
4. report any remaining problems without attempting unrelated fixes.
```
