# Brief dla Fable — poprawa doświadczenia po playteście

## Cel dokumentu

Ten dokument opisuje problemy zauważone podczas playtestu oraz oczekiwany efekt kolejnego dużego etapu prac. Cały opisany zakres ma zostać wykonany. Kolejność implementacji może wynikać z zależności technicznych, ale nie oznacza rezygnacji z późniejszych punktów.

Fable ma samodzielnie przeprowadzić potrzebny research, zaprojektować rozwiązania i od razu je wdrożyć. Nie jest wymagany osobny etap akceptacji makiet. Konkretne minigry, kompozycja ekranów, parametry balansu i szczegóły techniczne pozostają do jego oceny, o ile spełniają problemy i kryteria opisane poniżej.

## Problem i oczekiwany rezultat

Obecna wersja wygląda i zachowuje się zbyt technicznie, aby dobrze prezentować ją nowym graczom. UI przypomina narzędzie developerskie, Prywatne Cele są niejasne i sprowadzają się do przypadkowego chodzenia oraz klikania, Alibi szybko zaczynają się powtarzać, a Głos Przestrzenny pozwala usłyszeć rozmowy z nieprawidłowo dużej odległości i przez zamknięte pomieszczenia.

Po zmianach gra ma:

- wyglądać jak spójny, świadomie zaprojektowany produkt, a nie techniczny prototyp;
- jasno tłumaczyć każdemu graczowi jego aktualny cel, bez odbierania mu potrzeby eksploracji;
- oferować sensowne, różnorodne działania powiązane z sytuacją na posterunku;
- wspierać zapamiętywanie i porównywanie krótkich, zmiennych Alibi;
- pozwalać prowadzić rzeczywiście prywatne rozmowy dzięki dystansowi, ścianom i drzwiom;
- zapewniać podstawowe ustawienia potrzebne zwykłemu graczowi;
- zawierać rzadkie, nieprzewidywalne easter eggi, które urozmaicają Rundę bez zmiany jej zasad zwycięstwa.

## Źródła wymagań

### Zasady projektu, które pozostają bez zmian

- Alibi jest prawdziwą, ręcznie przygotowaną wersją wydarzeń. Niewinni widzą je w całości, Winny z wybranymi brakami, a Detektyw nie widzi go nigdy.
- Alibi jest dostępne Podejrzanym wyłącznie podczas Przygotowania i nie można go ponownie otworzyć podczas Rundy.
- Content nie jest generowany przez AI w czasie gry. Losowanie korzysta tylko z ręcznie napisanych, sprawdzonych i kompatybilnych modułów.
- Każdy Niewinny ma dokładnie jeden Prywatny Cel i wygrywa dopiero po jego ukończeniu oraz osiągnięciu Przetrwania.
- Winny może zdobywać Tropy do Alibi oraz przygotowywać Ucieczkę.
- Działania mają być widoczne, ale ich motyw ma pozostawać niejednoznaczny. System nie potwierdza obserwatorowi roli ani intencji gracza.
- Host jest właścicielem sekretów. Klient otrzymuje wyłącznie własne prywatne informacje.
- Runda ma jedną Egzekucję wykonywaną przez Detektywa. Nie dodajemy drugiej ścieżki zabijania graczy.
- Rozmowy korzystają wyłącznie z Głosu Przestrzennego. Nie tworzymy prywatnych kanałów voice.

### Nowe decyzje z tej rozmowy

- Fable wykonuje research UI/UX, tworzy spójny guide i od razu wdraża redesign bez osobnego zatwierdzania makiet.
- Przygotowanie trwa maksymalnie 30 sekund i korzysta z gotowości wszystkich graczy.
- Jedno Alibi ma dokładnie 6 czytelnych punktów oraz kontrolowaną zmienność nieistotnych szczegółów.
- Sprawa jest wybierana losowo; host nie wybiera scenariusza przed Rundą.
- Wszystkie istniejące 15 Spraw oraz 15 Osobistych Spraw ma zostać doprowadzone do grywalnego stanu. Fable ma również przygotować dodatkowe Sprawy ponad tę pulę.
- Poprawa aktywności obejmuje całość prywatnej rozgrywki: Osobiste Sprawy, Sekretne Cele i Wrobienie, Tropy do Alibi oraz Plan Ucieczki.
- Nie każdy krok ma mieć minigrę. Minigra pojawia się wyłącznie wtedy, gdy pasuje do wykonywanej czynności i rzeczywiście ją urozmaica.
- Dodajemy ograniczone noszenie i odkładanie istotnych przedmiotów.
- Zostawiamy obecny model mikrofonu i skrót `V` do mute. Naprawiamy zasięg oraz tłumienie, nie zmieniamy voice na push-to-talk.
- Easter eggi są obowiązkową częścią zakresu, ale nie mogą pozwalać na zabijanie graczy ani zmieniać warunków zwycięstwa.
- Nie powiększamy teraz mapy.
- Pomijamy temat skakania.
- Przeszukiwanie graczy przez Detektywa nie jest częścią tego zakresu. Może wrócić po powstaniu dojrzałego systemu przedmiotów.

