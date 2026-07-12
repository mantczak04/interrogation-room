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

Materiały są fizycznie wiarygodne, lecz uproszczone do cech rozpoznawalnych z dystansu rozgrywki. Najpierw czyta się rodzaj powierzchni i stopień zużycia, dopiero potem drobny detal.

- Docelowa texel density środowiska to **512 px/m**. Odstępstwa dla małych rekwizytów fabularnych lub powierzchni oglądanych z bliska wymagają jawnego uzasadnienia.
- Roughness zawsze zawiera wariację w skali makro i mikro. Jednolita wartość na całej powierzchni jest niedozwolona, nawet dla pozornie prostego plastiku czy metalu.
- Zużycie wynika z użycia: dotykane krawędzie, okolice uchwytów, ciągi komunikacyjne, kurz w zagłębieniach. Nie nakładać jednakowego grunge'u na każdy obiekt.
- Metalness rozdziela metal od niemetalu; zabrudzenie lub farba na metalu pozostają niemetaliczne. Materiał nie może być „trochę metaliczny” wyłącznie dla atrakcyjnego połysku.
- Duże powierzchnie otrzymują łagodną zmianę tonu, roughness lub zabrudzenia, aby nie wyglądały syntetycznie, ale nie mogą tworzyć szumu konkurującego z postaciami.
- Najjaśniejsza neutralna powierzchnia to `#E8E8E8`, a najciemniejsza `#0A0A0A`. Czyste `#FFFFFF` i `#000000` są zabronione.
- Emisja oznacza faktyczne źródło światła lub komunikat. Nie służy do przypadkowego „upiększania” materiału.
- Powtarzalne elementy kitu muszą zachować wspólną skalę, paletę i poziom zużycia. Warianty buduje się rozmieszczeniem śladów, kolorem wtórnym i detalem, nie zmianą języka materiałowego.

## Postacie

Postacie są absurdalne w formie, ale oświetlane i osadzane w świecie tak samo poważnie jak ludzie w kinowym kryminale. Ich czytelność wynika z sylwetki, proporcji i animacji całego ciała.

- Sylwetki muszą być rozróżnialne w ciemności, pod światło i przez szybę wenecką. Test akceptacyjny używa jednolitego czarnego wypełnienia bez tekstur.
- Każda postać ma inny dominujący obrys: Jak — szeroki górny kontur z rogami; Małpa — długie kończyny i pochylona linia; Wieprz — niski, masywny korpus z czytelnym pyskiem; Karton — kanciasta, prostokątna bryła.
- Czytelność ma pierwszeństwo przed detalem. Drobne akcesoria nie mogą być jedyną cechą odróżniającą postać.
- Brak riggu mimiki jest świadomą zasadą stylu. Sztywne pyski i maski budują absurd; emocję przenoszą poza, przechylenie głowy, gest, tempo i bezruch.
- Oczy i najważniejsze płaszczyzny pyska lub maski muszą pozostać czytelne w motywach świetlnych z sekcji „Światło”, bez samopodświetlenia.
- Paleta postaci może tworzyć kontrast z pomieszczeniem, ale nie może używać sygnałowej czerwieni `#C22E28` jako dużej dominującej plamy.
- Wygląd, materiał i kolor postaci nigdy nie kodują tajnej roli, `Alibi`, celu ani wyniku `Rundy`. Te same modele pozostają neutralne względem informacji gameplayowych.

## Zakazy

- Fotorealizm, skanowy nadmiar detalu i szum tekstur.
- Cel-shading, komiksowy kontur oraz kreskówkowe światło środowiska.
- Żartobliwe dekoracje tłumaczące absurd postaci; posterunek pozostaje szczery i wiarygodny.
- Globalny zielony filtr. Zieleń pochodzi ze świetlówek i materiałów, a skóra, papier oraz wolfram zachowują własną barwę.
- Czyste biele, czyste czernie, jednolity roughness i nieuzasadniona emisja.
- Neonowe akcenty bez funkcji oraz dekoracyjne użycie czerwieni `#C22E28`.
- Mrok ukrywający gracza, drzwi, drogę przejścia lub istotny obiekt.
- Bloom przepalający kształt lampy, twarz, maskę albo komunikat UI.
- Materiałowe zabrudzenie nakładane równomiernie na wszystkie krawędzie i powierzchnie.
- Cechy wizualne zdradzające tajną rolę lub prywatną informację gracza.

## Kontrola spójności

Każdy art pass w Fazach 3, 5 i 6 powinien cytować ten dokument oraz odpowiedzieć na cztery pytania:

1. Czy hierarchia kadru prowadzi najpierw do gracza lub interakcji?
2. Czy dominujący motyw i kontrapunkt temperaturowy odpowiadają sekcji „Światło”?
3. Czy materiały zachowują skalę 512 px/m, wariację roughness i granice neutralnej bieli/czerni?
4. Czy postać pozostaje rozpoznawalna jako czarna sylwetka i nie zdradza informacji gameplayowej?

Zmiana kierunku estetycznego wymaga aktualizacji tego dokumentu, a nie lokalnego wyjątku ukrytego w scenie, materiale lub modelu.
