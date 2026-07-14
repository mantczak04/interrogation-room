# Katalog contentu do playtestów

**Status:** roboczy materiał do review, niezatwierdzony content produkcyjny

**Zakres:** 15 kandydatów na Sprawę, biblioteka Prywatnych Celów i macierz integracji z przedmiotami

**Powiązane:** [content sprawy](./mechanics/content-sprawy.md), [Prywatne Cele, Incydenty i Ucieczka](./mechanics/prywatne-cele-incydenty-i-ucieczka.md), [lista propsów](./graphics/PROPSY-DO-GENERACJI.md)

## Jak czytać ten dokument

To jest katalog do oceny przez zespół, a nie automatyczny wsad do gry. Każdą Sprawę można odrzucić, przepisać albo połączyć z inną. Dopiero zatwierdzony content powinien zostać przeniesiony do `CaseAsset` i modułów Celów.

Rozdzielamy dwa rodzaje contentu:

- **content zależny od Sprawy:** Przestępstwo, pełne Alibi, kandydaci do ukrycia i Tropy do Alibi;
- **content wielokrotnego użytku:** Osobiste Sprawy, Wrobienia, działania Planu Ucieczki, punkty interakcji i Incydenty na posterunku.

Nie wiążemy na stałe każdej Osobistej Sprawy z konkretnym Alibi. Dzięki temu 15 Spraw nie wymaga 15 osobnych zestawów mechaniki świata, a te same obserwowalne działania mogą mieć różne motywy.

### Wspólny kontrakt pierwszego playtestu

- Każda Sprawa ma 8 krótkich faktów `F1–F8`.
- Dokładnie 3 fakty są kandydatami do ukrycia; pierwsza wersja Rundy ukrywa Winnemu 2 z nich.
- Każdy kandydat ma jeden ręcznie napisany Trop wymagający interpretacji.
- Dla konkretnej Rundy host aktywuje tylko **jeden** Trop powiązany z jednym z dwóch faktycznie ukrytych faktów. Trzeci Trop pozostaje contentem dla innych kombinacji braków, a drugiego braku nie da się uzupełnić systemowym Tropem w tej Rundzie.
- Zdobyty Trop pokazuje własną treść i nie przywraca tekstu Alibi.
- Winny nie musi i nie powinien móc odzyskać wszystkich braków w każdej Rundzie.
- Każda akcja przy przedmiocie jest dostępna dowolnemu Podejrzanemu; tylko właściciel właściwego Celu uzyskuje postęp.
- Każda podejrzana akcja ma co najmniej dwa wiarygodne motywy.
- Ciche Incydenty stają się formalnym wpisem dopiero po osobistej inspekcji Detektywa. Hałaśliwe są raportowane natychmiast.
- Wszystkie lokacje są logicznymi, ręcznie wybranymi punktami. Nie ma markerów prowadzących do celu ani losowania w przypadkowej geometrii.

## Co jest ustalone, a co pozostaje otwarte

Skróty `A*`, `B*` i `C*` w poniższych tabelach oznaczają wyłącznie tickety Linear projektu `Interrogation Room`; pełne identyfikatory i linki są podane w następnej sekcji. Nie są to kody animacji ani assetów z dokumentów graficznych.

