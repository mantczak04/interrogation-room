# Ruch gracza (FPP)

**Status:** ✅ Zaimplementowana (z lukami do domknięcia)
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/Gameplay/PlayerController.cs` wraz z wyspecjalizowanymi komponentami w tym katalogu, prefab `Assets/Prefabs/Player.prefab` (Mirror `NetworkTransform`)

## Cel

Runda jest ciągła i free-roamingowa (ADR-0005), więc każdy gracz — Detektyw i Podejrzani — potrzebuje płynnego poruszania się w pierwszej osobie po posterunku: chodzenia, obracania kamerą, skoku. Ruch jest też nośnikiem prywatności głosu (ADR-0009): to fizyczne przemieszczenie się decyduje, kto co słyszy.

## Zasada działania (stan obecny)

- `CharacterController` + sterowanie WASD, mysz obraca ciało (yaw) i kamerę (pitch, clamp ±80°), spacja = skok, stała grawitacja.
- **Sprint na Shift** — ograniczony budżetem czasu, patrz sekcja niżej.
- Obsługiwane są oba systemy inputu (`ENABLE_INPUT_SYSTEM` i legacy).
- Kamera i `AudioListener` włączane tylko dla lokalnego gracza; renderery lokalnego gracza są wyłączane (brak widoku własnego ciała).
- Kursor blokowany po spawnie lokalnego gracza, odblokowywany przy zatrzymaniu.
- Synchronizacja pozycji/rotacji przez `NetworkTransform` na prefabie `Player` — ruch jest **klient-autorytatywny**.

## Sprint (decyzja użytkownika, 2026-07-27)

Sprint jest krótkim zrywem, a nie drugim trybem chodzenia: ma pozwolić przebiec korytarz albo urwać się Detektywowi, po czym wymusza powrót do marszu. Runda ma być przechodzona, nie przebiegana.

- **Klawisz:** Shift (lewy lub prawy), tylko przy ruchu do przodu (`moveInput.y > 0.1`). Strafe'owanie i cofanie nie przyspieszają.
- **Prędkość:** `speed × 1.55` — na prefabie `2.5 → 3.88 m/s`.
- **Budżet:** 3 s ciągłego sprintu (ok. 11,6 m drogi), pełna regeneracja w 6 s, z 0,6 s zwłoki po puszczeniu klawisza.
- **Próg restartu:** 0,25 budżetu — wyczerpany gracz nie może „tapować" Shiftem i migotać między prędkościami.
- Budżet resetuje się przy siadaniu, śmierci i teleportacji do pokoju startowego.
- Czysta matematyka budżetu leży w `PlayerSprintStamina` (bez zależności od Unity/Mirror), więc jest pokryta testami Edit Mode. `PlayerController.SprintCharge01` wystawia stan dla przyszłego HUD-u.

**Znana luka kosmetyczna:** blend tree postaci ma tylko `Idle (0) → Walking (1)` — nie ma klipu biegu, więc sprintujący gracz odtwarza cykl chodu i lekko „ślizga" stopami. Do domknięcia razem z animacjami postaci.

## Skok

Strop posterunku jest na `y = 3.00`, a kapsuła stojącego gracza sięga `y = 1.82` — zostaje **1,18 m prześwitu**. Poprzednie `jumpHeight = 1.5` przebijało ten prześwit o 32 cm, więc każdy skok kończył się uderzeniem w sufit; `CharacterController` zatrzymywał wtedy ruch, ale **nie prędkość**, przez co gracz wisiał pod stropem aż grawitacja zjadła całą prędkość startową (skok trwał 1,1 s).

- `jumpHeight = 0.75`, `gravity = -20` → wierzchołek 0,75 m (0,43 m zapasu pod stropem), czas w powietrzu **0,55 s** zamiast 1,11 s.
- `PlayerJumpMotion.ClampVerticalVelocityAtCeiling` zeruje prędkość w górę przy kontakcie ze stropem (`CollisionFlags.Above`), więc skok pod niskim nadprożem od razu opada zamiast się kleić.

## Autorytet i sieć

Obecnie klient jest autorytetem swojej pozycji. Dla tej gry (towarzyska, znajomi przez lobby Steam friends-only) to akceptowalny kompromis MVP, ale serwer waliduje interakcje i strzały pozycją, którą raportuje klient — teleport-cheat pozwoliłby ominąć walidację dystansu w `PlayerInteractor`/`PlayerWeaponController`.

## Zależności

- Wejście dla `PlayerInteractor` (kamera to źródło raycastu interakcji).
- Wejście dla `PlayerWeaponController` (kierunek strzału z kamery).
- Fundament Głosu Przestrzennego — pozycja synchronizowana przez Mirror jest źródłem pozycji mówcy dla Vivox.

## Luki do domknięcia

1. **Blokada ruchu poza Rundą** — po Egzekucji / końcu Rundy oraz w menu/karcie Alibi ruch i kamera powinny być wyłączane centralnie (dziś nie ma żadnej bramki stanu gry).
2. **Widok własnego ciała** — wyłączanie wszystkich rendererów lokalnie oznacza brak widocznej broni w rękach z perspektywy FPP; trzymana broń jest childem prefabu gracza, więc lokalny gracz może jej nie widzieć (do weryfikacji: `RefreshHeldWeaponVisual` tworzy visual po `OnStartClient`, po tym jak `PlayerController` wyłączył renderery — nowe renderery broni nie są objęte wyłączeniem, ale warto to ujednolicić świadomą polityką „first-person rig").
3. **Brak kucania** — nie jest wymagane przez zatwierdzone reguły; dodawać tylko po decyzji użytkownika. Sprint został dodany decyzją użytkownika 2026-07-27 (sekcja wyżej).
4. **Brak animacji biegu** — sprint jedzie na cyklu chodu (patrz sekcja „Sprint").
5. **Sprint bez wskaźnika** — budżet jest dostępny przez `SprintCharge01`, ale HUD go nie pokazuje; do decyzji przy projektowaniu UI Rundy.

## Kryteria akceptacji

- Dwóch klientów (ParrelSync/KCP) widzi wzajemnie płynny ruch i rotację.
- Lokalny gracz nigdy nie słyszy podwójnego audio (jeden aktywny `AudioListener`).
- Ruch zablokowany, gdy Runda nie trwa (po wdrożeniu bramki stanu).
