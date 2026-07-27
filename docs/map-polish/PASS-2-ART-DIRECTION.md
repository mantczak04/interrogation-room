# Posterunek — Pass 2: kierunek artystyczny i playbook „anty-boks"

Status: **zatwierdzony kierunek** (decyzje użytkownika z 2026-07-11). Ten dokument prowadzi kolejny pass polishu. Układ pomieszczeń po Pass 1 (drzwi 1,5 m, południowe skrzydło do z = −7,6) jest **zamrożony** — Pass 2 nie zmienia obrysu ścian ani wymiarów pokoi.

## Nadrzędny cel Pass 2 — doprecyzowanie użytkownika

Jedynym problemem wymagającym rozwiązania w tym passie jest to, że architektura wygląda jak zestaw domyślnych prostopadłościanów Unity z nałożonymi teksturami. Pass 2 ma usunąć wrażenie „Minecrafta” przez **rzeczywistą głębię brył, zróżnicowane kształty, czytelne profile i nastrojowy światłocień**. Klimat ma być ciemny i mroczny.

Materiały, decale i propsy są wyłącznie środkami pomocniczymi. Nie stanowią celu ani obowiązkowej checklisty ilościowej, jeśli nie poprawiają bryły lub nastroju. Naprawione wcześniej bugi wizualne pozostają regułami regresji, ale nie rozszerzają zakresu Pass 2. Zamrożony układ oznacza zachowanie obrysu i przejezdności pomieszczeń; nie zabrania profilowania, fazowania, wnęk ani funkcjonalnych uskoków w obrębie powierzchni ścian i sufitów.

**Zatwierdzona swoboda geometryczna (2026-07-11):** wolno przebudować geometrię ścian i sufitów jako warstwową architekturę: z głębokimi ościeżami, profilowanymi futrynami, wnękami, uskokami, pilastrami, cokołami, gzymsami, fazowanymi narożnikami, belkami i obniżeniami sufitu. Elementy mają wyglądać jak części konstrukcji budynku, a nie losowe dekoracyjne boksy. Nie wolno przy tym zmienić obrysu pomieszczeń, położenia ani szerokości drzwi, ani naruszyć przestrzeni ruchu gracza.

**Zatwierdzone minimum materiałowe (2026-07-11):** każda pozostawiona większa płaszczyzna ściany musi mieć wiarygodną, widoczną z pozycji gracza fakturę powierzchni. Wymagane są oba składniki chropowatości: (1) drobny mikrorelief tynku, farby lub betonu reagujący na boczne światło przez normal/height oraz (2) matowa, lokalnie nieregularna odpowiedź odbić przez roughness/smoothness. Samo podpięcie map PBR nie spełnia tego kryterium. Należy dostroić skalę UV, siłę normal mapy, zakres roughness/smoothness i światło padające pod kątem. Efekt ma przypominać stary malowany tynk, nie plastikowy quad ani przesadnie wyboistą skałę. Ocena odbywa się na screenach z wysokości oczu i przy świetle bocznym, a nie tylko przez readback właściwości materiału.

**Zatwierdzona zasada nastroju (2026-07-11):** mapa ma wyglądać naturalnie i zachować ciemny, mroczny klimat. Mrok wynika z kontrastu, lokalnych plam światła i głębokich cieni, nie z równomiernego niedoświetlenia całej sceny. Światło powinno wydobywać fakturę oraz profile ścian, pozostawiając czytelne ciemne partie.

## Otwarty bug Pass 2 — przeciekanie światła przez ściany

Na screenie użytkownika niebieskie światło korytarza tworzy jasny pas po drugiej stronie ściany, przy styku ściana–sufit. Inspekcja aktywnej sceny `Assets/Scenes/Room.unity` wykazała:

- aktywne niebieskie spoty korytarza są baked i mają `Light.shadows = None`;
- ściany kończą się dokładnie na `y = 3,0`, a spód sufitu zaczyna się dokładnie na `y = 3,0`, więc bryły stykają się bez zakładki;
- wzór artefaktu pokrywa się z tym stykiem.

Najbardziej prawdopodobna przyczyna to połączenie braku cieni z podatnym na wyciek stykiem lightmapy. Pass 2 musi zapewnić światłoszczelne połączenia brył ściana–sufit, prawidłowe cienie/occlusion dla baked lights oraz świeży bake. Naprawa nie może polegać na zakryciu pasa listwą ani na przyciemnieniu materiału. Po naprawie trzeba sfotografować ten sam kadr oraz sprawdzić granice każdego pomieszczenia z obu stron.

**Zatwierdzony test regresji:** po każdym bake'u obejrzyj z obu stron wszystkie styki ściana–sufit oraz pełne przegrody między pomieszczeniami. Szukaj szczególnie obcego koloru światła pojawiającego się po stronie, której dane źródło nie powinno oświetlać. Test obejmuje całą mapę, nie tylko miejsce wskazane na screenie.

## Zatwierdzone decyzje

1. **Styl:** doszlifowany obecny mix — meble low-poly Kenney + czyste powierzchnie PBR. Nie wymieniamy mebli, nie idziemy w fotorealizm.
2. **Klimat:** retro posterunek lat 90. — drewno, butelkowa zieleń, kremowe ściany, korkowe tablice, ciepłe jarzeniówki (vibe Twin Peaks / True Detective).
3. **Architektura:** to główny zakres Pass 2. Ściany i sufity mają stać się warstwowymi elementami budynku przez ościeża, profile, wnęki, uskoki, fazy i zróżnicowane poziomy — bez losowego „boksa na boksie".
4. **Swoboda:** wolno przebudować bryłę architektoniczną w granicach zamrożonego layoutu. Zero zmian w obrysie pomieszczeń, położeniu i szerokości drzwi oraz wymaganej przestrzeni ruchu.

