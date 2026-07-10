# Podnoszenie przedmiotów do ręki

**Status:** ✅ Zaimplementowana (dla pistoletu; brak generalizacji i upuszczania)
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/Gameplay/Weapons/NetworkWeaponPickup.cs`, `PlayerWeaponController.cs` (equip + visual)

## Cel

Gracz podchodzi do leżącego przedmiotu, wciska E i przedmiot ląduje w jego ręce — widoczny dla wszystkich graczy. W MVP jedynym przedmiotem jest pistolet (narzędzie Egzekucji Detektywa), ale mechanika ma być bazą pod ewentualne przyszłe przedmioty.

## Zasada działania (stan obecny)

1. `NetworkWeaponPickup` implementuje `INetworkInteractable`; leży w scenie jako obiekt sieciowy z colliderem.
2. Serwer w `TryInteractServer`: sprawdza flagę `consumed` (ochrona przed podwójnym podniesieniem w tej samej klatce przez dwóch graczy), pobiera `PlayerWeaponController` interaktora i woła `TryEquipWeaponServer()`.
3. `TryEquipWeaponServer` odrzuca, jeśli gracz już ma broń; w przeciwnym razie ustawia `SyncVar hasWeapon = true`.
4. Pickup jest oznaczany `consumed` i niszczony przez `NetworkServer.Destroy` — znika u wszystkich.
5. Hook `OnHasWeaponChanged` na każdym kliencie instancjuje `heldWeaponVisualPrefab` w `weaponSocket` gracza — wszyscy widzą broń w ręce. Late-joiner dostaje poprawny stan przez `OnStartClient → RefreshHeldWeaponVisual(hasWeapon)`.

## Autorytet i sieć

Serwer jest jedynym autorytetem posiadania (`SyncVar` + `[Server]`-only mutacje). Wyścig dwóch graczy o jeden pickup rozstrzyga kolejność komend na serwerze — przegrany po prostu nie dostaje broni. Wzorzec poprawny, zostawić.

## Zasady projektowe dla rozszerzeń

- **Jeden slot ręki.** Gracz trzyma najwyżej jeden przedmiot. Podniesienie drugiego wymaga najpierw upuszczenia (albo jest blokowane — jak dziś).
- **Posiadanie to stan serwera, visual to pochodna.** Nigdy nie spawnować trzymanego przedmiotu jako osobnego obiektu sieciowego; visual jest lokalną pochodną `SyncVar`.
- **Kto może podnieść broń** — dziś każdy. Zatwierdzone reguły nie rozstrzygają, czy Podejrzany może trzymać pistolet (wątek Buntu jest nierozstrzygnięty). Do decyzji użytkownika przed slice'em; najprostszy wariant MVP: pistolet podnosi wyłącznie Detektyw.

## Luki do domknięcia

1. **Upuszczanie / odkładanie (G)** — brak. Jeśli potrzebne: serwer ustawia `hasWeapon = false` i spawnuje nowy pickup przed graczem (odwrotność podniesienia). Potrzebne dopiero, gdy pojawi się powód designowy.
2. **Generalizacja na inne przedmioty** — obecnie equip jest zaszyty w `PlayerWeaponController`. Przy drugim typie przedmiotu wydzielić `PlayerInventory`/slot ręki; **nie robić tego na zapas** przy jednym przedmiocie.
3. **Widoczność broni w FPP** — patrz luka nr 2 w [ruch-gracza.md](./ruch-gracza.md).
4. **Respawn pickupu** — pickup znika bezpowrotnie. Na potrzeby wielu Rund w jednej sesji spawn pickupów musi być częścią resetu Rundy ([silnik-rundy.md](./silnik-rundy.md)).

## Kryteria akceptacji

- Dwóch graczy naraz wciska E na tym samym pickupie → dokładnie jeden dostaje broń, pickup znika u obu.
- Gracz dołączający po podniesieniu widzi broń w ręce właściciela.
- Gracz z bronią nie może podnieść drugiej.
