# Rdzeń rozgrywki i regrywalność `Interrogation Room`

**Data:** 2026-08-21
**Status:** analiza badawcza i propozycje eksperymentów; dokument nie zmienia zatwierdzonych reguł produktu

## Najważniejsza odpowiedź

`Interrogation Room` nie jest nudne dlatego, że ma za mało przypadków, zadań albo minigier. Obecna pętla nie zamienia działań graczy we wspólną, zmieniającą się historię, na podstawie której można sensownie podejrzewać innych.

W praktyce działają obok siebie dwie słabo połączone gry:

1. krótki test informacji z przeszłości: kto zna pełne `Alibi`, a kto próbuje ukryć dwie luki;
2. swobodna gra przestrzenna: każdy realizuje prywatny łańcuch czynności, `Winny` zbiera `Tropy do Alibi` lub przygotowuje `Ucieczkę`, a czasem pojawia się `Incydent`.

Obie części spotykają się głównie w rozmowie i przy końcowej `Egzekucji`. Większość czynności pomiędzy nimi nie tworzy jednak nowej informacji, która zmienia hipotezę `Detektywa`. Dlatego gracze poprawnie reagują na system: chodzą wykonywać własne cele, `Detektyw` ustawia kolejkę indywidualnych przesłuchań, a najbardziej pamiętnym wydarzeniem pozostaje strzał.

Najbardziej obiecująca zmiana to nie „więcej rzeczy do roboty”, lecz **jeden wspólny, żywy stan sprawy**, wokół którego działają wszystkie role:

- `Niewinny` ma prywatny interes dotyczący tych samych osób, przedmiotów i miejsc;
- `Winny` musi aktywnie manipulować tym samym stanem, żeby się obronić lub przygotować `Ucieczkę`;
- `Detektyw` wybiera, które ślady ujawnić, zabezpieczyć albo skonfrontować;
- świat regularnie pokazuje skutki działań, lecz żaden pojedynczy ślad nie dowodzi roli;
- rozmowy wynikają z konkretnych zdarzeń: „kto miał dostęp?”, „dlaczego rejestr się zmienił?”, „kto widział przeniesienie dowodu?”, a nie z ogólnego „co robiłeś?”.

Rekomenduję najpierw zbudować ten kierunek jako papierowy lub grey-boxowy test jednej `Rundy`, bez nowej grafiki i bez produkowania kolejnych przypadków. Jeśli nie ożywi gry, należy rozważyć radykalne skrócenie pętli `Alibi` zamiast dalszego rozciągania jej contentem.

## Zakres i ograniczenia badania

Analiza łączy:

- relację z testów 4–6-osobowych: działający prototyp, niejasność części zadań, zapominanie `Alibi`, brak wiedzy, o co pytać, seryjne przesłuchania, błądzenie, losowy charakter części finałów i brak spontanicznej chęci rozegrania następnej `Rundy`;
- zatwierdzony model domeny z [`CONTEXT.md`](../../CONTEXT.md), architekturę pionowego wycinka z [`MVP-ARCHITECTURE.md`](../architecture/MVP-ARCHITECTURE.md), reguły `Prywatnych Celów`, `Incydentów` i `Ucieczki` z [dokumentu mechaniki](../design/mechanics/prywatne-cele-incydenty-i-ucieczka.md) oraz istotne ADR-y;
- istniejący zakres contentu opisany w [`FABLE-PLAYTEST-IMPROVEMENTS.md`](../design/FABLE-PLAYTEST-IMPROVEMENTS.md) i [`PLAYTEST-CONTENT-CATALOG.md`](../design/PLAYTEST-CONTENT-CATALOG.md);
- pierwszorzędne źródła: zasady gier, wypowiedzi projektantów i postmortemy; badania akademickie służą jako wsparcie, nie jako dowód, że konkretna zmiana na pewno zadziała w tym projekcie.

To nie jest audyt pełnej sesji wideo ani analiza telemetrii. Dokładne tempo, liczby oraz progi sukcesu poniżej są hipotezami testowymi. Najważniejszym dowodem pozostanie porównywalny playtest na tej samej grupie.

## Dlaczego obecna pętla jest płaska

### 1. System sam produkuje nudną kolejkę przesłuchań

Każdy `Niewinny` zna to samo pełne `Alibi`, a `Winny` otrzymuje tę samą historię z dwiema ukrytymi informacjami. `Detektyw` nie zna `Alibi` i ma je odtworzyć z zeznań ([ADR-0006](../adr/0006-guilty-receives-redacted-alibi.md), [ADR-0008](../adr/0008-detective-reconstructs-alibi-from-testimony.md)).

To tworzy niezamierzony wyciek informacji:

1. przed rozmową brakujący fakt odróżnia `Winnego` od `Niewinnych`;
2. gdy `Niewinny` poda go publicznie, `Winny` właśnie nauczył się poprawnej odpowiedzi;
3. wartość diagnostyczna kolejnych rozmów maleje;
4. racjonalną strategią `Detektywa` staje się rozdzielenie graczy i zebranie niezależnych zeznań.

Zaobserwowane „branie po kolei na przesłuchanie” nie jest więc wyłącznie problemem prowadzącego. Jest strategią wymuszaną przez ekonomię informacji. Przy pięciu graczach zwykle aktywnie uczestniczą wtedy dwie osoby, a pozostali wykonują prywatne czynności lub czekają. Lepsza lista pytań może usprawnić rozmowę, lecz nie usunie przepustowościowego wąskiego gardła.

Istnieje też drugi koszt. `Niewinni` posiadają dużo informacji redundantnej, ale mało informacji unikalnej. Rozmowy porównują pamięć tej samej historii, zamiast składać różne perspektywy bieżących wydarzeń. Pomyłka pamięciowa tworzy szum, lecz sama w sobie rzadko tworzy interesującą decyzję.

### 2. `Alibi` jest jednorazowym testem, a `Runda` jest dłuższym sandboxem

