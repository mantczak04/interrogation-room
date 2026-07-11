# Iteration 02 — powierzchnie PBR i wykończenie

## Krytyka po iteracji 01

1. Wszystkie powierzchnie konstrukcyjne nadal używały płaskich materiałów URP Lit bez map tekstur.
2. Połączenia ściana–podłoga wyglądały jak graybox; brakowało listew przypodłogowych.
3. Zmiana materiału podłogi w otworach drzwiowych była odsłonięta i wyglądała jak przypadkowy szew.
4. Jedna wspólna skala UV rozciągałaby tekstury na segmentach ścian o bardzo różnych długościach.
5. Brakowało jawnego śladu pochodzenia i licencji dla materiałów zewnętrznych.

## Zmiany

- Dodano zestaw CC0: Painted Plaster Wall, Concrete Floor Worn 001, Rubber Tiles i Oak Veneer 01 z Poly Haven oraz Office Ceiling 001 z ambientCG.
- W każdym katalogu źródłowym zapisano `SOURCE.md`, pełną licencję CC0 1.0 i sumy SHA-256 zachowanych map.
- Skonfigurowano mapy 1K jako Repeat; normalne jako `NormalMap`, AO i roughness jako dane liniowe, mipmapy i anizotropię `8×`.
- Z roughness wygenerowano pięć map metallic/smoothness (metallic = 0, smoothness = 1 − roughness) dla URP Lit.
- Każda ściana otrzymała osobny, trwały wariant materiału z tilingiem wyliczonym z jej rzeczywistych granic. Zakres gęstości wynosi 256–512 px/m.
- Zachowano kodowanie barw pomieszczeń przez subtelne tints podłogi: Sala Wspólna niebieska, Pokój Przesłuchań czerwonawy, Korytarz neutralny, Pokój Socjalny zielony, Archiwum chłodno-fioletowe.
- Dodano 14 listew przypodłogowych o wysokości `0,10 m` oraz cztery drewniane progi. Wszystkie są statyczną dekoracją bez colliderów.
- Kenney Furniture Kit pozostaje płasko kolorowany zgodnie z kierunkiem artystycznym.

## Dowody

- 47/47 rendererów w grupach `Podlogi`, `Sciany`, `Drzwi` i `Wykonczenie` używa URP Lit z Base Map, Normal, packed Smoothness oraz AO.
- Przykład readback: `Mat_Wall_SalaN`, tiling `(6,20; 1,50)`, `_NORMALMAP == true`, `_METALLICSPECGLOSSMAP == true`.
- `Wykonczenie`: 18 rendererów, 0 colliderów; 14 listew + 4 progi.
- Pełny physics gauntlet: PASS — 6 spawnów, 4 drzwi dla `r = 0,45` i `r = 0,70`, korytarz, 7 odcinków tras, 2 pola rozmowy, 5 stref.
- Console Errors: 0.
- Scena zapisana przez Unity MCP: `Assets/Scenes/Room.unity`.

Zrzuty po zmianie znajdują się w `docs/map-polish/screenshots/iteration-02/`.
