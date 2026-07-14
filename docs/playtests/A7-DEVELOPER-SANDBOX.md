# A7a — jednoosobowy sandbox developerski Rundy

Ten tryb pozwala jednej osobie uruchomić prawidłową Rundę i sprawdzić fizyczne akcje B4/B5 bez otwierania czterech klientów. Brakujące miejsca w Składzie Rundy są technicznymi wpisami domeny: nie mają postaci, AI, połączenia Mirror ani voice.

Nie jest to zamiennik pełnego A7. Rozmowy, prywatność między ekranami, Egzekucja przez prawdziwy hitbox, zachowania graczy i Głos Przestrzenny nadal wymagają wielu prawdziwych klientów.

## Uruchomienie

1. Otwórz scenę `Room` i Play Mode.
2. Naciśnij `Esc`, wybierz `Sieć / Host`, a następnie `Host (Server + Client)`.
3. Wróć do `Menu` i wybierz `Sandbox Rundy` albo użyj skrótu `F8`. Panel `A7a — SANDBOX RUNDY` pojawi się po prawej stronie.
4. Wybierz testowanego prawdziwego gracza, docelowy skład 4/5/6 i scenariusz.
5. W Przygotowaniu sprawdź prywatny widok, a następnie kliknij `Zakończ Przygotowanie`.
6. Podejdź do wskazanego obiektu i przytrzymaj `E`. Panel pokazuje bieżący krok.
7. Po wyniku kliknij `Wróć do lobby i zresetuj świat`, zanim uruchomisz kolejny scenariusz.

Wszystkie menu są domyślnie ukryte. `Esc` otwiera wybór między siecią, zwykłą Rundą i sandboxem; `F8` bezpośrednio pokazuje lub ukrywa sandbox. Po skonfigurowaniu scenariusza wybierz `Graj z panelem`, żeby przypiąć instrukcję, schować kursor i wrócić do chodzenia. `F8` ponownie otwiera obsługę albo ukrywa przypięty panel. Panel można dodatkowo zwinąć przyciskiem `—`. Normalny przycisk Start Rundy nadal wymaga 4–6 prawdziwych graczy.

## Scenariusze fizyczne

### Niewinny — Osobista Sprawa

1. `Records Cabinet` albo `Evidence Shelf` — przygotowanie.
2. `Locker` albo `Archive Slot` — zakończenie.
3. Wymuś upływ Limitu Rundy i sprawdź ukończenie Celu, Przetrwanie oraz indywidualną wygraną.

### Niewinny — Sekretny Cel

Wymaga składu 5 lub 6.

1. `Evidence Tray` — zdobycie podejrzanego przedmiotu.
2. `Target Locker` — Wrobienie i Cichy Incydent.
3. Użyj `Wymuś Egzekucję Celu` i sprawdź indywidualny wynik oraz ujawnienie autora Incydentu.

### Winny — Trop i Ucieczka

1. Opcjonalnie `Crumpled Receipt` — Trop do Alibi.
2. `Maintenance Cabinet` — pierwszy wspólny krok Planu.
3. `Service Panel` — drugi wspólny krok.
4. `Vent Control` albo `Loading Gate Control` — przygotowanie wyjścia.
5. Odpowiadający `Service Vent` albo `Loading Gate Exit` — widoczny finał Ucieczki.

Po przerwaniu finału to samo wyjście wymaga ponownego przygotowania. Drugie wyjście pozostaje niezależne.

### Detektyw — Incydenty

1. `Archive Alarm` — Hałaśliwy Incydent powinien trafić do Rejestru natychmiast.
2. `Target Locker` — Cichy Incydent; odejdź i podejdź ponownie, aby sprawdzić osobiste odkrycie.
3. Panel przypisuje te dwie akcje technicznemu Podejrzanemu, ponieważ Detektyw nie może być domenowym autorem Incydentu.
4. Użyj symulowanej Egzekucji Winnego albo Niewinnego, aby sprawdzić oba wyniki.

Prawdziwy strzał Egzekucji wymaga co najmniej drugiego rzeczywistego klienta z postacią i hitboxem.

## Minimalna regresja

Po każdym scenariuszu uruchom drugą Rundę i potwierdź, że zresetowały się: postęp interakcji, wizualne skutki Incydentów, Rejestr, Trop, Plan Ucieczki, eliminacja oraz autoryzacja broni. Dla testu celowanych prywatnych widoków uruchom hosta i co najmniej jednego klienta KCP/ParrelSync; sandbox dopełni tylko brakujące miejsca.