## Naprawione bugi (2026-07-11, commit na main) — reguły na przyszłość

1. **Prześwitywanie przez ścianę przy krawędzi ekranu:** róg near-plane kamery (near 0.3, FOV 60) sięgał 0,463 m od oka; przy przytuleniu do ściany oko jest ~0,45 m od lica, a ściana ma 0,2 m. Naprawione: `nearClipPlane = 0.08` w prefabie gracza. **Reguła:** near plane zostaje ≤ 0.1; ściany zewnętrzne obrysu nigdy cieńsze niż 0,2 m.
2. **Migotanie tekstur przy drzwiach (z-fighting):** cztery skrzydła drzwi były zatopione 10 cm w segmentach ścian — koplanarne powierzchnie walczyły o piksel przy ruchu kamery. Naprawione: skrzydła odsunięte o 0,11 m. **Reguła:** ŻADNE dwie powierzchnie nie mogą być koplanarne ani się przecinać; minimalny odstęp równoległych płaszczyzn to 1 cm. Po każdej iteracji uruchom test przecięć bounds (skrypt w sekcji Weryfikacja).

## Playbook „anty-boks" — jak realne produkcje łamią płaskie ściany

Kluczowa uwaga użytkownika: „kolejne boksy nałożone na boks nic nie dadzą". Pass 2 nie może polegać na dekorowaniu istniejących cube'ów. Kolejność pracy jest obowiązkowa: **makroforma architektury → mikrorelief materiału → światłocień → opcjonalne decale i propsy**. Element geometryczny musi czytać się jako część konstrukcji budynku, a nie doklejony prymityw.

### Materiał po przebudowie bryły: lamperia i wiarygodna powierzchnia
Klasyka polskich/amerykańskich instytucji lat 90.: dolne ~1,2 m ściany w **butelkowej zieleni półmat** (lamperia olejna), wyżej **kremowa farba mat**, rozdzielone wąską listwą (2–3 cm). Stosuj ten podział tam, gdzie wygląda naturalnie i wspiera bryłę pomieszczenia; nie jest obowiązkowym wzorem na każdej ścianie. Technicznie preferuj prawdziwy podział pasma geometrii lub profilowaną listwę. Nie doklejaj drugiego pełnego boxa tylko po to, by zmienić kolor.

### Opcjonalne decale URP
Jeżeli wzmacnia to bryłę lub klimat, włącz **Decal Renderer Feature** w URP Rendererze (PC_RPAsset → Renderer) i użyj `DecalProjector`: przetarcia przy klamkach, smugi butów przy podłodze, zacieki pod sufitem, ślady po zdjętych obrazach lub rdza pod grzejnikami. Nie obowiązuje minimalna liczba decali. NIGDY nie rozmieszczaj ich w regularnych odstępach. Tekstury: ambientCG kategorie Decal*/Dirt*, CC0.

### Opcjonalne propsy naścienne
Retro posterunek może używać **grzejników żeberkowych**, **zegara ściennego**, **gaśnicy na haku**, **skrzynki elektrycznej z kablem natynkowym**, **wieszaków** i **korkowych tablic**, ale tylko gdy wzmacniają głębię lub uzasadniają element architektury. Nie obowiązuje minimalna liczba propsów. Źródła modeli: poly.pizza (filtr CC0), Kenney, Quaternius.

### 4. Pilastry i wnęki — ale z funkcją
Pilaster co ~3,5–4 m na długich ścianach (Sala, Korytarz), głębokość 8–12 cm, ale KAŻDY pilaster ma powód: niesie kinkiet, dzieli lamperię, stoi za nim grzejnik. Wnęka = płytkie cofnięcie 10 cm z tablicą ogłoszeń albo ławką w środku. Pilaster bez funkcji to właśnie „boks na boksie" — nie rób.

### 5. Sufit podwieszany zróżnicowany
Korytarz: obniż do 2,6 m osobną płaszczyzną z **kasetonami** (siatka 0,6 m z paneli, co któryś panel to oprawa świetlna, jeden przesunięty/przebarwiony). Sala: sufit zostaje na 3 m, ale dostaje **belki/skrzynki instalacyjne** przy ścianach i kratki wentylacyjne. Pokój Przesłuchań: goły ciemny sufit — kontrast z resztą jest zamierzony.

### 6. Fazowanie i futryny (ProBuilder)
Unity ma ProBuilder (MCP: `manage_probuilder`) — użyj do: fazowania zewnętrznych narożników ścian (bevel 2–3 cm), profili listew przypodłogowych i futryn drzwi z prawdziwym profilem (nie prostopadłościan). To usuwa „ostrza" charakterystyczne dla boksów.

### 7. Światło łamie płaskość za darmo
Kinkiety co nieregularne odstępy (już częściowo jest), wall-washery robiące plamy na lamperii, jedna migocząca jarzeniówka w Archiwum (animowana intensywność — jedyne światło realtime poza spotem przesłuchań, bez cienia). Po zmianach ponowny bake.

## Paleta retro lat 90. (trzymaj się jej)

| Element | Kolor | Hex orientacyjny |
| --- | --- | --- |
| Lamperia (dół ścian) | butelkowa zieleń półmat | `#2F4F3E` |
| Ściany (góra) | krem/ecru mat | `#E8E0CC` |
| Listwy, futryny, drzwi | ciemniejsze drewno | `#6B4A2E` |
| Podłoga Sala/Korytarz | linoleum oliwkowo-brązowe, drobna szachownica | `#8A7B5C` / `#5C5140` |
| Pokój Przesłuchań | zimna szarość, goły beton — wyjątek od palety | `#5A5D63` |
| Akcenty | korkowe tablice, mosiądz klamek, zieleń roślin | — |

