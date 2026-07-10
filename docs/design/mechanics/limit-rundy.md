# Limit Rundy (wspólny timer)

**Status:** ❌ Do zaimplementowania
**Priorytet:** Must-have (MVP)
**Docelowy kod:** logika w `RoundEngine` (rozstrzygnięcie) + `NetworkRoundCoordinator` (zegar) + HUD (render)

## Cel

Jeden wspólny budżet czasu na całą Rundę (ADR-0004). Detektyw sam gospodaruje czasem przesłuchań; upływ Limitu Rundy bez Egzekucji natychmiast kończy Rundę przegraną Detektywa. Brak turowych timerów, brak pauz.

## Zasada działania

### Podział odpowiedzialności

1. **`RoundEngine`** nie zna zegara. Zna tylko: skonfigurowaną długość Limitu (z konfiguracji Rundy) i komendę `TimeExpired`, na którą odpowiada przejściem do stanu Zakończona z wynikiem „przegrana Detektywa" (o ile Runda trwa).
2. **`NetworkRoundCoordinator`** prowadzi autorytatywny zegar na serwerze: zapamiętuje `NetworkTime.time` w chwili `EndPreparation` i gdy `elapsed >= limit`, wstrzykuje `TimeExpired`.
3. **Klienci** dostają w `PlayerRoundView` znacznik końca (np. `endsAtNetworkTime`), a HUD sam odlicza lokalnie od `NetworkTime.time` — bez wysyłania ticków co sekundę.

### Reguły

- Limit startuje w chwili wejścia w fazę Rundy (koniec Przygotowania), nie w Lobby.
- Przygotowanie ma osobny, krótki czas (parametr konfiguracyjny) — nie konsumuje Limitu Rundy.
- Egzekucja zatrzymuje zegar definitywnie (Runda kończy się natychmiast, ADR-0003).
- Wyścig „Egzekucja vs upływ czasu" rozstrzyga kolejność komend na serwerze — `RoundEngine` przyjmie pierwszą, drugą odrzuci; obie ścieżki są poprawne i deterministyczne.
- Brak pauzy w MVP (rozłączenia nie zatrzymują zegara).

### Konfiguracja

- Długość Limitu Rundy: parametr konfiguracji Rundy ustawiany przez hosta (rozsądny default do playtestu: 15–20 min dla 5 graczy; do strojenia).
- Długość Przygotowania: osobny parametr (default 60–120 s).

## Widoczność

- Pozostały czas jest **jawny dla wszystkich** (element presji społecznej) — wisi w HUD każdego gracza.
- Opcjonalnie (nice-to-have): sygnał dźwiękowy w przestrzeni przy ostatniej minucie.

## Kryteria akceptacji / testy

- Test domenowy: `TimeExpired` w fazie Rundy → Zakończona, przegrana Detektywa; `TimeExpired` po Egzekucji → odrzucone.
- Test sieciowy: dwaj klienci widzą odliczanie zgodne w ~±0,5 s (dzięki `NetworkTime`).
- Zmiana FPS/hitchy na hoście nie kumulują błędu (porównanie do znacznika czasu, nie inkrementacja delta).
