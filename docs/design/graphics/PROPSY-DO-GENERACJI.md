# Assety do wygenerowania — środowisko i mechaniki Rundy

- **Status:** Propozycja rozszerzona o zatwierdzone mechaniki (2026-07-14)
- **Powiązane:** [FAZA-3-materialy-kit.md](./FAZA-3-materialy-kit.md), [ART-DIRECTION.md](./ART-DIRECTION.md), [MAP-MVP.md](../MAP-MVP.md), [Prywatne Cele, Incydenty i Ucieczka](../mechanics/prywatne-cele-incydenty-i-ucieczka.md), [ADR-0013](../../adr/0013-private-goals-and-emergent-rebellion.md), [ADR-0014](../../adr/0014-readable-actions-ambiguous-motives.md)
- **Decyzja:** postacie generowane (Jak, Małpa, Wieprz, Karton) mają wyższą jakość niż darmowe paczki, więc propsy również generujemy zamiast importować z Asset Store / Kenney. Aktualizuje to decyzję wykonawczą z Fazy 3 („paczki z Asset Store") — źródłem propsów staje się generacja w stylu spójnym z postaciami.

Dokument jest wspólnym backlogiem assetów 3D, stanów wizualnych, animacji, audio i VFX potrzebnych do przedstawienia zatwierdzonych mechanik. Nie zatwierdza nowych reguł gry. Elementy opisane jako **otwarte** albo **kandydat** nie powinny dostać finalnego modelu przed decyzją projektową; do prototypu wolno użyć neutralnego placeholdera.

## Co zastępujemy, co zostaje

| Źródło | Zawartość | Decyzja |
|---|---|---|
| `Assets/ThirdParty/Kenney/FurnitureKit/` | 31 mebli low-poly flat-shaded | **Zastąpić** — niezgodne z art direction (zakaz low-poly flat-shaded) |
| `Assets/ThirdParty/Quaternius/UltimateGunPack/` | `Pistol_1.fbx` | **Zastąpić** — broń Detektywa widoczna z bliska |
| `Assets/ThirdParty/PolyHaven/`, `ambientCG/` | tekstury CC0 PBR | **Zostaje** — tekstury są zgodne z Fazą 3 |
| `Assets/ThirdParty/OpenGameArt/MuzzleFlash0` | sprite VFX | Zostaje do Fazy 6 |
| `Assets/ThirdParty/Freesound/` | audio | **Zostaje jako źródło** — nie wymaga podmiany, ale musi pokryć wymagania audio mechanik z dalszej części dokumentu |

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

## Priorytet 5 — Wspólny kit fizycznych interakcji

To najważniejsza nowa partia gameplayowa. Te same obiekty i czynności mają obsługiwać Osobiste Sprawy, Wrobienia, Plan Ucieczki, Tropy do Alibi i dobrowolny blef. Model ani efekt nie może zdradzać, czy akcję wykonuje Winny albo Niewinny.

| # | Asset | Wymagane warianty i zastosowanie |
|---|---|---|
| 41 | Modułowa szafka depozytowa / dowodowa | drzwi zamknięte, otwarte i uchylone; wnętrze pełne, puste oraz z wolnym slotem na podłożony przedmiot; osobne skrzydło, uchwyt i zamek pod animację |
| 42 | Szuflada biurka / kartoteki jako element interaktywny | zamknięta, wysunięta i przeszukana; środek czytelny z perspektywy gracza; może korzystać z korpusu assetów 9 i 11, ale potrzebuje osobnych ruchomych części |
| 43 | Mała skrytka / kasetka na rzeczy osobiste | zamknięta, otwarta, pusta; logiczny punkt dla skonfiskowanych przedmiotów i Tropów do Alibi |
| 44 | Półka lub oznaczony slot depozytowy | stan z przedmiotem, bez przedmiotu i z przedmiotem podmienionym; zmiana musi być zauważalna bez komunikatu UI |
| 45 | Koperta dowodowa / worek depozytowy | zamknięty, otwarty, pusty i z zawartością; neutralne pole na ręcznie authorowaną etykietę |
| 46 | Pęk kluczy posterunku | 2–3 sylwetki kluczy, metalowe kółko i czytelna zawieszka; stan wiszący na haczyku, trzymany oraz brak na haczyku |
| 47 | Tablica / listwa na klucze | kompletna i z pustym, wyraźnym miejscem po kluczu; używana do osobistego odkrycia Cichego Incydentu |
| 48 | Telefon osobisty zgodny z epoką | skonfiskowany aparat przenośny lub „cegła" z lat 80.–90., nigdy smartfon; stan w depozycie, zabrany i podłożony |
| 49 | Paczka papierosów + luźne papierosy | zamknięta, otwarta, pełna i opróżniona; rozpoznawalna jako coś innego niż popielniczka z pozycji 6 |
| 50 | Zapałki / zapalniczka zgodna z epoką | mały przedmiot wspierający alternatywne motywy dla kradzieży papierosów i manipulacji przy innych obiektach |
| 51 | Neutralny dokument kompromitujący | złożony i rozłożony arkusz, bez tekstu konkretnej Sprawy; możliwy do zabrania, ukrycia, podmiany i podłożenia |
| 52 | Teczka osobista / kartoteka Podejrzanego | zamknięta, otwarta, z dokumentem i bez dokumentu; neutralne oznaczenie pozwalające powiązać miejsce z graczem bez ujawniania roli |
| 53 | Mały podejrzany przedmiot do Wrobienia | neutralny zestaw bazowy, np. cudzy klucz, oznaczona koperta albo drobny przedmiot osobisty; minimum 3 czytelne sylwetki do rotowania między modułami |
| 54 | Zestaw punktów ukrycia | kompatybilne sloty w koszu, kartonie archiwalnym, teczce, szafce i za luźnym elementem mebla; każdy ze stanem pustym i zajętym |
| 55 | Zestaw narzędzi drobnych | śrubokręt, wytrych/drut, małe szczypce i podobne przedmioty zgodne z posterunkiem; kandydaci do Celów i przygotowania Ucieczki, bez bezpieczników i „napraw technicznych" wykonywanych przez przesłuchiwanych |

### Wymagane stany każdego obiektu interaktywnego

- Stan świata musi być czytelny fizycznie: `obecny / zabrany`, `zamknięty / otwarty`, `pełny / pusty`, `oryginał / podmiana` albo `nienaruszony / zmanipulowany` — zależnie od obiektu.
- Cichy Incydent nie otrzymuje globalnego błysku, alarmu ani znacznika. Detektyw ma rozpoznać go po osobistym obejrzeniu zmienionego obiektu.
- Obiekt potrzebny obowiązkowemu Celowi nie może zniknąć bez możliwości dokończenia Celu. Warianty wizualne muszą wspierać reset Rundy i ponowne użycie punktu.
- Ważne przedmioty muszą mieć czytelną sylwetkę z typowego dystansu obserwacji, także w dłoni innego gracza. Nie wolno polegać wyłącznie na drobnym napisie lub kolorze.
- Żaden wariant materiału, emisji ani ikony nie może kodować roli, prawdziwego motywu, właściciela Celu ani postępu Winnego.

## Priorytet 6 — Tropy do Alibi i content zależny od Sprawy

Geometria bazowa jest wielokrotnego użytku, ale właściwa treść Tropu powstaje ręcznie dla konkretnej Sprawy. Generujemy pusty nośnik i layout materiału; nie generujemy w runtime tekstu, zdjęć ani rozwiązania Alibi.

| # | Asset | Zakres |
|---|---|---|
| 56 | Paragon z epoki | baza papieru, awers/rewers, wariant złożony i rozwinięty; ręcznie authorowane data, pozycje i kwoty dla konkretnej Sprawy |
| 57 | Fotografia analogowa | polaroid lub odbitka z neutralnym rewersem; właściwy obraz i opis zależne od Sprawy |
| 58 | Wiadomość papierowa | notatka, list albo wyrwana kartka; tekst tylko ręcznie authorowany, nie kopiuje wprost ukrytego faktu Alibi |
| 59 | Formularz / protokół policyjny | wielokrotnego użytku layout instytucjonalny; pola treści wypełniane osobno dla Sprawy |
| 60 | Kaseta magnetofonowa / nośnik zapisu | neutralny fizyczny Trop lub rzecz skonfiskowana; etykieta zależna od Sprawy, jeśli wariant zostanie użyty |
| 61 | Zestaw neutralnych etykiet dowodowych | numery, datowniki, pieczątki i pola nazw; nie mogą automatycznie podawać sprawcy ani roli |

Każdy Trop musi dać się obejrzeć z bliska i zinterpretować przez gracza, ale nie może wyświetlać gotowego brakującego zdania z Alibi. Lista konkretnych paragonów, zdjęć i wiadomości powstaje razem z każdą nową Sprawą, a nie jako jeden uniwersalny pakiet.

## Priorytet 7 — Wrobienie oraz Ciche i Hałaśliwe Incydenty

| # | Asset | Wymagane stany / efekt |
|---|---|---|
| 62 | Punkt podłożenia przedmiotu przy Celu | neutralny slot w teczce, depozycie, szufladzie albo rzeczy osobistej; pusty, zajęty i odkryty; nie może sam potwierdzać autora Wrobienia |
| 63 | Podmieniony dokument lub zawartość koperty | oryginał i podmiana muszą różnić się wystarczająco do inspekcji, ale nie przez sygnałowy kolor ani ikonę roli |
| 64 | Ślad manipulacji przy zamku / szafce | subtelny wariant nienaruszony i naruszony: przekrzywiona plomba, rysa, niedomknięcie albo zerwana etykieta |
| 65 | Moduł alarmu posterunku | obudowa zgodna z epoką, lampka, dzwonek/syrena i stan aktywny; używany wyłącznie dla Hałaśliwego Incydentu |
| 66 | Tłuczone szkło | obiekt bazowy, wersja pęknięta i odłamki; VFX krótkiego rozbicia oraz bezpieczny stan po Incydencie |
| 67 | Ślad wystrzału w świecie | decal uderzenia i drobny pył/iskry zależne od materiału; wystrzał pozostaje Hałaśliwym Incydentem, ale nie wskazuje nazwiska strzelającego w Rejestrze |
| 68 | Ogólny moduł awarii/hałasu zgodny z lokacją | kandydat do zatwierdzenia po playteście; musi być wiarygodnym skutkiem działania Podejrzanego, nie zadaniem typu „sprawdź bezpieczniki" |

### Zasady czytelności Incydentów

- Hałaśliwy Incydent potrzebuje rozpoznawalnego dźwięku przestrzennego i fizycznego źródła w świecie. Informacja natychmiastowa dotyczy skutku i miejsca, nie sprawcy.
- Cichy Incydent potrzebuje wyłącznie trwałego stanu do odkrycia przy obiekcie; nie dostaje automatycznego globalnego VFX ani komunikatu dla Detektywa przed inspekcją.
- Inni gracze mogą zauważyć oba rodzaje zmian, ale asset nie może wizualnie odróżniać „akcji Celu" od blefu lub działania Winnego.
- Jednorazowy albo stanowy skutek musi uniemożliwiać wizualny spam tą samą zmianą świata.

## Priorytet 8 — Plan i finał Ucieczki

Zatwierdzono co najmniej dwa możliwe punkty końcowe w pierwszym prototypie, losowane między kompatybilnymi wariantami. Nie zatwierdzono jeszcze ich konkretnej formy ani umiejscowienia, dlatego przed finalnym modelowaniem wymagany jest krótki wybór level-designowy. Nie zakładamy powiększenia obecnej mapy przed playtestem.

| # | Asset | Zakres |
|---|---|---|
| 69 | Moduł punktu Ucieczki A | finalna forma **otwarta**; potrzebuje stanu zablokowanego, przygotowanego, aktywnej próby, przerwanego i ukończonego |
| 70 | Moduł punktu Ucieczki B | inna lokalizacja i sylwetka niż A, aby Detektyw nie mógł campić jednego wyjścia; ten sam komplet stanów |
| 71 | Wymienne elementy przygotowania wyjścia | klucz, narzędzie, zdjęta blokada, otwarta osłona lub zgodny z wybranym punktem odpowiednik; korzystać najpierw z kitu 41–55 |
| 72 | Blokada ponownej próby | czytelny stan po przerwaniu, np. zatrzaśnięcie, uszkodzona część albo utracony element; wymusza dodatkowy krok lub zmianę wyjścia |
| 73 | Fizyczny alarm finałowej Ucieczki | źródło głośnego Incydentu przy punkcie końcowym; raportuje miejsce, a bezpośredni widok akcji pozwala rozpoznać Winnego |
| 74 | Stan ukończonej Ucieczki | otwarte/przełamane wyjście albo inny jednoznaczny rezultat zgodny z wybranym wariantem; nie jest samodzielną cutscenką |

Finał Ucieczki nie dostaje twardej blokady czasowej ani stałego assetu pojawiającego się dopiero w połowie Rundy. Tempo ma wynikać z poszukiwania przedmiotów bez markerów, sekwencji kroków i ryzykownych animacji.

## Animacje gameplayowe do wykonania

To wspólny zestaw bazowy dla wszystkich postaci. Konkretna akcja powinna być widoczna całym ciałem dla obserwatora i zachować czytelność obiektu. Czasy są wartościami roboczymi do playtestu, nie finalnym tuningiem.

| # | Animacja | Zakres |
|---|---|---|
| A1 | Krótkie obejrzenie / inspekcja | roboczo 1–2 s; pochylenie lub zbliżenie dłoni do obiektu bez zabrania go |
| A2 | Przeszukiwanie szuflady, szafki i kartonu | roboczo 4–6 s; warianty wysokości niski/średni/wysoki, żeby nie tworzyć osobnej animacji dla każdego mebla |
| A3 | Kradzież / zabranie konkretnego przedmiotu | roboczo 4–6 s jako pełna podejrzana akcja, z czytelnym momentem przejścia przedmiotu do dłoni/ekwipunku |
| A4 | Ukrycie / podłożenie | roboczo 4–6 s; wariant niski, blat i szafka; używany przez Osobistą Sprawę, Wrobienie, Plan i blef |
| A5 | Podmiana dokumentu lub zawartości | roboczo 4–6 s; obie wersje przedmiotu czytelne w kluczowym momencie |
| A6 | Manipulacja zamkiem, plombą lub urządzeniem | roboczo 4–6 s; neutralna animacja pracy dłońmi, bez kodowania motywu |
| A7 | Przerwanie każdej akcji | szybkie, naturalne wyjście do lokomocji; anulowanie nie może pokazywać zakończonego stanu ani pozostawiać częściowego progresu |
| A8 | Finał Ucieczki A | roboczo 6–7 s, zależny od wybranego punktu; głośny i jednoznacznie czytelny dla świadka |
| A9 | Finał Ucieczki B | osobna animacja albo dopasowany wariant A, jeśli geometria na to pozwala; ten sam poziom czytelności i ryzyka |
| A10 | Przerwana Ucieczka | reakcja na anulowanie bez specjalnej akcji chwytania przez innych graczy; przejście do stanu blokującego natychmiastową ponowną próbę |

Wspólne wymagania techniczne animacji: Humanoid tam, gdzie pozwalają na to riggi; eventy tylko do prezentacji, nie do rozstrzygania reguł; dłonie dopasowane przez IK do jawnych punktów interakcji; ruch gracza zablokowany podczas długiej akcji, ale rozglądanie i Głos Przestrzenny pozostają aktywne.

## Audio i VFX mechanik

Istniejące źródła Freesound i dotychczasowe VFX mogą zostać użyte lub przerobione; poniższa lista opisuje wymagany rezultat, a nie obowiązek generowania każdego pliku od zera.

| # | Asset | Zakres |
|---|---|---|
| AV1 | Foley otwierania i przeszukiwania | szuflada metalowa, szafka, kasetka, karton i papiery; wariant start/pętla/koniec lub zestaw odporny na przerwanie |
| AV2 | Foley konkretnych przedmiotów | klucze, telefon, paczka papierosów, dokument, koperta, kaseta i drobne narzędzia; obserwator powinien móc rozpoznać kategorię czynności |
| AV3 | Foley ukrycia, podłożenia i podmiany | osobne krótkie dźwięki dla papieru, metalu i twardego przedmiotu; bez „złego" stingu zdradzającego motyw |
| AV4 | Tłuczone szkło | impuls przestrzenny + krótki VFX odłamków; natychmiastowy Hałaśliwy Incydent |
| AV5 | Alarm Incydentu | syrena/dzwonek zgodny z epoką, lokalizowalny w przestrzeni; stan start, trwanie i wygaszenie |
| AV6 | Alarm finałowej Ucieczki | odróżnialny od zwykłego tła i czytelny pod Głosem Przestrzennym; raportuje zagrożenie i miejsce, nie nazwisko |
| AV7 | VFX manipulacji | minimalny pył, poruszenie papieru albo drobiny tylko tam, gdzie fizycznie uzasadnione; brak poświaty zadania i magicznych markerów |
| AV8 | VFX punktu Ucieczki | stan aktywnej próby, przerwania i ukończenia dopasowany do wybranych punktów A/B; nie może zasłaniać sylwetki Winnego Detektywowi |
| AV9 | Prywatne UI audio | dyskretny sygnał nowego wpisu Rejestru lub zmiany własnego kroku; nie może być słyszalny innym graczom ani zdradzać ich Celów |

## UI i grafiki 2D powiązane z mechanikami

| # | Asset | Decyzja produkcyjna |
|---|---|---|
| UI1 | Placeholder aktualnego kroku Prywatnego Celu | wykonać neutralnie do prototypu; docelowa forma kartki, telefonu, clipboardu albo panelu pozostaje otwarta |
| UI2 | Wpis Rejestru Incydentów | neutralne piktogramy skutku, miejsca i czasu zgłoszenia/odkrycia; żadnej ikony sprawcy, roli, intencji ani rzeczywistego czasu cichej akcji |
| UI3 | Ujawnienie po Rundzie | sloty na role, Cele, postęp, Wrobienie, Plan Ucieczki, Tropy, autorów Incydentów i indywidualne wyniki; styl finalnego ekranu może powstać po sprawdzeniu czytelności prototypu |
| UI4 | Szablony Tropów oglądanych z bliska | layouty paragonu, zdjęcia, wiadomości i protokołu; treści ręcznie authorowane per Sprawa |
| UI5 | Notatki Detektywa | **nie generować finalnej oprawy** przed wyborem między tablicą, kartką/clipboardem i panelem tekstowym |
| UI6 | Prezentacja Alibi | **nie generować finalnej oprawy** przed decyzją lista faktów kontra narracyjny akapit |

## Macierz ponownego użycia — kontrola niejednoznacznych motywów

Każdy finalny Łańcuch Celu ma korzystać z fizycznej akcji, która ma co najmniej dwa wiarygodne motywy. Poniższa macierz określa oczekiwane pokrycie produkcyjne; nie przydziela konkretnych Celów na Rundę.

| Akcja / asset | Osobista Sprawa | Wrobienie | Plan Ucieczki | Trop do Alibi | Blef |
|---|:---:|:---:|:---:|:---:|:---:|
| Zabrać klucz | ✓ | ✓ | ✓ |  | ✓ |
| Przeszukać depozyt lub szufladę | ✓ | ✓ | ✓ | ✓ | ✓ |
| Zabrać telefon, papierosy lub dokument | ✓ | ✓ |  | ✓ | ✓ |
| Ukryć albo podłożyć przedmiot | ✓ | ✓ | ✓ |  | ✓ |
| Podmienić dokument lub zawartość | ✓ | ✓ | ✓ | ✓ | ✓ |
| Manipulować przy zamku/plombie | ✓ | ✓ | ✓ | ✓ | ✓ |

To pokrycie jest warunkiem doboru finalnych assetów: sam widok konkretnej czynności ma dostarczać Detektywowi miękki trop, ale nie pewność roli.

## Kolejność produkcji nowych assetów

1. Placeholdery 41–55 oraz co najmniej po jednym stanie `przed / po`, aby dało się zagrać Celami bez finalnego artu.
2. Wspólne animacje A1–A7 i podstawowe foley AV1–AV3, bo to one decydują o czytelności oraz ryzyku akcji.
3. Jeden kompletny Łańcuch Osobistej Sprawy i jeden kompletny Łańcuch Wrobienia, złożone wyłącznie ze wspólnego kitu.
4. Jeden Trop do Alibi dla testowej Sprawy na bazie 56–61.
5. Po wyborze dwóch punktów: placeholdery Ucieczki 69–74, animacje A8–A10 oraz AV5–AV8.
6. Playtest na obecnej mapie: czas poszukiwania, liczba świadków, czytelność przedmiotów i możliwość campienia wyjść.
7. Dopiero po wyniku playtestu finalne modele, tekstury, warianty contentowe oraz decyzja, czy mapa wymaga powiększenia.

## Kryteria gotowości partii gameplayowej

- Każda akcja jest rozpoznawalna bez tekstowego komunikatu: świadek potrafi powiedzieć, czy gracz zabrał klucz, telefon, papierosy albo dokument.
- Żaden asset, materiał, dźwięk ani VFX nie potwierdza roli lub motywu; każda akcja Planu Ucieczki ma co najmniej jedno wiarygodne użycie dla Niewinnego.
- Długa akcja ma czytelny start, trwanie, ukończenie i przerwanie. Przerwany wariant nie wygląda jak zakończony.
- Cichy Incydent ma trwały i możliwy do osobistego odkrycia stan świata, a Hałaśliwy Incydent ma lokalizowalne źródło dźwięku.
- Assety wspierają reset Rundy oraz wszystkie wymagane stany bez duplikowania całego modelu, jeśli wystarczy ruchoma część lub wariant materiału.
- Tropy do Alibi korzystają z ręcznie authorowanej treści konkretnej Sprawy i wymagają interpretacji.
- Minimum dwa punkty finałowej Ucieczki dają się odróżnić lokalizacją i stanem, a aktywna próba nie zasłania sylwetki Winnego.
- Wszystkie elementy przechodzą kontrolę `ART-DIRECTION.md`: stylizowany realizm, zgodność z epoką, czytelna sylwetka, PBR i brak dekoracyjnego kodowania gameplayu.
- Czasy, liczba wariantów, liczba punktów ukrycia i ewentualne powiększenie mapy pozostają parametrami playtestu, a nie zaszytymi właściwościami assetu.

## Elementy świadomie niewymagające osobnego assetu

- `Bunt` nie ma hasła, przycisku, symbolu, animacji ani dedykowanego przedmiotu. Wynika z zachowania i interesów graczy.
- Niewinni nie dostają specjalnej akcji pomagania ani przerywania Ucieczki. Wpływają głosem, drzwiami, pozycją i zasłanianiem widoku.
- Detektyw nie potrzebuje przedmiotu do mechanicznego zatrzymywania Podejrzanego; każdy może opuścić Prywatne Przesłuchanie.
- Nie powstają markery prowadzące do przedmiotów ani poświaty informujące, że akcja należy do Celu.
- Nie powstaje osobny model „przedmiotu Winnego" i „przedmiotu Niewinnego". Role korzystają ze wspólnego kitu.

Nie zastępujemy 1:1 pozycji Kenneya bez roli w Posterunku (`stoolBar`, `tableRound`, `rugDoormat`, `laptop` — nowoczesny, niezgodny z epoką).

## Proces

1. Wygenerować tablicę referencyjną stylu (prompt poniżej w rozmowie / GPT) i zatwierdzić ją względem `ART-DIRECTION.md`.
2. Generować propsy partiami wg priorytetów; każdy props porównywać z tablicą i postaciami. Dla assetów gameplayowych najpierw wykonać pełny zestaw placeholderów i playtest, a dopiero potem finalny art.
3. Import do `Assets/Prefabs/Props/` + materiały URP Lit, `ContributeGI` dla statycznych.
4. Assety interaktywne trzymać jako osobne prefaby z jawnymi punktami chwytu, IK, ruchomymi częściami i wariantami stanu; ich reguły pozostają poza warstwą grafiki.
5. Po każdej partii statycznej: podmiana w `Room.unity`, re-bake GI, screenshot per pomieszczenie.
6. Po każdej partii gameplayowej: test stanów `przed / w trakcie / po / przerwane`, obserwacja z perspektywy świadka oraz reset Rundy.