Meble Kenney zostają w swoich kolorach — paleta ścian ma je „osadzić", nie konkurować.

## Korekta palety i światła (decyzja użytkownika z 2026-07-27)

Referencją nastroju jest **grafika z ekranu MainMenu**: ciemny, brudny beton, jedna
ciepła żarówka, głębokie czernie, minimalna saturacja. Poprzednia paleta czytała się
jako „marna szkoła", ponieważ ściany były jasną, płaską, *nieteksturowaną* farbą.
Ta korekta ma pierwszeństwo przed tabelą powyżej tam, gdzie się z nią rozjeżdża;
sam podział lamperia/ściana i układ pomieszczeń pozostają bez zmian.

- **Ściany muszą mieć albedo, nie sam kolor.** `P2_PlasterCream`, `P2_BottleGreen`
  i `P2_Ceiling` używają CC0 `PaintedPlasterWall_Diffuse_1K` (tiling 4–5).
  Płaski `_BaseColor` bez mapy albedo jest regresją — nie wracać do niego.
- **Nowe wartości:** ściana górna `≈#8A7D6B` × plaster, lamperia `≈#4D5C4F` × plaster
  (zieleń schodzi do szeptu, dominantą jest brud), sufit `≈#544F4A` × plaster.
- **Podłogi tracą kolorowe tinty per-pokój** (niebieski w Sali, różowy w Przesłuchaniach,
  zielony w Socjalnym). Wszystkie są ciepłym, matowym betonem; `_Smoothness ≈ 0,45–0,50`,
  bo `1,0` dawało plastikowy połysk i zimne odbicie.
- **Ambient, fog i grading są ciepłe, nie chłodne.** `WhiteBalance.temperature` jest
  dodatnia; `LiftGammaGain.lift` podbija czerwień, nie błękit. Poprzednie ustawienia
  (temp −5, lift w stronę cyjanu) dawały „niebieski telewizor".
- **Sala Wspólna ma jedną lampę na środku** `(0, 2.42, 4.70)`, ciepłą. Pozostałe trzy
  oprawy i światła są wyłączone, nie usunięte — wracają jednym `SetActive(true)`,
  gdyby playtest pokazał, że pokój jest za ciemny.
- **Stożek światła w Pokoju Przesłuchań** ma odpowiadać geometrii spota. Mesh i kąt
  reflektora trzymamy zgodne (`spotAngle 56°` ↔ pół-kąt 28° stożka); rozjazd 92° ↔ 48°
  był powodem, dla którego stożek czytał się jak płaski trójkąt.

### Trzy pułapki wykryte przy tej korekcie (nie powtarzać)

1. **`RenderSettings.defaultReflectionMode` był ustawiony na `Skybox`**, czyli domyślne
   proceduralne *niebieskie niebo* Unity. Bezokienny posterunek odbijał błękit nieba na
   wszystkich gładszych powierzchniach — podłogi czytały się jako zimne, szare płyty
   mimo ciepłego albedo. Wnętrze bez okien ma mieć `Custom` + brak tekstury; za odbicia
   odpowiadają wyłącznie Reflection Probes pokoi. Do tego podłogi mają `_Smoothness ≈ 0,20`,
   bo przy `0,50` Fresnel pod kątem i tak dominował nad diffuse.
2. **Ciepło łatwo nałożyć cztery razy.** Żarówka + ambient + `WhiteBalance` + `LiftGammaGain`
   to cztery niezależne mnożniki. Ciepłe ma być *światło*, nie grading — `temperature`
   trzymamy w okolicy `+3`, a `lift`/`gain` blisko neutralnych. Przy `temperature +12`
   i mocno ciepłym `gain` cała Sala robiła się pomarańczowa.
3. **Emisyjne kwadraty opraw.** `Panel_Oprawa_*` to płaskie quady; przy `_EmissionColor`
   powyżej ~1,5 wypalają się na czysto biały prostokąt zamiast świecić. Jasność ma
   pochodzić z Bloomu, nie z wartości emisji.

### Druga korekta: opadanie światła i cienie w Sali Wspólnej (2026-07-27)

Użytkownik zgłosił, że po pierwszej korekcie „aura z sufitu" w pokoju startowym jest
za duża, poziom światła jest wszędzie taki sam mimo jednej lampy, i że nie ma cieni.

- **Przyczyna aury: `Swiatlo_Sala` było lampą Point tuż pod sufitem.** Sufit ma spód na
  `y = 3,00`, żarówka wisiała na `y = 2,42`, więc górna półsfera świeciła prosto w sufit
  pod bardzo płaskim kątem i rozmywała się na kilka metrów. Klosz tego nie zatrzymywał,
  bo wszystkie `Oprawa_*` miały `shadowCastingMode = Off`.
- **Podnoszenie żarówki w klosz nie działa.** Mesh `Visual_Lamp_Pendant` jest zamknięty
  od góry: promień maleje z `0,196` przy krawędzi (`y ≈ 2,52`) do `0,061` przy trzonku
  (`y ≈ 2,77`). Żarówka na `2,78` siedzi *nad* kloszem, który wtedy zasłania światło
  **w dół** — pokój dostawał wyłącznie odbite światło, czyli był płaski i bez cieni.
- **Rozwiązanie: `Swiatlo_Sala` to teraz Spot skierowany w dół** (`rotation 90°`,
  `y = 2,45`, tuż pod krawędzią klosza), `spotAngle 155 / inner 25`, `range 12`,
  `intensity 105`, kolor `(1,00, 0,87, 0,72)`. Spot z definicji nie oświetla sufitu,
  więc aura znika niezależnie od geometrii klosza, a pozostaje zdefiniowana plama
  z miękkim opadaniem ku narożnikom.
