# Prywatne Cele, Incydenty i Ucieczka

**Status:** ✅ Zatwierdzona koncepcja, do zaimplementowania po bazowym vertical slice
**Priorytet:** Następny filar rozgrywki po spięciu podstawowej Rundy
**Decyzje:** [ADR-0013](../../adr/0013-private-goals-and-emergent-rebellion.md), [ADR-0014](../../adr/0014-readable-actions-ambiguous-motives.md)

## Problem i oczekiwany rezultat

Samo przesłuchiwanie zachęca Detektywa do długiego izolowania jednej osoby, a pozostałym Podejrzanym nie daje wystarczającego powodu do poruszania się po posterunku. Formalny Bunt oparty na haśle i ukrytym przycisku był trudny do kontrolowania, oderwany od świata oraz podatny na oczywiste sygnały roli.

Nowy model ma równocześnie:

- zmusić każdego Niewinnego do aktywności potrzebnej do indywidualnego zwycięstwa;
- dać Winnemu alternatywną Ucieczkę oraz ryzykowną możliwość zdobywania Tropów do Alibi;
- tworzyć obserwowalne działania bez automatycznego potwierdzania roli;
- generować Incydenty, które odrywają Detektywa od przesłuchań i wymagają osobistego śledztwa;
- pozwolić na emergentny Bunt wynikający z interesów graczy, bez osobnego systemu sygnału.

## Źródła wymagań

### Zatwierdzone wcześniej

- Runda jest ciągła i free-roamingowa; Prywatne Przesłuchanie nie jest formalną turą ani mechanicznym zatrzymaniem ([ADR-0005](../../adr/0005-continuous-free-roaming-rounds.md)).
- Dokładnie jedna Egzekucja natychmiast kończy Rundę ([ADR-0003](../../adr/0003-one-execution-ends-the-round.md)).
- Winny otrzymuje Alibi z ukrytymi faktami, a Detektyw nie dostaje prawdziwego Alibi ([ADR-0006](../../adr/0006-guilty-receives-redacted-alibi.md), [ADR-0008](../../adr/0008-detective-reconstructs-alibi-from-testimony.md)).
- Host posiada sekrety i wysyła każdemu klientowi wyłącznie jego prywatny widok ([ADR-0011](../../adr/0011-server-owns-secrets-and-exposes-private-views.md)).
- Content jest ręcznie przygotowany i modularny; runtime nie generuje go przez AI ([ADR-0010](../../adr/0010-authored-modular-case-content.md)).

### Nowe decyzje z rozmowy

- Każdy Niewinny zawsze ma dokładnie jeden obowiązkowy Prywatny Cel i wygrywa tylko przez `ukończenie Celu + Przetrwanie`.
- Osobista Sprawa jest podstawowym Prywatnym Celem. Sekretny Cel zastępuje ją, zamiast tworzyć dodatkowy obowiązek.
- Przy 4 graczach Sekretny Cel jest niedozwolony. Przy 5–6 graczach domyślnie występuje jeden, a host może go wyłączyć.
- Sekretny Cel wymaga dwukrokowego Wrobienia, Przetrwania właściciela i Egzekucji wskazanego Niewinnego.
- Niewinny, który ukończył Cel i Przetrwał, wygrywa także po Ucieczce Winnego. Może zatem zacząć sprzyjać Ucieczce; jest to emergentny Bunt.
- Winny może mieszać przygotowanie Ucieczki ze zdobywaniem Tropów do Alibi. Nie otrzymuje jednego losowo narzuconego trybu.
- Wszystkie działania są wizualnie czytelne, ale ich motyw pozostaje niejednoznaczny.
- Nie ma twardego czasowego odblokowania Ucieczki ani automatycznych markerów prowadzących do przedmiotów.

### Nierozstrzygnięte i pozostawione do playtestu

- Docelowe czasy zwykłych działań, finałowej Ucieczki oraz ewentualnych cooldownów.
- Liczba ręcznie przygotowanych punktów ukrycia, modułów Celów i wariantów wyjść.
- Czy mapa wymaga powiększenia; pierwszy prototyp korzysta z obecnego posterunku.
- Czy przy sześciu graczach później dopuścić dwa Sekretne Cele.
- Forma fizycznego podglądu Prywatnego Celu oraz finalna forma Notatek Detektywa.
- Czy pełne Alibi jest ujawniane na ekranie wyników.