## 1. Pełny redesign UI

### Problem

Obecny UI jest nieestetyczny, zbyt techniczny i nie nadaje grze jakości potrzebnej do pokazania jej nowym osobom. Dostępne funkcje nie tworzą czytelnego i spójnego doświadczenia.

### Wymagany rezultat

Fable ma:

1. przeprowadzić głęboki research dobrych interfejsów w grach dedukcyjnych, narracyjnych i multiplayerowych;
2. sprawdzić dobre praktyki Unity UI dotyczące skalowania, stanów, nawigacji, hierarchii informacji i utrzymania spójnego systemu wizualnego;
3. przygotować krótki guide UI obejmujący co najmniej paletę, typografię, odstępy, przyciski, karty, ikony, stany interakcji i animacje;
4. od razu zastosować guide w działającym UI;
5. przebudować wszystkie ekrany widoczne dla gracza: lobby, Przygotowanie, HUD Rundy, prezentację Prywatnego Celu i działań Winnego, Rejestr Incydentów, menu pauzy/ustawień oraz ekran wyniku;
6. schować panele i opcje developerskie przed zwykłym graczem;
7. zachować istniejący kierunek artystyczny: poważny policyjny/noir klimat, stylizowany realizm, pożółkły papier, zgaszona zieleń, grafit i oszczędna czerwień używana tylko do istotnych sygnałów;
8. ubrać techniczne funkcje w zrozumiałe, estetyczne ekrany zamiast eksponować surowe kontrolki prototypu.

UI nadal ma wyłącznie renderować informacje przeznaczone dla lokalnego gracza. Redesign nie może powodować wycieku cudzej roli, Celu, postępu, autorstwa Incydentu ani danych Planu Ucieczki.

### Kryteria akceptacji

- Każdy stan Rundy ma spójną, ukończoną oprawę i nie pokazuje domyślnych lub developerskich kontrolek.
- Najważniejsza informacja i następna dostępna akcja są zrozumiałe dla gracza, który nie zna projektu.
- UI poprawnie skaluje się w używanych rozdzielczościach i nie zasłania istotnego obrazu podczas Rundy.
- Wszystkie stany są pokazane na zrzutach ekranu i sprawdzone w działającej grze, nie tylko w statycznym dokumencie.

## 2. Przygotowanie i czytanie Alibi

### Wymagane zachowanie

- Przygotowanie trwa maksymalnie 30 sekund.
- Każdy gracz ma przycisk `Gotowy`.
- Podejrzany widzi własną wersję Alibi. Detektyw widzi swoją rolę, jawne Przestępstwo i krótką instrukcję, ale żadnej treści ani struktury Alibi.
- Gdy wszyscy gracze klikną `Gotowy`, pozostały czas zostaje skrócony do 3 sekund. Jeżeli pozostało już mniej niż 3 sekundy, timer nie jest wydłużany.
- Gotowości nie można cofnąć.
- Brak gotowości nie blokuje Rundy: po 30 sekundach zaczyna się ona automatycznie.
- Po rozpoczęciu Rundy Alibi znika bezpowrotnie i nie można go odzyskać przez UI, reconnect ani ponowne otwarcie ekranu.

## 3. Krótsze i zmienne Alibi

### Problem

Alibi jest za mało, są powtarzalne, a nadmiernie dokładne informacje brzmią nienaturalnie i są trudne do zapamiętania. Jednocześnie całkowicie przypadkowe losowanie faktów mogłoby tworzyć sprzeczne historie.

### Wymagany model contentu