- **Tryb `Mixed` zamiast `Baked`.** Direct + cienie liczą się w czasie rzeczywistym
  (Shadowmask), więc meble w końcu rzucają widoczne cienie, a strojenie kąta i mocy
  nie wymaga bake'u. To jedyny sposób na szybką iterację — patrz uwaga niżej.
- Wypełnienie: `Wash_SalaE` / `Wash_SalaN_W` `1,15 → 0,85`, ambient
  `(0,150, 0,134, 0,112)` — ciemność ma być *ciepła*, nie niebieska.
- `Bloom` `intensity 0,65 → 0,50`, `scatter 0,72 → 0,55`, `Mat_Lamp_ColdBlue`
  emisja `→ (0,90, 0,72, 0,50)` — halo wokół oprawy ma być małe.
- Stożek w Pokoju Przesłuchań: `_Intensity 0,62 → 0,34`, `_Color → (1,00, 0,80, 0,55)`,
  `_SoftEdgePower 6,5`, `_BottomFade 0,80`; `Swiatlo_Przesluchania` `4,2 → 3,4`, bo
  blat wypalał się na czysto biały i przez to stożek czytał się jako biały słup.
- `Carpet_Brown_Material` miał `_BaseColor` czysto biały i `_Smoothness 0,5`, przez co
  dywany świeciły jak plamy światła. Teraz `(0,46, 0,36, 0,27)` i `_Smoothness 0,10`.
- Korytarz: punktowe `Swiatlo_Korytarz*` `4,0 → 5,6` (`range 6,5`), listwy `6,5 → 7,4`.

**Pułapka iteracyjna:** przełączenie światła na `Realtime` w Edit Mode **nie** daje
podglądu — statyczna geometria i tak czyta stary lightmap, więc cztery warianty
wyszły identyczne. Żeby stroić bez bake'u, światło musi być `Mixed` **i** trzeba raz
zbake'ować; dopiero potem zmiany direct/kąta/mocy widać natychmiast.

### Trzecia korekta: cienie w pozostałych pokojach, ocieplenie i ustawienie propsów (2026-07-27)

Po drugiej korekcie Sala Wspólna czytała się dobrze, ale reszta mapy nie: cienie były
tylko w Sali, światło było zimne, a większość mebli stała bokiem albo w ścianie.

- **Cienie poza Salą.** `Swiatlo_Socjalny` i `Swiatlo_Archiwum` były lampami Point w
  trybie `Baked` — Baked nie rzuca cieni w czasie rzeczywistym, a Point tuż pod sufitem
  robi tę samą aurę co wcześniej w Sali. Oba przerobione na Spot w dół (`y = 2,45`,
  `150°/26°`, `range 9`) w trybie `Mixed`.
- **Korytarz.** Trzy świetlówki sufitowe były lampami typu `Rectangle`, a te w URP są
  **wyłącznie do wypieku** — nie da się z nich dostać cieni realtime. Zamienione na
  szerokie Spoty (`145°/34°`, `Mixed`). Kinkiety `Oprawa_Korytarz*` zostają `Baked`
  jako wypełnienie.
- **Pułapka nazw:** świetlówki sufitowe i kinkiety nazywają się identycznie
  (`Swiatlo_KorytarzW`, `Swiatlo_KorytarzE`). Skrypt szukający po `Light.name` trafia
  w oba i cicho nadpisuje jedno drugim — trzeba rozróżniać po ścieżce w hierarchii.
- **Ocieplenie.** Zimne źródła (`0,68–0,78` w kanale niebieskim) i niebieski
  `Wash_Archiwum` (`0,38 / 0,55 / 1,00`) zamienione na ciepłe. To była też najbardziej
  prawdopodobna przyczyna wrażenia „światło przechodzi zza ściany": niebieski grazing
  wash na ścianie Archiwum nie ma widocznego źródła i czyta się jak przeciek.
- **Dwie dodatkowe lampy w Sali.** `Oprawa_Sala3` i `Oprawa_Sala4` (z panelami
  i światłami) włączone jako przyciemnione Spoty `Mixed`, żeby pokój miał trzy
  oddzielne plamy zamiast jednej i wypełnienia.
- **Mgiełka.** `RenderSettings.fog` był włączony, ale kolor mgły (`0,098`) był
  ciemniejszy niż większość sceny, więc fog tylko przygaszał dal zamiast tworzyć
  zawiesinę. Podniesiony do `0,195 / 0,170 / 0,140` przy gęstości `0,065`.
- **Materiały wypalające się na biało.** `RoundTable_Material` i szafki kuchenne
  Kenneya miały jasne albedo tintowane czystą bielą. Stoły przyciemnione do
  `0,56 / 0,52 / 0,46`; szafki dostały własne instancje
  `Mat_KitchenCabinet_Body/Door`, bo materiały wbudowane w FBX są tylko do odczytu.

**Orientacja propsów — jak to ustalić, zamiast zgadywać.** Prefaby `Visual_*` mają
różne `localEulerAngles` (`270/270/0`, `270/269/0`, `270/0/0`), więc z samego `rotY`
rodzica nic nie wynika. Dwie metody, które działają:

1. **Render w pustej scenie.** `NewScene(EmptyScene, Additive)`, `Instantiate` kopii
   z `rotation = identity`, własna warstwa + `cullingMask`, zdjęcia z `-Z / +X / +Z / -X`.
   Od razu widać, która ściana prefabu jest przodem. Tak wyszło, że szafki kartotekowe
   (`Visual_Cabinet_Filing`) mają front na lokalnym `-Z`, a meble kuchenne Kenneya na `+Z`.
