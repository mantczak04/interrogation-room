# Art direction

- **Status dokumentu:** Propozycja do zatwierdzenia
- **Obowiązuje:** Fazy 2–6 po zatwierdzeniu przez użytkownika
- **Kierunek:** Poważne kinowe światło + absurdalne postacie

## Moodboard

Do uzupełnienia przez użytkownika: 8–15 obrazów referencyjnych wraz z linkami lub ścieżkami do plików.

## Styl

Stylizowany realizm o filmowej kompozycji. Posterunek ma wiarygodną skalę, konstrukcję i ślady użytkowania, ale bez fotorealistycznego szumu. Geometria zachowuje rozpoznawalne proporcje, a tekstury upraszczają drobny detal na rzecz dużych, czytelnych plam materiału.

Powaga otoczenia jest grana na serio: niski klucz, kontrolowany kontrast, noc i zużyte wnętrza komisariatu z lat 80–90. Humor wynika z obecności absurdalnych podejrzanych w tej wiarygodnej przestrzeni, nie z żartobliwych dekoracji ani kreskówkowego światła.

Hierarchia obrazu:

1. sylwetka i zamiar gracza;
2. twarz lub maska postaci oraz obiekt interakcji;
3. bryła pomieszczenia i droga przejścia;
4. detal środowiskowy.

Każdy kadr powinien mieć jeden dominujący motyw barwny, jeden kontrapunkt temperaturowy i spokojne tło. Czytelność rozgrywki ma pierwszeństwo przed nastrojem.

## Paleta

Wartości HEX opisują docelowy kolor w sRGB i są punktem odniesienia, nie próbką do bezwarunkowego kopiowania na każdy materiał. Temperatura dotyczy źródła światła; barwę wynikową należy oceniać po tonemappingu.

| Rola | HEX | Temperatura | Zastosowanie |
|---|---|---:|---|
| Świetlówka — zgniła zieleń | `#C8E0C0` | około 4500 K | chłodne światło bazowe posterunku |
| Wolfram — ciepły bursztyn | `#FFB868` | około 3000 K | lampy stołowe i klucz w pokoju przesłuchań |
| Noc za oknem | `#3A4A6B` | około 7500 K | okna, dalekie wypełnienie i chłodny kontur |
| Sygnał gameplayowy | `#C22E28` | nie dotyczy | UI, stan krytyczny i `Egzekucja`; nie jako zwykła dekoracja |
| Ściany — zgaszona szałwia | `#7C8277` | nie dotyczy | duże płaszczyzny wnętrz |
| Drewno — stary orzech | `#6B4935` | nie dotyczy | meble i drobne ciepłe równoważenie kadru |
| Metal — grafit z niebieską nutą | `#465059` | nie dotyczy | szafki, ramy, wyposażenie techniczne |
| Papier — pożółkła kość słoniowa | `#D5CCB6` | nie dotyczy | dokumenty i tablice sprawy |
| Najjaśniejsza neutralna powierzchnia | `#E8E8E8` | nie dotyczy | górna granica dla „bieli” materiałów |
| Najciemniejsza neutralna powierzchnia | `#0A0A0A` | nie dotyczy | dolna granica dla „czerni” materiałów |

Kolor `#C22E28` jest zarezerwowany. Element środowiska może być czerwony tylko wtedy, gdy nie konkuruje z komunikatem gameplayowym; w przeciwnym razie należy go przesunąć w stronę rdzy lub brązu.

## Światło

### Zasady wspólne

- Bazą jest niski, ale nie niedoświetlony klucz. Czerń nie może pochłaniać sylwetek, drzwi ani przejść.
- W jednym kadrze działają maksymalnie około 4 światła realtime rzucające cienie. Dalsze źródła należy reprezentować emisją, światłem bake/mixed albo bez cieni.
- Ciepłe i chłodne źródła muszą mieć czytelną funkcję: ciepło skupia uwagę, chłód opisuje noc i instytucjonalne tło.
- Postać powinna odcinać się od tła co najmniej luminancją lub temperaturą barwową. Nie wolno polegać wyłącznie na kolorze.
- Cienie są miękkie na dużych oprawach świetlówkowych i wyraźniejsze przy małej lampie stołowej. Kontakt z podłożem pozostaje czytelny.
- Ekspozycja ma być stabilna między sąsiednimi pomieszczeniami. Krótkie przejście nie może powodować utraty czytelności gracza.

### Motywy per pomieszczenie

| Pomieszczenie | Motyw dominujący | Kontrapunkt | Cel czytelności |
|---|---|---|---|
| Pokój przesłuchań | Wolfram `#FFB868`, około 3000 K, jako skupiony klucz nad stołem | Słabe świetlówki `#C8E0C0`, około 4500 K, oraz nocny kontur `#3A4A6B`, około 7500 K | Twarz lub maska i dłonie są czytelne; narożniki pozostają ciemne, ale wyjście nie znika |
| Korytarz | Rytm lekko nieregularnych świetlówek 4300–4500 K | Ciepłe 3000 K wycieki spod drzwi i chłodny 6500–7500 K koniec osi | Powtarzalne plamy światła prowadzą do drzwi; gracz nie zlewa się ze ścianami |
| Archiwum | Płaskie, przygaszone świetlówki 4200–4500 K nad alejkami | Pojedyncza lampa robocza około 3000 K przy ważnym punkcie | Krawędzie regałów budują rytm, numery i przejścia są czytelne; mrok nie udaje horroru |
| Pokój obserwacyjny | Bardzo niskie, neutralne wypełnienie około 4000 K | Ciepły obraz pokoju przesłuchań za szybą i chłodny kontur 6500–7500 K | Sylwetki są czytelne przez szybę wenecką; odbicia nie zasłaniają obserwowanego pokoju |

Faza 2 może dobrać intensywności, zasięgi i technikę bake/mixed, ale nie powinna zmieniać ról barwnych ani hierarchii źródeł bez aktualizacji tego dokumentu.

## Materiały

Do opracowania.

## Postacie

Do opracowania.

## Zakazy

Do opracowania.
