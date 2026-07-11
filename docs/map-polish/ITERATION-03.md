# Iteration 03 — światło, nastrój i dressing

## Krytyka po iteracji 02

1. Sala Wspólna nadal miała zbyt dużo pustej powierzchni i tylko jeden wyraźny klaster społeczny.
2. Źródła światła nie miały czytelnych opraw, więc wyglądały jak unoszące się punkty i nie tłumaczyły rozkładu światła.
3. Osiem świateł działało w czasie rzeczywistym, a geometria nie korzystała z lightmap, Light Probes ani lokalnych Reflection Probes.
4. Korytarz i pokoje miały podobny nastrój; brakowało ciepłego centrum społecznego, chłodnego Archiwum i twardego światła przesłuchania.
5. Pierwsza wersja napisów na tabliczkach korzystała z legacy shaderu ignorującego głębokość, przez co litery przebijały przez ściany. Zastąpiono je fizycznymi, emissive piktogramami bez tekstu i bez modyfikacji zasobów TextMesh Pro.
6. Materiały opraw odziedziczyły trzy tekstury bez lokalnego dowodu pochodzenia. Wszystkie sloty tekstur wyczyszczono; oprawy używają wyłącznie własnych kolorów i emisji.

## Zmiany

- Dodano drugi klaster wypoczynkowy w Sali Wspólnej: sofę, stolik i dywan, ustawione według zmierzonych granic rendererów.
- Dodano tablicę ogłoszeń z sześcioma papierowymi akcentami oraz cztery czarne tabliczki z odrębnymi piktogramami dla `Sala Wspólna`, `Pokój Przesłuchań`, `Pokój Socjalny` i `Archiwum`.
- Dodano widoczną oprawę do każdego z ośmiu aktywnych świateł. Korytarz otrzymał trzy kinkiety, a pokoje oprawy sufitowe z panelami emissive.
- Ustawiono siedem świateł jako `Baked`; punktowy reflektor w Pokoju Przesłuchań jest jedynym światłem `Mixed` i jedynym rzucającym cień.
- Wyłączono stare unoszące się światła korytarza i światło kierunkowe, pozostawiając je w scenie jako nieaktywne obiekty referencyjne.
- Oznaczono 124 aktywne renderery konstrukcji, wykończenia i mebli jako `ContributeGI`; drzwi pozostały dynamiczne i korzystają z Light Probes.
- Dodano grupę 178 pozycji Light Probe na dwóch wysokościach oraz po obu stronach każdych drzwi. Bake utworzył 172 użyteczne próbki.
- Dodano pięć baked Reflection Probes: po jednej dla Sali Wspólnej, Pokoju Przesłuchań, Korytarza, Pokoju Socjalnego i Archiwum.
- Pierwszy bake wygenerował trzy atlasy lightmap przy `30 texels/m`, maksymalnym atlasie `2048`, Progressive GPU i wyłączonym Realtime GI.
- Dostrojono `PosterunekPostFX`: ACES, exposure `+0,10`, contrast `+8`, saturation `-5`, bloom `0,22` przy threshold `1,10` oraz vignette `0,22`.

## Dowody

- Pełny physics gauntlet po dressingu: PASS — 6/6 spawnów, 4/4 drzwi, korytarz, 7/7 odcinków tras, dwa pola rozmowy i pięć stref dla kapsuły `r = 0,45`, `h = 2,0`.
- Dodatkowy zapas: wszystkie drzwi i trasy pokoi przechodzą dla efektywnego `r = 0,70`; korytarz przechodzi z łagodnym ominięciem wieszaka przy wschodniej ścianie.
- Topologia po czasowym ukryciu `Meble`: PASS — 514 próbek obwodu bez nieplanowanych otworów, 45/45 próbek podłogi i sufitu, dokładnie jedno wejście do Pokoju Przesłuchań. `Meble` ponownie włączono.
- Strefy: dokładnie pięć; każda ma `isTrigger == true`, a pozycja i rozmiar X/Z odpowiadają podłodze pokoju.
- Lighting readback: trzy lightmapy, 124/124 renderery z prawidłowym indeksem lightmapy, 172 baked Light Probes, 5/5 baked Reflection Probes.
- Light readback: osiem aktywnych świateł, każde przy emissive oprawie w odległości `0,000–0,054 m`; siedem `Baked`, jedno `Mixed`, zero realtime shadow-casters.
- Materiały: 133/133 aktywne renderery mają przypisany materiał; brak `Default-Material`, nulli i error shaderów.
- Console Errors po zapisie: 0.
- Scena zapisana przez Unity MCP: `Assets/Scenes/Room.unity`.

Pełny zestaw 14 poprawionych widoków ma prefiks `I03F_` w `docs/map-polish/screenshots/iteration-03/`.
