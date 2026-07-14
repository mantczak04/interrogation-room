# Egzekucja i stan po eliminacji

**Status:** 🔶 Reguła domenowa oraz fundament pistoletu działają; spięcie roli, trafienia i zakończenia Rundy pozostaje do implementacji
**Priorytet:** Must-have (MVP)
**Docelowy kod:** rozstrzygnięcie w `RoundEngine`; intencja z pistoletu przez `NetworkRoundCoordinator`; efekty w scenie

## Cel

Jedyny, nieodwracalny wybór Detektywa: wskazanie Podejrzanego do eliminacji, natychmiast kończące Rundę (ADR-0003). Egzekucja Winnego = wygrana Detektywa; Egzekucja Niewinnego = przegrana. To kulminacja całej Rundy — musi być czytelna, dramatyczna i niemożliwa do cofnięcia lub powtórzenia.

## Zasada działania

### Egzekucja przez strzał z pistoletu (zatwierdzona)

Skoro istnieje już sieciowy pistolet z serwerowymi hitboxami ([strzelanie-i-hitboxy.md](./strzelanie-i-hitboxy.md)), naturalna forma Egzekucji to fizyczny strzał, a nie przycisk w menu:

1. Detektyw ma pistolet od początku Rundy. Podejrzani nie mogą go odebrać, podnieść ani użyć.
2. Serwerowy event `ShotHitbox.HitReceivedServer` na graczu, gdy strzelcem jest Detektyw, tłumaczony jest przez `NetworkRoundCoordinator` na `RoundCommand.Execute(trafiony PlayerId)`.
3. `RoundEngine` rozstrzyga wynik i kończy Rundę; koordynator rozsyła finalne widoki; scena odtwarza efekt eliminacji.
4. Strzał, który nie trafia żywego Podejrzanego, **nie** konsumuje Egzekucji. Detektyw może strzelać dalej, aż pierwszy raz trafi gracza.
5. Pierwsze trafienie żywego Podejrzanego jest jedyną Egzekucją i natychmiast kończy Rundę.

### Reguły (egzekwowane w `RoundEngine`)

- Dokładnie jedna Egzekucja na Rundę; kolejne `Execute` odrzucane.
- `Execute` przyjmowane wyłącznie w fazie Rundy (nie w Przygotowaniu) i wyłącznie od Detektywa.
- Celem może być tylko żywy Podejrzany (nie Detektyw, nie sam strzelec).
- Wynik: cel Winny → wygrana Detektywa; cel Niewinny → przegrana Detektywa i śmierć Niewinnego (dla jego indywidualnego wyniku: brak Przetrwania).
- Niewinny wygrywa tylko po ukończeniu własnego Prywatnego Celu i Przetrwaniu. Właściciel Sekretnego Celu potrzebuje dodatkowo ukończonego Wrobienia i Egzekucji swojego Celu.
- Skuteczna Ucieczka jest drugim możliwym końcem Rundy i blokuje późniejszą Egzekucję. Jeżeli Detektyw zobaczy finałową próbę, może ją zakończyć trafieniem Winnego przed ukończeniem akcji.

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
- Podejrzany nie może otrzymać ani użyć pistoletu; niezależnie od klienta serwer waliduje rolę właściciela strzału.
- Dwóch szybkich trafień w jednej klatce → dokładnie jedna Egzekucja.
- Dowolna liczba strzałów w świat nie kończy Rundy; pierwsze trafienie żywego Podejrzanego kończy ją zawsze.

## Otwarte pytanie prezentacyjne

- Forma efektu śmierci (ragdoll vs animacja) — czysto prezentacyjna, nie blokuje reguły.
