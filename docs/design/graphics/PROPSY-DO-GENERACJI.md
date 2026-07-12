# Propsy do generacji — zastąpienie darmowych modeli

- **Status:** Propozycja (2026-07-12)
- **Powiązane:** [FAZA-3-materialy-kit.md](./FAZA-3-materialy-kit.md), [ART-DIRECTION.md](./ART-DIRECTION.md), [MAP-MVP.md](../MAP-MVP.md)
- **Decyzja:** postacie generowane (Jak, Małpa, Wieprz, Karton) mają wyższą jakość niż darmowe paczki, więc propsy również generujemy zamiast importować z Asset Store / Kenney. Aktualizuje to decyzję wykonawczą z Fazy 3 („paczki z Asset Store") — źródłem propsów staje się generacja w stylu spójnym z postaciami.

## Co zastępujemy, co zostaje

| Źródło | Zawartość | Decyzja |
|---|---|---|
| `Assets/ThirdParty/Kenney/FurnitureKit/` | 31 mebli low-poly flat-shaded | **Zastąpić** — niezgodne z art direction (zakaz low-poly flat-shaded) |
| `Assets/ThirdParty/Quaternius/UltimateGunPack/` | `Pistol_1.fbx` | **Zastąpić** — broń Detektywa widoczna z bliska |
| `Assets/ThirdParty/PolyHaven/`, `ambientCG/` | tekstury CC0 PBR | **Zostaje** — tekstury są zgodne z Fazą 3 |
| `Assets/ThirdParty/OpenGameArt/MuzzleFlash0` | sprite VFX | Zostaje do Fazy 6 |
| `Assets/ThirdParty/Freesound/` | audio | Zostaje — poza zakresem |

## Wytyczne wspólne dla każdego propsa

- Styl: stylizowany realizm wg `ART-DIRECTION.md` — wiarygodne proporcje, uproszczony detal, duże czytelne plamy materiału, ślady użycia tam, gdzie faktycznie dotykają ręce/stopy.
- Epoka: posterunek z lat 80.–90. (Europa Wschodnia). Bez nowoczesnej elektroniki — CRT, maszyna do pisania, telefon tarczowy, magnetofon.
- Paleta: orzech `#6B4935`, grafit `#465059`, papier `#D5CCB6`, szałwia `#7C8277`; bez czystej bieli/czerni, bez sygnałowej czerwieni `#C22E28`.
- Budżet: props hero (oglądany z bliska) 3–8k trisów, props tła 1–3k, clutter 0.3–1.5k. Pokój łącznie ≤ 150k trisów (Faza 3).
- Tekstury: PBR Metallic (BaseMap + Normal + MetallicGloss + AO), texel density 512 px/m, roughness zawsze z wariacją.
- Pivot na podstawie obiektu, skala rzeczywista w metrach, poprawne osadzenie na gridzie 0.5 m.

## Priorytet 1 — Pokój przesłuchań (serce gry, kadry z bliska)

| # | Asset | Uwagi |
|---|---|---|
| 1 | Stół przesłuchań | masywny, metalowa rama + blat, uchwyt na kajdanki, przetarcia na krawędziach |
| 2 | Krzesło metalowe proste (podejrzany) | zastępuje `chair.fbx`; wariant lekko pogięty |
| 3 | Krzesło detektywa | prostsze biurowe, inne niż krzesło podejrzanego |
| 4 | Lampa wisząca nad stołem | emaliowany klosz, wolfram ~3000 K — kluczowy motyw świetlny Fazy 2 |
| 5 | Magnetofon szpulowy/kasetowy | rejestracja przesłuchań, klimat epoki |
| 6 | Popielniczka + zgniecione niedopałki | clutter hero — leży na stole w kadrze |
| 7 | Teczka akt / rozłożone dokumenty | papier `#D5CCB6`; bez treści fabularnych (ADR-0010) |
| 8 | Rewolwer / pistolet Detektywa | zastępuje `Pistol_1.fbx`; widoczny w FPP przy Egzekucji |

## Priorytet 2 — Sala wspólna (spawn, najwięcej czasu graczy)

| # | Asset | Uwagi |
|---|---|---|
| 9 | Biurko policyjne | metalowo-drewniane, zastępuje `desk.fbx` |
| 10 | Krzesło biurowe obrotowe | zużyta tapicerka, zastępuje `chairDesk.fbx` |
| 11 | Szafka kartotekowa (filing cabinet) | grafit `#465059`, wgniecenia; zastępuje `kitchenCabinet.fbx` w tej roli |
| 12 | Maszyna do pisania | props epoki na wybranych biurkach |
| 13 | Komputer CRT + klawiatura | zastępuje `computerScreen.fbx` + `computerKeyboard.fbx` |
| 14 | Telefon stacjonarny (tarczowy) | na biurkach |
| 15 | Lampka biurkowa (bankierka/metalowa) | wolframowy akcent na biurkach |
| 16 | Tablica korkowa ze sprawą | pinezki, sznurki, puste zdjęcia — bez treści fabularnych |
| 17 | Regał/biblioteczka na segregatory | zastępuje `bookcaseClosed/Open.fbx` |
| 18 | Kosz na śmieci metalowy | zastępuje `trashcan.fbx` |
| 19 | Wieszak stojący | zastępuje `coatRackStanding.fbx`; płaszcz i kapelusz jako detal |
| 20 | Clutter biurkowy (zestaw) | kubki, sterty papierów, segregatory, pieczątki — jeden atlas |

## Priorytet 3 — Korytarz i archiwum

| # | Asset | Uwagi |
|---|---|---|
| 21 | Ławka poczekalni | drewno + metal, zastępuje `bench.fbx` |
| 22 | Gaśnica + wieszak ścienny | czerwień przesunięta w rdzę (zakaz `#C22E28`) |
| 23 | Automat z kawą (vending) | koniec osi korytarza, subtelna emisja panelu |
| 24 | Zegar ścienny | instytucjonalny, lekko przekrzywiony |
| 25 | Grzejnik żeliwny | pod oknami |
| 26 | Regał magazynowy metalowy | archiwum — rytm krawędzi wg motywu świetlnego |
| 27 | Kartony archiwalne (2–3 warianty) | zastępują `cardboardBox*.fbx`; sterty na regałach |
| 28 | Lampa robocza archiwum | pojedynczy ciepły akcent ~3000 K |
| 29 | Drabinka/schodki biblioteczne | archiwum |

## Priorytet 4 — Pokój socjalny i obserwacyjny

| # | Asset | Uwagi |
|---|---|---|
| 30 | Sofa zużyta | zastępuje `loungeSofa.fbx`; zapadnięte siedzisko |
| 31 | Stolik kawowy | zastępuje `tableCoffee.fbx` |
| 32 | Mały telewizor CRT | zastępuje `televisionModern.fbx` + `cabinetTelevision.fbx` |
| 33 | Mała lodówka | zastępuje `kitchenFridgeSmall.fbx` |
| 34 | Ekspres przelewowy / czajnik | zastępuje `kitchenCoffeeMachine.fbx` |
| 35 | Zlew z szafką | pokój socjalny |
| 36 | Roślina doniczkowa (podwiędła) | zastępuje `pottedPlant.fbx`, `plantSmall2.fbx` |
| 37 | Radio biurkowe | zastępuje `radio.fbx` |
| 38 | Konsola obserwacyjna | stolik pod szybą wenecką + słuchawki + rejestrator |
| 39 | Oprawa świetlówkowa sufitowa | zastępuje `lampSquareCeiling.fbx`; klosz do fake volumetrics |
| 40 | Kinkiet ścienny | zastępuje prefaby `Wall_Light_Left/Right` |

Nie zastępujemy 1:1 pozycji Kenneya bez roli w Posterunku (`stoolBar`, `tableRound`, `rugDoormat`, `laptop` — nowoczesny, niezgodny z epoką).

## Proces

1. Wygenerować tablicę referencyjną stylu (prompt poniżej w rozmowie / GPT) i zatwierdzić ją względem `ART-DIRECTION.md`.
2. Generować propsy partiami wg priorytetów; każdy props porównywać z tablicą i postaciami.
3. Import do `Assets/Prefabs/Props/` + materiały URP Lit, `ContributeGI` dla statycznych.
4. Po każdej partii: podmiana w `Room.unity`, re-bake GI, screenshot per pomieszczenie.
