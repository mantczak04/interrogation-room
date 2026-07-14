# Lobby i sieć (Steam + KCP)

**Status:** ✅ Zaimplementowana (fundament); brak przepływu startu Rundy
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/SteamManager.cs`, `SteamLobby.cs`, `CenteredNetworkManagerHUD.cs`
**Architektura:** [STEAM-NETWORKING.md](../../architecture/STEAM-NETWORKING.md), ADR-0012

## Cel

Gracze (3–6 znajomych) łączą się przez lobby Steam bez konfiguracji sieci; deweloper testuje lokalnie na KCP + ParrelSync bez Steama.

## Zasada działania (stan obecny — szczegóły w STEAM-NETWORKING.md)

- `SteamManager` inicjalizuje SteamAPI przed wszystkim (`DefaultExecutionOrder(-2000)`).
- `SteamLobby` wybiera transport w `Awake` (Fizzy gdy Steam działa i `useSteamWhenAvailable`, inaczej KCP) — przed `Awake` NetworkManagera.
- Hostowanie: `CreateLobby` (friends-only, rozmiar z `maxConnections`) → `HostAddress` w danych lobby → `StartHost()`.
- Dołączanie: nakładka Steam („Dołącz do gry") lub `+connect_lobby <id>` przy starcie gry.
- FizzySteamworks w trybie SteamSockets — zawsze przez relay Valve, bez ujawniania IP.
- HUD: „Host Steam Lobby (Friends)" / „Invite Friends" albo IP+port dla KCP.

## Luki do domknięcia (pod Rundę)

1. **Przepływ startu Rundy z lobby** — po połączeniu graczy nie ma żadnego kroku „zacznij grę": host musi dostać przycisk Start (aktywny przy 3–6 graczach), który buduje skład i woła `StartRound` przez `NetworkRoundCoordinator` ([prywatne-widoki-sieciowe.md](./prywatne-widoki-sieciowe.md)).
2. **Tożsamość gracza** — mapowanie na `PlayerId` (SteamID / connectionId) i nick (persona Steam / fallback) do listy graczy w lobby i ekranu wyników.
3. **Blokada late-join podczas Rundy** — MVP: odrzucenie do lobby / komunikat; dziś nowy klient po prostu się spawnuje.
4. **Powrót do lobby po Rundzie** — reset stanu sceny (pickupy, pozycje graczy, drzwi) bez restartu procesu; patrz reset Rundy w [silnik-rundy.md](./silnik-rundy.md).
5. **AppID produkcyjny** — `steam_appid.txt` = 480 (Spacewar); wymiana przed wydaniem.

## Kryteria akceptacji

- Dwie maszyny/konta Steam: host + invite przez nakładkę działa end-to-end (reguła testowa z AGENTS.md).
- ParrelSync na KCP działa bez Steama i bez zmian konfiguracji.
- Runda nie startuje przy <3 graczach; siódmy gracz nie wejdzie do lobby.
