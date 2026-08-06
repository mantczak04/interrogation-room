# Audyt kierunku repozytorium — 2026-07-28

## Zakres i metoda

- Przeczytano **68 pierwszorzędnych dokumentów Markdown / 5339 linii**: `AGENTS.md`, `CLAUDE.md`, `CONTEXT.md`, `REFACTOR-PLAN.md` oraz wszystkie `*.md` pod `docs/` z wyłączeniem zabronionego `docs/map-polish/screenshots/`.
- Po audycie dokumentów wykonano ograniczony source-check w `Assets/Scripts/`, `Assets/Editor/` i wybranych testach, wyłącznie dla ważnych twierdzeń o statusie i roadmapie. Nie używano Unity Editora i nie inspekcjonowano scen/prefabów jako źródła prawdy.
- Priorytety:
  - **P0** — dokumentacja może skierować implementację przeciw zatwierdzonym regułom albo kazać powtórzyć zakończoną pracę;
  - **P1** — blokuje wiarygodne planowanie, playtest lub utrzymanie;
  - **P2** — dług dokumentacyjny o dużym koszcie nawigacji, ale bez natychmiastowego konfliktu produktu.
- Granica dowodowa: source-check potwierdza istnienie kontraktów i implementacji w kodzie, ale bez Unity Editora nie potwierdza aktualnego wiring sceny, poprawności assetów, przejścia testów ani zachowania builda.

## Brief wykonawczy

Największym problemem repozytorium nie jest brak pomysłów, lecz brak jednego aktualnego obrazu produktu. Kanon w `AGENTS.md` i `CONTEXT.md`, ADR-y, specyfikacje mechanik, brief Fable, plany grafiki, raporty playtestów i `REFACTOR-PLAN.md` opisują różne momenty rozwoju jako stan bieżący. Skutki są konkretne:

1. dokumenty nadal ograniczają część logiki do 6 graczy, mimo kanonicznego i zaimplementowanego zakresu 3–8;
2. brief Fable ustala parametry, które są już w kodzie, ale `AGENTS.md` nadal nazywa nierozstrzygniętym strojeniem;
3. overview i architektura deklarują brak systemów, które istnieją w domenie, runtime i testach;
4. `REFACTOR-PLAN.md` zleca refaktory i decyzje już zakończone;
5. content ma jednocześnie kontrakt 6 i 8 punktów Alibi, roadmapę 4/15 oraz 18/15 definicji w źródłach;
6. repozytoryjna instrukcja bezpiecznego buildera MainMenu wskazuje legacy uGUI narzędzie, które może nadpisać aktualną scenę UI Toolkit.

**Rekomendowany kierunek, bez rozszerzania mapy:** najpierw zamknąć kontrakt produktu i aktualny status, potem wykonać pełny wieloklientowy gate Rundy oraz voice, a dopiero następnie weryfikować i pogłębiać istniejący content. Dokumenty same zabraniają powiększania mapy przed danymi z playtestu (`docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:49`, `docs/design/mechanics/prywatne-cele-incydenty-i-ucieczka.md:190-197`).

## Aktualny stan produktu potwierdzony w źródłach

1. **Rdzeń Rundy jest zaimplementowany, nie planowany.** `RoundEngine` obsługuje skład 3–8, role, redagowane Alibi, gotowość, Egzekucję, Limit Rundy, `Prywatne Cele`, Incydenty, Tropy do Alibi, przygotowanie/przerwanie/ukończenie Ucieczki oraz końcowe wyniki i ujawnienie (`Assets/Scripts/Game/Domain/RoundEngine.cs:16-18`, `Assets/Scripts/Game/Domain/RoundEngine.cs:45-74`, `Assets/Scripts/Game/Domain/RoundEngine.cs:154-326`, `Assets/Scripts/Game/Domain/RoundEngine.cs:349-597`, `Assets/Scripts/Game/Domain/RoundEngine.cs:798-855`).
2. **Mirror adapter i fizyczny vertical slice istnieją.** `NetworkRoundCoordinator` ma lobby readiness i ustawienia Rundy, prywatne targeted views, adaptery fizycznych Celów/Incydentów/Tropów/Ucieczki, timer oraz Egzekucję przez trafienie z autoryzowanej broni (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:125-137`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:179-244`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:465-600`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:849-904`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1108-1153`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1259-1321`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1404-1427`).
3. **Authoring contentu przekroczył stare roadmapy.** Generator zawiera C01–C18, mimo nieaktualnego logu „C01 through C15”; istnieje OS-01–OS-15, cztery warianty `Wrobienia`, trzy narracyjne Plany Ucieczki współdzielące jeden kontrakt fizyczny oraz cztery Easter Eggi (`Assets/Scripts/Editor/Content/CaseAssetSync.cs:19-25`, `Assets/Scripts/Editor/Content/CaseAssetSync.cs:434-513`, `Assets/Scripts/Editor/Content/PersonalMatterAssetSync.cs:19-38`, `Assets/Scripts/Game/Domain/PrivateObjectiveDefinition.cs:189-235`, `Assets/Scripts/Game/Domain/EscapePlanDefinition.cs:182-225`, `Assets/Scripts/Game/EasterEggs/InitialEasterEggCatalog.cs:11-38`). Source-only audit nie dowodzi, ile z tych definicji jest rzeczywiście podpiętych w scenie.
4. **UI Toolkit jest obecnym standardem runtime.** Main menu, menu sieci, ustawienia i UI Rundy działają jako `UIDocument`; wyjątkiem jest developerski `RoundDeveloperPanel`, który nadal używa IMGUI (`Assets/Scripts/UI/MainMenuPresenter.cs:12-18`, `Assets/Scripts/UI/CenteredNetworkManagerHUD.cs:17-27`, `Assets/Scripts/UI/CenteredNetworkManagerHUD.cs:263-279`, `Assets/Scripts/UI/SettingsMenu.cs:12-25`, `Assets/Scripts/UI/SettingsMenu.cs:150-165`, `Assets/Scripts/Game/UI/RoundPresenter.cs:124-133`, `Assets/Scripts/Game/Networking/RoundDeveloperPanel.cs:62-117`).
5. **Steam/KCP i Vivox są implementacją, nie tylko decyzją.** `SteamLobby` wybiera FizzySteamworks lub KCP, tworzy/dołącza lobby i obsługuje zaproszenia; `VivoxVoiceRuntime` loguje do usługi, przełącza globalny/spatial channel i tworzy przestrzenne participant taps z okluzją (`Assets/Scripts/Steam/SteamLobby.cs:16-35`, `Assets/Scripts/Steam/SteamLobby.cs:53-66`, `Assets/Scripts/Steam/SteamLobby.cs:125-135`, `Assets/Scripts/Steam/SteamLobby.cs:240-298`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs:172-245`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs:315-353`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs:488-567`). Operacyjna konfiguracja i realna akceptacja akustyczna nadal nie są dowiedzione.
6. **Warstwa gracza jest grywalnym prototypem, nie pustym placeholderem.** Jest pięć postaci, chodzenie/skok/sprint, first/third person, siedzenie, taniec, cios, śmierć oraz autoryzowana broń z serverowym rozstrzyganiem strzału (`Assets/Scripts/Game/Characters/CharacterId.cs:3-10`, `Assets/Scripts/Gameplay/PlayerController.cs:31-72`, `Assets/Scripts/Gameplay/PlayerController.cs:259-331`, `Assets/Scripts/Gameplay/PlayerController.cs:637-728`, `Assets/Scripts/Gameplay/PlayerController.cs:730-789`, `Assets/Scripts/Gameplay/Weapons/PlayerWeaponController.cs:98-178`, `Assets/Scripts/Gameplay/Weapons/PlayerWeaponController.cs:180-252`).
7. **Automatyzacja jest szeroka, lecz jej bieżący wynik nie został sprawdzony w tym audycie.** Testy obejmują m.in. reguły Rundy, content Celów/Ucieczki, serializację widoków, UI, voice oraz PlayMode fizycznych interakcji; audyt nie uruchamiał Unity ani test runnera (`Assets/Tests/EditMode/RoundEngineTests.cs:82-1167`, `Assets/Tests/EditMode/ObjectiveContentDefinitionTests.cs:7-59`, `Assets/Tests/PlayMode/TimedInteractionRuntimeTests.cs:1-161`, `Assets/Tests/PlayMode/NetworkRoomsAndDoorsTests.cs:1-179`).

