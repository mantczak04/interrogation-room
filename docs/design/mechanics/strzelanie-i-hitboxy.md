# Strzelanie i hitboxy pocisków

**Status:** ✅ Zaimplementowana (hitscan + feedback; trafienia nie mają jeszcze skutku w regułach)
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/Gameplay/Weapons/PlayerWeaponController.cs`, `IShotHitReceiver.cs`, `ShotHitbox.cs`, `ShotTracer.cs`, `ShotImpactEffect.cs`, `WeaponMuzzle.cs`

## Cel

Detektyw fizycznie strzela z pistoletu — docelowo to nośnik Egzekucji ([egzekucja.md](./egzekucja.md)). Strzał musi być rozstrzygany autorytatywnie przez serwer, a widoczny i słyszalny dla wszystkich (tracer, błysk, impact, dźwięk), bo huk wystrzału to jawna informacja społeczna w Rundzie.

## Zasada działania (stan obecny)

### Strzał (klient → serwer)

1. LPM przy `hasWeapon` → `CmdTryFire(kierunek kamery)`.
2. Serwer waliduje: gracz ma broń; wektor skończony i niezerowy; kierunek w stożku `maxAimAngle` (85°) względem przodu ciała (anty „strzał za plecy"); rate-limit `shotInterval` liczony w `NetworkTime` po stronie serwera.
3. Serwer wykonuje hitscan `RaycastAll` z pozycji oczu (`serverCameraHeight` nad transformem, a nie z pozycji raportowanej przez klienta) na `shotRange`, ignorując własne collidery strzelca.

### Rozstrzyganie trafienia

- Najbliższy nie-własny hit wygrywa. Trigger-collider liczy się tylko, jeśli ma w rodzicach `IShotHitReceiver` (dzięki temu hitboxy graczy mogą być triggerami, a np. strefy dźwięku nie łapią kul).
- Jeśli trafiony obiekt ma `IShotHitReceiver`, serwer woła `ReceiveShotServer(ShotHitContext)` z pełnym kontekstem: strzelec (`NetworkIdentity`), collider, punkt, normalna, kierunek.
- `ShotHitbox` (implementacja `IShotHitReceiver`) niesie `ShotHitKind` (`Miss/Surface/Player/Prop`), zlicza `ServerHitCount` i emituje serwerowy event `HitReceivedServer` — **to jest seam, pod który podpina się logika gry** (Egzekucja, niszczenie rekwizytów).

### Feedback (serwer → wszyscy)

- `RpcShowShot(origin, endpoint, normal, hitKind)` na wszystkich klientach: tracer z pozycji lufy (`WeaponMuzzle.Position`), błysk lufy, `ShotImpactEffect` zależny od `hitKind`, dźwięk 3D `PlayClipAtPoint` przy lufie.
- Dzięki temu każdy gracz w pobliżu słyszy i widzi strzał — zgodnie z zasadą, że informacja pochodzi z przestrzeni.

## Autorytet i sieć

W pełni server-authoritative: klient wysyła tylko kierunek; pozycja startowa, trafienie i rate-limit są serwerowe. Znana słabość: pozycja gracza jest klient-autorytatywna ([ruch-gracza.md](./ruch-gracza.md)), więc origin strzału pośrednio kontroluje klient.

## Luki do domknięcia

1. **Skutek trafienia gracza** — `ShotHitbox` na graczu zgłasza event, ale nikt go nie konsumuje. Docelowo: trafienie gracza przez Detektywa = intencja Egzekucji przekazana do `RoundEngine` ([egzekucja.md](./egzekucja.md)). Nie budować osobnego systemu HP — zatwierdzone reguły nie przewidują obrażeń, tylko jedną Egzekucję.
2. **Hitboxy na prefabie gracza** — do weryfikacji w scenie: czy prefab `Player` ma skonfigurowany `ShotHitbox` z `HitKind.Player` (konfiguracja edytorowa, przez Unity MCP).
3. **Egzekucja konsumowana trafieniem, nie strzałem** — Detektyw może oddawać kolejne strzały w świat. Dopiero pierwsze trafienie żywego Podejrzanego jest jedyną Egzekucją i kończy Rundę.
4. **Strzały poza trafieniem gracza** — nie zmieniają wyniku, ale pozostają Hałaśliwymi Incydentami: są widoczne i słyszalne w przestrzeni.

## Kryteria akceptacji

- Trafienie rozstrzyga serwer: klient z podkręconym rate-of-fire nie strzela szybciej niż `shotInterval`.
- Tracer/impact/dźwięk widoczne u strzelca, ofiary i obserwatora (3 instancje).
- Trigger bez `IShotHitReceiver` nie zatrzymuje pocisku; ściana zatrzymuje.
- `ShotHitbox.HitReceivedServer` odpala wyłącznie na serwerze, dokładnie raz na trafienie.
- Dowolna liczba pudeł nie zużywa Egzekucji; pierwsze trafienie żywego Podejrzanego kończy Rundę.