- Jedno Alibi zawsze zawiera dokładnie 6 krótkich, czytelnych punktów.
- Każde Alibi ma stały, spójny rdzeń wydarzeń oraz kontrolowaną pulę zmiennych szczegółów.
- W każdym Alibi występuje co najmniej jeden charakterystyczny, ale nieistotny dla przebiegu wydarzeń szczegół, który gracze mogą zapamiętać i porównać w zeznaniach, np. kolor przedmiotu, smak przekąski, piosenka albo element ubrania.
- Zmienne szczegóły nie mogą zmieniać sensu całego Alibi ani tworzyć sprzeczności z pozostałymi punktami.
- Większość godzin jest opisywana naturalnie i ogólnie, np. `około 22:00` albo `w okolicach 10:00`.
- Nietypowo dokładna godzina, np. `21:41`, pojawia się rzadko i tylko przy wydarzeniu, które uzasadnia jej zapamiętanie, np. wystrzale lub alarmie.
- Kolory, drobne przedmioty, przybliżone godziny i podobne detale mogą rotować, jeżeli autor Sprawy oznaczył je jako zgodne warianty.
- Winny może mieć ukryty również nieistotny szczegół, ale taki szczegół nie powinien być jego jedynym brakiem. Dokładna liczba i dobór braków wymagają balansu playtestami.
- Gra losuje Sprawę oraz jej zgodne warianty. Host nie wybiera scenariusza.
- Wszystkie moduły są ręcznie napisane i walidowane. Runtime AI pozostaje zabronione.

### Zakres contentu

- Doprowadzić wszystkie 15 Spraw opisanych w `PLAYTEST-CONTENT-CATALOG.md` do grywalnego stanu.
- Doprowadzić wszystkie 15 istniejących Osobistych Spraw do grywalnego stanu.
- Przygotować dodatkowe Sprawy ponad istniejącą piętnastkę. Nie ma ustalonego maksimum.
- Jakość, spójność i możliwość sensownego redagowania są ważniejsze niż liczba.
- Dodatkowe Sprawy, których nie da się bezpiecznie wdrożyć i sprawdzić w tym etapie, mogą zostać zapisane jako kompletny katalog gotowy do późniejszej implementacji.

## 4. Sensowne aktywności wszystkich ról

### Problem

Obecne taski są niejasne. Gracz chodzi losowo po mapie, nie wie, czego szuka, gdzie powinien się udać ani dlaczego wykonuje daną czynność. Interakcje często sprowadzają się do pojedynczego kliknięcia i nie budują historii ani podejrzeń.

### Wymagany rezultat

Zmiana obejmuje wszystkie prywatne aktywności:

- Osobiste Sprawy Niewinnych;
- Sekretne Cele i Wrobienie;
- Tropy do Alibi Winnego;
- przygotowanie Planu Ucieczki.

Każda aktywność ma:

- wynikać z czytelnego, fabularnie sensownego motywu;
- tworzyć jeden spójny łańcuch zwykle 2–3 powiązanych kroków zamiast listy niezależnych czynności;
- jasno informować właściciela, czego aktualnie szuka i co powinien zrobić;
- podawać użyteczną wskazówkę miejsca, np. magazyn dowodów, archiwum albo konkretna grupa szafek;
- nie wymagać bezmyślnego przeszukiwania całej mapy;
- nie ujawniać obserwatorom, czy działanie wynika z Celu, Wrobienia, Planu Ucieczki czy blefu;
- pozostawiać czytelny stan świata albo Incydent, jeżeli charakter działania tego wymaga.

Dokładny marker przez ściany nie jest wymagany. Fable ma dobrać poziom prowadzenia gracza tak, aby Cel był zrozumiały, ale nadal wymagał eksploracji i obserwacji.

Nie wprowadzamy obowiązkowego, jednakowego cooldownu po każdym kroku. Tempo powinno przede wszystkim wynikać z przejścia do logicznego miejsca, czasu działania, ryzyka zauważenia i ewentualnej minigry. Cooldown może zostać użyty tylko tam, gdzie ma czytelną funkcję i zostanie sprawdzony w playteście.

## 5. Minigry

- Nie każdy Cel ani krok musi zawierać minigrę.
- Fable sam wybiera, gdzie minigra ma sens i jaki powinna mieć przebieg.
- Minigra ma być związana z wykonywaną czynnością, np. przeglądaniem akt, podmianą dokumentu, znalezieniem właściwego przedmiotu, odczytaniem informacji, obsługą terminala albo prostym zamkiem.
- Nie dodajemy przypadkowych zręcznościówek oderwanych od sytuacji na posterunku.
- Te same mechaniczne podstawy mogą być rozsądnie używane w różnych kontekstach; nie wymagamy unikalnej minigry dla każdej Osobistej Sprawy.
- Pomyłka może kosztować czas, wymusić ponowienie lub wywołać ślad/Incydent, ale nie może bezpowrotnie uniemożliwić ukończenia obowiązkowego Prywatnego Celu.
- Minigry nie mogą zdominować rozmów, dedukcji i obserwowania innych graczy.