Formalna struktura `Alibi` jest bliska grom typu *The Chameleon*: prawie wszyscy znają sekretną informację, jedna osoba jej nie zna i próbuje blefować. Oficjalny opis *The Chameleon* zamyka tę próbę w około 15 minutach dla całej partii, przy czym każdy daje jedną wskazówkę, po czym następuje bezpośrednia debata i głosowanie ([Big Potato Games](https://bigpotato.com/products/the-chameleon)).

To porównanie nie oznacza, że należy kopiować tę grę. Pokazuje problem skali: pojedyncza asymetria „wszyscy wiedzą, jedna osoba ma lukę” naturalnie zasila krótki test werbalny. W `Interrogation Room` ma utrzymać swobodną `Rundę` z ruchem, prywatnością przestrzenną i aktywnościami. Jeżeli w jej trakcie nie powstają kolejne różne, przecinające się informacje, początkowy test zostaje rozciągnięty, a nie pogłębiony.

### 3. Prywatne cele zajmują czas, ale nie budują wspólnego problemu

Katalog OS-01–OS-15 składa się głównie z prywatnych, dwuetapowych łańcuchów: znajdź/przenieś/użyj/zamień/usuń przedmiot, często zakończonych `Cichym Incydentem`. Te czynności mogą być poprawnie zaimplementowane, zabawne fizycznie i dobrze animowane, a mimo to pozostawać fillerem, jeśli ich wynik nie zmienia decyzji innych osób.

Obowiązująca zasada, że widoczna czynność powinna mieć co najmniej dwa możliwe motywy, daje wiarygodne zaprzeczenie ([ADR-0014](../adr/0014-readable-actions-ambiguous-motives.md)). Sama liczba motywów nie gwarantuje jednak sygnału diagnostycznego. Jeśli dana czynność jest równie prawdopodobna u `Winnego` i `Niewinnego`, obserwacja nie zmienia podejrzeń. Gracz może wyglądać „podejrzanie”, ale nikt nie ma powodu zaktualizować hipotezy.

Inaczej mówiąc: **niejasność jest potrzebna, lecz czysta niejasność nie jest dedukcją**. Potrzebna jest niejasność ustrukturyzowana: pojedynczy ślad pasuje do kilku osób i motywów, natomiast przecięcie miejsca, czasu, dostępu, narzędzia i zeznania stopniowo zawęża pole.

### 4. `Incydent` może tworzyć klimat bez tworzenia decyzji

Obecny model celowo nie pokazuje w rejestrze autora, motywu ani prawdziwego czasu wykonania. `Cichy Incydent` wymaga dodatkowo osobistego odkrycia przez `Detektywa`. Chroni to sekret gracza, lecz może pozbawić zdarzenie wartości użytkowej: widoczny jest skutek, ale brakuje uchwytu pozwalającego wybrać następną osobę, miejsce albo pytanie.

Zdarzenie staje się paliwem społecznym dopiero wtedy, gdy jednocześnie:

- zmienia wspólny stan lub stawkę;
- różnym osobom daje różne części informacji;
- wskazuje sensowną następną decyzję;
- pozostawia przynajmniej dwie wiarygodne interpretacje.

Losowe zdarzenie bez autora, kosztu, świadka i dalszego skutku może przerwać ciszę, ale nie stworzy pętli.

### 5. `Winny` może grać obok śledztwa zamiast przeciwko niemu

Najsilniejszy antagonista społeczny nie tylko ukrywa swoją rolę. Zmusza innych do reagowania i zostawia ślady przy realizacji własnego planu. W obecnym modelu `Tropy do Alibi`, przygotowanie `Ucieczki` i prywatne czynności `Niewinnych` mogą przebiegać równolegle. Jeżeli `Winny` nie musi zmieniać stanu, na którym zależy pozostałym, nie ma stałego silnika konfliktu.

Skutkiem jest gra reaktywna: `Winny` czeka, aż zostanie zapytany, a jego najważniejszą umiejętnością jest zapamiętanie lub podsłuchanie poprawnej wersji. To znacznie słabsza fantazja niż aktywne manipulowanie sytuacją pod presją świadków.

### 6. `Detektyw` ma odpowiedzialność za całą zabawę, ale mało czasowników systemowych

`Detektyw` ma wybrać pytania, zapamiętać sprzeczności, organizować przestrzeń, pilnować czasu i podjąć jedyną nieodwracalną decyzję. Jego podstawowym narzędziem jest jednak rozmowa. Gdy system nie wytwarza dobrych punktów zaczepienia, prowadzący musi sam tworzyć dramaturgię.

Przykładowe pytania są wartościowym onboardingiem, lecz prompt „zapytaj, gdzie byłeś” nie tworzy informacji, której nie ma w stanie gry. `Detektyw` potrzebuje działań w świecie, które wyrażają hipotezę: zabezpiecz ten przedmiot, porównaj te dwa wpisy, ujawnij czas zdarzenia, zamknij dostęp do miejsca, skonfrontuj dwie wersje. Wybór powinien kosztować, żeby nie dało się odsłonić wszystkiego.

### 7. Jedyna mocna konsekwencja pojawia się na końcu

`Egzekucja` działa emocjonalnie, bo jest czytelna, publiczna, nieodwracalna i natychmiast zmienia wynik. Środek `Rundy` ma mniej konsekwencji tego rodzaju. Gracze mogą wykonać cel, ale pozostali często nie muszą zmienić planu. Napięcie nie rośnie; końcowy strzał próbuje stworzyć kulminację bez wcześniejszych zwrotów.

## Czego uczą właściwe porównania

### `Among Us`: zadania rozwiązują bezcelowość tylko dlatego, że są wspólne

Projektanci *Among Us* opisują, że pierwotny prototyp odziedziczył po zabawie w domu bezcelowe chodzenie. Zadania dodano, żeby dać graczom cel. Wczesna wersja z niemal ciągłym kryzysem okazała się z kolei zbyt stresująca i nie zostawiała czasu na pracę detektywistyczną oraz świadome zebrania ([wywiad Nintendo z Forestem Willardem](https://www.nintendo.com/us/whatsnew/among-us-dev-recounts-how-the-game-took-flight/)).

Oficjalne zasady pokazują pełny mechanizm, którego nie oddaje samo hasło „mamy zadania”:

- załoga ma wspólny pasek postępu i wspólne zwycięstwo;
- ruch do zadań tworzy trasy, świadków i nieobecności;
- ciało lub podejrzenie uruchamia spotkanie, w którym omawia się konkretną nową historię;
- sabotaż zmienia wspólny priorytet, rozdziela grupę i daje Impostorowi okazję;
- kamery, mapa administracyjna i obserwacja tworzą częściowe informacje;
- Impostor udaje tę samą pracę, zamyka drzwi i sabotuje ten sam system ([Innersloth, „How to Play”](https://www.innersloth.com/games/among-us/)).

Transfer do `Interrogation Room`: potrzebna jest **wspólna konsekwencja i wspólna historia ruchu**, nie kolejne minigierki. Nie należy natomiast kopiować zabójstw, głosowań ani awaryjnych zebrań; przeczyłoby to jednej `Egzekucji` i ciągłej `Rundzie`.

Warto też zachować ostrożność wobec twardych dowodów. Innersloth usunęło możliwość ustawienia animacji skanera tak, by stanowiła stuprocentowe potwierdzenie niewinności, ponieważ pewność nie pasowała do ducha gry ([oficjalny wpis deweloperski](https://store.steampowered.com/news/posts/?appids=945360&enddate=1550268429)). Dowód powinien zawężać, nie rozstrzygać.

### `Project Winter`: wspólna praca jest sceną dla zdrady

W *Project Winter* ocaleni wspólnie zbierają zasoby, naprawiają cele i przygotowują ucieczkę, a zdrajcy bezpośrednio sabotują te same cele i korzystają z narzędzi do manipulacji ([oficjalny press kit](https://www.projectwinter.co/presskit/presskit.html)). Prywatne cele istnieją, lecz są losowym, drugorzędnym źródłem kolizji wobec dwóch głównych wspólnych celów. Projektanci przyznają także, że brak nawigacji w prototypie prowadził do monotonnego, nudnego gubienia się; dodano ograniczone drogowskazy i minimapę ([wywiad z dyrektorem rozwoju](https://www.gamedeveloper.com/design/q-a-designing-for-deception-in-werewolf-inspired-i-project-winter-i-)).

Transfer: `Prywatny Cel` może powodować podejrzane odstępstwo, ale powinien odginać gracza od wspólnego problemu albo zmuszać do wykorzystania wspólnego zasobu. Gdy prywatne cele są główną treścią dla prawie wszystkich, gracze nie mają wspólnego punktu odniesienia, od którego można podejrzanie odstąpić.

### `Trouble in Terrorist Town`: informacja musi wyciekać z działania

Twórca *Trouble in Terrorist Town* opisuje rdzeń jako grę o tym, kto co wie i jak zdobywa informacje. Ciała i działania zdrajców powodują wyciek informacji; `Detective` skupia niewinną drużynę na jej zbieraniu. Zbyt skomplikowany system DNA gracze ignorowali, więc został uproszczony do bezpośredniego tropu, ale z kontrą i możliwością wiarygodnego wyjaśnienia się. Projekt wymagał też napraw exploitów tak, aby zachować „plausible deniability” ([Facepunch, rozmowa z twórcą TTT](https://gmod.facepunch.com/news/gmod-stories--trouble-in-terro)).

Transfer: informacja nie musi być realistyczna ani złożona. Musi być czytelna, prowadzić do działania i mieć kontrę. `Detektyw` powinien pomagać grupie organizować poszukiwanie informacji, a nie być jedynym procesorem rozmów.

### `Blood on the Clocktower` i `Secret Hitler`: rozmowa potrzebuje przecinających się faktów

Projektant *Blood on the Clocktower* przeciwstawia grę zgadywaniu na podstawie mowy ciała. Role dostarczają wiele częściowych, przecinających się informacji, które gracze mogą porównywać; kontrolowana dezinformacja zapobiega automatycznemu rozwiązaniu i daje złej drużynie obronę. Zdolności jawnie kierują też graczy ku współpracy: pytanie brzmi „co możemy zrobić?”, a nie tylko „kto wygląda podejrzanie?” ([Steven Medway, porównanie *Werewolf* i *Clocktower*](https://bloodontheclocktower.com/blogs/news/behind-the-curtain-4-werewolf-clocktower-how-they-are-different-how-they-are-the-same)).

W *Secret Hitler* każda runda zostawia publiczny ślad: nominację, głosy i wynik polityki, ale tylko część graczy zna prywatny przebieg decyzji i może o nim kłamać. Zasady same podpowiadają sensowną oś dochodzenia: kto podjął decyzję, dlaczego i z którego z ograniczonych źródeł mógł pochodzić podejrzany wynik ([oficjalna instrukcja](https://www.secrethitler.com/assets/Secret_Hitler_Rules.pdf)).

Transfer: dobra rozmowa nie musi być formalnie wymuszona. System powinien regularnie produkować publiczny skutek oraz nierówno rozdzieloną wiedzę o jego przyczynie. Wtedy gracze sami mają kogo i o co pytać.

### `Phasmophobia`: wszystkie narzędzia odpowiadają na jedno pytanie

W *Phasmophobia* różne role, narzędzia, ryzyko i eksploracja karmią wspólną hipotezę o rodzaju ducha. Zróżnicowane cechy duchów, przedmioty przeklęte i zadania dają kolejne drogi zdobywania informacji ([oficjalny opis Kinetic Games](https://store.steampowered.com/app/739630/Phasmophobia/)). Przebudowa wyposażenia uczyniła część pasywnego odczytu bardziej interaktywną i różnicowała jakość oraz sposób uzyskania dowodu ([oficjalny wpis o aktualizacji sprzętu](https://store.steampowered.com/news/posts/?appgroupname=Phasmophobia&appids=739630&enddate=1692549426&feed=steam_community_announcements)).

Transfer: różne aktywności są spójne, gdy wszystkie odpowiadają na to samo główne pytanie. Nie należy przenosić deterministycznej tabeli „trzy dowody = rola”, ponieważ w `Phasmophobia` nie ma ludzkiego antagonisty, który musi wiarygodnie się bronić.

### Pacing: szczyty potrzebują oddechu, ale oddech nie może być pustką

Prezentacja Valve o *Left 4 Dead* rozdziela narastanie intensywności, krótki szczyt, wygaszenie i okres relaksu; adaptacyjne tempo ma tworzyć różne przebiegi, a nie utrzymywać stały maksymalny stres ([Valve, GDC 2009](https://steamcdn-a.akamaihd.net/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf)). To nie jest argument za ciężkim „AI Directorem” w małym projekcie. Jest argumentem przeciwko obu skrajnościom: długiej pustce oraz kryzysowi wystrzeliwanemu automatycznie co minutę bez czasu na interpretację.

### Co wspierają badania akademickie

Eksperyment Depping i Mandryk rozdzielił współpracę od współzależności. Oba czynniki poprawiały doświadczenie społeczne, a efekt współzależności był wyjaśniany większą liczbą tur rozmowy ([CHI PLAY 2017, DOI](https://doi.org/10.1145/3116595.3116639)). To wspiera kierunek, w którym gracze naprawdę potrzebują działań innych, zamiast samotnie realizować checklisty.

Małe badanie jakościowe graczy *Among Us* opisuje, że sama rozmowa bez wspólnych zdarzeń szybko staje się nudna, podczas gdy wzajemnie zależna rozgrywka dostarcza konkretnego tematu i bodźca do rozmowy ([„Toward a Design Theory of Game-Mediated Social Experiences”, CHI PLAY 2021](https://vtechworks.lib.vt.edu/bitstream/handle/10919/112058/3450337.3483469.pdf)). Próbę stanowiło tylko sześciu studentów, więc jest to pomocna obserwacja mechanizmu, nie uniwersalny dowód.

## Zasady projektowe wynikające z diagnozy

### Test wartości działania

Każda ważniejsza czynność w prototypie powinna spełniać przynajmniej dwa z pięciu warunków:

1. zmienia stan ważny dla więcej niż jednej osoby;
2. daje różne części informacji przynajmniej dwóm graczom;
3. tworzy świadka, ślad dostępu lub okazję do bycia zauważonym;
4. zmienia dostępne później decyzje;
5. zużywa ograniczony zasób albo podnosi stawkę.

Jeżeli czynność tylko wypełnia pasek osobistego celu, prawdopodobnie jest fillerem bez względu na jakość minigry.

### Anatomia dobrego „beatu sprawy”

Każdy większy beat powinien zawierać:

- **publiczny skutek:** coś jest teraz inaczej i grupa może to zauważyć;
- **prywatne perspektywy:** nie wszyscy widzieli to samo;
- **ustrukturyzowaną niejednoznaczność:** co najmniej dwa wiarygodne motywy lub sprawcy, ale nie wszyscy równie prawdopodobni;
- **następną decyzję:** śledzić, zabezpieczyć, wymienić się, skonfrontować, zaryzykować użycie narzędzia;
- **kontrę:** `Winny` może zanieczyścić, opóźnić albo przekierować informację, płacąc kosztem lub ryzykiem.

Losowanie jednego `Incydentu` bez tej struktury zmienia dekorację. Beat zmienia stan gry.

### Dowody diagnostyczne, nie rozstrzygające

Obawa, że lepsza informacja „za łatwo ujawni `Winnego`”, jest zasadna tylko wtedy, gdy dowód wskazuje rolę wprost. Lepszy model rozdziela trzy poziomy:

| Poziom | Przykład | Wartość |
|---|---|---|
| kontekst | drzwi do magazynu otwarto w krótkim przedziale | zawęża miejsce i czas |
| dostęp | tylko trzy osoby mogły wtedy użyć klucza, ale klucz można było przekazać | zawęża osoby, pozostawia kontrę |
| zachowanie | jedna osoba podała wersję niezgodną ze świadkiem | zmienia wiarygodność, nie dowodzi roli |

Żaden pojedynczy ślad nie powinien rozstrzygać. Dopiero przecięcie dwóch lub trzech wymiarów buduje mocną hipotezę. Formalnie nie chodzi o to, by czynność była możliwa dla dwóch ról, lecz by obserwacja umiarkowanie zmieniała ich względne prawdopodobieństwo. `Niewinny` musi mieć prawdziwy powód do podejrzanej czynności, ale `Winny` powinien częściej potrzebować określonej kombinacji dostępu, czasu lub ingerencji.

Dobry ślad mówi „sprawdź te dwie osoby i ten przedmiot”, a nie „to na pewno `Winny`” ani „nic z tego nie wynika”.

### `Alibi` jako początkowa hipoteza, nie egzamin pamięciowy

`Alibi` może pozostać ważne, lecz powinno określać początkową wersję wydarzeń, którą bieżąca gra potwierdza, komplikuje lub podważa. Przykładowo jego fakty mogą określać:

- kto twierdzi, że miał dostęp do miejsca;
- gdzie powinien znajdować się przedmiot;
- w jakiej kolejności wykonano czynności;
- czyj bieżący interes koliduje z dawnym zeznaniem.

Samo zapamiętanie pięciu punktów nie powinno być główną umiejętnością. Eksperyment z pomocą pamięciową naruszałby obecną decyzję o ukryciu `Alibi` po `Przygotowaniu` ([ADR-0007](../adr/0007-alibi-is-hidden-after-preparation.md)), dlatego należy go oznaczyć jako test alternatywy, a nie cichą zmianę kanonu.

## Trzy spójne kierunki

### Kierunek A — podłączenie istniejących systemów

**Cel:** sprawdzić, czy fundament można ożywić bez zmiany warunków zwycięstwa i głównej struktury `Rundy`.

Zmiany testowe:

- przepisać jedną paczkę pięciu `Prywatnych Celów` tak, aby wszystkie korzystały z 2–3 wspólnych hotspotów, przedmiotów albo uprawnień;
- wykonanie celu zmienia czytelny stan wspólny, nie tylko prywatny licznik;
- `Incydent` podaje jedną użyteczną cechę: przybliżony przedział czasu, kategorię narzędzia, strefę albo listę osób z dostępem; nadal nie podaje autora i motywu;
- dać `Detektywowi` dwie lub trzy ograniczone czynności dochodzeniowe wyrażające hipotezę;
- pozostawić jedną `Egzekucję`, swobodny ruch, przestrzenny głos, warunki `Prywatnych Celów` i `Ucieczkę`.

Przykład: trzy osoby z różnych powodów potrzebują pieczęci z magazynu dowodów. `Niewinny` musi podmienić dokument dla `Osobistej Sprawy`, drugi potrzebuje świadka przy spisie, a `Winny` może zniszczyć zapis dostępu, by przygotować `Ucieczkę`. Otwarcie magazynu jest widoczne; rejestr pokazuje przedział czasu, lecz jedna czynność pozwala go przesunąć. Wspólna przestrzeń tworzy obserwacje, a motywy pozostają różne.

**Zaleta:** niski koszt i duża wartość diagnostyczna.
**Ryzyko:** jeżeli `Alibi` nadal dominuje, a `Winny` nie musi ingerować w te hotspoty, gra może pozostać zbiorem ciekawszych pobocznych czynności.

### Kierunek B — `Żywa Sprawa` (rekomendowany)

**Cel:** zachować fantazję, role i finał, ale zbudować jeden system produkujący działania, informacje i rozmowy.

Jedna testowa `Sprawa` ma 2–4 współdzielone węzły, na przykład:

- fizyczny dowód i jego łańcuch przechowywania;
- rejestr wejść lub zapis urządzenia;
- narzędzie konieczne do weryfikacji;
- zabezpieczenie wyjścia powiązane z `Ucieczką`.

Węzły mają kilka czytelnych stanów, np. niezweryfikowany, zabezpieczony, naruszony i sprzeczny. Nie jest to wspólny pasek z *Among Us*. Jest to wspólny **stan sprawy**, o którego interpretację walczą role.

- `Niewinni` realizują dokładnie jedną `Osobistą Sprawę` poprzez te same węzły. Mają prawdziwe powody, żeby coś przenieść, przemilczeć, odblokować lub zmienić.
- `Winny` musi ingerować w przynajmniej część tych samych węzłów. Każda ingerencja daje korzyść, ale zostawia miękki ślad albo wymaga ryzykownej okazji.
- `Detektyw` wybiera ograniczoną liczbę weryfikacji. Każda odsłania jeden wymiar i zamyka inne możliwości, więc nie istnieje optymalna checklista ujawniająca wszystko.
- `Tropy do Alibi` są narzędziami do budowania wiarygodnej wersji wokół bieżących śladów, a nie pobocznym zbieractwem.
- `Ucieczka` staje się publicznie narastającą konsekwencją ingerencji w tę samą sprawę, nie odrębną grą.

#### Przykładowy przebieg grey-boxowej `Rundy` pięcioosobowej

Podane czasy są profilem testowym, nie docelowym balansem.

1. **0:00–0:30 — czytelny start.** Każdy zna rolę, jeden następny krok i trzy kotwice `Alibi`. Grupa widzi, że za chwilę do magazynu trafi dowód wymagający rejestracji.
2. **0:30–2:00 — pierwszy wspólny wybór.** Dwie osoby muszą przetransportować dowód albo zostawić go bez świadka. Jeden `Niewinny` potrzebuje jego opakowania do celu. `Winny` może zamienić plombę lub poczekać.
3. **2:00–3:30 — czytelny skutek.** System ujawnia niezgodność plomby, ale nie sprawcę. Jedna osoba widziała transport, inna słyszała otwarcie drzwi, trzecia ma dostęp do rejestru. Powstają konkretne rozmowy i pierwsza zmiana podejrzeń.
4. **3:30–6:00 — decyzja `Detektywa`.** Może ujawnić przedział czasu albo listę dostępów, lecz nie oba. `Winny` otrzymuje możliwość zanieczyszczenia jednego źródła kosztem postępu `Ucieczki`; `Niewinny` może potrzebować tej samej ingerencji do `Osobistej Sprawy`.
5. **6:00–9:00 — kolizja interesów.** Drugi beat sprawy powoduje wybór: zabezpieczyć rejestr czy pilnować wyjścia. Grupa musi się podzielić. Ktoś kłamie o motywie, ktoś inny o fakcie, a świadek może negocjować ujawnienie sekretu.
6. **9:00–12:00 — zawężanie.** Przecięcie miejsca, dostępu i dwóch zeznań daje 2–3 wiarygodne hipotezy. `Winny` musi zaryzykować ostatnią ingerencję lub przyspieszyć widoczną `Ucieczkę`.
7. **12:00–14:00 — kulminacja.** Presja nie pochodzi wyłącznie z timera: nierozwiązany stan sprawy ma widoczną konsekwencję. `Detektyw` dokonuje jednej `Egzekucji` na podstawie historii, którą grupa naprawdę rozegrała.

W tej wersji rozmowa i chodzenie po mapie nie konkurują o czas. Ruch produkuje materiał do rozmowy, a rozmowa zmienia kolejne działania.

**Zaleta:** najlepiej zachowuje obietnicę „*Among Us* w mrocznej, przestrzennej atmosferze śledztwa”, lecz odróżnia grę jednym śledczym i pojedynczą konsekwencją.
**Ryzyko:** wymaga przeprojektowania contentu celów oraz jasnej prezentacji stanów. Pierwszy test powinien używać kart, napisów i istniejących interakcji, nie produkcyjnego UI.

### Kierunek C — prowokacyjny test fundamentu

Są dwie radykalne odpowiedzi na tę samą diagnozę. Nie należy ich od razu wdrażać; służą do ustalenia, co naprawdę jest wartościowym rdzeniem.

#### C1. Krótkie `Alibi`

Usunąć na jeden test wszystkie cele i zagrać 5–7-minutową wersję skoncentrowaną na niezależnych zeznaniach, jednym przestrzennym zdarzeniu oraz szybkim finale. Jeśli ta wersja jest napięta i natychmiast prosi się o rewanż, rdzeń `Alibi` działa, ale był rozciągnięty ponad swoją naturalną długość. Wtedy właściwą drogą mogą być bardzo krótkie, wielokrotnie rozgrywane sprawy.

#### C2. Przestępstwo trwa teraz

`Alibi` staje się jedynie otwarciem, natomiast tuszowanie `Przestępstwa` odbywa się na żywo. `Winny` musi zmienić dwa z trzech łańcuchów dowodowych, `Niewinni` mają kompromitujące interesy wokół tych samych łańcuchów, a `Detektyw` prowokuje działania ograniczonymi decyzjami. To najmocniejsza maszyna zdarzeń, ale kwestionuje część zatwierdzonej fantazji i ADR-ów. Może istnieć tylko jako jawnie odseparowany prototyp.

## Tempo bez długich przerw i bez sztucznego spamowania

Stały `Incydent` co 60–90 sekund byłby zbyt wolny przy obecnym odczuciu pustki, ale automatyczne zdarzenie co 20 sekund pozbawiłoby graczy czasu na rozumienie skutków. Lepsza jest hierarchia:

1. **mikrosygnały zależne od graczy** — drzwi, przeniesienie, dźwięk, zmiana stanu, świadek; pojawiają się naturalnie nawet co 10–20 sekund, bo wynikają z działań;
2. **większy beat zależny od decyzji** — naruszenie dowodu, zablokowany dostęp, konflikt zasobów; średnio kilka razy w `Rundzie`;
3. **limit ciszy** — prosty system uruchamia beat tylko wtedy, gdy przez około 35–55 sekund nie zmienił się ważny stan i nikt nie rozpoczął istotnego działania;
4. **okno interpretacji** — po większym beacie 15–30 sekund bez kolejnego automatycznego kryzysu, żeby gracze mogli reagować i rozmawiać.

Te liczby są punktem startowym. Ważniejsza jest zasada: systemowy timer zabezpiecza przed pustką, ale główne tempo pochodzi z graczy. Po mocnym wydarzeniu gra powinna opaść, lecz „relaks” nadal zawierać wybór, obserwację lub rozmowę, a nie bezcelowe chodzenie.

## Regrywalność systemowa zamiast mnożenia contentu

Nowy przypadek zmienia tekst. Systemowa regrywalność zmienia zależności i decyzje. Jedna działająca `Sprawa` powinna generować odmienne historie przez kombinację:

- kolejności aktywacji węzłów;
- przestrzennego rozkładu dostępu i świadków;
- przydziału prywatnych motywów do wspólnych przedmiotów;
- decyzji `Detektywa`, które źródło zweryfikować;
- decyzji `Winnego`, który ślad zanieczyścić, który wykorzystać i kiedy zaryzykować;
- ograniczonych narzędzi, które można przekazać, ukraść lub zużyć;
- różnych poziomów wiarygodności śladów;
- skutków wcześniejszych działań zmieniających późniejsze możliwości.

Najważniejszym mnożnikiem jest **topologia wiedzy**: w każdej `Rundzie` inne osoby widzą inne fragmenty tego samego zdarzenia i potrzebują siebie nawzajem. Dopiero wtedy ręcznie tworzone przypadki mnożą działający rdzeń.

Praktyczny test modułu contentu:

> Czy podmiana tego elementu zmieni to, kto potrzebuje kogo, kto może być świadkiem, jakie ryzyko podejmuje `Winny` lub którą hipotezę może sprawdzić `Detektyw`?

Jeśli odpowiedź brzmi „nie, zmienia tylko nazwę przedmiotu i animację”, element daje świeżość tematyczną, lecz nie regrywalność systemową.

## Ranking hipotez

Skala kosztu: niski / średni / wysoki. „Pewność” oznacza siłę diagnozy, nie gwarancję wyniku.

| # | Hipoteza | Wpływ | Pewność | Koszt | Koszt contentu | Ryzyko komplikacji | Zgodność z fantazją | Łatwość testu |
|---:|---|---|---|---|---|---|---|---|
| 1 | Wspólny stan sprawy sprawi, że ruch będzie produkował rozmowy i decyzje | bardzo wysoki | wysoka | średni | niski dla prototypu | średnie | bardzo wysoka | wysoka |
| 2 | Obowiązkowa, ryzykowna ingerencja `Winnego` w ten sam stan stworzy stały konflikt | bardzo wysoki | wysoka | średni | niski | średnie | bardzo wysoka | wysoka |
| 3 | Miękkie ślady o miejscu/czasie/dostępie umożliwią dedukcję bez ujawnienia roli | wysoki | średnio wysoka | średni | niski | średnie | wysoka | wysoka |
| 4 | Działania dochodzeniowe `Detektywa` rozproszą wąskie gardło przesłuchań | wysoki | średnio wysoka | niski–średni | niski | średnie | bardzo wysoka | wysoka |
| 5 | Powiązanie prywatnych motywów z tymi samymi zasobami zwiększy współzależność | wysoki | wysoka | średni | średni przy wdrożeniu | średnie | wysoka | wysoka |
| 6 | Limit ciszy i łuk napięcia usuną martwe odcinki bez przeciążenia graczy | średni | średnia | niski | niski | niskie | wysoka | wysoka |
| 7 | Zmniejszenie kosztu pamięciowego `Alibi` poprawi jakość pytań, ale nie naprawi samo rdzenia | średni | średnia | niski | niski | niskie | średnia; konflikt z ADR-0007 | bardzo wysoka |
| 8 | Więcej przypadków, celów i minigier o obecnej strukturze poprawi regrywalność | niski | niska | wysoki | bardzo wysoki | wysokie | powierzchownie wysoka | niska |

Hipotezy 1–5 tworzą system i powinny ostatecznie zostać sprawdzone razem, lecz pierwsze testy muszą izolować zmienne. Hipoteza 8 jest negatywną kontrolą: istniejący zakres contentu oraz relacja z testów już wskazują, że samo mnożenie tej struktury nie rozwiązuje problemu.

## Tanie eksperymenty

Każdy test powinien wykorzystywać pięciu graczy, jeśli to możliwe, i tę samą grupę w wariancie kontrolnym oraz zmienionym. Kolejność wariantów warto odwracać między sesjami, żeby efekt nowości nie udawał poprawy.

### Test 0 — naturalna długość `Alibi`

- **Jedna zmiana:** usuń prywatne cele i skróć `Rundę` do 5–7 minut; zachowaj przygotowanie, niezależne zeznania i jedną `Egzekucję`.
- **Oczekiwany efekt:** jeżeli samo `Alibi` jest mocne, tempo i chęć natychmiastowego rewanżu wzrosną.
- **Obserwuj:** czas do pierwszego konkretnego podejrzenia, liczbę powtórzonych pytań, czy gracze chcą od razu zamienić role.
- **Decyzja:** jeśli wersja jest krótka, ale nadal obojętna, nie należy traktować `Alibi` jako wystarczającego rdzenia. Jeśli działa, rozważ kompresję zamiast rozbudowy.

### Test 1 — wspólny hotspot

- **Jedna zmiana:** wszystkie cele w jednej `Rundzie` wymagają użycia jednego z trzech wspólnych obiektów; reszta reguł bez zmian.
- **Oczekiwany efekt:** więcej świadków, negocjacji i konfliktów o dostęp.
- **Obserwuj:** ile rozmów zaczyna się od konkretnej obserwacji, ilu graczy zmienia trasę z powodu innej osoby, czy hotspot tworzy decyzję czy tylko korek.
- **Decyzja:** zachowaj, jeśli rozmowy o zdarzeniach wyraźnie rosną bez dominacji czekania; popraw, jeśli powstaje kolejka; odrzuć, jeśli gracze nadal traktują obiekt jak osobny automat zadaniowy.

### Test 2 — aktywny `Winny`

- **Jedna zmiana:** `Winny` musi wykonać jedną z dwóch ryzykownych ingerencji w publiczny stan, aby odblokować `Ucieczkę` lub pełną obronę `Alibi`.
- **Oczekiwany efekt:** antagonista inicjuje sytuacje zamiast tylko reagować na pytania.
- **Obserwuj:** czy inni reagują na skutek, czy `Winny` ma co najmniej dwie strategie, czy ingerencja jest konieczna, ale nie samobójcza.
- **Decyzja:** zachowaj, jeśli ingerencja generuje historię i kontrgrę; popraw, jeśli wszyscy od razu poznają rolę; odrzuć konkretną implementację, jeśli najlepszą strategią nadal jest bezczynność.

### Test 3 — ślad diagnostyczny

- **Jedna zmiana:** jeden istniejący `Incydent` ujawnia przybliżony czas i kategorię dostępu, ale nie autora.
- **Oczekiwany efekt:** `Detektyw` i świadkowie mają konkretny następny krok.
- **Obserwuj:** czy ślad zawęża do 2–3 osób, czy tworzy nowe pytanie, czy da się go wiarygodnie zanieczyścić albo wyjaśnić.
- **Decyzja:** zachowaj, jeśli zmienia podejrzenia, lecz nie wskazuje automatycznie `Winnego`; osłab lub dodaj kontrę, jeśli staje się testem roli; wzmocnij, jeśli nikt nie wie, co z nim zrobić.

### Test 4 — czasownik `Detektywa`

- **Jedna zmiana:** daj `Detektywowi` dwie żetony weryfikacji; może ujawnić jeden wymiar wybranego zdarzenia albo zabezpieczyć węzeł przed zmianą.
- **Oczekiwany efekt:** śledczy stawia hipotezę w świecie, a grupa reaguje; spada udział seryjnych wywiadów.
- **Obserwuj:** czy wybór jest trudny, czy tworzy wspólne działanie, ile czasu zajmuje kolejka rozmów jeden-na-jeden.
- **Decyzja:** zachowaj, jeśli decyzja ma koszt alternatywny i generuje dyskusję; odrzuć, jeśli jest oczywistą checklistą.

### Test 5 — koszt pamięci `Alibi`

- **Jedna zmiana:** w wariancie eksperymentalnym podejrzani dostają trzy ikony-kotwice bez pełnych zdań albo `Detektyw` otrzymuje pusty formularz kategorii pytań.
- **Oczekiwany efekt:** mniej pomyłek czysto pamięciowych, więcej pytań o związki i sprzeczności.
- **Obserwuj:** rodzaj pomyłek, powtarzalność pytań, czy `Winny` staje się oczywisty.
- **Decyzja:** jeśli rozmowy są płynniejsze, ale nadal pozbawione stawki, uznaj to za poprawę UX, nie rdzenia. Każde trwałe wdrożenie wymaga ponownej decyzji wobec ADR-0007.

### Test 6 — limit ciszy

- **Jedna zmiana:** moderator uruchamia przygotowany beat tylko po 35–55 sekundach bez zmiany wspólnego stanu lub rozmowy o konkretnym zdarzeniu.
- **Oczekiwany efekt:** znikają długie martwe odcinki bez spamowania zdarzeniami w aktywnym momencie.
- **Obserwuj:** czas bez decyzji, czy beat przerwał sensowną rozmowę, ile beatów w ogóle trzeba było uruchomić.
- **Decyzja:** dobry system powinien z czasem potrzebować mniej awaryjnych beatów, bo gracze napędzają kolejne wydarzenia sami.

### Test 7 — zintegrowana `Żywa Sprawa`

- **Jedna zmiana względem bieżącej gry:** zastąp pakiet prywatnych łańcuchów jedną papierową siecią trzech wspólnych węzłów; wewnątrz testu nie zmieniaj już zasad między rundami.
- **Oczekiwany efekt:** środek `Rundy` tworzy co najmniej dwa pamiętane zwroty, a finał wynika z przecięcia śladów i zeznań.
- **Obserwuj:** pełny arkusz poniżej.
- **Decyzja:** dopiero ten test mówi, czy kierunek jest wart implementacji w Unity. Pojedynczy zabawny incydent nie wystarczy.

## Jak mierzyć „czy jest mniej nudno” bez telemetrii

Nuda ma obserwowalne skutki. Jeden obserwator z kartką może po każdym beacie zaznaczać:

- czy po 30–60 sekundach każdy potrafi powiedzieć, co może teraz sensownie zrobić;
- momenty bez decyzji, reakcji albo rozmowy o zdarzeniu;
- rozpoczęcia rozmowy oparte na konkretnym fakcie kontra ogólne „co robiłeś?”;
- zmiany podejrzeń: krótki anonimowy wybór po starcie, w połowie i przed `Egzekucją`;
- liczbę osób potrzebnych do wykonania lub zinterpretowania ważnej czynności;
- udział czasu spędzony w seryjnych przesłuchaniach jeden-na-jeden;
- czy każdy gracz spowodował, zobaczył albo poznał co najmniej dwa istotne zdarzenia;
- czy decyzja o `Egzekucji` opiera się na przynajmniej dwóch niezależnych sygnałach, czy głównie na intuicji;
- co gracze spontanicznie opowiadają po `Rundzie`;
- anonimową odpowiedź „czy chcesz natychmiast zagrać jeszcze raz?” i dlaczego.

Proponowany próg przejścia z papierowego testu do wdrożenia:

- w co najmniej trzech kolejnych `Rundach` większość graczy chce rewanżu;
- każdy potrafi wskazać co najmniej jeden zwrot ze środka gry, nie tylko finał;
- grupa zmienia podejrzenia przynajmniej kilka razy na podstawie zdarzeń;
- seryjne przesłuchania przestają dominować czas;
- `Winny` ma przynajmniej dwie skuteczne strategie i nie wygrywa głównie przez podsłuchanie poprawnego `Alibi`.

To celowo są progi behawioralne. Dokładne wartości procentowe należy ustalić dopiero po zapisaniu obecnego baseline'u, zamiast wymyślać precyzję bez danych.

## Czego teraz nie dodawać

- kolejnych odizolowanych `Prywatnych Celów`, zanim jeden zestaw nie tworzy wspólnych historii;
- większej liczby przypadków jako lekarstwa na słabą pętlę;
- rozbudowanych minigier, które odciągają uwagę, ale nie zmieniają wiedzy ani stawki;
- samych przykładowych pytań jako głównej naprawy;
- losowych zdarzeń klimatycznych bez sprawcy, konsekwencji i następnej decyzji;
- deterministycznej kryminalistyki, skanera roli albo „wykrywacza kłamstw”;
- większej mapy; przy obecnej strukturze zwiększy czas dojścia i błądzenie;
- dodatkowych zabójców, eliminacji albo formalnych tur przesłuchań;
- ciężkiego systemu AI do reżyserowania tempa przed sprawdzeniem prostego limitu ciszy;
- metaprogressji i odblokowań mających maskować brak chęci rozegrania następnej `Rundy`.

## Co zachować

Badanie nie wskazuje, że cały pomysł jest zły. Najbardziej wartościowe i odróżniające elementy to:

- jedna osoba w roli aktywnego `Detektywa`;
- przestrzenna prywatność rozmowy i fizyczne obserwowanie zachowań;
- absurdalne `Przestępstwo` w tonie napiętej czarnej komedii;
- niepełne informacje i prawdziwe niewinne powody do ukrywania zachowań;
- jedna konsekwencyjna `Egzekucja`, która pozostaje finałem;
- `Ucieczka` jako widoczna presja alternatywna;
- ręcznie tworzone moduły contentu, ale dopiero po udowodnieniu systemowego rdzenia.

Problemem nie jest obietnica. Problemem jest brak maszyny, która przez całą `Rundę` wielokrotnie zamienia tę obietnicę w zdarzenie, częściową wiedzę, decyzję, reakcję i nową sytuację.

## Niepewności i ryzyka

- Relacja pochodzi głównie z jednej zgranej grupy. Inne grupy mogą inaczej tolerować improwizację, ciszę i koszt pamięci.
- Dokumenty projektowe i obecna implementacja nie zawsze mają identyczny zakres; raport ocenia strukturę, nie potwierdza każdej liczby contentu w buildzie.
- Nie przeanalizowano nagrań rozmów ani ruchu graczy. Nie wiadomo jeszcze, jaki procent nudy pochodził z kolejki przesłuchań, błądzenia, nieczytelnych celów, jakości głosu i samej ekonomii informacji.
- Część źródeł to wypowiedzi projektantów opisujących własne decyzje. Są cennym dowodem mechanizmu, ale nie kontrolowanym eksperymentem.
- Badania akademickie dotyczą innych gier i grup. Wspierają współzależność i wspólne tematy rozmowy, lecz nie wyznaczają gotowej recepty.
- Zbyt czytelny wspólny stan może spłaszczyć kłamstwo; zbyt nieczytelny odtworzy obecny problem. Dlatego należy testować przecięcia miękkich śladów, a nie dodawać jeden mocny dowód.
- Zmuszenie `Winnego` do działania może uczynić rolę oczywistą, jeśli tylko on ma dostęp do danej animacji, trasy albo narzędzia. Każda ingerencja wymaga niewinnego odpowiednika oraz realnej kontry.
- Powiązanie wszystkich celów z kilkoma hotspotami może stworzyć tłok. Układ przestrzenny musi oferować co najmniej dwie drogi, możliwość przekazania zasobu i powód do czasowego rozdzielenia.
- Kierunek C2 oraz trwała pomoc pamięciowa mogą konfliktować z zatwierdzonymi ADR-ami. Powinny pozostać odseparowanymi eksperymentami do czasu nowej decyzji produktowej.

## Rekomendowana kolejność decyzji

1. Zapisz jedną bieżącą `Rundę` jako baseline za pomocą prostego arkusza obserwacji.
2. Wykonaj Test 0, aby ustalić, czy samo `Alibi` ma naturalnie krótki, powtarzalny rdzeń.
3. Wykonaj Testy 1–4 na kartach lub przez ustne reguły, bez nowego contentu i UI.
4. Złóż zwycięskie elementy w jedną 12–14-minutową `Żywą Sprawę` i rozegraj przynajmniej trzy `Rundy` z rotacją ról.
5. Dopiero gdy środek gry tworzy pamiętane zwroty oraz natychmiastową chęć rewanżu, zaprojektuj produkcyjny model danych, UI i moduły przypadków.

Najtańsza istotna decyzja brzmi zatem nie „jakie zadanie dodać?”, lecz:

> **Jaki jeden wspólny stan gracze będą dziś zmieniać, obserwować, ukrywać i interpretować — oraz dlaczego każda rola musi wejść z nim w konflikt?**
