# Egzekucja i stan po eliminacji

**Status:** ❌ Do zaimplementowania (fundament — pistolet i hitboxy — już działa)
**Priorytet:** Must-have (MVP)
**Docelowy kod:** rozstrzygnięcie w `RoundEngine`; intencja z pistoletu przez `NetworkRoundCoordinator`; efekty w scenie

## Cel

Jedyny, nieodwracalny wybór Detektywa: wskazanie Podejrzanego do eliminacji, natychmiast kończące Rundę (ADR-0003). Egzekucja Winnego = wygrana Detektywa; Egzekucja Niewinnego = przegrana. To kulminacja całej Rundy — musi być czytelna, dramatyczna i niemożliwa do cofnięcia lub powtórzenia.

## Zasada działania

### Rekomendacja: Egzekucja przez strzał z pistoletu (diegetyczna)

Skoro istnieje już sieciowy pistolet z serwerowymi hitboxami ([strzelanie-i-hitboxy.md](./strzelanie-i-hitboxy.md)), naturalna forma Egzekucji to fizyczny strzał, a nie przycisk w menu:

1. Pistolet może podnieść wyłącznie Detektyw (walidacja roli w `TryEquipWeaponServer` przez koordynatora).
2. Serwerowy event `ShotHitbox.HitReceivedServer` na graczu, gdy strzelcem jest Detektyw, tłumaczony jest przez `NetworkRoundCoordinator` na `RoundCommand.Execute(trafiony PlayerId)`.
3. `RoundEngine` rozstrzyga wynik i kończy Rundę; koordynator rozsyła finalne widoki; scena odtwarza efekt eliminacji.
4. Strzał, który nie trafia gracza (ściana, pudło), **nie** konsumuje Egzekucji — jest tylko hukiem. Alternatywa „jedna kula = jedna Egzekucja, pudło przepada" jest ostrzejsza designersko; **do decyzji użytkownika**, MVP: pudło nie konsumuje.

Wariant zapasowy z MVP-ARCHITECTURE (wybór celu z panelu UI) pozostaje najtańszym fallbackiem, gdyby diegetyczna wersja opóźniała slice — oba warianty kończą się tą samą komendą `Execute`, więc wybór nie zmienia domeny.

### Reguły (egzekwowane w `RoundEngine`)

- Dokładnie jedna Egzekucja na Rundę; kolejne `Execute` odrzucane.
- `Execute` przyjmowane wyłącznie w fazie Rundy (nie w Przygotowaniu) i wyłącznie od Detektywa.
- Celem może być tylko żywy Podejrzany (nie Detektyw, nie sam strzelec).
- Wynik: cel Winny → wygrana Detektywa; cel Niewinny → przegrana Detektywa i śmierć Niewinnego (dla jego indywidualnego wyniku: brak Przetrwania).
- Rozstrzygnięcie Sekretnych Celów (jeśli włączone): eliminacja Celu przez Egzekucję spełnia warunek eliminacji dla właściciela Sekretnego Celu, o ile właściciel przetrwał.

### Stan po eliminacji

Runda kończy się natychmiast, więc MVP **nie potrzebuje** trybu ducha/obserwatora:

- Zamrożenie sterowania wszystkim graczom (bramka stanu z [ruch-gracza.md](./ruch-gracza.md)).
- Efekt na ofierze: ragdoll/animacja upadku (nice-to-have; MVP może być proste przewrócenie modelu + dźwięk).
- Po krótkiej pauzie dramaturgicznej (2–3 s) ekran wyników ([ui-rundy.md](./ui-rundy.md)).

## Zależności

- `RoundEngine` (rozstrzygnięcie), `NetworkRoundCoordinator` (tłumaczenie trafienia na komendę), pistolet + `ShotHitbox` na prefabie gracza, role ([role-i-sklad-rundy.md](./role-i-sklad-rundy.md)).

## Kryteria akceptacji / testy

- Domena: testy 5–7 z listy `RoundEngine` (wygrana/przegrana/odrzucenie drugiej Egzekucji).
- Sieć: strzał Detektywa w Podejrzanego u 3 klientów → wszyscy natychmiast widzą koniec Rundy i spójny wynik.
- Strzał gracza bez roli Detektywa (gdyby zdobył broń) nie wywołuje Egzekucji — walidacja roli po stronie serwera.
- Dwóch szybkich trafień w jednej klatce → dokładnie jedna Egzekucja.

## Otwarte pytania

- Czy pudło konsumuje Egzekucję (patrz wyżej).
- Czy Podejrzani mogą w ogóle trzymać broń (wątek Buntu — nierozstrzygnięty, poza MVP).
- Forma efektu śmierci (ragdoll vs animacja) — czysto prezentacyjna, nie blokuje.
