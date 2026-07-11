# Sieć Steam: Steamworks.NET + FizzySteamworks nad Mirror

Ten dokument opisuje zaimplementowaną integrację multiplayer przez Steam. Decyzja: [ADR-0012](../adr/0012-steam-lobby-with-runtime-transport-fallback.md).

## Stos

- `com.rlabrecque.steamworks.net` (Steamworks.NET) — binding C# do Steamworks API.
- `com.mirror.steamworks.net` (FizzySteamworks) — transport Mirror przesyłający ruch przez sieć Steam; kod transportu leży w `Assets/Mirror/Transports/FizzySteamworks/`.
- KCP pozostaje transportem lokalnym dla ParrelSync zgodnie z kolejnością implementacji w [MVP-ARCHITECTURE.md](./MVP-ARCHITECTURE.md).
- `steam_appid.txt` w katalogu projektu zawiera `480` (testowy AppID „Spacewar”). Przed wydaniem trzeba go zastąpić własnym AppID.

## Komponenty w scenie `Room.unity`

- **`SteamManager`** (osobny GameObject, `Assets/Scripts/SteamManager.cs`) — inicjalizuje i zamyka SteamAPI, pompuje callbacki. Ma `[DefaultExecutionOrder(-2000)]`, żeby SteamAPI było gotowe przed wyborem transportu.
- **`SteamLobby`** (na obiekcie `NetworkManager`, `Assets/Scripts/SteamLobby.cs`) — całe klejenie lobby i wybór transportu. Ma `[DefaultExecutionOrder(-1000)]`: w `Awake` ustawia `NetworkManager.transport` na `steamTransport` (FizzySteamworks), gdy klient Steam działa i pole `useSteamWhenAvailable` jest włączone; w przeciwnym razie na `localTransport` (KCP). Wybór musi zapaść przed `Awake` NetworkManagera (kolejność 0), bo Mirror zapamiętuje tam `Transport.active`.
- **`FizzySteamworks`** (na obiekcie `NetworkManager`) — pracuje w trybie SteamSockets (`UseNextGenSteamNetworking`), zawsze przez relay Valve, więc gracze nie widzą swoich adresów IP i nie potrzebują przekierowania portów.
- **`CenteredNetworkManagerHUD`** — w trybie Steam pokazuje „Host Steam Lobby (Friends)” oraz „Invite Friends” (nakładka Steam); bez Steam pokazuje dotychczasowe UI adresu IP i portu dla KCP.

## Przepływy

- **Hostowanie:** `SteamLobby.HostLobby()` → `SteamMatchmaking.CreateLobby` (lobby tylko dla znajomych, rozmiar z `maxConnections`) → `OnLobbyCreated` zapisuje SteamID hosta w danych lobby pod kluczem `HostAddress` i woła `StartHost()`.
- **Dołączanie przez nakładkę:** znajomy wybiera „Dołącz do gry” → callback `GameLobbyJoinRequested_t` → `JoinLobby` → `OnLobbyEntered` czyta `HostAddress`, ustawia `networkAddress` i woła `StartClient()`.
- **Zaproszenie przy wyłączonej grze:** Steam uruchamia grę z argumentem `+connect_lobby <id>`; `SteamLobby.Start()` czyta argument i dołącza do lobby.
- **Rozłączenie:** przyciski Stop w HUD oraz `OnDestroy` opuszczają lobby (`LeaveLobby`).

## Rozwój lokalny i testy

- Bez uruchomionego klienta Steam `SteamAPI_Init()` nie przechodzi i gra automatycznie spada na KCP — ParrelSync działa bez żadnej konfiguracji. Można też ręcznie wyłączyć pole `useSteamWhenAvailable` na komponencie `SteamLobby`.
- Build lub Editor uruchomiony z argumentem `-force-kcp` zawsze wybiera KCP, nawet gdy klient Steam działa. To zalecany tryb dla powtarzalnych lokalnych testów i buildów QA.
- Prawdziwy test Steam wymaga dwóch maszyn z dwoma kontami Steam, obu na AppID 480, z uruchomionym klientem Steam — zgodnie z regułą testowania FizzySteamworks w `AGENTS.md`.
- Dwie instancje na jednym koncie/komputerze nie przetestują ścieżki Steam; do testów lokalnych służy KCP.