## Stan obecnej implementacji

Bazowy `RoundEngine` ma już role, Alibi, Limit Rundy, Egzekucję i wcześniejszy opcjonalny model `SecretObjective`. Ten model jest fundamentem technicznym, ale nie spełnia nowych reguł produktu: nie przydziela każdemu Niewinnemu obowiązkowego Prywatnego Celu, nie wymaga Wrobienia i nie obsługuje Incydentów, Tropów ani Ucieczki. Przy implementacji należy go świadomie rozwinąć lub zmigrować; samo ustawienie innej domyślnej liczby `SecretObjective` nie wystarczy.

## Prywatne Cele Niewinnych

### Osobista Sprawa

Każdy Niewinny bez Sekretnego Celu otrzymuje Osobistą Sprawę: drobny sekret lub interes niezwiązany z głównym Przestępstwem. Niewinny jest niewinny w sprawie głównej, ale może chcieć odzyskać skonfiskowany telefon, ukryć kompromitujący dokument, zabrać papierosy albo dostać się do własnego depozytu.

Osobista Sprawa:

- ma porównywalny ślad zachowania do Planu Winnego;
- roboczo składa się z dwóch sekwencyjnie odkrywanych kroków;
- wymaga działań w różnych logicznych miejscach posterunku;
- pozostaje obowiązkowa, lecz nie zastępuje Przetrwania;
- jest znana i widoczna wyłącznie właścicielowi oraz hostowi.

### Sekretny Cel i Wrobienie

Właściciel Sekretnego Celu zna tożsamość Celu oraz informację, że jest on Niewinny. Cel nie wie o przypisaniu.

Wrobienie powinno:

1. wymagać zdobycia podejrzanego przedmiotu albo przygotowania tropu;
2. wymagać podłożenia go w miejscu powiązanym z Celem;
3. tworzyć Cichy Incydent możliwy do odkrycia przez Detektywa;
4. pozostawiać interpretację sprawcy i motywu zeznaniom graczy.

Właściciel wygrywa tylko po ukończeniu Wrobienia, Egzekucji Celu i własnym Przetrwaniu.

## Łańcuchy Celu i interakcje

- Gracz widzi przez całą Rundę wyłącznie aktualny krok i własny postęp. Alibi nadal znika po Przygotowaniu.
- Kolejny krok pojawia się dopiero po zaakceptowanym ukończeniu poprzedniego.
- Przedmiot lub interakcja losuje się spośród kilku ręcznie przygotowanych, logicznych punktów; nie ma markerów mapy ani losowania w przypadkowej geometrii.
- Miejsca poszukiwań są wspólne, ale generator nie może przydzielić dwóch obowiązkowych Celów wymagających tego samego unikalnego przedmiotu.
- Inny gracz nie może trwale uczynić obowiązkowego Celu niewykonalnym.
- Każdy Podejrzany może wykonać niepotrzebną mu akcję dla blefu i wywołać Incydent, lecz nie zwiększa to postępu jego Celu.
- Incydent-generujące akcje odpowiadają jednorazowej albo stanowej zmianie świata, dzięki czemu nie można spamować Rejestru samym wciskaniem interakcji.
- Robocze czasy do testu: 1–2 sekundy na zwykłe obejrzenie, 4–6 sekund na przeszukanie, kradzież, podmianę lub manipulację.
- Podejrzana akcja blokuje ruch, pozostawia głos i rozglądanie, ma czytelną animację, a anulowanie zeruje bieżący postęp.

## Czytelne działanie, niejednoznaczny motyw

Obserwator ma widzieć, czy gracz zabrał klucz, telefon, papierosy albo dokument. Nie może natomiast uzyskać systemowej informacji, czy czynność należała do Osobistej Sprawy, Wrobienia, Planu Ucieczki lub blefu.

Każda akcja Planu Ucieczki powinna mieć co najmniej jedno wiarygodne zastosowanie w Prywatnym Celu Niewinnego. Prywatnego Celu nie można mechanicznie pokazać innym graczom; właściciel może jedynie złożyć zeznanie i poprzeć je obserwowalnym stanem świata.

## Incydenty i śledztwo Detektywa

### Hałaśliwy Incydent

Alarm, wystrzał, tłuczone szkło albo podobny skutek jest natychmiast słyszalny przestrzennie. Detektyw otrzymuje fakt o skutku i miejscu bez potwierdzenia sprawcy.