2. **Pomiar liczbowy dla krzeseł.** Oparcie jest wysoko, więc różnica środka masy
   wierzchołków powyżej 72% wysokości i tych w pasie 30–55% wskazuje tył krzesła.
   Kierunek patrzenia to wektor przeciwny. Wynik zgadzał się co do znaku z `SeatPoint`
   każdego krzesła, więc mapowanie siedzenia nigdy nie było zepsute — obrócone było
   całe krzesło.

Znalezione i poprawione: wszystkie 5 krzeseł w Sali i oba w Pokoju Przesłuchań były
odwrócone dokładnie o 180°, krzesło w Archiwum patrzyło w bok zamiast na biurko,
szafki socjalnego (z ekspresem i radiem) stały bokiem do pokoju, a trzy szafki
kartotekowe frontem do ściany. Ławki trzyosobowe wchodziły w ścianę o 14–19 cm —
**licem ściany jest listwa przypodłogowa, która wystaje 4 cm przed tynk**, więc
odsunięcie liczy się od niej, nie od ściany.

**Drzwi Archiwum a szafka.** `NetworkDoor.ResolveHingeLocalOffset()` sam wylicza zawias
na krawędzi skrzydła, więc drzwi Archiwum obracają się wokół `x = 1,75` i po otwarciu
leżą wzdłuż `z` od `-2,60` do `-4,10`. Dokładnie tam stała `B4 Archive Alarm`.
Przeniesiona pod ścianę południową, w wolną wnękę między szafką akt a biurkiem.
Pozostałe troje drzwi sprawdzone — ich łuki są czyste.

**Spawny.** Sześć `SpawnPoint_*` stało w przypadkowych miejscach z `yaw = 80`. Teraz
tworzą okrąg `r = 2,4` wokół `(-0,20, 5,20)`, metr od lampy, każdy zwrócony do środka.
Promień i środek wybrane wyszukiwaniem po siatce z `Physics.OverlapCapsule` (r = 0,42),
więc żaden punkt nie koliduje z meblami.

### Czwarta korekta: klosze, artefakty lightmapy, szafki i migotanie drzwi (2026-07-27)

**Biały kwadrat w każdej lampie.** `Panel_Oprawa_*` to były emisyjne *sześciany* 22 × 1,2 × 22 cm
zawieszone pod wylotem klosza. Klosz `Lamp_Pendant` jest pełną kopułą o promieniu 0,195
z najniższą krawędzią na `y = 2,520`, więc kwadrat wystawał spod niej rogami i czytał się
jak kartka papieru wetknięta w lampę. Zamienione na **dysk** (wbudowany `Cylinder`,
`scale = (0,35, 0,006, 0,35)`, `r = 0,175` — 2 cm zapasu do krawędzi klosza) osadzony
na `y = rimMinY + 0,010`, z `shadowCastingMode = Off`. Emisja materiałów paneli ścięta
do ok. 0,5 — przy `postExposure = 1,45` (czyli ×2,73) wartości 0,9–1,25 wchodziły w twarde
przepalenie i dawały płaską biel bez żadnej tonacji.

**Pułapka współdzielonego materiału.** `Mat_Lamp_Cool` używały *zarówno* `Panel_Oprawa_Archiwum`,
jak i trzy świetlówki korytarza (`Oprawa_Korytarz*/Mesh`). Nie wolno go było ściemnić pod panel,
więc panel Archiwum przepięty na `Mat_Lamp_Warm`, a sam `Mat_Lamp_Cool` tylko ocieplony
(2,15/2,35/2,55 → 1,30/1,15/0,92), bo lodowaty błękit kłócił się z ciepłymi reflektorami.

**„Dziwne wypukłości" na kanapie i fotelu to nie była geometria.** Render izolowany
(pusta scena, `rotation = identity`, cztery kierunki) pokazał idealnie gładkie poduszki —
czyli winne musiało być oświetlenie. Przyczyna: **14 modeli FBX miało
`generateSecondaryUV = False`, a mimo to renderery były `ContributeGI`**. Bez UV2 lightmapper
wypieka na UV0, a UV0 siatek z Tripo to ciasny atlas bez marginesów między wyspami —
sąsiednie wyspy przeciekają na siebie i dają dokładnie te kanciaste, wielokątne
jasne/ciemne kliny. Naprawa: `generateSecondaryUV = true`, `secondaryUVPackMargin = 16`
(domyślne 4 przy 20 tekselach/jednostkę zostawia wyspy w odległości ~1 teksela)
oraz `scaleInLightmap = 2` na miękkich, dużych meblach (kanapa, fotel, ławki, dywany).

> Reguła: model **AI-generowany bez UV2 nie może być `ContributeGI`**. Sprawdzaj
> `mesh.uv2.Length > 0` dla każdego renderera z flagą `ContributeGI` — cichy fallback
> na UV0 nie zgłasza żadnego ostrzeżenia.

**Szafki kartotekowe.** Front `Cabinet_Filing` to lokalne **−X** siatki (ustalone renderem
izolowanym), więc kierunek w świecie liczy się jako `visual.rotation * Vector3.left` —
nigdy z `rotY` korzenia, bo `Visual_*` ma własną rotację konwersji `(270, 270, 0)`.
Wszystkie trzy egzemplarze celowały na północ. `B4 Records Cabinet` w Archiwum jest tak
poprawnie (front w pokój), ale:

- `B4 Personal Locker` (socjalny) frontem **wchodził w zabudowę kuchenną** — jego `z max = -6,500`
  nachodziło na `Socjalny_Szafka2` (`z min = -6,530`),
