# Autonomiczny przebieg pakietu grafiki

## Decyzja użytkownika

Użytkownik zatwierdził Fazy 0 i 1 dnia 2026-07-12, ale chce ocenić cały pakiet przed jakimkolwiek merge do `main`. Zatwierdzone zmiany są integrowane na branchu `gfx/graphics-overhaul`.

Kolejność pracy bez dodatkowej ingerencji użytkownika:

1. Faza 4 — post-processing i kamera.
2. Weryfikacja Fazy 4; status `Review` po spełnieniu jej Definition of Done.
3. Faza 2 — oświetlenie, z użyciem zatwierdzonego `ART-DIRECTION.md`.
4. Weryfikacja Fazy 2; status `Review` po spełnieniu jej Definition of Done.
5. Raport porównawczy całego pakietu. Bez merge do `main`.

## Zakres samodzielnych decyzji agenta

Agent może bez pytania użytkownika:

- dobierać wartości w granicach i wartościach startowych dokumentów Faz 4 i 2;
- korygować intensywność, kontrast, bloom, vignette, temperaturę i rozmieszczenie świateł, jeśli wymaga tego czytelność oraz budżet wydajności;
- wykonywać drafty, bake'i, zrzuty i wąskie testy potrzebne do oceny;
- tworzyć pliki i komponenty wyraźnie wymagane przez dokument fazy;
- wykonywać małe, jednotematyczne commity na `gfx/graphics-overhaul`;
- naprawiać błędy kompilacji lub konfiguracji spowodowane własnymi zmianami;
- cofnąć wyłącznie własny, nieudany eksperyment przed jego commitowaniem.

## Granice upoważnienia

Agent nie może bez nowej zgody użytkownika:

- modyfikować, scalać ani commitować do `main`;
- pushować brancha, otwierać PR ani publikować artefaktów;
- dodawać pakietów, zmieniać vendor code lub `Packages/packages-lock.json`;
- ręcznie edytować YAML scen, prefabów, assetów ani plików `.meta`;
- usuwać lub nadpisywać zastanych zmian użytkownika;
- rozszerzać zakresu o Fazy 3, 5, 6 lub 7;
- omijać STOP RULE z `AGENTS.md` przez automatyzację systemową lub edycję plików Unity jako tekstu.

## Polityka pytań i blokerów

Nie pytaj o preferencje mieszczące się w dokumentach faz i `ART-DIRECTION.md`; wybierz najbezpieczniejszą wartość, zapisz decyzję w commicie i kontynuuj.

Zatrzymaj odpowiednią część pracy tylko wtedy, gdy:

- Unity MCP nie ma wymaganej możliwości i obowiązuje STOP RULE;
- występuje konflikt z niezwiązaną zmianą użytkownika;
- wykonanie wymaga nowego pakietu, danych uwierzytelniających, drugiej maszyny lub decyzji spoza zatwierdzonego zakresu;
- deterministyczna weryfikacja wykazuje błąd, którego nie da się naprawić bez rozszerzenia zakresu.

Jeżeli jedna część zostanie zablokowana, wykonaj wszystkie pozostałe bezpieczne i niezależne zadania fazy, nie oznaczaj jej jako `Review`, zapisz dokładny raport oraz gotowy prompt zgodny ze STOP RULE.

## Wymagany raport agenta wykonawczego

Po każdym tematycznym commicie raportuj orkiestratorowi:

- hash i temat commita;
- zmienione pliki/obiekty;
- wykonane dowody i ich wynik;
- pozostałe checkboxy;
- ryzyka lub blokery.

Przed statusem `Review` wymagane są wszystkie dowody zapisane w dokumencie fazy, `git diff --check` oraz kontrola, że `main` pozostał nietknięty.