**Wniosek o dojrzałości:** repozytorium ma zaawansowany, testowany kodem vertical slice i narzędzia developerskie, ale nie ma udokumentowanego dowodu release-ready: pełnego real-client gate’u KCP, testu Steam na dwóch maszynach, potwierdzonej konfiguracji Vivox ani audytu wiring wszystkich treści.

---

## P0 — naprawić przed kolejnym dużym zadaniem

### P0.1 — Zakres 3–8 graczy nie jest spójny w specyfikacjach i testach

**Problem**

Kanon mówi 3–8 graczy i 1–6 `Niewinny`, ale specyfikacja ról nadal każe odrzucać 7 graczy i waliduje maksimum 6. Reguła `Sekretnego Celu` jest kanonicznie opisana dla 5–8, lecz ADR i kilka dokumentów ogranicza ją do 5–6.

**Dowody dokumentacyjne**

- Kanon: `AGENTS.md:31-41`, `CONTEXT.md:24-25`.
- Błędne maksimum 6 i test odrzucający 7: `docs/design/mechanics/role-i-sklad-rundy.md:13-17`, `docs/design/mechanics/role-i-sklad-rundy.md:48-55`.
- ADR nadal opisuje tylko 5–6: `docs/adr/0013-private-goals-and-emergent-rebellion.md:3-7`.
- To samo ograniczenie powtarza specyfikacja rozszerzenia: `docs/design/mechanics/prywatne-cele-incydenty-i-ucieczka.md:29-45`, `docs/design/mechanics/prywatne-cele-incydenty-i-ucieczka.md:168-172`.
- Overview oraz sandbox również utrwalają 5–6: `docs/design/MECHANICS-OVERVIEW.md:60-71`, `docs/playtests/A7-DEVELOPER-SANDBOX.md:28-34`.

**Ryzyko**

Nowe testy lub migracja domeny mogą ponownie wprowadzić limit 6, a 7–8 graczy pozostaje bez opisanego przydziału `Sekretnego Celu` i bez jawnej macierzy akceptacji.

**Następna zmiana**

Jednym patchem decyzyjnym:

1. poprawić `ADR-0013` albo dodać ADR zastępujący jego zakres liczebności;
2. zaktualizować wszystkie specyfikacje do 3–8 i 5–8;
3. dodać jawne przypadki 7 i 8 graczy do kryteriów akceptacji;
4. zapisać, czy dla 7–8 nadal domyślnie występuje dokładnie jeden `Sekretny Cel` — `AGENTS.md` mówi, że tak.

**Source-check:** kod jest zgodny z kanonem, nie z tymi specyfikacjami: `RoundEngine` ma `MinPlayers = 3`, `MaxPlayers = 8`, próg `Sekretnego Celu = 5`, a od pięciu graczy domyślnie dokładnie jeden `Sekretny Cel` (`Assets/Scripts/Game/Domain/RoundEngine.cs:16-18`, `Assets/Scripts/Game/Domain/RoundEngine.cs:161-165`, `Assets/Scripts/Game/Domain/RoundEngine.cs:217-235`). To jest potwierdzony błąd dokumentacji.

### P0.2 — Brief Fable konkuruje z repozytoryjnym kanonem

**Problem**

`FABLE-PLAYTEST-IMPROVEMENTS.md` ogłasza cały zakres jako obowiązkowy i ustanawia dokładne parametry: maks. 30 sekund Przygotowania, dokładnie 5 punktów Alibi oraz grywalność wszystkich 15 Spraw i 15 Osobistych Spraw. `AGENTS.md` nadal mówi, że dokładne timingi i wolumen contentu są strojeniem playtestowym, nie zatwierdzoną stałą.

**Dowody dokumentacyjne**