- `B5 Maintenance Cabinet` (korytarz) stał tyłem do zachodniej ściany i frontem na północ,
  co geometrycznie było poprawne, ale gracz idzie korytarzem od wschodu i widzi wyłącznie
  ślepy bok.

Obie obrócone o **+90° yaw** (front `+Z` → `+X`) przez `RotateAround` wokół środka bounds
*wizualnej* siatki, potem dosunięte analitycznie. **Licem ściany bywa listwa przyścienna
(`ChairRail`), nie cokół** — `ChairRail_ScianaW` wystaje do `x = -5,953`, czyli 7 mm dalej
niż `Listwa_ScianaW` (`-5,960`). Szafki mają 1,42 m wysokości, więc kolidowałyby właśnie
z listwą. Ustawione na 8 mm luzu od najbardziej wystającego elementu.

**Migotanie przy drzwiach.** Skrzydła miały `scale = (0,05, 2,100, 1,500)`, a otwory
w ścianach dokładnie tę samą szerokość — boczne ścianki skrzydła były **idealnie
współpłaszczyznowe** z czołami segmentów ściany (0,0 mm, 0,11 m² na stronę), plus góra
tkwiła 3 mm w nadprożu. To klasyczny z-fighting: przy ruchu kamery obie powierzchnie
walczą o piksele i całe obramowanie drzwi „wibruje". Skrzydła przeskalowane do
`(0,05, 2,033, 1,488)` i przesunięte na `y = 1,069`, co daje **6 mm luzu na bok,
15 mm pod nadprożem i 2 mm nad progiem** (skrzydło przestało też przecinać próg podczas
otwierania). `NetworkDoor.hingeLocalOffset` jest zerowy na wszystkich czworgu, więc zawias
wylicza się z kolidera i sam podąża za nową szerokością — nic nie trzeba było przestawiać.

> Detektor, który to znalazł, warto zachować: dla każdej pary rendererów licz przecięcie
> bounds i zgłaszaj te, w których **najcieńsza oś ma < 4 mm, a pole pozostałych dwóch > 0,05 m²**.
> To odsiewa zwykłe stykanie się brył i zostawia realne ryzyko z-fightingu.

### Piąta korekta: mgła zjadała kontrast, dwie lampy w Sali (2026-07-27)

Po naprawie UV lightmapy scena zrobiła się **płaska i za jasna** — użytkownik zgłosił „nie ma
cieni". Pomiar tego samego kadru przed i po pokazał, że **średnia jasność się nie zmieniła
(0,085 → 0,095), ale odsetek pikseli bliskich czerni spadł z 38 % do 1 %**. Taka sygnatura —
średnia bez zmian, czernie podniesione — to zawsze **addytywne podbicie**, a nie więcej światła.

Test kontrolowany (każdy element wyłączany osobno, mediana z 5 klatek) wskazał winnego:

| wariant | czerń |
|---|---|
| mgła 0,065 (stan po korekcie) | 0,8 % |
| mgła wyłączona | 19,3 % |
| mgła 0,030 | 6,2 % |
| bloom wyłączony | 0,8 % (bez zmian) |
| panele lamp wyłączone | 0,8 % (bez zmian) |

Złożyły się dwie rzeczy naraz:

1. **Mgła `ExponentialSquared` o gęstości 0,065 i jasnym kolorze** `(0,195, 0,170, 0,140)`
   mieszała każdy odległy piksel w stronę średniej szarości — żaden piksel nie mógł już być
   czarny. Przy poprzednim wypieku to nie rzucało się w oczy, bo…
2. …**meble z popsutymi lightmapami były niemal czarne** i to one wnosiły większość ciemnych
   pikseli w pobliżu kamery, gdzie mgła jest słaba. Naprawa UV usunęła tę *przypadkową* czerń.

> Reguła: przypadkowa czerń z artefaktów potrafi udawać art direction. Po każdej naprawie
> oświetlenia porównuj **odsetek czerni**, nie samą średnią jasność — inaczej naprawa
> techniczna wygląda jak regresja artystyczna.

Kontrast odbudowany świadomie: mgła `0,065 → 0,026` i kolor `→ (0,115, 0,101, 0,083)`,
`postExposure 1,45 → 1,60`, `contrast 5 → 20`. Wynik: v0 `0,053 / 33 % czerni`,
v1 `0,071 / 25 %`, przesłuchania `0,054 / 66 %`, zero przepalonych pikseli.

**Sala Wspólna wróciła do dwóch lamp.** Wyłączone `Swiatlo_Sala4`, `Oprawa_Sala4`
i `Panel_Oprawa_Sala4` (róg południowo-zachodni, nad kanapą i TV). Zostały `Swiatlo_Sala`
(centralna, `I = 88`) i `Swiatlo_Sala3` (północny wschód, nad stolikiem, `I = 32`).
Alternatywny układ do rozważenia, gdyby ten nie siadł: przekątna `Sala4 + Sala3` bez lampy
centralnej — daje równiejsze rozłożenie, ale traci mocną plamę w środku pokoju.
**Wyłączenie światła w trybie `Mixed` wymaga ponownego wypieku**, inaczej jego odbicie
pośrednie zostaje w lightmapie.

Podniesienie ekspozycji rozjaśniło przy okazji zabudowę socjalnego, więc `Mat_KitchenCabinet_Body`
`0,52 → 0,42`, `Mat_KitchenFridge` `0,60 → 0,50` i `Swiatlo_Socjalny` `15 → 12`.

### Szósta korekta: przyczyna źródłowa braku cieni — `Mixed` w URP nie świeci (2026-07-27)

Po trzech rundach strojenia użytkownik dalej zgłaszał brak cieni. Powód okazał się
fundamentalny i **unieważnia diagnozę z trzeciej korekty** (twierdzenie, że przerobienie
lamp na `Mixed` dało im cienie rzucane w czasie rzeczywistym — nie dało).

