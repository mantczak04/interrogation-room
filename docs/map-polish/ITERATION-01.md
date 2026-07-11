# Iteration 01 — przestrzeń i ergonomia

## Krytyka stanu wejściowego

1. Pokój Socjalny i Archiwum nie miały drożnej trasy od drzwi do przeciwległej ściany; test kapsułą trafiał odpowiednio w cztery i cztery przeszkody.
2. Biurko Archiwum wchodziło w podejście do drzwi, a krzesło i stół blokowały trasę w Pokoju Przesłuchań.
3. Drzwi o szerokości 1,2 m nie zapewniały wymaganych 0,25 m zapasu po obu stronach kapsuły o promieniu 0,45 m.
4. Południowe pokoje o głębokości 3,5 m nie mieściły swobodnie 2–3 graczy z istniejącym umeblowaniem.
5. Otwarta skrzydła drzwi i pudła zasłaniały wejścia oraz pogarszały czytelność pomieszczeń z korytarza.

## Zmiany

- Przesunięto południową ścianę z `z = -6,3` do `z = -7,6`; Pokój Socjalny i Archiwum mają teraz wymiary stref `4,0 × 4,8 m`.
- Cały obrys strukturalny ma `17,6 × 17,3 m`, więc pozostaje poniżej limitu `20 × 18 m`.
- Poszerzono cztery otwory drzwiowe do `1,5 m`; przebudowano segmenty ścian, nadproża i otwarte skrzydła.
- Ustawiono umeblowanie południowych pokoi według zmierzonych granic rendererów. Zachowano wyposażenie, a środek każdego pokoju pozostawiono na przejście i rozmowę.
- Przesunięto zestaw stołu w Pokoju Przesłuchań, aby zachować wolne podejście oraz trasę po zachodniej stronie.
- Zaktualizowano rozmiar i pozycję obu południowych `Strefa_*`.

## Dowody

- `OverlapCapsule`, `r = 0,45`, `h = 2,0`: 6/6 punktów spawnu bez trafień.
- `CapsuleCastAll`: 4/4 drzwi bez trafień dla `r = 0,45` oraz dodatkowo dla `r = 0,70`.
- Korytarz: trasa koniec–koniec bez trafień.
- Pomieszczenia: 7/7 odcinków zapisanych tras bez trafień.
- Wolna przestrzeń rozmowy `1,5 × 1,5 × 2,0 m`: Pokój Socjalny i Archiwum bez trafień.
- Strefy: dokładnie 5; wszystkie `BoxCollider.isTrigger == true`; granice południowych stref `4,0 × 3,0 × 4,8 m`.
- Console: wyłącznie znany, wcześniejszy szum Vivox `Callback dispatcher is not initialized`.
- Scena zapisana przez Unity MCP: `Assets/Scenes/Room.unity`.

Zrzuty przed zmianą znajdują się w `docs/map-polish/screenshots/baseline/`, a po zmianie w `docs/map-polish/screenshots/iteration-01/`.