| Element | Status | Gdzie żyje / kiedy rozstrzygamy |
|---|---|---|
| Ręcznie authorowane Przestępstwo i Alibi | Ustalone | `CaseAsset` / `CaseDefinition` |
| 6–10 krótkich faktów; w tym katalogu zawsze 8 | Ustalone dla katalogu | Content przed implementacją |
| 3 kandydatów do ukrycia i 2 braki w pierwszym playteście | Roboczy parametr katalogu | Potwierdzić po pierwszych Rundach |
| 1 aktywny Trop przy 2 brakach | Roboczy parametr katalogu | Chroni zasadę, że Tropy nie uzupełniają wszystkich braków; potwierdzić po playteście |
| Paragon, zdjęcie, wiadomość i dokument jako Tropy | Ustalone jako rodziny nośników | Konkretna treść powstaje teraz per Sprawa |
| Trop wymaga interpretacji i nie pokazuje faktu wprost | Ustalone | A3 oraz B5 |
| Dokładnie jeden Prywatny Cel każdego Niewinnego | Ustalone | A1 |
| Osobista Sprawa i Wrobienie są dwukrokowe w prototypie | Ustalone roboczo | A1, B4 |
| Widoczna czynność, niejednoznaczny motyw | Ustalone | B2, B4, B5 |
| Klucz, telefon, papierosy, dokumenty i depozyt jako wspólny kit | Ustalone jako kierunek | B4/B5 na placeholderach, C4/C5 jako finalny art |
| Dokładne teksty, identyfikatory, przedmioty i konflikty Celów | Brakowało właściciela | Ten katalog, potem osobny ticket contentowy |
| Fizyczna forma dwóch punktów Ucieczki | Otwarte | Placeholder w B5/A6; finalna decyzja po playteście |
| Czasy interakcji, liczba Tropów i rozmieszczenie | Do strojenia | [BSPL-15 / A7](https://linear.app/sunomvp/issue/BSPL-15/a7-pelny-playtest-kcp-456-graczy-i-raport-strojenia), po grywalnym tracerze |
| Powiększenie mapy | Odłożone | Dopiero po danych z playtestu |
| Finalna forma UI Alibi/Celu/Notatek | Otwarte | Osobna decyzja UI |

## Pokrycie przez obecne tickety

| Obszar | Co obecne tickety już zapewniają | Czego nie powinny same wymyślać |
|---|---|---|
| [BSPL-3 / A1](https://linear.app/sunomvp/issue/BSPL-3/a1-prywatne-cele-kontrakty-przydzial-i-indywidualne-wyniki) | Definicje, przydział, prywatność i wyniki Prywatnych Celów | Teksty i konkretne łańcuchy Celów |
| [BSPL-5 / A2](https://linear.app/sunomvp/issue/BSPL-5/a2-incydenty-i-prywatny-rejestr-detektywa-w-domenie) | Model Cichych/Hałaśliwych Incydentów oraz Rejestr Detektywa | Konkretne skutki przedmiotów i ich nazwy w świecie |
| [BSPL-7 / A3](https://linear.app/sunomvp/issue/BSPL-7/a3-tropy-do-alibi-plan-ucieczki-i-wszystkie-konce-rundy) | Schema Tropów, powiązanie z faktem, Plan Ucieczki i końce Rundy | Treść paragonu, zdjęcia, wiadomości i wybór clue per Sprawa |
| [BSPL-6 / B2](https://linear.app/sunomvp/issue/BSPL-6/b2-wspolny-runtime-trwajacych-i-przerywalnych-interakcji) | Wspólny runtime trwających, przerywalnych działań | Znaczenie fabularne działania |
| [BSPL-9 / B4](https://linear.app/sunomvp/issue/BSPL-9/b4-fizyczny-tracer-niewinnego-osobista-sprawa-wrobienie-i-incydenty) | Jeden grywalny tracer Osobistej Sprawy, Wrobienia i Incydentów | Pełna biblioteka Celów |
| [BSPL-11 / B5](https://linear.app/sunomvp/issue/BSPL-11/b5-fizyczny-tracer-winnego-trop-do-alibi-i-dwa-warianty-ucieczki) | Jeden grywalny Trop i placeholderowe warianty Ucieczki | Biblioteka Tropów i finalna forma wyjść |
| [BSPL-14 / A6](https://linear.app/sunomvp/issue/BSPL-14/a6-ui-i-integracja-sceny-cele-incydenty-tropy-ucieczka-i-wyniki) | Rozmieszczenie prefabów, anchory, UI i integracja sceny | Samodzielne tworzenie contentu podczas integracji |
| [BSPL-23 / C4](https://linear.app/sunomvp/issue/BSPL-23/c4-batch-4-interaktywny-kit-gameplayowy-41-55) i [BSPL-22 / C5](https://linear.app/sunomvp/issue/BSPL-22/c5-batch-5-tropy-wrobienie-i-incydenty-56-68) | Finalne neutralne modele przedmiotów i nośników | Tekst rozwiązania Alibi i logika Celu |

Po review katalogu warto utworzyć osobny obszar `Gameplay Content Authoring`: pierwszy ticket zatwierdza bibliotekę, drugi przenosi wybrane moduły do assetów i waliduje kompatybilność. Nie należy wrzucać authoringu 15 Spraw do A3 ani integracji sceny A6.

## Biblioteka Osobistych Spraw Niewinnych

Każdy moduł jest dwukrokowy, korzysta z rezerwowanych zasobów i może zostać przypisany niezależnie od wybranej Sprawy. Inny gracz może wykonać podobną akcję dla blefu, ale nie otrzymuje postępu. Jeżeli unikalny przedmiot został zarezerwowany przez obowiązkowy Cel, generator nie przydziela drugiego konfliktującego Celu.

| ID | Prywatny motyw | Krok 1 | Krok 2 | Widoczny ślad / Incydent |
|---|---|---|---|---|
| OS-01 | Odzyskaj skonfiskowane papierosy | Zabierz właściwy klucz z listwy | Otwórz depozyt i zabierz paczkę | Brak klucza i otwarty depozyt; Ciche |
| OS-02 | Usuń kompromitujący list | Odszukaj kopertę w kartotece | Wyjmij list i ukryj go w neutralnej skrytce | Pusta koperta; Cichy |
| OS-03 | Odzyskaj prywatny telefon | Znajdź żeton/numer depozytu w rejestrze | Otwórz odpowiadającą szafkę i zabierz telefon | Pusty slot po telefonie; Cichy |
| OS-04 | Usuń wpis o długu | Zabierz pieczątkę lub korektor z biurka | Zmień wskazany wpis w księdze | Zmieniony dokument; Cichy |
| OS-05 | Odzyskaj rodzinne zdjęcie | Przeszukaj oznaczone pudełko dowodowe | Wyjmij fotografię i zostaw pustą przekładkę | Naruszona plomba; Cichy |
| OS-06 | Zniszcz kupon bukmacherski | Odczytaj sygnaturę z indeksu archiwum | Odszukaj teczkę i wyrzuć kupon do kosza | Brak strony/śmieć w koszu; Cichy |
| OS-07 | Odzyskaj pierścionek | Zabierz mały klucz z koperty depozytowej | Otwórz kasetkę i zabierz pierścionek | Otwarta kasetka; Cichy |
| OS-08 | Nadpisz kompromitujące nagranie | Zabierz właściwą kasetę z półki | Włóż ją do magnetofonu i nadpisz fragment | Brak kasety na półce i kaseta w urządzeniu; **Cichy**. Dźwięk magnetofonu pozostaje zwykłym audio przestrzennym, nie globalnym raportem |
| OS-09 | Odzyskaj leki | Znajdź receptę w osobistej teczce | Użyj jej, aby otworzyć oznaczoną szafkę | Pusty slot po butelce; Cichy |
| OS-10 | Ukryj sfałszowane zwolnienie | Zabierz pieczątkę z biurka | Ostempluj dokument i włóż go do swojej teczki | Przeniesiona pieczątka i nowa strona; Cichy |
| OS-11 | Usuń niewygodne zeznanie | Zdobądź klucz do archiwum | Podmień wskazaną stronę protokołu | Otwarta szafa i podmieniony dokument; Cichy |
| OS-12 | Odzyskaj szczęśliwą monetę | Sprawdź numer pakietu w księdze depozytu | Otwórz właściwą kopertę i zabierz monetę | Rozerwana koperta; Cichy |
| OS-13 | Usuń numer telefonu bliskiej osoby | Zabierz kartę wiadomości z biurka | Podmień ją w teczce na neutralną kartę | Brak oryginału; Cichy |
| OS-14 | Odzyskaj kwit z lombardu | Przeszukaj rzeczy osobiste w szafce | Przenieś kwit do książki lub skrytki | Naruszony depozyt; Cichy |
| OS-15 | Ukryj kompromitującą fotografię | Zabierz zdjęcie z koperty dowodowej | Podmień je neutralną odbitką | Zmieniona zawartość koperty; Cichy |

### Minimalny zestaw do pierwszego wdrożenia

Nie implementujemy wszystkich 15 naraz. Pierwszy tracer powinien użyć `OS-01`, `OS-03`, `OS-11` i `OS-15`, ponieważ razem pokrywają klucz, telefon, dokument, zdjęcie, depozyt i podmianę. Pozostałe moduły są backlogiem contentowym, nie piętnastoma nowymi systemami.

## Biblioteka Wrobienia

| ID | Krok 1 | Krok 2 | Rezultat możliwy do interpretacji |
|---|---|---|---|
| WR-01 | Zabierz papierosy albo zapalniczkę z depozytu | Podłóż je w rzeczy osobiste Celu | Przedmiot powiązany z Celem, bez dowodu autora |
| WR-02 | Zabierz brakujący klucz z listwy | Umieść go w kopercie lub szafce Celu | Klucz znaleziony przy Celu, ale mógł zostać podrzucony |
| WR-03 | Przygotuj podmienioną stronę protokołu | Włóż ją do teczki Celu | Dokument sugeruje związek, lecz ma widoczne ślady manipulacji |
| WR-04 | Zabierz cudzy telefon albo kartę wiadomości | Zostaw ją przy depozycie Celu | Obecność rzeczy nie potwierdza, kto ją przeniósł |

Wszystkie cztery warianty tworzą Cichy Incydent odkrywany dopiero przy osobistej inspekcji Detektywa.

## Kandydaci na działania Planu Ucieczki

To nie są jeszcze finalne Plany ani wyjścia. Są pulą obserwowalnych przygotowań do użycia na placeholderach. Każde ma pokrycie w działaniach Niewinnych.

| ID | Działanie | Alternatywny motyw Niewinnego | Status |
|---|---|---|---|
| PU-01 | Zabierz klucz z listwy | `OS-01`, `OS-07`, `OS-11` | Gotowe do prototypu |
| PU-02 | Przeszukaj szafkę depozytową | `OS-03`, `OS-05`, `OS-14` | Gotowe do prototypu |
| PU-03 | Zabierz małe narzędzie z kasetki | Otwarcie prywatnej kasetki lub odzyskanie rzeczy | Motyw Niewinnego trzeba dopisać jako osobny moduł |
| PU-04 | Wyjmij dokument z archiwum | `OS-02`, `OS-06`, `OS-11` | Gotowe do prototypu |
| PU-05 | Użyj telefonu stacjonarnego | Sprawdzenie prywatnej wiadomości lub numeru | Wymaga decyzji, jaki skutek przygotowuje Ucieczkę |
| PU-06 | Przygotuj jeden z dwóch punktów końcowych | Brak — finał ma celowo ujawniać Winnego świadkowi | Forma punktów nadal otwarta |

Nie dodajemy przesłuchiwanym napraw bezpieczników, instalacji elektrycznej ani innych zadań technicznych oderwanych od ich sytuacji na posterunku.

## Rejestr przedmiotów i integracji

| Przedmiot / stan | Zastosowania | Ticket systemowy | Asset finalny |
|---|---|---|---|
| Paragon | Trop do konkretnego faktu Alibi; papier do obejrzenia z bliska | A3, B5, A6 | C5 / pozycja 56 |
| Fotografia | Trop albo przedmiot Osobistej Sprawy/Wrobienia | A3, B4, B5 | C5 / pozycja 57 |
| Wiadomość lub list | Trop, Osobista Sprawa, podmiana dokumentu | A3, B4, B5 | C5 / pozycja 58 |
| Protokół/formularz | Trop, zmiana wpisu, Wrobienie | A2, A3, B4 | C5 / pozycje 59 i 63 |
| Kaseta | Trop lub Osobista Sprawa; możliwy lokalny hałas | A2, A3, B4/B5 | C5 / pozycja 60 |
| Szafka/kasetka/depozyt | Wspólny punkt poszukiwania i trwałych stanów | B2, B4, B5, A6 | C4 / pozycje 41–45 |
| Klucz i listwa | Cel Niewinnego, Wrobienie, Plan Ucieczki, blef | B2, B4, B5 | C4 / pozycje 46–47 |
| Telefon | Cel Niewinnego, Wrobienie, możliwy krok Ucieczki, blef | B4, B5 | C4 / pozycje 48–49 |
| Papierosy/zapalniczka | Cel Niewinnego, Wrobienie i blef | B4 | C4 / pozycje 50–51 |
| Narzędzia drobne | Kandydat do przygotowania Ucieczki i prywatnej kasetki | B5 | C4 / pozycja 55 |

### Dodatkowe wymagania ujawnione przez katalog

Katalog nie powinien niepostrzeżenie stworzyć kilkudziesięciu nowych modeli 3D. Większość Tropów korzysta z już zaplanowanych neutralnych nośników i różni się wyłącznie ręcznie authorowaną treścią:

- paragon, karta, kwit, formularz, karta kolejki, setlista i wydruk używają baz papierowych 56, 58 albo 59;
- zdjęcie, negatyw i stykówka używają bazy fotografii 57;
- mały fizyczny ślad, guzik, gaza, próbka, żeton lub zawieszka trafia do koperty/woreczka 45 z etykietą 61;
- treść telefonu jest ręcznie przygotowanym ekranem na bazie modeli 48–49;
- klucze, szafki, kasetki, teczki i depozyty korzystają z pozycji 41–47;
- kaseta i jej etykieta korzystają z pozycji 60.

Do zatwierdzenia po wyborze pierwszych Spraw pozostaje osobna lista **tekstur i układów graficznych Tropów**. Maksymalny katalog to 45 wariantów treści, ale pierwszy tracer C01 potrzebuje tylko trzech. Nie generujemy osobnego modelu fartucha, tabliczki licytacyjnej albo zabawki wyłącznie dlatego, że pojawia się na jednym Tropie — w pierwszej wersji może to być fotografia lub mały przedmiot w neutralnej kopercie. Nowy model 3D trafia do Area C dopiero wtedy, gdy zatwierdzona interakcja wymaga, by gracz wyraźnie widział i przenosił właśnie ten przedmiot w świecie.

## Kandydaci na Sprawy

Poniższe propozycje są materiałem do krytycznego review. Fakty oznaczone `H` są kandydatami do ukrycia Winnemu.

### Kryteria review każdej Sprawy

Przed zatwierdzeniem odpowiedz osobno dla każdego kandydata:

1. Czy Przestępstwo jest zabawne, ale daje się wyjaśnić jednym zdaniem?
2. Czy osiem faktów tworzy jedną chronologiczną wersję, którą da się opowiedzieć głosem bez czytania?
3. Czy każdy kandydat do ukrycia jest istotny dla rozmowy, ale brak nie uniemożliwia Winnemu całego zeznania?
4. Czy Trop pozwala wyciągnąć wniosek, zamiast kopiować fakt?
5. Czy treść nośnika nie ma tylko jednej możliwej interpretacji?
6. Czy dwukrokowe zdobycie Tropu jest logiczne na posterunku?
7. Czy obserwator rozumie wykonywaną czynność, ale nie poznaje jej motywu?
8. Czy żaden Trop nie wymaga finalnego assetu, którego nie ma w C4/C5?
9. Czy wybrane przedmioty nie blokują obowiązkowych Celów pozostałych graczy?
10. Czy Sprawa nadal działa przy 4, 5 i 6 graczach bez zmiany treści Alibi?

### C01 — Różowy pomnik burmistrza

**Źródło:** istniejący `SprawaRozowyPomnik.asset`; treść bazowa zachowana.

**Przestępstwo:** Ktoś przemalował pomnik burmistrza na różowo i przykleił mu wąsy z waty cukrowej, gdy Niewinni byli razem na kolacji.

**Pełne Alibi:**

- `F1` O 19:00 wszyscy spotkali się w pizzerii „U Grubego” przy rynku.
- `F2 H` Zamówili trzy pizze: hawajską, capricciosę i podwójny ser.
- `F3 H` Kelner pomylił rachunek i doliczył cztery kompoty, których nikt nie zamawiał.
- `F4` Około 19:40 na kilka minut zgasło światło w całym lokalu.
- `F5 H` Kucharz opowiedział dowcip o strażaku, z którego nikt się nie zaśmiał.
- `F6` Po kolacji grali w rzutki; zwycięzca dostał darmowy deser.
- `F7` Wyszli po 21:00 i razem doszli do przystanku przy fontannie.
- `F8` W drodze powrotnej minęli patrol straży miejskiej.

**Tropy:**

| Fakt | Nośnik i widoczna treść | Wniosek | Zdobycie i Incydent |
|---|---|---|---|
| `F2` | Oryginalny **paragon**: trzy pozycje oznaczone skrótami `HAW`, `CAP` i `2×SER`; nagłówek lokalu jest naderwany | Zamówiono trzy różne pizze, w tym jedną z podwójnym serem; nie wynika, kto je jadł | Odczytaj numer koperty z protokołu → otwórz właściwą szufladę depozytu; zerwana plomba, **Cichy** |
| `F3` | Fragment reklamacji: kilka identycznych pozycji napoju przekreślonych jedną kreską; liczba, nazwa napoju i dopisek klienta są częściowo urwane | Przy rachunku kwestionowano kilka powtórzonych napojów, ale nie wiadomo dokładnie ile ani jakich | Znajdź sygnaturę w indeksie → wyjmij kartę z teczki; brak dokumentu, **Cichy** |
| `F5` | Kartka kucharza z rysunkiem hełmu strażackiego i skreślonym punchline'em; obok cztery puste kreski ocen | Kucharz opowiadał nieudany dowcip o strażaku, ale karta nie potwierdza reakcji grupy | Zabierz klucz do kasetki → wyjmij kartkę z rzeczy osobistych; pusty hak/otwarta kasetka, **Cichy** |

**Wspólne akcje:** paragon może być Tropem, prywatnym kwitem Niewinnego albo rzeczą podmienianą we Wrobieniu; szukanie teczki i zabranie klucza pokrywa `OS-06`, `OS-11`, `WR-02`, `PU-01` i blef.

**Do review:** To powinien być pierwszy tracer contentowy. Trzeba sprawdzić, czy paragon nie daje jednocześnie zbyt łatwo `F2` i `F3` oraz czy nieudany dowcip jest wystarczająco użytecznym faktem w przesłuchaniu.

### C02 — Wesele bez Tortu

**Źródło:** istniejący `Case_Wesele.asset`; siedem bazowych faktów zachowanych, `F8` jest proponowanym domknięciem.

**Przestępstwo:** Ktoś wystrzelił weselny tort z armatki konfetti na dach remizy.

**Pełne Alibi:**

- `F1` O 18:30 wszyscy zajęli miejsca przy stole obok orkiestry.
- `F2 H` Świadek pana młodego rozlał kompot z agrestu na biały obrus.
- `F3 H` Kapela zagrała polkę, gdy zegar nad barem wskazywał 18:47.
- `F4` Ciocia Grażyna rozdawała papierowe korony z napisem „Król Parkietu”.
- `F5 H` Kelner wniósł tort na wózku z jednym skrzypiącym kołem.
- `F6` Przez pięć minut zgasło światło, ale muzyka nadal grała.
- `F7` O 19:05 grupa wyszła przed remizę oglądać pokaz zimnych ogni.
- `F8` Po pokazie wszyscy razem czekali na zamówioną taksówkę przy bramie remizy.

**Tropy:**

| Fakt | Nośnik i widoczna treść | Wniosek | Zdobycie i Incydent |
|---|---|---|---|
| `F2` | Fotografia obrusa z jasnozieloną plamą obok winietki świadka; dzbanek jest poza kadrem | Przy miejscu świadka rozlano zielony napój, ale zdjęcie nie mówi jaki | Odczytaj numer zgłoszenia → wyjmij stykówkę z archiwum; pusta koszulka, **Cichy** |
| `F3` | Setlista kapeli: symbol polki obok godziny `18:47`, bez opisu zegara i wykonawców | O tej godzinie grano taneczny utwór określonego rodzaju | Znajdź numer zespołu w książce kontaktów → odsłuchaj nagranie z kasety; słyszalny fragment muzyki, **Hałaśliwy** |
| `F5` | Kwit naprawy wózka z zaznaczonym jednym przednim kołem i dopiskiem „piszczy pod obciążeniem” | Jeden z używanych wózków miał charakterystycznie skrzypiące koło | Zabierz klucz z listwy → otwórz szafę dokumentów dostaw; pusty hak, **Cichy** |

**Wspólne akcje:** zdjęcie, kaseta i klucz mogą obsługiwać `OS-05`, `OS-08`, `OS-11`, `WR-02`, `PU-01` oraz blef.

**Do review:** `F8` nie istnieje jeszcze w assetcie. Jest roboczym domknięciem wymagającym zatwierdzenia przed migracją; katalog zachowuje standard ośmiu faktów.

### C03 — Incydent w Obserwatorium

**Źródło:** istniejący `Case_Obserwatorium.asset`; siedem bazowych faktów zachowanych, `F8` jest propozycją.

**Przestępstwo:** Ktoś przykleił do teleskopu ogromne rzęsy i skierował go na komin piekarni.

**Pełne Alibi:**

- `F1` O 21:12 grupa otrzymała niebieskie ochraniacze na buty.
- `F2 H` Astronom niósł termos w kształcie rakiety.
- `F3 H` Pierwszą obserwowaną gwiazdą była Wega, wskazana zielonym laserem.
- `F4` Projektor zgasł po komunikacie o błędzie numer 17.
- `F5 H` Pani Marta znalazła srebrny guzik pod obrotowym krzesłem.
- `F6` W kopule pachniało świeżymi drożdżówkami z pobliskiej piekarni.
- `F7` O 21:46 wszyscy podpisali księgę gości przy wyjściu.
- `F8` Chwilę później cała grupa odjechała razem nocnym busem spod obserwatorium.

**Tropy:**

| Fakt | Nośnik i widoczna treść | Wniosek | Zdobycie i Incydent |
|---|---|---|---|
| `F2` | Fotografia rzeczy znalezionej: metalowy pojemnik z płetwami i stożkową nakrętką, bez skali i opisu | Ktoś miał pojemnik przypominający rakietę; nie wiadomo, kto go niósł | Znajdź sygnaturę w rejestrze → wyjmij zdjęcie z teczki; brak fotografii, **Cichy** |
| `F3` | Program obserwacji: pierwsze pole zawiera literę `W` i zielony ślad wskaźnika, reszta nazwy jest zalana | Pierwszy obiekt zaczynał się na „W” i wskazywano go zielonym światłem | Odszukaj kopertę według daty → wyjmij program z depozytu; zerwana plomba, **Cichy** |
| `F5` | Woreczek z pojedynczym srebrnym guzikiem i szkicem okrągłej podstawy mebla | Srebrny guzik znaleziono przy czymś obrotowym/okrągłym, ale bez nazwiska znalazcy | Zabierz klucz → otwórz przegródkę drobnych dowodów; pusty hak, **Cichy** |

**Wspólne akcje:** fotografia i woreczek mogą być Tropem, pamiątką Niewinnego albo przedmiotem do Wrobienia; klucz i depozyt pokrywają kilka Celów oraz przygotowanie Ucieczki.

**Do review:** skojarzenie rakiety z obserwatorium jest łatwe; Trop `F2` powinien pomagać odtworzyć detal termosu, nie samo miejsce.

### C04 — Noc w Muzeum Osobliwości

**Źródło:** istniejący `Case_Muzeum.asset`; siedem bazowych faktów zachowanych, `F8` jest propozycją.

**Przestępstwo:** Ktoś zamienił woskową figurę burmistrza na gigantyczny ser.

**Pełne Alibi:**

- `F1` O 20:10 grupa weszła do muzeum bocznym wejściem przy szatni.
- `F2 H` Przewodniczka miała czerwony parasol, mimo że nie padało.
- `F3 H` W sali zegarów rozległ się alarm dokładnie o 20:24.
- `F4` Pan Leon upuścił bilet obok gabloty z mechaniczną kaczką.
- `F5 H` Grupa zrobiła zdjęcie przy szkielecie wieloryba w papierowej koronie.
- `F6` Kustosz poczęstował wszystkich miętowymi cukierkami.
- `F7` O 20:41 wszyscy wyszli razem przez sklep z pamiątkami.
- `F8` Po wyjściu wspólnie wsiedli do autobusu stojącego naprzeciw muzeum.

**Tropy:**

| Fakt | Nośnik i widoczna treść | Wniosek | Zdobycie i Incydent |
|---|---|---|---|
| `F2` | Kwit szatni z nadrukiem czerwonego parasola i suchą pieczątką pogodową; brak nazwiska | W szatni pozostawiono czerwony parasol w dzień bez deszczu | Znajdź numer kwitu → otwórz kopertę rzeczy znalezionych; naruszona koperta, **Cichy** |
| `F3` | Papierowy log alarmu: `20:24`, ikona dzwonka i częściowo nieczytelny kod pomieszczenia | Alarm uruchomił się o konkretnej godzinie, ale nośnik nie mówi wprost, że chodziło o salę zegarów | Użyj klucza z listwy → wyjmij log z szafy; pusty hak, **Cichy** |
| `F5` | Negatyw: fragment żeber wielkiego szkieletu, papierowy ząb korony i kilka sylwetek bez twarzy | Grupa pozowała przy dużym szkielecie z papierową koroną, ale nie wiadomo gdzie | Zadzwoń do laboranta → otwórz kasetkę podanym kodem; mechaniczny dzwonek, **Hałaśliwy** |

**Wspólne akcje:** parasol/kwit, log i negatyw mogą być również prywatnymi dokumentami Niewinnego, materiałem Wrobienia albo blefem; telefon i kasetka pokrywają `OS-05`, `OS-13`, `OS-15` i `WR-03`.

**Do review:** `F8` wymaga dopisania. Negatyw nie może pokazywać kompletnej sceny ani liczby graczy, bo byłby zbyt bezpośredni.

### C05 — Poranek Stu Gumowych Kaczek

**Przestępstwo:** Winny wrzucił sto gumowych kaczek do miejskiej fontanny, a największej założył policyjną czapkę i szarfę „Komendant Stawu”.

**Pełne Alibi:**

- `F1` O 05:40 grupa przyniosła znalezionego żółwia do całodobowej lecznicy.
- `F2 H` Recepcjonistka dała im kartę kolejki z rysunkiem lisa.
- `F3` Żółwia zważono w niebieskiej misce sałatkowej.
- `F4 H` Radio w poczekalni ostrzegło przed gęstą poranną mgłą.
- `F5` Mały terier kichnął i przewrócił stojak na parasole.
- `F6 H` Weterynarz narysował na skorupie zmywalną białą gwiazdę.
- `F7` Wyszli drzwiami apteki, bo myto matę przy głównym wejściu.
- `F8` O 06:05 razem wsiedli do pierwszego autobusu numer 3.

**Tropy:**

| Fakt | Nośnik i widoczna treść | Wniosek | Zdobycie i Incydent |
|---|---|---|---|
| `F2` | Karta kolejki: lis, numer 12 i odcisk łapy; nazwa placówki jest oderwana | Ktoś czekał w numerowanej kolejce w miejscu związanym ze zwierzętami | Odczytaj numer koperty → wyjmij kartę z depozytu; brak banderoli, **Cichy** |
| `F4` | Wydruk z godz. `05:47`, trzy symbole mgły i „widoczność bardzo mała”, bez miejsca odbioru | O tej porze nadawano ostrzeżenie o gęstej mgle | Znajdź numer serwisu → odtwórz komunikat telefonem; słyszalna zapowiedź, **Hałaśliwy** |
| `F6` | Gaza z fragmentem białego, zmywalnego znaku i wzorem przypominającym łuski | Małe zwierzę oznaczono tymczasowym białym symbolem; nie wiadomo jakim w całości | Znajdź kod dowodu → otwórz szafkę kluczem; brak klucza/otwarte drzwiczki, **Cichy** |

**Wspólne akcje:** karta z lisem może być Tropem, pamiątką Niewinnego albo podrzuconym symbolem; telefon, koperta i klucz pokrywają wspólne moduły Celów.

**Do review:** liczba zwierzęcych detali może przeciążać pamięć. Gwiazda na gazie nie może układać się w kompletny obraz żółwia.

### C06 — Kogut w kajdankach

**Przestępstwo:** Winny ukradł pozłacanego koguta z szyldu hali drobiowej, skuł go kajdankami z miejską fontanną i odczytał mu prawa zatrzymanego.

**Pełne Alibi:**

- `F1` Podczas ulewy grupa weszła do pralni samoobsługowej „Bąbel”.
- `F2` Włożyli mokre płaszcze do kosza ze złamaną rączką.
- `F3 H` W pustej pralce znaleźli jedną czerwoną dziecięcą skarpetkę.
- `F4` Obsługująca rozmieniła banknot na mosiężne żetony.
- `F5 H` Automat wydał dwa kakao po jednym wyborze.
- `F6` Czekając, grali kartami znalezionymi na półce z czasopismami.
- `F7 H` Suszarka zapiszczała trzy razy, lecz pranie zostało wilgotne.
- `F8` Wyszli razem pod jednym zielonym parasolem reklamowym.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F3` | Fotografia czerwonej prążkowanej tkaniny przy okrągłym stalowym otworze; sugeruje małą część garderoby znalezioną w pralce | Sygnatura z kartoteki → fotografia z teczki; pusta koszulka, **Cichy** |
| `F5` | Reklamacja z jednym symbolem monety i dwiema obręczami po kubkach; sugeruje podwójne wydanie napoju | Klucz z listwy → koperta w szafie; brak klucza, **Cichy** |
| `F7` | Woreczek z wilgotnym filtrem kłaczków i trzema stemplami kontroli; sugeruje powtarzane zakończenie bez wysuszenia | Numer półki z protokołu → zerwanie plomby woreczka; dzwonek depozytu, **Hałaśliwy** |

**Wspólne akcje:** archiwum, klucz, koperta i zaplombowany woreczek mogą służyć Tropom, odzyskaniu prywatnej rzeczy, Wrobieniu albo blefowi.

**Do review:** F7 może być za łatwy, jeśli nośnik jednocześnie jasno pokazuje trzy sygnały i wilgoć.

### C07 — Koza Jej Wysokość

**Przestępstwo:** Winny użył skradzionej miejskiej pieczęci, by podczas transmisji z rynku koronować kozę na tymczasową burmistrzynię.

**Pełne Alibi:**

- `F1` Grupa weszła na ostatnią godzinę do kręgielni „Meteor”.
- `F2 H` Dostali biało-czerwone buty z niedobranymi sznurówkami.
- `F3` Przydzielono im tor szósty pod nazwą „Borsuki”.
- `F4` Pierwsza kula przewróciła tylko jeden kręgiel.
- `F5 H` Gdy podajnik stanął, zamówili miskę kiszonych ogórków.
- `F6` Pracownik ręcznie wyciągnął zieloną kulę.
- `F7 H` Tablica wyników zamarła na liczbie 77.
- `F8` Wyszli, kiedy zgasły kolorowe światła nad torami.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Kartonik zwrotu obuwia z końcówkami białej i czerwonej sznurówki; sugeruje niedopasowaną parę | Kwit depozytowy → kieszeń skonfiskowanej kurtki; pusty numer na wieszaku, **Cichy** |
| `F5` | Podstawka z zielonym słonym zaciekiem, rysunkiem kuli i pieczątką obsługi podajnika; sugeruje przekąskę w zalewie podczas awarii | Odczytaj sygnaturę podstawki z rejestru → otwórz przypisane pudełko drobnych dowodów; brak nośnika, **Cichy** |
| `F7` | Zdjęcie dwóch czerwonych siódemek i odbicia numeru toru bez graczy; sugeruje zablokowany wynik | Telefon z kodem laboranta → szuflada fotograficzna; otwarta szuflada, brak zdjęcia i mechaniczny alarm, **Hałaśliwy** |

**Wspólne akcje:** odbiór kurtki, karta dostępu i telefon mogą obsługiwać odzyskanie rzeczy Niewinnego, Trop, krok Ucieczki albo blef.

**Do review:** tor 6, wynik 77 i „Borsuki” mogą nadmiernie obciążać pamięć; kolor nie może być jedynym rozróżnieniem butów.

### C08 — Galaretobus

**Przestępstwo:** Winny napełnił autobus miejski zieloną galaretą, zatopił w niej ogromną marchew i wystawił pojazd przed ratuszem jako „największą zimną nóżkę świata”.

**Pełne Alibi:**

- `F1` Grupa przyszła na ostatnie zajęcia do pracowni ceramicznej.
- `F2` Instruktorka rozdała im szare fartuchy.
- `F3 H` Każdy ulepił z białej gliny rybę.
- `F4` Podczas lepienia radio nadawało prognozę pogody.
- `F5 H` Niebieskie szkliwo rozlało się obok stołu.
- `F6` Wytarli je dużą żółtą gąbką.
- `F7 H` Instruktorka ustawiła prace na drugiej półce pieca.
- `F8` Przed wyjściem podpisali papierowe etykiety inicjałami.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F3` | Kartka szkiców z podobnymi sylwetkami mającymi płetwy i odciskami białej gliny; sugeruje wspólny motyw ryby | Skorowidz pracowni → teczka zajęć; brak kartki, **Cichy** |
| `F5` | Nakładka z odciskiem podeszwy przeciętym kobaltową smugą; sugeruje rozlane niebieskie szkliwo | Klucz z zaplombowanego piórnika → płaska szuflada; brzęczyk plomby, **Hałaśliwy** |
| `F7` | Karta załadunku z glinianymi łuskami na drugim poziomie; sugeruje miejsce prac, nie moment ustawiania | Separator z kancelarii → karta w segregatorze; brak separatora i karty, **Cichy** |

**Wspólne akcje:** kartka, piórnik, klucz i dokument z segregatora pasują do usuwania prywatnego szkicu, odzyskiwania rzeczy, Wrobienia albo blefu.

**Do review:** trzeba jednoznacznie ustalić liczenie półek od dołu; `F6` nie może automatycznie zdradzać całego `F5`.

### C09 — Hejnał z kaczką

**Przestępstwo:** Winny podmienił nagranie ratuszowego hejnału, przez co w samo południe z wieży przez minutę rozlegało się uroczyste kwakanie.

**Pełne Alibi:**

- `F1` Po odwołanym seansie grupa weszła do baru karaoke „Pod Fałszem”.
- `F2` Wylosowali prywatny pokój numer trzy.
- `F3 H` Zaczęli od duetu o białych różach.
- `F4` Kelner przyniósł dzban lemoniady na metalowej tacy.
- `F5 H` Mikrofon zamilkł przed ostatnim refrenem.
- `F6` Dokończyli piosenkę z tamburynem ze ściany.
- `F7 H` Potem zrobili wspólne zdjęcia w budce.
- `F8` Wyszli z niebieskimi stemplami na dłoniach.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F3` | Naderwany formularz z dwiema rubrykami wykonawców, dwoma różami i literą `B`; sugeruje duet o białych różach | Numer pieczęci baru → koszulka w aktach; brak formularza, **Cichy** |
| `F5` | Blister po bateriach z wykresem dźwięku urwanym przed trzecim refrenem; sugeruje awarię mikrofonu przed końcem | Brelok depozytowy → szafka elektroniki; otwarta szafka, **Cichy** |
| `F7` | Tył paska zdjęć z kilkoma cieniami głów i kolejnymi numerami klatek, bez twarzy | Telefon do laboranta → kasetka z negatywami; dzwonek, **Hałaśliwy** |

**Wspólne akcje:** akta, brelok, telefon i fotografie mają alternatywne użycia w `OS-03`, `OS-05`, `OS-13`, `OS-15`, Wrobieniu i blefie.

**Do review:** nie opierać faktu na realnej, licencjonowanej piosence; liczba sylwetek na zdjęciu nie może zdradzać liczby graczy.

### C10 — Pieróg odpływa

**Przestępstwo:** Winny odwiązał ogromny dmuchany pieróg z festynu, założył perukę sędziego i popłynął na nim rzeką, wydając wyroki mijanym kaczkom.

**Pełne Alibi:**

- `F1` Podczas deszczu grupa pomagała wieczorem w schronisku dla zwierząt.
- `F2 H` Wpisano ich do sektora boksów oznaczonego literą C.
- `F3` Nakarmili starego beagla z niebieskiej miski.
- `F4` Czarny kot uciekł do magazynu pościeli.
- `F5 H` Próbowali wywabić go zabawką z piór.
- `F6` Czekając, składali podarowane koce.
- `F7 H` Beagle przewrócił wiadro z wodą.
- `F8` Po złapaniu kota zamknęli go w żółtym transporterze i wyszli.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Klips identyfikatora z wycięciem `C`, kreskami boksów i logo schroniska; sugeruje literowy sektor | Numer woreczka z listy → wydanie rzeczy kwitem; brak woreczka, **Cichy** |
| `F5` | Zabawka z połamanymi piórami, czarnymi włosami i śladami ciągnięcia; sugeruje wabienie czarnego kota | Karta przegródki → szuflada dowodów; otwarta przegródka, **Cichy** |
| `F7` | Zdjęcie mokrych psich łap, obrysu wiadra i rozlanej wody; sugeruje przesunięcie pojemnika przez psa | Odczytaj numer fotografii z logu materiałów → wyjmij właściwą odbitkę z płaskiej szuflady; brak zdjęcia, otwarta szuflada i dzwonek, **Hałaśliwy** |

**Wspólne akcje:** woreczek, przegródka i podmiana fotografii mogą być Tropem, odzyskaniem pamiątki, Wrobieniem albo blefem.

**Do review:** `F4` mocno podpowiada `F5`; sprawdzić, czy ukrycie samego `F5` nadal tworzy sensowną lukę.

### C11 — Wirująca przykrywka

**Przestępstwo:** Winny wypuścił podczas posiedzenia rady miasta trzysta nakręcanych kaczek, które zagłuszyły głosowanie nad budżetem.

**Pełne Alibi:**

- `F1` O 20:05 grupa weszła na nocną giełdę kwiatową „Irys”.
- `F2 H` Pracownica dała im fioletową tabliczkę licytacyjną numer 6.
- `F3` Wszyscy oglądali lilie pachnące cytryną.
- `F4 H` Kurier w żółtym płaszczu przywiózł skrzynię oznaczoną numerem 27.
- `F5` Automat kasowy połknął monetę starszego klienta.
- `F6 H` Z radia leciało tango, gdy z wózka spadła czerwona rękawiczka.
- `F7` Grupa złożyła trzy niebieskie arkusze papieru do pakowania.
- `F8` O 20:43 wszyscy wyszli za ciężarówką odbierającą szkło z wazonów.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Fragment kartonowej tabliczki z fioletową krawędzią, logo irysa i niepełnym dwuczęściowym numerem | Grupa używała kolorowego, numerowanego znacznika licytacyjnego, ale Trop nie odtwarza pełnego koloru i numeru | Kwit depozytowy → skrytka rzeczy osobistych; zerwana plomba, **Cichy** |
| `F4` | Kalka dostawy z symbolem skrzyni i `27`, rozmazaną godziną i bez nazwiska; sugeruje oznaczoną dostawę | Karta indeksowa „Irys” → manifest w archiwum; brak karty, **Cichy** |
| `F6` | Zdjęcie radia z napisem `TANGO` i czerwonym przedmiotem rozmazanym przy wózku; łączy muzykę z upadkiem rzeczy | Klucz z listwy → koperta fotograficzna; brak klucza, **Cichy** |

**Wspólne akcje:** otwarcie skrytki, zabranie manifestu i użycie klucza pasują do Tropu, odzyskania prywatnej rzeczy, dokumentowego Wrobienia albo blefu.

**Do review:** trzy liczby w jednym Alibi mogą być za ciężkie; F2/F4 powinny zostać przetestowane w różnych kombinacjach braków.

### C12 — Zupa urzędowa

**Przestępstwo:** Winny zastąpił wszystkie tabliczki z nazwiskami radnych szkliwionymi talerzami z napisem „ZUPA DNIA”.

**Pełne Alibi:**

- `F1` O 17:20 grupa weszła na warsztaty introligatorskie przez tylny dziedziniec.
- `F2 H` Każdy dostał pomarańczowy fartuch z naszytą igłą.
- `F3` Instruktorka złamała korbę pokazowej prasy do papieru.
- `F4 H` Grupa wybrała kobaltowe płótno na okładkę wspólnego albumu.
- `F5 H` Czarny kot przeszedł po stole i zostawił ślady łap w kleju.
- `F6` Podczas przerwy wszyscy jedli słone precle.
- `F7` Cztery gotowe notesy ustawiono na lewej półce.
- `F8` O 18:02 grupa wyszła razem na tramwaj numer 4.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Pomarańczowy fartuch z naszywką igły, klejem i numerem garderoby; sugeruje odzież warsztatową | Klucz z kieszeni płaszcza → klatka depozytowa; pusty hak, **Cichy** |
| `F4` | Próbka kobaltowego płótna z kodem `K-7` i odciskiem prostokątnej okładki; sugeruje materiał albumu | Kod z katalogu → podmiana karty wypożyczenia i wyjęcie próbki; fałszywa karta, **Cichy** |
| `F5` | Zbliżenie kilku małych odcisków w przezroczystym kleju i pojedynczego czarnego włosa; bez całego kota, stołu i notesów | Małe czarne zwierzę przeszło przez świeży klej, ale miejsce i pełny przebieg trzeba odtworzyć z zeznań | Numer telefonu z pokwitowania → właściwa szafka depozytowa; brak telefonu i otwarta szafka, **Cichy** |

**Wspólne akcje:** fartuch, próbka materiału, telefon i podmiana formularza pasują do Prywatnych Celów, Wrobienia, Tropu i blefu.

**Do review:** mocny zestaw koloru i naszywki może uczynić `F2` za łatwym; kod telefonu nie może być arbitralnie zapisany na dowodzie.

### C13 — Rejs bez biletu ulgowego

**Przestępstwo:** Winny wprowadził furgonetkę z lodami do sali obrad i przez godzinę odtwarzał z niej hymn miasta wspak.

**Pełne Alibi:**

- `F1` O 18:05 grupa przeszła przez bramkę przystani.
- `F2` Usiedli na dolnym pokładzie obok pomarańczowego koła ratunkowego.
- `F3 H` Naprzeciwko siedziała kobieta z dużym słonecznikiem.
- `F4 H` Przy czerwonej boi prom zatrąbił dwa razy.
- `F5` Wszyscy pili herbatę z papierowych kubków z kotwicą.
- `F6 H` Akordeonista zagrał melodię o dziewczynie idącej przez las.
- `F7` Marynarz upuścił niebieską linę przy trapie.
- `F8` O 18:38 grupa zeszła razem przy targu rybnym.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F3` | Notes z zasuszonym płatkiem słonecznika, fragmentem biletu i szkicem przeciwległych ławek; sugeruje osobę z dużym kwiatem | Bilet z kieszeni torby → pudełko rzeczy osobistych; otwarta torba, **Cichy** |
| `F4` | Karta trasy z czerwoną boją i dwoma wgłębieniami przy symbolu rogu; sugeruje powtórzony sygnał | Telefon z sygnaturą rejsu → segregator żeglugi; słuchawka poza widełkami, **Cichy** |
| `F6` | Kartka nutowa bez tytułu, z ilustracją dziewczyny w lesie i dopiskiem „dolny pokład”; sugeruje temat melodii i instrument | Klucz z biurka → szafa dowodowa; dzwonek depozytu, **Hałaśliwy** |

**Wspólne akcje:** telefon, klucz, notes i nuty mogą realizować prywatny kontakt, odzyskanie pamiątki, Trop, Wrobienie albo blef.

**Do review:** Trop melodii nie może wymagać znajomości konkretnej piosenki; dwa wgłębienia muszą być czytelne jako dwa sygnały.

### C14 — Smok za kulisami

**Przestępstwo:** Winny podmienił młotek sędziego na gumowego kurczaka, który zapiszczał podczas publicznego ogłaszania wyroku.

**Pełne Alibi:**

- `F1` O 19:15 grupa weszła do teatru lalek wejściem dla aktorów.
- `F2 H` Bileter odbił każdemu na prawej dłoni zielony półksiężyc.
- `F3 H` Sceniczny smok nie miał lewego filcowego zęba.
- `F4` Próba rozpoczęła się od trzech uderzeń w bęben.
- `F5` Gołąb usiadł na balkonie i wszyscy na chwilę zamilkli.
- `F6 H` W przerwie podano herbatę jabłkową w metalowych kubkach.
- `F7` Grupa pomogła zwinąć czerwoną kurtynę.
- `F8` O 19:52 wszyscy wyszli razem przez główne foyer.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Zielona bibuła z rozmazanymi półksiężycami i odciskami dłoni bez strony; sugeruje znakowanie gości | Kluczyk spod tacki → szuflada biletów; brak kluczyka, **Cichy** |
| `F3` | Karta napraw ze zdjęciem asymetrycznej szczęki smoka, pustą kieszenią i próbką filcu; sugeruje brak miękkiego elementu po jednej stronie | Klucz archiwisty → płaska szuflada; pusty hak, **Cichy** |
| `F6` | Kupon z jabłkiem, metalowym kubkiem i pieczątką „przerwa”, bez nazwy napoju; sugeruje jabłkowy gorący napój | Dopasuj numer kuponu do kwitu depozytowego → odbierz właściwy portfel i przeszukaj kieszeń; pusty slot depozytu, **Cichy** |

**Wspólne akcje:** klucze, dokumentacja rekwizytu, portfel i podmiana formularza pasują do Tropu, odzyskiwania rzeczy, Wrobienia albo blefu.

**Do review:** `F2` ma trzy szczegóły naraz; karta smoka nie może wprost wskazywać „lewego zęba”.

### C15 — Dożynki pod podejrzeniem

**Przestępstwo:** Winny ustawił na murawie stadionu czterdzieści ogrodowych krasnali i rozegrał nimi oficjalny rzut wolny przed pełnymi trybunami.

**Pełne Alibi:**

- `F1` O 16:40 grupa weszła na konkurs działkowy przez bramę z dyni.
- `F2 H` Organizator przydzielił im czerwoną taczkę numer 12.
- `F3` Wspólna dynia ważyła dokładnie czternaście kilogramów.
- `F4 H` Koza odgryzła róg karty z wynikami.
- `F5` Sędzia przywiązał niebieską wstążkę do najdłuższego pora.
- `F6 H` Nagły deszcz zagonił wszystkich pod pasiastą altanę.
- `F7` Grupa piła lemoniadę koperkową ze słoików.
- `F8` O 17:25 wszyscy wyszli zachodnią bramą za zielonym traktorem.

| Fakt | Nośnik i wniosek | Zdobycie i Incydent |
|---|---|---|
| `F2` | Plakietka z czerwonymi otarciami, symbolem jednego koła i niepełnym numerem `1_` | Używano czerwonego, ręcznie prowadzonego sprzętu z dwucyfrowym numerem, ale nie wiadomo jeszcze, że była to taczka numer 12 | Mały klucz z pęku → kasetka drobiazgów; brak klucza, **Cichy** |
| `F4` | Fragment karty z półkolistymi śladami zębów, białą sierścią i urwanym wynikiem; sugeruje nadgryzienie przez zwierzę gospodarskie | Skorowidz konkursu → arkusz w teczce; otwarta teczka, **Cichy** |
| `F6` | Zdjęcie kropli, pasiastych elementów dachu i mokrych sylwetek; sugeruje schronienie pod altaną | Żeton wydania → telefon w szafce; otwarta szafka, brak telefonu i alarm depozytu, **Hałaśliwy** |

**Wspólne akcje:** plakietka, karta ocen i telefon mogą służyć Tropowi, pamiątce Niewinnego, dokumentowemu Wrobieniu, przygotowaniu Ucieczki albo blefowi.

**Do review:** liczby 12 i 14 mogą się mieszać; fotografia nie może pokazywać całej altany ani oznaczeń konkursu.

## Proponowana kolejność oceny i wdrożenia

1. **C01 Różowy pomnik** — pierwszy tracer, bo ma istniejący asset i zachowuje ważny paragon.
2. **C04 Muzeum** — test zdjęcia/negatywu oraz logu alarmu.
3. **C09 Hejnał z kaczką** — test kasety/telefonu, fotografii i lokalnego Hałaśliwego Incydentu.
4. **C13 Rejs** — test bardziej pośrednich Tropów i różnic wiedzy kulturowej.
5. Dopiero po tych czterech: ocena pozostałych kandydatów i przenoszenie kolejnych do assetów.

Nie należy implementować wszystkich 15 przed pierwszym playtestem. Celem katalogu jest ustalenie kontraktu i zachowanie pomysłów; pierwszy kod powinien otrzymać jedną kompletną Sprawę, cztery Osobiste Sprawy, jedno Wrobienie, jeden Trop end-to-end oraz placeholdery dwóch wyjść.