- Kanon pozostawia timingi i wolumen contentu otwarte: `AGENTS.md:45-49`.
- Brief mówi „cały zakres ma zostać wykonany”: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:3-7`.
- Dokładne decyzje 30 s / 5 punktów / wszystkie 15 + więcej: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:37-49`, `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:81-118`.
- Starszy dokument timera nadal podaje 60–120 s: `docs/design/mechanics/limit-rundy.md:27-30`.
- Specyfikacja Alibi nadal zakłada przycisk hosta + timer bezpieczeństwa: `docs/design/mechanics/alibi-i-redagowanie.md:57-60`.

**Ryzyko**

Nie wiadomo, czy parametry Fable są nowym kanonem, jednorazowym briefem wykonawczym, czy eksperymentem. Agent może legalnie wybrać dowolną z trzech sprzecznych instrukcji.

**Następna zmiana**

Wymagana jest jedna decyzja właściciela:

- **wariant A:** Fable jest zatwierdzonym następcą — przenieść 30 s, gotowość i 5 punktów do `AGENTS.md`/`CONTEXT.md` lub ADR-ów, a starsze dokumenty zaktualizować;
- **wariant B:** to parametry playtestu — oznaczyć je jako profil testowy, nie trwały kontrakt produktu;
- **wariant C:** brief został wykonany i jest historyczny — dodać status ukończenia oraz link do raportu dowodowego.