### Cichy Incydent

Brak klucza, telefonu, dokumentu albo podmieniony przedmiot nie generuje automatycznego raportu. Detektyw odkrywa Incydent dopiero, gdy podejdzie do właściwego obiektu i ma go bezpośrednio w polu widzenia. Inni gracze mogą zauważyć zmianę i opowiedzieć o niej, lecz nie generują systemowego potwierdzenia.

### Rejestr Incydentów

Prywatny Rejestr Detektywa przechowuje:

- rodzaj skutku;
- miejsce;
- czas zgłoszenia albo odkrycia.

Nie przechowuje sprawcy, roli, motywu ani czasu wykonania ukrytej czynności. Rejestr nie jest automatyczną transkrypcją i nie zastępuje Notatek Detektywa.

## Strategie Winnego

### Tropy do Alibi

Trop do Alibi jest ręcznie napisany dla konkretnej Sprawy i powiązany z jednym ukrytym faktem. Może być paragonem, zdjęciem, wiadomością lub innym fragmentem prywatnych, skonfiskowanych rzeczy. Wymaga interpretacji; nie pokazuje gotowego zdania z Alibi i nie może automatycznie odtworzyć wszystkich braków.

Zdobycie Tropu wymaga ryzykownego Łańcucha Celu i pozostawia Incydent. Tropy pomagają Winnemu osiągnąć Przetrwanie przez lepsze zeznania, ale nie są osobnym warunkiem zwycięstwa.

### Plan Ucieczki

- Plan jest jednym z kilku równocześnie dostępnych kierunków działania Winnego.
- Szczegóły, przedmioty i punkty końcowe losują się między kompatybilnymi, ręcznie przygotowanymi wariantami.
- Pierwszy prototyp używa co najmniej dwóch możliwych punktów Ucieczki, aby uniemożliwić stałe campienie jednego wyjścia.
- Finał nie ma twardego odblokowania po określonej części Limitu Rundy; tempo tworzą poszukiwanie, zależności i ryzykowne animacje.
- Finałowa Ucieczka jest Hałaśliwym Incydentem i roboczo trwa 6–7 sekund.
- Raport ujawnia miejsce, ale nie nazwisko. Bezpośrednie zobaczenie osoby wykonującej finałową akcję jednoznacznie ujawnia Winnego.
- Anulowanie akcji zeruje bieżący postęp. Przerwana próba blokuje natychmiastowe ponowienie i wymaga dodatkowego kroku albo przygotowania innego wyjścia.
- Skuteczna Ucieczka natychmiast kończy Rundę zwycięstwem Winnego i przegraną Detektywa.

## Egzekucja i wpływ innych graczy

- Detektyw ma pistolet od początku Rundy. Podejrzani nie mogą go odebrać ani użyć.
- Strzały w świat nie zużywają Egzekucji. Pierwsze trafienie żywego Podejrzanego jest jedyną Egzekucją i kończy Rundę.
- Trafienie Winnego daje zwycięstwo Detektywowi; trafienie Niewinnego daje mu przegraną.
- Podejrzani nie mają specjalnej akcji pomagania ani przerywania Ucieczki. Mogą wpływać na nią głosem, zeznaniami, drzwiami, pozycją i zasłanianiem widoku.
- Detektyw nie może mechanicznie zatrzymać Podejrzanego w Pokoju Przesłuchań. Każdy może wyjść w dowolnym momencie.

## Wyniki i ujawnienie po Rundzie

- Niewinny wygrywa wyłącznie po ukończeniu własnego Prywatnego Celu i osiągnięciu Przetrwania.
- Ucieczka Winnego nie odbiera zwycięstwa Niewinnemu, który spełnił oba indywidualne warunki.
- Niewinny bez ukończonego Celu przegrywa nawet wtedy, gdy Przetrwał.
- Niewinny poddany Egzekucji przegrywa niezależnie od ukończenia Celu.
- Winny wygrywa przez uniknięcie poprawnej Egzekucji albo skuteczną Ucieczkę.
- Po zakończeniu Rundy ujawniane są role, Prywatne Cele, postęp, właściciel i Cel Wrobienia, działania Planu Ucieczki, zdobyte Tropy do Alibi, autorzy Incydentów oraz indywidualne wyniki.

## Granice modułów

### Zatwierdzone granice