Pomiar rozstrzygający, ten sam kadr:

| test | jasność |
|---|---|
| stan wyjściowy | 0,1816 |
| `Swiatlo_Sala3` podkręcone do **`I = 900`** | 0,1816 |
| wyłączone **wszystkie 21** świateł dodatkowych | 0,1816 |
| **nowy** reflektor `Realtime`, `I = 60` | **0,2403** |

> **W URP światła dodatkowe (Spot/Point) w trybie `Mixed` nie renderują się w czasie
> rzeczywistym w ogóle — są wypiekane w całości.** Realtime dostaje tylko główne światło
> kierunkowe. Wynikają z tego trzy rzeczy, każda kosztowała tu osobną rundę:
>
> 1. Lampy `Mixed` **nie rzucają cieni w czasie rzeczywistym**. To, co wygląda na cień,
>    to shadowmask, którego ostrość ogranicza rozdzielczość lightmapy (20 teksli/jednostkę
>    dało ~30 teksli na metr podłogi — rozmyte plamy czytane jako „brak cieni").
> 2. Obiekty dynamiczne, w tym **gracz, nie rzucają żadnego cienia**.
> 3. **Strojenie natężenia i kąta jest bezskuteczne** — każdy sweep „zbiega" do wartości
>    startowej, bo nic się nie renderuje. Nie wolno stroić `Mixed` światła dodatkowego
>    i ufać wynikowi.

**Test, którym to się wykrywa w jednym pomiarze:** wyrenderuj klatkę, ustaw `shadows = None`
na wszystkich światłach, wyrenderuj ponownie i policz piksele różniące się o więcej niż 0,02.
Przy oświetleniu wyłącznie wypieczonym wychodziło **11,7 % powierzchni, max 0,38** (i to był
sam shadowmask). Po konwersji: **33,5 % powierzchni, max 2,31**.

**Zastosowane rozwiązanie (hybryda):**

- 9 reflektorów kluczowych `Mixed → Realtime`, `shadowStrength = 1,0`,
  `shadowBias = 0,04`, `shadowNormalBias = 0,25`.
- 8 świateł `Wash_*` zostaje `Baked` — dają odbicie pośrednie i muśnięcie ścian,
  bo światła `Realtime` nie odbijają się bez GI w czasie rzeczywistym.
- `m_AdditionalLightsPerObjectLimit` `4 → 8`, bo w Sali i korytarzu potrafi się nałożyć
  więcej niż cztery światła na jeden obiekt.
- SSAO wzmocnione: promień `0,30 → 0,70`, intensywność `0,80 → 1,10`,
  `DirectLightingStrength` `0,25 → 0,50` — przedmioty przestały lewitować.

Wynik: reflektory dają **92 % obrazu** (wcześniej 0 %), Sala `0,082 / 52 % czerni`,
przesłuchania `0,050 / 80 %`, korytarz `0,170 / 21 %`. Gracz rzuca ostry cień.
Atlas cieni 4096 mieści 8 reflektorów, a naraz widoczne są 2–3, więc koszt jest nieistotny.

### Siódma korekta: gęstość tekstury ścian i powrót trzeciej lampy (2026-07-27)

**Plamy na ścianach to była rozciągnięta tekstura, nie SSAO.** Ściany grayboxa to skalowane
sześciany, więc każda ściana ma to samo UV `0..1` niezależnie od swojej realnej wielkości.
Pojedyncza wartość `tiling = (4, 4)` na współdzielonym materiale dawała więc zupełnie inną
gęstość na każdym segmencie:

| element | rozmiar | kafel | gęstość |
|---|---|---|---|
| `ScianaW` | 16,90 × 3,20 m | 4,23 × 0,80 m | 242 px/m |
| `KorytarzS_Seg3` | 9,15 × 3,20 m | 2,29 × 0,80 m | 448 px/m |
| `Lamperia_KorytarzS_Seg3` | 9,11 × 1,18 m | 2,28 × 0,30 m | 450 px/m |
| `Lamperia_KorytarzS_Seg1` | 1,41 × 1,18 m | 0,35 × 0,30 m | 2905 px/m |

Rozrzut **12-krotny**, a do tego kafel był anizotropowy — rozciągnięty 2,9× na ścianie
korytarza i **7,6× na lamperii**. Przy świetle padającym pod ostrym kątem czyta się to jako
rozmazane, wielkoskalowe plamy.

Dwie naprawy, obie potrzebne:

1. **Jednolita, izotropowa gęstość** — 1 kafel = 1,2 m, liczone z rozmiaru renderera.
2. **Mapa normalnych ściszona**: `P2_PlasterCream` `0,26 → 0,05`, `P2_BottleGreen` `0,30 → 0,06`.
   Sweep czterech wariantów pokazał, że dopiero przy 0,05 znika efekt „skórki pomarańczowej"
   i tynk czyta się jak malowana ściana. Ten sam sweep z wyłączonym SSAO dał obraz
   praktycznie identyczny — **SSAO nie miało z tym nic wspólnego** i zostaje włączone.

> Pułapka warta zapamiętania: `Renderer.SetPropertyBlock` **nie zapisuje się do sceny**.
> Ustawienie tilingu skryptem wygląda poprawnie w Edit Mode i znika po przeładowaniu.
> Dlatego powstał `Assets/Scripts/Graphics/WallTextureDensity.cs` — `[ExecuteAlways]`,
> liczy tiling z `renderer.bounds` w `OnEnable`/`OnValidate`. Dzięki temu materiał
> współdzielony zostaje nietknięty, a zmiana rozmiaru ściany od razu dopasowuje teksturę.
> Komponent siedzi na 65 rendererach; kosztem jest wypadnięcie ich z SRP Batchera.

**Trzecia lampa wróciła.** `Swiatlo_Sala4`, `Oprawa_Sala4` i `Panel_Oprawa_Sala4` włączone
z powrotem — po przejściu na `Realtime` róg z kanapą i telewizorem robił się zbyt ciemny,
bo lampy Realtime nie dają odbicia pośredniego, które wcześniej dopalało ten kąt z wypieku.

**Znany, wcześniejszy bug (nie z tej zmiany):** przy drzwiach Sali Wspólnej w korytarzu
renderuje się biały prostokąt zamiast tabliczki/tekstu. `Znak_*` i `Mat_SignPlate` są
ciemne i poprawne; podejrzany jest obiekt `Tekst_SalaWspolna`, który ma zerowe bounds.
Widać go już na screenach sprzed tej korekty.

## Źródła CC0 (tylko te, z licencją commitowaną do repo)

- Tekstury PBR i decale: **ambientCG.com** (CC0) — Paint*, PaintedPlaster*, Linoleum/Tiles, Decal*, Dirt*.
- Modele: **Kenney** (kenney.nl — Building Kit ma ściany/okna/drzwi), **Quaternius** (quaternius.com), **poly.pizza** (agregator, filtruj CC0), **KayKit**.
- Każdy nowy zestaw: `Assets/ThirdParty/<Autor>/<Kit>/` + License/SOURCE.md, jak dotychczas.

## Twarde zasady techniczne (wnioski z Pass 1)

1. Unity MCP dla wszystkich operacji edytora; przy braku możliwości — STOP i handoff (AGENTS.md).
2. `component_properties` przy tworzeniu obiektów bywa ignorowane — ustawiaj właściwości OSOBNYM krokiem i CZYTAJ JE Z POWROTEM.
3. Kolory w MCP jako obiekt `{r,g,b,a}`, nie tablica.
4. Pivoty Kenney/FBX są przesunięte — pozycjonuj po zmierzonych bounds rendererów.
5. Zero edycji w Play Mode (zmiany przepadają). Zero wejścia w Play Mode w ogóle.
6. Po każdej iteracji: bake świateł jeśli zmieniono geometrię/światła, save sceny przez MCP, commit.

## Weryfikacja po każdej iteracji (wszystko musi być zielone)

1. Physics gauntlet z Pass 1: 6 spawnów OverlapCapsule czyste, 4 drzwi + korytarz + trasy przez pokoje CapsuleCast czyste (r = 0,45), strefy `Strefa_*` — isTrigger i rozmiar pokoju.
2. **Nowy test koplanarności:** dla każdej pary rendererów w `Map_Graybox` sprawdź przecięcia bounds; dopuszczalne tylko celowe zagłębienia (listwa w ścianie ≤ 5 mm licowania jest ZABRONIONE — zawsze 5 mm PRZED licem). Raportuj każdą parę z przecięciem > 0.
3. Screeny z wysokości oczu (y ≈ 1,7): 2 na pokój + korytarz w obu kierunkach + przez każde drzwi; porównanie z poprzednią iteracją.
4. Konsola: zero błędów (szum Vivox „Callback dispatcher is not initialized" ignoruj — pre-existing).
5. Budżet: scena ≤ 150 tys. trójkątów łącznie, ≤ 60 materiałów, 3–4 atlasy lightmap; decal projectors ≤ 60.

## Kryteria akceptacji Pass 2

- [ ] Z pozycji gracza ściany nie czytają się jako pojedyncze domyślne cube'y Unity: mają rzeczywistą głębię, profile, uskoki, ościeża lub inne wiarygodne podziały konstrukcyjne.
- [ ] Wszystkie większe płaszczyzny ścian pokazują w bocznym świetle drobny mikrorelief oraz matową, lokalnie nieregularną odpowiedź odbić; nie wyglądają jak plastikowe quady ani skała.
- [ ] Narożniki zewnętrzne ścian są fazowane, a futryny i ościeża mają czytelny profil oraz głębokość.
- [ ] Światło buduje ciemny, mroczny nastrój i wydobywa geometrię oraz fakturę ścian.
- [ ] Żadne źródło światła nie oświetla przez nieprzezroczystą ścianę ani zamknięty sufit; szczególnie niebieskie światła korytarza nie tworzą pasa przy styku z Salą.
- [ ] Decale i propsy są opcjonalne; każdy użyty element wzmacnia bryłę lub nastrój i nie jest wypełniaczem do osiągnięcia limitu.
- [ ] Paleta pozostaje spójna z retro posterunkiem; Pokój Przesłuchań może celowo wyłamywać się zimniejszym charakterem.
- [ ] Test koplanarności czysty, gauntlet czysty, bake świeży, konsola czysta, sceny zapisane, commity wypchnięte.
- [ ] Raport końcowy w `docs/map-polish/PASS-2-REPORT.md` ze screenami przed/po.

## Źródła researchu

- polycount — Modular Environment Techniques: https://polycount.com/discussion/209426/modular-environment-techniques
- The Level Design Book — Environment Art: https://book.leveldesignbook.com/process/env-art
- World of Level Design — Modular Environment Design 101: https://www.worldofleveldesign.com/categories/game_environments_design/modular-environment-design-101.php
- Beyond Extent — Balancing modularity and uniqueness: https://www.beyondextent.com/articles/balancing-modularity-and-uniqueness-in-environment-art
- Kenney Building Kit (CC0): https://kenney.nl/assets/building-kit • Quaternius: https://quaternius.com • ambientCG (CC0 PBR/decale): https://ambientcg.com
