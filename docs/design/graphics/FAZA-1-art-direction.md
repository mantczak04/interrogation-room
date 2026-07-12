# Faza 1 — Art direction

- **Status:** In progress
- **Branch:** `gfx/faza-1-art-direction`
- **Zależności:** brak twardych; można prowadzić równolegle z Fazami 0/4/2. Blokuje Fazy 3 i 5.
- **Szacowany czas:** 2–4 dni, w większości decyzje użytkownika

Przeczytaj najpierw: [README.md](./README.md) i `AGENTS.md`.

## Cel / Definition of Done

Istnieje zatwierdzony przez użytkownika dokument `docs/design/graphics/ART-DIRECTION.md` definiujący: styl, paletę, motywy świetlne i zasady spójności. Wszystkie późniejsze fazy artystyczne (3, 5, 6) cytują ten dokument zamiast podejmować własne decyzje estetyczne.

## Kierunek wyjściowy (propozycja do zatwierdzenia, nie dogmat)

**„Poważne kinowe światło + absurdalne postacie."** Posterunek wygląda jak z filmu noir — niskie klucze świetlne, zgniła zieleń świetlówek, ciepły wolfram nad stołem przesłuchań, zimna noc za oknami. Na tym tle absurdalni podejrzani (Jak, Małpa, Wieprz, Karton) tworzą kontrast, który jest tożsamością wizualną gry. Stylizowany realizm: geometria realistyczna, tekstury lekko uproszczone, żadnego fotorealizmu.

## Zadania

- [ ] [CZŁOWIEK] Zebrać moodboard (8–15 obrazów): L.A. Noire, Interrogation (2019), Twelve Minutes, Twin Peaks (pokój przesłuchań), zdjęcia komisariatów lat 80–90. Zapisać linki/pliki w `ART-DIRECTION.md`.
- [x] [AGENT] Utworzyć szkielet `docs/design/graphics/ART-DIRECTION.md` z sekcjami: Styl, Paleta, Światło, Materiały, Postacie, Zakazy.
- [x] [AGENT] Zaproponować paletę jako konkretne wartości HEX + temperatury barwowe świateł. Punkt startowy:
  - świetlówki: `#C8E0C0`, ~4500K, lekko zgniłozielone;
  - lampa przesłuchań: `#FFB868`, ~3000K wolfram;
  - noc za oknem: `#3A4A6B`, ~7500K;
  - akcent gameplayowy (UI/`Egzekucja`): jeden kolor sygnałowy, np. `#C22E28`.
- [x] [AGENT] Spisać motywy świetlne per pomieszczenie (pokój przesłuchań, korytarz, archiwum, pokój obserwacyjny) — wejście dla Fazy 2.
- [x] [AGENT] Spisać zasady materiałowe — wejście dla Fazy 3: texel density 512 px/m, roughness zawsze z wariacją (nigdy jednolita), zakaz czystych bieli (max `#E8E8E8`) i czystych czerni (min `#0A0A0A`).
- [x] [AGENT] Spisać zasady postaci — wejście dla Fazy 5: sylwetki rozróżnialne w ciemności i przez szybę wenecką, brak riggu mimiki (sztywne pyski/maski — celowy absurd), czytelność > detal.
- [ ] [CZŁOWIEK] Przegląd i zatwierdzenie dokumentu. Dopiero po zatwierdzeniu Fazy 3 i 5 mogą ruszyć.

## Weryfikacja

- [ ] `ART-DIRECTION.md` istnieje, jest po polsku, ma wszystkie sekcje wypełnione.
- [ ] Statusy zaktualizowane (ten plik + `README.md`). Status `Review` = czeka na zatwierdzenie użytkownika.

## Poza zakresem

Jakiekolwiek zmiany w Unity (sceny, materiały, ustawienia). Ta faza produkuje wyłącznie dokument.