- `RoundEngine` pozostaje jedynym źródłem reguł przypisania Celów, postępu, Ucieczki i wyników. Nie zna scen, animacji, zegara Unity ani Mirror.
- `NetworkRoundCoordinator` pozostaje jedynym adapterem Mirror Rundy. Waliduje właściciela intencji, przekazuje komendy do domeny i wysyła prywatne widoki celowanymi wiadomościami.
- Host posiada pełną mapę Celów, braków Alibi, Tropów, autorów Incydentów i postępu Ucieczki.
- Interakcje scenowe renderują animację i zgłaszają intencję; nie rozstrzygają Celu ani zwycięstwa.
- UI renderuje własny prywatny krok, Rejestr Detektywa i ujawnienie po Rundzie; nie wyprowadza reguł lokalnie.

### Proponowany seam do potwierdzenia przy implementacji

Wielokrotnie używane Osobiste Sprawy, Wrobienia i Plany Ucieczki powinny powstawać jako ręcznie authorowane moduły mapy konwertowane przed Rundą do niezmiennych definicji domenowych. Tropy do Alibi powinny rozszerzyć dane konkretnej Sprawy i wskazywać powiązany identyfikator faktu. Dokładne nazwy klas, format assetów i podział definicji są propozycją implementacyjną, nie zatwierdzonym publicznym API.

## Kryteria akceptacji

1. Każdy Niewinny otrzymuje dokładnie jeden Prywatny Cel; jego ukończenie i Przetrwanie są wymagane do zwycięstwa.
2. Konfiguracja 4 graczy wymusza 0 Sekretnych Celów; konfiguracja 5–6 domyślnie przydziela 1 i pozwala hostowi wybrać 0.
3. Żaden klient nie otrzymuje cudzych Celów, postępu, autora Incydentu ani informacji o Planie Winnego przed końcem Rundy.
4. Konkretna widoczna akcja ma co najmniej dwa prawdopodobne motywy i nie potwierdza roli systemowo.
5. Anulowana kilkusekundowa akcja nie zmienia świata ani postępu Celu.
6. Cichy Incydent nie trafia do Rejestru przed osobistym odkryciem przez Detektywa; Hałaśliwy jest raportowany natychmiast.
7. Inny gracz nie może trwale uniemożliwić właścicielowi ukończenia obowiązkowego Celu.
8. Winny może zdobyć Trop do Alibi bez otrzymania gotowego brakującego faktu i bez ujawnienia Tropu Detektywowi.
9. Skuteczna Ucieczka kończy Rundę; osoba obserwująca finałową akcję potrafi jednoznacznie rozpoznać Winnego.
10. Pierwsze trafienie Podejrzanego przez Detektywa kończy Rundę, a pudła nie zużywają Egzekucji.
11. Ekran wyników poprawnie ujawnia role, Cele, autorów Incydentów i indywidualne wyniki.

## Decyzje testowe i poziomy weryfikacji

- Edit Mode przez publiczny interfejs `RoundEngine`: przydział Celów, ograniczenia 4/5/6 graczy, sekwencje kroków, wyniki, Ucieczka, Egzekucja i prywatne widoki.
- Edit Mode contentu: walidacja kompatybilności modułów, unikalnych przedmiotów, punktów końcowych oraz powiązań Tropów z faktami Alibi.
- Play Mode: przerwanie animacji, trwałe zmiany świata, odkrywanie Cichego Incydentu przez line-of-sight i Rejestr Detektywa.
- KCP/ParrelSync: host i 3–5 klientów z różnymi prywatnymi widokami, Wrobieniem, Hałaśliwym Incydentem, Ucieczką oraz ujawnieniem po Rundzie.
- Playtest projektowy: czas znalezienia kroków, częstotliwość świadków, ryzyko campienia wyjść, czy obecna mapa jest wystarczająca oraz czy działania dominują nad rozmową.

## Poza zakresem

- Rozpoznawanie mowy, hasło lub Sygnał Buntu.
- Mechaniczna akcja pomagania albo przerywania Ucieczki dla Niewinnych.
- Prywatne kanały voice.
- Automatyczne wskazywanie sprawcy Incydentu albo weryfikowanie deklarowanego Prywatnego Celu.
- Runtime AI generujące Sprawy, Tropy lub Cele.
- Powiększanie mapy przed uzyskaniem danych z playtestu.