## 6. Noszenie i odkładanie istotnych przedmiotów

### Wymagane zachowanie

- Gracz może nieść jeden istotny przedmiot związany z Celem, Tropem, Wrobieniem, Ucieczką albo Incydentem.
- Niesiony przedmiot jest czytelnie widoczny przy postaci oraz w lokalnym UI.
- Przedmiot można umieścić w logicznych, przygotowanych miejscach albo upuścić.
- Inni gracze mogą zauważyć, podnieść lub przenieść pozostawiony przedmiot.
- Cel zostaje zaliczony dopiero po wykonaniu właściwej czynności lub umieszczeniu właściwego przedmiotu we właściwym miejscu.
- Działania muszą być obserwowalne, ale system nie zdradza obserwatorowi motywu.
- Inny gracz nie może trwale uczynić obowiązkowego Celu niewykonalnym. Potrzebny jest bezpieczny sposób odzyskania, odtworzenia albo ponownego udostępnienia krytycznego przedmiotu.
- Zakres obejmuje istotne przedmioty gameplayowe, nie pełną fizykę i podnoszenie każdego elementu dekoracji mapy.

## 7. Naprawa Głosu Przestrzennego

### Zaobserwowany problem

Gracz mówiący w Pokoju Przesłuchań bywa słyszalny na całej mapie. Zamknięte drzwi i ściany nie zapewniają oczekiwanej prywatności. Możliwą przyczyną jest wcześniejszy problem z synchronizacją drzwi, błędne przypisanie pomieszczeń, nieprawidłowa occlusion albo zbyt duży zasięg Vivox — wymaga to diagnozy, nie zgadywania.

### Wymagany rezultat

- Zachować Vivox oraz obecny model voice activation/ciągłego nasłuchu ze skrótem `V` przełączającym mute.
- Wyraźnie zmniejszyć praktyczny zasięg rozmowy. Żeby dobrze rozumieć gracza, trzeba znajdować się blisko niego.
- W tym samym pomieszczeniu głos ma naturalnie cichnąć wraz z dystansem.
- Na końcu długiego korytarza głos może delikatnie docierać, ale nie może brzmieć jak rozmowa prowadzona obok słuchacza.
- Pełna ściana ma praktycznie blokować głos.
- Zamknięte drzwi mają bardzo mocno ściszać i filtrować mowę.
- Podsłuchiwanie przez zamknięte drzwi ma być możliwe wyłącznie bezpośrednio przy drzwiach. Dźwięk powinien być wtedy cichy, stłumiony i trudniejszy do zrozumienia.
- Otwarcie drzwi ma w czytelny sposób przywracać słyszalność.
- Naprawa musi uwzględniać rzeczywisty, zsynchronizowany stan drzwi oraz relację pomieszczeń, nie tylko prosty dystans między graczami.

### Test odbiorczy

Test na co najmniej dwóch klientach ma objąć:

1. rozmowę bezpośrednio obok siebie w jednym pokoju;
2. rozmowę z przeciwnych końców tego samego pomieszczenia;
3. graczy na dwóch końcach korytarza;
4. graczy po przeciwnych stronach pełnej ściany;
5. graczy po przeciwnych stronach otwartych drzwi;
6. graczy po przeciwnych stronach zamkniętych drzwi, zarówno daleko od nich, jak i bezpośrednio przy nich;
7. otwieranie i zamykanie drzwi podczas rozmowy.

## 8. Menu ustawień

- `Esc` otwiera estetyczne menu dostępne zwykłemu graczowi.
- Menu zawiera co najmniej ustawienie czułości myszy.
- Zmiana działa natychmiast i jest zapamiętywana między uruchomieniami gry.
- Opcje techniczne i developerskie nie mogą zaśmiecać podstawowego menu. Jeżeli muszą pozostać dostępne, powinny być wyraźnie oddzielone i domyślnie ukryte przed zwykłym graczem.
- Menu multiplayer nie zatrzymuje czasu całej Rundy.

## 9. Obowiązkowe, rzadkie easter eggi

Fable ma zaprojektować i wdrożyć system rzadkich easter eggów oraz początkową pulę zdarzeń lub przedmiotów.

Easter egg:

- nie pojawia się w każdej Rundzie;
- powinien być na tyle rzadki, aby pozostał zaskoczeniem;
- może pojawiać się w różnych logicznych miejscach i działać w różny sposób;
- może być interaktywnym przedmiotem, drobnym zdarzeniem, sekretem mapy albo nietypową reakcją świata;
- może tworzyć zabawną sytuację, miękki trop lub Incydent;
- nie może ujawniać tajnej roli, gwarantować zwycięstwa ani trwale blokować obowiązkowego Celu;
- nie może pozwalać na zabijanie graczy.

