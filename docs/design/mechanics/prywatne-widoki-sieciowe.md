# Prywatne widoki sieciowe (`NetworkRoundCoordinator` + `PlayerRoundView`)

**Status:** ✅ Bazowy `NetworkRoundCoordinator`, wiadomości i `PlayerRoundView` zaimplementowane; rozszerzenie oraz test end-to-end pozostają do wykonania
**Priorytet:** Must-have (MVP) — krok 3 kolejności implementacji
**Docelowy kod:** `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs`, `RoundMessages.cs`

## Cel

Jedyny most między światem Mirror a domeną: mapuje połączenia na `PlayerId`, przekazuje intencje graczy do `RoundEngine` i rozsyła każdemu klientowi **wyłącznie jego** `PlayerRoundView`. To techniczne serce tajności (ADR-0011): sekret, który nigdy nie został wysłany, nie może wyciec.

## Zasada działania

### Odpowiedzialności

1. **Mapowanie tożsamości**: `NetworkConnectionToClient` ↔ `PlayerId` (jedyne miejsce w kodzie z tą wiedzą). Docelowo `PlayerId` pochodny od SteamID, w KCP — od connectionId.
2. **Wejście**: odbiera komendy klientów (intencja Egzekucji, gotowość), weryfikuje nadawcę (np. Egzekucję może zgłosić tylko połączenie zmapowane na Detektywa) i dopiero wtedy woła `RoundEngine.Handle`.
3. **Wyjście**: po każdej `RoundTransition` woła `ViewFor(player)` dla każdego gracza i wysyła wynik **celowaną wiadomością / `TargetRpc`** — nigdy broadcastem, nigdy `SyncVar`.
4. **Czas**: prowadzi serwerowy zegar Limitu Rundy i wstrzykuje `TimeExpired` do silnika ([limit-rundy.md](./limit-rundy.md)); rozsyła klientom tylko pozostały czas (do renderu), nie autorytet.
5. **Ponowne dostarczenie**: po dołączeniu/reconnect klienta wysyła mu aktualny widok. Ponieważ widok jest liczony na bieżąco przez `ViewFor`, reconnect po Przygotowaniu **nie** odzyska Alibi.

### Czego koordynator NIE robi

- Nie przydziela ról, nie redaguje Alibi, nie rozstrzyga zwycięstwa — to `RoundEngine`.
- Nie renderuje niczego — to `RoundPresenter`.

### Format wiadomości (`RoundMessages.cs`)

- `RoundViewMessage { PlayerRoundView view }` — serwer → konkretny klient.
- `RoundIntentMessage` (start, koniec Przygotowania, Egzekucja z celem) — klient → serwer.
- Widok bazowy zawiera: fazę, rolę odbiorcy, kto jest Detektywem, jawne Przestępstwo, wersję Alibi (tylko w Przygotowaniu), pozostały czas i wynik po zakończeniu.
- Rozszerzony widok Podejrzanego zawiera wyłącznie jego Prywatny Cel, aktualny krok, własny postęp oraz — dla Winnego — zdobyte Tropy i dozwolone dane Planu Ucieczki. Widok Detektywa zawiera jego Rejestr Incydentów. Cudze Cele, prawdziwi autorzy Incydentów i ukryty postęp nigdy nie są wysyłane przed końcem Rundy.

## Zasady bezpieczeństwa (twarde)

1. **Zero sekretów w stanie globalnym** — żadnych ról/Alibi w `SyncVar`, `SyncList`, nazwach obiektów ani komponentach widocznych u wszystkich.
2. **Walidacja nadawcy każdej intencji** po stronie serwera (rola + faza), niezależnie od blokad UI.
3. **Widok liczony przy wysyłce**, nie cachowany per klient — jedna ścieżka prawdy (`ViewFor`).

## Zależności

- `RoundEngine` (domena), `SteamLobby`/`NetworkManager` (skład graczy przy starcie), `RoundPresenter` (konsument widoków), pistolet jako źródło intencji Egzekucji ([egzekucja.md](./egzekucja.md)).

## Kryteria akceptacji / testy

- Host + 2 klientów (KCP/ParrelSync): każdy klient dostaje inny, poprawny widok; sniffowanie ruchu innego klienta niczego nie ujawnia, bo wiadomości są celowane.
- Egzekucja wysłana przez klienta-Niewinnego jest odrzucona serwerowo.
- Reconnect w fazie Rundy dostaje aktualny widok bez Alibi.
- Late-join do trwającej Rundy: MVP — odrzucany do lobby (obserwator poza zakresem).
- Klienci nie dostają cudzych Prywatnych Celów, postępu, autorów Incydentów, Tropów ani stanu przygotowania Ucieczki przed ujawnieniem po Rundzie.