**Source-check:** implementacja odpowiada bieżącemu profilowi: Przygotowanie ma limit 30 s, pełna gotowość skraca je do 3 s, `CaseDefinition` wymaga dokładnie 5 faktów i 2 luk Winnego, a UI pokazuje gotowość i licznik (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs`, `Assets/Scripts/Game/Domain/RoundEngine.cs`, `Assets/Scripts/Game/UI/RoundPresenter.cs`).

### P0.3 — Dokumentacja stanu produktu jest co najmniej o jeden duży etap spóźniona

**Problem**

Overview i architektura opisują `Prywatne Cele`, Incydenty, Tropy i Ucieczkę jako niezaimplementowane rozszerzenie po bazowym slice. Późniejszy raport regresji twierdzi, że pełne fizyczne etapy, minigry, prywatne widoki, reset i świeża druga Runda zostały przetestowane. Handoffy B4/B5 opisują konkretne prefaby, ID i bindery jak istniejący kontrakt integracyjny.

**Dowody dokumentacyjne**

- „Kodu brak” i implementacja dopiero po bazowym slice: `docs/design/MECHANICS-OVERVIEW.md:20-30`, `docs/design/MECHANICS-OVERVIEW.md:85-94`.
- Architektura nadal umieszcza ten filar poza pierwszym slice i na końcu kolejności: `docs/architecture/MVP-ARCHITECTURE.md:3-13`, `docs/architecture/MVP-ARCHITECTURE.md:125-133`.
- Raport regresji obejmuje wszystkie fizyczne etapy, minigry, prywatne widoki i reset: `docs/playtests/2026-07-21-task-sandbox-regression.md:3-10`, `docs/playtests/2026-07-21-task-sandbox-regression.md:62-86`.
- Handoffy wskazują gotowe kontrakty integracyjne: `docs/integration/B4-PHYSICAL-OBJECTIVE-HANDOFF.md:3-21`, `docs/integration/B5-GUILTY-PHYSICAL-HANDOFF.md:3-23`.

**Ryzyko**

Planowanie z `MECHANICS-OVERVIEW.md` może zlecić ponowne budowanie całych systemów. Jednocześnie raport sandboxa może nadmiernie sugerować gotowość produkcyjną, choć sam sandbox nie zastępuje pełnego testu wielu klientów (`docs/playtests/A7-DEVELOPER-SANDBOX.md:1-5`).

**Następna zmiana**

Zaktualizować `MECHANICS-OVERVIEW.md` do statusu zweryfikowanego po source-checku i rozdzielić cztery poziomy:

1. reguła domenowa istnieje;
2. adapter/runtime istnieje;
3. scena i UI są spięte;
4. pełny host + klienci został zaliczony.

Nie używać jednego znaku ✅ zarówno dla „decyzja zatwierdzona”, jak i „działa end-to-end”; obecna legenda miesza te znaczenia (`docs/design/MECHANICS-OVERVIEW.md:7-30`).

**Source-check:** klasy domenowe, adaptery B4/B5 i UI istnieją w źródłach (`Assets/Scripts/Game/Domain/RoundEngine.cs:45-74`, `Assets/Scripts/Gameplay/Interaction/RoundPhysicalActionBinder.cs:12-39`, `Assets/Scripts/Gameplay/Interaction/RoundPhysicalActionBinder.cs:175-265`, `Assets/Scripts/Game/UI/RoundPresenter.cs:405-457`). Nadal nie potwierdzono wiring sceny ani aktualnego wyniku testów, więc właściwy status to „implemented in source; end-to-end gate pending”, nie „future work” ani „release-ready”.

### P0.4 — `REFACTOR-PLAN.md` jest aktywnym planem dla pracy już opisanej jako wykonana

**Problem**

Plan nadal nakazuje utworzenie asmdefów `Gameplay`, `Voice`, `Steam` i `UI`, przenoszenie skryptów oraz podjęcie decyzji o lobby/global voice. Architektura twierdzi już, że wszystkie te moduły mają asmdefy, a `AGENTS.md` i specyfikacja voice rozstrzygają tryb globalny w Lobby i przestrzenny w Rundzie. Plan nadal zleca też utworzenie istniejącego `docs/README.md`.

**Dowody dokumentacyjne**

- Niewykonane etapy asmdefów według planu: `REFACTOR-PLAN.md:7-29`.
- Architektura opisuje asmdefy jako stan bieżący: `docs/architecture/MVP-ARCHITECTURE.md:76-112`.
- „Decision needed” dla voice: `REFACTOR-PLAN.md:61-67`.
- Decyzja jest już zapisana jako obowiązująca: `AGENTS.md:198-207`, `docs/design/mechanics/glos-przestrzenny.md:14-18`, `docs/design/mechanics/glos-przestrzenny.md:30-34`.
- Plan zleca dodanie `docs/README.md`: `REFACTOR-PLAN.md:75-80`; plik istnieje i pełni rolę indeksu: `docs/README.md:1-38`.
- Sekcja „pre-commit checklist” zawiera chwilowe, gałęziowe uwagi bez daty ważności: `REFACTOR-PLAN.md:82-86`.

**Ryzyko**

To najbardziej bezpośrednia instrukcja wykonawcza w repo, więc kolejny agent może przenosić ponownie pliki, odwrócić obowiązującą decyzję voice albo badać nieistniejący stan roboczy.

**Następna zmiana**

Zamienić dokument w jeden z dwóch wariantów:

- plan żywy z checkboxem/status/date/evidence przy każdym etapie i tylko niewykonanymi punktami;
- raport historyczny przeniesiony logicznie do zakończonych planów, z nowym krótkim backlogiem pozostałych refaktorów.

**Source-check:** wymagane asmdefy istnieją dla Domain, Game, Gameplay, Steam, UI i Voice (`Assets/Scripts/Game/Domain/InterrogationRoom.Domain.asmdef:1-15`, `Assets/Scripts/Game/InterrogationRoom.Game.asmdef:1-19`, `Assets/Scripts/Gameplay/InterrogationRoom.Gameplay.asmdef:1-20`, `Assets/Scripts/Steam/InterrogationRoom.Steam.asmdef:1-19`, `Assets/Scripts/UI/InterrogationRoom.UI.Runtime.asmdef:1-23`, `Assets/Scripts/Voice/InterrogationRoom.Voice.Runtime.asmdef:1-23`), a Vivox/global-spatial flow jest zaimplementowany. Pozostały realne długi: legacy main-menu path, IMGUI panel developerski oraz nierozwiązana stabilna tożsamość gracza w Steam/reconnect.

### P0.5 — `MAP-MVP.md` jest linkowany jako aktualny kontrakt, ale przeczy kanonowi Egzekucji

**Problem**

Dokument jest wyraźnie „niezatwierdzoną propozycją”, nadal pozostawia setting otwarty i opisuje Egzekucję jako decyzję UI. Kanon zatwierdza fizyczne trafienie z pistoletu. Mimo to `docs/README.md` przedstawia `MAP-MVP.md` jako bieżący zakres mapy.

**Dowody dokumentacyjne**

- Status propozycji: `docs/design/MAP-MVP.md:1-3`.
- Setting nadal „do decyzji”, Egzekucja w UI: `docs/design/MAP-MVP.md:46-51`.
- Fizyczna Egzekucja z pistoletu jest kanoniczna: `AGENTS.md:35-38`, `CONTEXT.md:41-48`.
- Indeks linkuje dokument bez ostrzeżenia historycznego: `docs/README.md:15-22`.

**Ryzyko**

Dokument mapy może przywrócić odrzucony flow Egzekucji albo stworzyć fałszywe pytanie o setting.

**Następna zmiana**

Oznaczyć `MAP-MVP.md` jako historyczny input do istniejącej sceny lub zaktualizować go do faktycznego kontraktu obecnego `Posterunku`. Nie rozwijać obrysu mapy; opisać tylko obowiązujące założenia gameplayowe, akustyczne i resetowe.

**Source-check:** fizyczna ścieżka Egzekucji jest jednoznaczna: host autoryzuje i wyposaża tylko `Detektywa`, strzał jest rozstrzygany po stronie serwera, a pierwsze dozwolone trafienie żywego Podejrzanego wysyła `RoundCommand.Execute` (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1259-1273`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1276-1321`, `Assets/Scripts/Gameplay/Weapons/PlayerWeaponController.cs:112-178`). Stref i wiring punktów Ucieczki nie potwierdzono bez inspekcji sceny.

### P0.6 — Instrukcja przebudowy MainMenu może zniszczyć aktualny UI Toolkit scene

**Problem**

`AGENTS.md` nakazuje ponowne uruchamianie `Tools/Setup Main Menu Scene` jako kanonicznego buildera całej sceny. Builder tworzy jednak stary uGUI `Canvas`, dodaje legacy `MainMenuManager` i zapisuje scenę od zera, podczas gdy obecny runtime jawnie deklaruje `MainMenuPresenter`/UI Toolkit jako następcę uGUI.

**Dowody**

- Instrukcja obowiązkowego buildera: `AGENTS.md:187-196`.
- Builder tworzy nową pustą scenę, uGUI Canvas i legacy manager, a potem nadpisuje `MainMenu.unity`: `Assets/Editor/MainMenuSetup.cs:12-16`, `Assets/Editor/MainMenuSetup.cs:36-46`, `Assets/Editor/MainMenuSetup.cs:95-102`.
- Aktualny presenter mówi wprost, że UI Toolkit zastępuje uGUI: `Assets/Scripts/UI/MainMenuPresenter.cs:12-18`.
- Legacy manager nadal szuka `TextMeshProUGUI` i nazw `Button_Host Game`/`Button_Join Server`: `Assets/Scripts/UI/MainMenuManager.cs:12-55`.

**Ryzyko**

Agent wykonujący repozytoryjną instrukcję może legalnie odtworzyć przestarzałą scenę i skasować bieżące MainMenu. To jest bardziej niebezpieczne niż zwykły nieaktualny roadmap item.

**Następna zmiana**

Natychmiast usunąć tę pozycję z listy bezpiecznych builderów albo przepisać `MainMenuSetup` tak, aby budował aktualny kontrakt UI Toolkit. Do czasu naprawy oznaczyć menu item jako legacy/nie uruchamiać. Następnie zdecydować, czy legacy `MainMenuManager` pozostaje potrzebny; repo powinno mieć jeden runtime path MainMenu.

---

## P1 — zamknąć przed pełnym playtestem / skalowaniem contentu

### P1.1 — Content ma kontrakt 6 vs 8 oraz roadmapę 4/15 spóźnioną wobec 18/15 w źródłach

**Problem**

Brief Fable wymaga dokładnie 5 punktów i grywalności całej piętnastki. Katalog oraz ogólna specyfikacja zostały ujednolicone do bieżącego kontraktu pięciu faktów.

**Dowody dokumentacyjne**

- 5 punktów i pełna piętnastka: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:93-118`.
- 8 faktów jako kontrakt katalogu: `docs/design/PLAYTEST-CONTENT-CATALOG.md:20-31`.
- Minimalne cztery Osobiste Sprawy: `docs/design/PLAYTEST-CONTENT-CATALOG.md:92-95`.
- „Nie implementować wszystkich 15 przed pierwszym playtestem”: `docs/design/PLAYTEST-CONTENT-CATALOG.md:562-570`.
- Ogólny model 5 faktów: `docs/design/mechanics/content-sprawy.md:25-30`.

**Następna zmiana**

Źródła rozstrzygają bieżący kontrakt implementacyjny na **5 faktów**, **18 definicji Spraw** i **15 Osobistych Spraw**. Zamiast authoringu kolejnych pozycji potrzebna jest macierz: `definition exists` → `asset synced` → `configured in coordinator` → `physical anchors wired` → `automated test` → `real-client playtest`.

**Source-check:** `RoundEngine` odrzuca przypadki inne niż 6-faktowe (`Assets/Scripts/Game/Domain/RoundEngine.cs:167-177`); generator ma wpisy C01–C18 (`Assets/Scripts/Editor/Content/CaseAssetSync.cs:27-513`), a generator Osobistych Spraw synchronizuje OS-01–OS-15 (`Assets/Scripts/Editor/Content/PersonalMatterAssetSync.cs:19-38`). Sam komunikat generatora Spraw nadal błędnie mówi C01–C15 (`Assets/Scripts/Editor/Content/CaseAssetSync.cs:22-25`), co jest dodatkowym przykładem driftu statusu. Bez sceny nie potwierdzono runtime wiring tych 18/15 definicji.

### P1.2 — Wybór Sprawy i start Przygotowania mają dwa różne flow

**Problem**

Starsze specyfikacje dają hostowi listę Spraw i hostowy przycisk końca Przygotowania. Brief Fable i przewodnik UI mówią o losowaniu Sprawy bez wyboru hosta oraz gotowości wszystkich graczy z automatycznym limitem 30 s.

**Dowody dokumentacyjne**

- Host wybiera Sprawę: `docs/design/mechanics/content-sprawy.md:44-47`, `docs/design/mechanics/ui-rundy.md:13-18`.
- Sprawa losowa, niewidoczna jako wybór hosta: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:39-43`, `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:99-110`, `docs/design/ui/UI-STYLE-GUIDE.md:160-170`.
- Stary kontrakt zakończenia Przygotowania: `docs/design/mechanics/alibi-i-redagowanie.md:57-60`.
- Nowy kontrakt gotowości: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:81-91`.

**Następna zmiana**

Aktualizować wspólnie `content-sprawy.md`, `ui-rundy.md`, `alibi-i-redagowanie.md`, `limit-rundy.md` i `MVP-ARCHITECTURE.md` do obecnego flow: serwer losuje poprawną Sprawę; wszyscy w lobby muszą być gotowi do startu; w Przygotowaniu każdy potwierdza zapamiętanie Alibi; limit 30 s zawsze kończy etap, a komplet gotowych skraca pozostały czas do 3 s. Jeśli właściciel chce inne zachowanie, powinien zmienić kanon i kod świadomie, a nie odzyskiwać je ze starej specyfikacji.

**Source-check:** serwer losuje spośród poprawnych `CaseAsset`, nie przyjmuje wyboru hosta (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:849-904`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:952-976`); lobby wymaga gotowości wszystkich (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:849-855`); Przygotowanie ma indywidualną gotowość, deadline 30 s i skrócenie do 3 s (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:19-21`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1110-1132`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1337-1402`).

### P1.3 — Brak jednego statusu „vertical slice gotowy” i jego dowodów

**Problem**

Sandbox i regresja lokalnego hosta są dobrze udokumentowane, ale same dokumenty mówią, że nie zastępują wielu prawdziwych klientów, prawdziwego hitboxa Egzekucji ani voice. Overview nadal wymienia pełny host + klienci jako najważniejszy brak. Test Steam wymaga dwóch maszyn i kont.

**Dowody dokumentacyjne**

- Sandbox nie zastępuje pełnego A7: `docs/playtests/A7-DEVELOPER-SANDBOX.md:1-5`, `docs/playtests/A7-DEVELOPER-SANDBOX.md:53-57`.
- Overview wymaga pełnej Rundy host + klienci: `docs/design/MECHANICS-OVERVIEW.md:32-54`, `docs/design/MECHANICS-OVERVIEW.md:85-94`.
- Steam wymaga dwóch maszyn i kont: `docs/architecture/STEAM-NETWORKING.md:26-31`.

**Następna zmiana**

Utworzyć po wykonaniu testu jeden datowany raport exit-gate, który osobno potwierdza:

1. KCP: host + minimum 2 realnych klientów, pełne Przygotowanie → Runda → Egzekucja/Ucieczka → wynik → druga Runda;
2. prywatność widoków dla różnych ról;
3. prawdziwe trafienie hitboxa jako Egzekucję;
4. voice i drzwi w tej samej sesji;
5. Steam na dwóch maszynach dopiero po KCP.

**Source-check:** istnieją testy PlayMode dla interakcji, drzwi/pomieszczeń, Celów, Ucieczki i carry/minigames oraz szerokie testy EditMode, ale nie znaleziono w źródłach substytutu dla realnego wieloklientowego testu transportu/voice. W tym audycie testów nie uruchamiano (`Assets/Tests/PlayMode/TimedInteractionRuntimeTests.cs:1-161`, `Assets/Tests/PlayMode/NetworkRoomsAndDoorsTests.cs:1-179`, `Assets/Tests/PlayMode/PhysicalObjectiveTracerTests.cs:1-120`, `Assets/Tests/PlayMode/GuiltyPhysicalTracerTests.cs:1-130`).

### P1.4 — Vivox ma nierozstrzygnięty bloker operacyjny i brak aktualnego raportu odbioru

**Problem**

Setup B6 twierdzi, że credentiale Vivox nadal wymagają ręcznego odblokowania w Dashboardzie. Nowszy brief playtestowy raportuje, że voice działa źle przez dystans, ściany lub drzwi. Specyfikacja voice ma status „w toku”, ale brak datowanego raportu, że macierz akustyczna została zaliczona.

**Dowody dokumentacyjne**

- Wymagane credentiale i symptom błędu: `docs/playtests/B6-VIVOX-SETUP.md:3-13`.
- Wymagany test KCP/ParrelSync: `docs/playtests/B6-VIVOX-SETUP.md:15-25`.
- Status implementacji B6 w toku: `docs/design/mechanics/glos-przestrzenny.md:1-6`.
- Zgłoszona nieskuteczna prywatność i pełna macierz odbioru: `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:172-200`.

**Następna zmiana**

Po potwierdzeniu credentiali wykonać jeden datowany raport voice acceptance: ten sam pokój, dystans, korytarz, pełna ściana, otwarte/zamknięte drzwi, podsłuch przy drzwiach, przełączanie Lobby ↔ Runda. Oddzielić „usługa skonfigurowana” od „akustyka zaakceptowana”.

**Source-check:** runtime Vivox jest rozbudowany: anonimowe logowanie UGS, osobny channel Lobby/Runda, 3D position, własna krzywa rolloff, participant taps, lokalne mute/volume i komponent okluzji (`Assets/Scripts/Voice/VivoxVoiceRuntime.cs:172-245`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs:315-353`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs:431-567`). Kod nie potwierdza jednak, że dashboard credentials są poprawne ani że macierz akustyczna przeszła realny test; to pozostaje blokerem operacyjnym, nie implementacyjną pustką.

### P1.5 — Rozłączenie `Detektywa` lub `Winnego` jest najważniejszą nierozstrzygniętą regułą awarii Rundy

**Problem**

`OPEN-QUESTIONS.md` nie rozstrzyga wyniku po utracie kluczowej roli. Specyfikacja ról jednocześnie mówi „MVP — Runda trwa dalej” i proponuje zakończenie bez rozstrzygnięcia dla `Detektywa`/`Winnego`.

**Dowody dokumentacyjne**

- Brak decyzji: `docs/design/OPEN-QUESTIONS.md:19-21`.
- Wewnętrznie niespójny przypadek brzegowy: `docs/design/mechanics/role-i-sklad-rundy.md:43-46`.

**Dlaczego to blokuje**

Bez tej polityki nie da się jednoznacznie zaakceptować reconnectu, late-join, wyniku indywidualnego, resetu ani pełnego testu wieloklientowego z awarią połączenia.

**Następna zmiana**

Podjąć ADR przed deklaracją gotowości multiplayer: no-contest, grace period na reconnect albo trwały fallback. Dla MVP najprostszy jest jawny no-contest z osobnym powodem zakończenia, ale dokumentacja nie powinna wybierać go bez decyzji właściciela.

**Source-check:** po disconnectcie coordinator usuwa połączenie z bieżącego rosteru, ale nie kończy Rundy ani nie zastępuje kluczowej roli (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:658-725`). Reconnect w aktywnej Rundzie jest akceptowany tylko wtedy, gdy nowy transport odtworzy ten sam `PlayerId`; jednocześnie jedyna mapa Mirror → `PlayerId` nadal używa `connection.connectionId`, a komentarz odkłada SteamID na przyszłość (`Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:698-713`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1478-1483`). Otwarta decyzja produktowa i brak stabilnej tożsamości są więc jednym połączonym blockerem, szczególnie dla Steam/reconnectu.

### P1.6 — Dokumenty grafiki są trackerem historycznych branchy, nie stanu `main`

**Problem**

Tracker nadal ma Fazy 0–1 jako `Approved`, 2 i 4 jako `Review`, a 3/5/6/7 jako `Open`; instrukcje nakazują pracę na dawnych branchach i aktualizowanie statusu w commitach. Jednocześnie raporty map polish opisują wielokrotne ukończone passy. `ART-DIRECTION.md` nadal ma status propozycji, choć Faza 1 i UI Guide nazywają go zatwierdzonym. Pass 2 ma wszystkie kryteria nadal odznaczone, choć osobny raport ogłasza finalną weryfikację.

**Dowody dokumentacyjne**

- Historyczne branche i statusy: `docs/design/graphics/README.md:5-18`, `docs/design/graphics/README.md:31-40`.
- `ART-DIRECTION.md` jako propozycja: `docs/design/graphics/ART-DIRECTION.md:1-5`.
- Faza 1 jako zatwierdzona: `docs/design/graphics/FAZA-1-art-direction.md:1-12`.
- UI Guide uznaje art direction za nadrzędne źródło: `docs/design/ui/UI-STYLE-GUIDE.md:1-5`.
- Pass 2: odznaczone kryteria i wymagany raport: `docs/map-polish/PASS-2-ART-DIRECTION.md:650-660`.
- Raport Pass 2 deklaruje zakończoną weryfikację: `docs/map-polish/PASS-2-REPORT.md:7-13`, `docs/map-polish/PASS-2-REPORT.md:48-59`.

**Następna zmiana**

Rozdzielić:

- **obowiązujące zasady artystyczne** (`ART-DIRECTION.md`);
- **aktualny backlog grafiki**;
- **historyczne execution logs/branche**.

Tracker powinien opisywać stan na `main`, zawierać datę ostatniej weryfikacji i linkować raport dowodowy zamiast branchy, które mogą już nie być właściwym miejscem pracy.

**Granica source-checku:** kod źródłowy nie jest wystarczającym dowodem stanu oświetlenia, materiałów, propsów i postprocessingu na `main`; do zamknięcia tego punktu potrzebna jest późniejsza inspekcja sceny/assetów w Unity. Sprzeczność statusów dokumentów jest jednak niezależnie potwierdzona i powinna zostać naprawiona bez kolejnego passu mapy.

### P1.7 — Faza 3 i backlog propsów mają przeciwne decyzje sourcingowe

**Problem**

Faza 3 nakazuje użytkownikowi import realistycznych paczek Asset Store. Nowszy backlog propsów mówi wprost, że aktualizuje tę decyzję i propsy mają być generowane, a Kenney zastępowany.

**Dowody dokumentacyjne**

- Asset Store jako decyzja wykonawcza: `docs/design/graphics/FAZA-3-materialy-kit.md:21-25`, `docs/design/graphics/FAZA-3-materialy-kit.md:54-59`.
- Generacja zastępuje tę decyzję: `docs/design/graphics/PROPSY-DO-GENERACJI.md:1-7`.
- Kenney do zastąpienia: `docs/design/graphics/PROPSY-DO-GENERACJI.md:9-17`.
- Starszy Pass 2 zatwierdza pozostawienie Kenneya: `docs/map-polish/PASS-2-ART-DIRECTION.md:29-34`.

**Następna zmiana**

W `FAZA-3-materialy-kit.md` usunąć superseded sourcing i linkować jedną aktualną politykę assetów. Historyczny Pass 2 powinien być oznaczony jako decyzja lokalna dla tamtego passu, nie bieżący zakaz wymiany mebli.

**Granica source-checku:** bez inspekcji assetów/sceny nie ustalono faktycznego udziału Kenney, Asset Store i generowanych propsów. Dokumentacyjny konflikt sourcingu pozostaje jasny i wymaga jednego aktywnego policy document, lecz nie uzasadnia teraz ekspansji mapy.

---

## P2 — porządek i nawigacja

### P2.1 — Repo ma wiele „źródeł prawdy”, ale nie ma formalnej kolejności pierwszeństwa

**Problem**

`AGENTS.md`, `CONTEXT.md`, ADR-y, `MVP-ARCHITECTURE.md`, mechanics specs, `MECHANICS-OVERVIEW.md`, brief Fable i UI Guide używają języka normatywnego. `docs/README.md` indeksuje je, lecz nie opisuje, co wygrywa przy konflikcie i jak oznacza się supersession.

**Dowody dokumentacyjne**

- `AGENTS.md` nazywa się repozytoryjnym źródłem prawdy: `CLAUDE.md:3-5`.
- `CONTEXT.md`, architektura, ADR-y i mechanics specs są wspólnie nazwane źródłami prawdy: `docs/design/MECHANICS-OVERVIEW.md:1-4`.
- Indeks opisuje kategorie, ale nie hierarchię ani statusy: `docs/README.md:1-38`.
- Tylko ADR-0002 jawnie sygnalizuje częściowe zastąpienie: `docs/adr/0002-innocents-play-for-their-own-survival.md:1-5`.

**Następna zmiana**

Dodać do `docs/README.md` krótką hierarchię:

1. `AGENTS.md` — aktualne reguły operacyjne i skrót kanonu;
2. `CONTEXT.md` + aktywne ADR-y — kanon produktu;
3. mechanics specs — aktualny kontrakt wykonawczy;
4. architecture — granice modułów;
5. briefs/plany/raporty — historyczny lub czasowy zakres.

Każdy plan i brief powinien mieć pola: `Status`, `Data decyzji`, `Supersedes`, `Superseded by`, `Evidence`.

### P2.2 — Stare ścieżki kodu i chwilowe instrukcje podważają zaufanie do dokumentów

**Przykłady**

- Lobby spec wskazuje skrypty w starym root `Assets/Scripts/`: `docs/design/mechanics/lobby-i-siec.md:1-6`; architektura Steam wskazuje `Assets/Scripts/Steam/`: `docs/architecture/STEAM-NETWORKING.md:12-17`.
- `REFACTOR-PLAN.md` zawiera chwilowy pre-commit checklist zamiast trwałego planu: `REFACTOR-PLAN.md:82-86`.
- `PASS-2-ART-DIRECTION.md` ma taskowe polecenia „zero Play Mode”, „commit” i „commity wypchnięte”, choć jest jednocześnie linkowalnym kierunkiem artystycznym: `docs/map-polish/PASS-2-ART-DIRECTION.md:633-660`.

**Następna zmiana**

Zrobić jeden mechaniczny pass linków i ścieżek oraz oznaczyć execution logs jako historyczne. Nie usuwać wartościowych diagnoz technicznych; przenieść ich status z „instrukcja bieżąca” na „lessons learned”. Source-check potwierdza obecne lokalizacje m.in. `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs`, `Assets/Scripts/Steam/SteamLobby.cs`, `Assets/Scripts/Voice/VivoxVoiceRuntime.cs` i `Assets/Scripts/UI/CenteredNetworkManagerHUD.cs`; stare ścieżki w specyfikacjach są więc realnym driftem, nie tylko zmianą nazewnictwa.

### P2.3 — Otwarte kwestie UI są poprawnie odłożone i nie powinny blokować kolejnego gate’u

**Dowody dokumentacyjne**

- Otwarte: Notatki, prezentacja Alibi, podgląd Celu i pełne Alibi po Rundzie: `docs/design/OPEN-QUESTIONS.md:1-17`.
- Kanon jawnie mówi, że finalne Notatki i prezentacja Alibi pozostają nierozstrzygnięte: `AGENTS.md:45-47`.
- UI Guide ma już działający kierunek dla bieżących ekranów bez rozstrzygania pełnego Alibi: `docs/design/ui/UI-STYLE-GUIDE.md:160-171`.

**Wniosek**

Nie są to obecnie blokery domeny, sieci ani pełnego playtestu. Nie należy otwierać kolejnego dużego redesignu przed naprawą statusów i przejściem exit-gate’u Rundy/voice.

### P2.4 — Brakuje czterech małych dokumentów/sekcji operacyjnych, które zastąpiłyby roadmap chaos

1. **Current-state ledger** — jedna tabela `Domain / Runtime / Scene / Automated / Real clients / Last verified / Next gate`; obecny `docs/README.md` jest tylko indeksem (`docs/README.md:1-38`).
2. **Disconnect/reconnect contract** — decyzja wyniku po utracie kluczowej roli oraz wymagania stabilnej tożsamości; dziś pytanie jest otwarte, a kod używa `connectionId` (`docs/design/OPEN-QUESTIONS.md:19-21`, `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1478-1483`).
3. **Content wiring matrix** — oddziela 18/15 definicji w generatorach od assetów, konfiguracji i fizycznego wiring (`Assets/Scripts/Editor/Content/CaseAssetSync.cs:27-513`, `Assets/Scripts/Editor/Content/PersonalMatterAssetSync.cs:19-38`).
4. **Dated release/playtest exit gate** — KCP, role-private views, fizyczna Egzekucja/Ucieczka, druga Runda, voice/drzwi, reconnect, potem Steam (`docs/playtests/A7-DEVELOPER-SANDBOX.md:1-5`, `docs/architecture/STEAM-NETWORKING.md:26-31`).

Nie potrzeba kolejnego szerokiego design doc. Potrzeba krótkich, utrzymywanych sekcji z właścicielem, datą i evidence linkiem.

---

## Genuinely blocking unresolved decisions

| Decyzja / bloker | Dlaczego blokuje | Dowód | Zalecenie |
|---|---|---|---|
| Czy parametry z briefu Fable są kanonem czy profilem testowym | Bez tego nie ma jednego kontraktu Przygotowania, Alibi i wolumenu contentu | `AGENTS.md:45-47`; `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:37-49` | Decyzja właściciela + aktualizacja kanonu/ADR |
| Rozłączenie `Detektywa`/`Winnego` + stabilna tożsamość reconnectu | Blokuje pełną semantykę wyniku i odzyskanie prywatnego widoku; kod nadal mapuje `PlayerId` z `connectionId` | `docs/design/OPEN-QUESTIONS.md:19-21`; `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:698-713`; `Assets/Scripts/Game/Networking/NetworkRoundCoordinator.cs:1478-1483` | ADR polityki disconnectu + SteamID-derived identity przed deklaracją multiplayer-ready |
| Credentiale i akceptacja Vivox | Voice jest must-have; dokumenty nie potwierdzają zakończonej konfiguracji i strojenia | `docs/playtests/B6-VIVOX-SETUP.md:3-25`; `docs/design/FABLE-PLAYTEST-IMPROVEMENTS.md:172-200` | Dated setup + acoustic acceptance report |
| Brak pełnego, realnego testu host + klienci udokumentowanego jako exit gate | Sandbox jawnie nie pokrywa kluczowych zachowań | `docs/playtests/A7-DEVELOPER-SANDBOX.md:1-5`, `docs/playtests/A7-DEVELOPER-SANDBOX.md:53-57` | KCP gate, potem Steam na dwóch maszynach |

## Otwarte, ale nieblokujące teraz

- Finalna forma `Notatek Detektywa`.
- Lista vs narracja w prezentacji Alibi.
- Diegetyczna forma podglądu `Prywatnego Celu`.
- Ujawnianie pełnego Alibi na ekranie wyniku.
- Dokładne krzywe/timingi jako strojenie, o ile właściciel nie awansuje parametrów Fable do kanonu.

Źródło: `docs/design/OPEN-QUESTIONS.md:1-25`, `AGENTS.md:45-47`.

---

## Priorytetowy plan następnych zmian dokumentacyjnych

### 0. Zablokować niebezpieczny legacy MainMenu builder

Usunąć z `AGENTS.md` polecenie uruchamiania `Tools/Setup Main Menu Scene` do czasu przepisania buildera na aktualny UI Toolkit contract. To jedyna znaleziona instrukcja, której poprawne wykonanie może bezpośrednio odtworzyć przestarzałą scenę.

### 1. Patch kanonu i sprzeczności liczebności

Pliki: `AGENTS.md`, `CONTEXT.md`, ADR-0013, `role-i-sklad-rundy.md`, `prywatne-cele-incydenty-i-ucieczka.md`, overview i sandbox. Cel: jednoznaczne 3–8 / 5–8 oraz testy 7–8.

### 2. Decyzja o statusie briefu Fable

Oznaczyć go jako `Active`, `Playtest profile`, `Completed` albo `Superseded`. Następnie ujednolicić: 30 s, gotowość, 6/8 faktów, losowanie Sprawy oraz różnicę między briefowymi 15 a obecnymi 18 definicjami Spraw.

### 3. Aktualny ledger stanu w `docs/README.md`

Dla każdego filaru dodać: `Decyzja`, `Domain`, `Runtime`, `Scene/UI`, `Automated evidence`, `Multiplayer evidence`, `Last verified`, `Next gate`. To zastąpi niejednoznaczne ✅ w overview.

### 4. Zamknąć lub zarchiwizować plany historyczne

Najpierw `REFACTOR-PLAN.md`, potem graphics tracker i Pass 2 playbook. Zachować lessons learned, usunąć charakter aktywnego task briefu z ukończonych etapów.

### 5. Polityka disconnect/reconnect, potem pełny exit-gate Rundy i voice

Najpierw rozstrzygnąć zachowanie utraty `Detektywa`/`Winnego` i zastąpić `connectionId` stabilną tożsamością tam, gdzie ma działać reconnect. Następnie, bez powiększania mapy: KCP host + realni klienci, fizyczna Egzekucja, Ucieczka, prywatne widoki, rozłączenie/reconnect, reset drugiej Rundy, drzwi + voice; osobny późniejszy test Steam.

### 6. Zweryfikować istniejący content zamiast dodawać kolejny

Kod już zawiera 18 Spraw i 15 Osobistych Spraw. Po rozstrzygnięciu statusu 6 vs 8 zbudować macierz synchronizacji, wiring, fizycznych anchorów, testów i playtestów dla istniejącej puli. Nie zwiększać liczby assetów ani mapy przed zamknięciem kontraktu i pełnym playtestem.

---

## Granice weryfikacji i brakujące dowody

Source-check zamknął pytania o limity 3–8/5–8, 30-sekundowe Przygotowanie, 6-faktowy kontrakt, losowanie Sprawy, asmdefy, istnienie B4/B5, UI Toolkit, Steam/KCP i runtime Vivox. Nadal nie wolno z tego wyprowadzać twierdzenia „vertical slice jest release-ready”. Poza zakresem audytu pozostały:

- wiring sceny i Inspector references dla wszystkich 18 Spraw / 15 Osobistych Spraw;
- aktualny wynik EditMode/PlayMode tests i brak błędów kompilacji;
- pełna sesja KCP z hostem i co najmniej dwoma realnymi klientami;
- prawdziwy reconnect oraz utrata kluczowej roli;
- test Steam na dwóch maszynach/kontach;
- credentiale Vivox i realna macierz akustyczna;
- wizualny stan faz grafiki/map-polish na `main`.

Te punkty wymagają Unity/real-client evidence i powinny trafić do jednego datowanego exit-gate reportu, nie do kolejnego roadmap briefu.