Fable ma sam wymyślić konkretne easter eggi. Priorytetem jest ich różnorodność, rzadkość i dopasowanie do świata, a nie duża liczba pozbawionych znaczenia żartów.

## Granice techniczne, których nie wolno złamać

- Reguły Rundy, przydział Celów, postęp, losowanie dozwolonego contentu i wyniki pozostają w `RoundEngine`.
- `NetworkRoundCoordinator` pozostaje jedynym adapterem Mirror mapującym połączenia na graczy i rozsyłającym prywatne widoki.
- Interakcje scenowe oraz minigry zgłaszają intencje; nie rozstrzygają lokalnie postępu ani zwycięstwa.
- UI renderuje własny widok i nie przechowuje sekretów pozostałych graczy.
- Głos Przestrzenny pozostaje niezależny od `RoundEngine`.
- Content jest authorowany ręcznie i konwertowany do niezmiennych definicji domenowych przed Rundą.
- Sekrety nie mogą być rozsyłane globalnymi polami synchronizowanymi.

## Weryfikacja całości

### Testy automatyczne

- Edit Mode: 30-sekundowe Przygotowanie, gotowość wszystkich graczy, skrócenie do 3 sekund i bezpowrotne usunięcie Alibi.
- Edit Mode: losowanie Spraw oraz kompatybilnych wariantów bez sprzeczności i z dokładnie 6 punktami.
- Edit Mode: prywatność widoków, sekwencje Celów, brak trwałego zablokowania obowiązkowego przedmiotu i poprawne indywidualne wyniki.
- Edit Mode contentu: walidacja wszystkich grywalnych Spraw, powiązań Tropów, ukrywalnych faktów, unikalnych identyfikatorów i konfliktujących przedmiotów.
- Play Mode: noszenie, upuszczanie, odkładanie i przejmowanie przedmiotów; minigry; przerwanie działania; zmiany świata i Incydenty.
- Testy UI: wszystkie fazy oraz role renderują właściwe informacje bez wycieku sekretów.

### Testy multiplayer i wizualne

- Najpierw KCP/ParrelSync: host oraz klienci przechodzą pełne Przygotowanie, Rundę i wynik z różnymi prywatnymi widokami.
- Głos Przestrzenny przechodzi pełną macierz testów opisaną w sekcji voice.
- Każdy przebudowany ekran jest sprawdzony wizualnie w grze i pokazany na zrzutach.
- Po zmianach kod kompiluje się bez błędów, Console nie zawiera nowych błędów, a `git diff --check` przechodzi.
- Test Steam/FizzySteamworks następuje dopiero po udanym teście lokalnym KCP.

## Poza zakresem

- Powiększanie lub gruntowna przebudowa mapy.
- Zmiany skakania.
- Śmiercionośny nóż lub jakakolwiek dodatkowa metoda zabijania graczy.
- Zmiana warunku zwycięstwa Niewinnego na zabicie Winnego.
- Przeszukiwanie graczy przez Detektywa i cooldown takiego przeszukania.
- Prywatne kanały głosowe lub przejście na push-to-talk.
- Runtime AI generujące Alibi, Cele, Tropy albo easter eggi.
- Pełny system podnoszenia każdego dekoracyjnego obiektu na mapie.
- Finalna forma Notatek Detektywa oraz decyzja o ujawnianiu pełnego Alibi na ekranie wyników.

## Decyzje pozostawione Fablowi

Poniższe elementy nie są brakami produktu blokującymi pracę. Fable ma rozstrzygnąć je na podstawie researchu, istniejącej architektury i testów:

- dokładna kompozycja i animacja ekranów UI;
- konkretne typy oraz liczba minigier;
- miejsca, treść i liczba pierwszych easter eggów;
- docelowe krzywe dystansu, poziomy tłumienia i filtry Głosu Przestrzennego;
- dokładna liczba zmiennych wariantów w każdej Sprawie oraz balans braków Winnego;
- forma wskazówek miejsca dla Celów, o ile nie zamienia się w bezmyślne markery prowadzące przez całą mapę;
- szczegóły bezpiecznego odzyskiwania obowiązkowych przedmiotów;
- kolejność realizacji prac wynikająca z zależności technicznych.

Wszystkie te decyzje muszą wspierać główny cel: czytelniejszą, bardziej różnorodną i atrakcyjną Rundę, w której aktywności wzmacniają rozmowę, podejrzenia i dedukcję zamiast je zastępować.
