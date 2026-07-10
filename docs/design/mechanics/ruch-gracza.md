# Ruch gracza (FPP)

**Status:** ✅ Zaimplementowana (z lukami do domknięcia)
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/PlayerController.cs`, prefab `Assets/Prefabs/Player.prefab` (Mirror `NetworkTransform`)

## Cel

Runda jest ciągła i free-roamingowa (ADR-0005), więc każdy gracz — Detektyw i Podejrzani — potrzebuje płynnego poruszania się w pierwszej osobie po posterunku: chodzenia, obracania kamerą, skoku. Ruch jest też nośnikiem prywatności głosu (ADR-0009): to fizyczne przemieszczenie się decyduje, kto co słyszy.

## Zasada działania (stan obecny)

- `CharacterController` + sterowanie WASD, mysz obraca ciało (yaw) i kamerę (pitch, clamp ±80°), spacja = skok, stała grawitacja.
- Obsługiwane są oba systemy inputu (`ENABLE_INPUT_SYSTEM` i legacy).
- Kamera i `AudioListener` włączane tylko dla lokalnego gracza; renderery lokalnego gracza są wyłączane (brak widoku własnego ciała).
- Kursor blokowany po spawnie lokalnego gracza, odblokowywany przy zatrzymaniu.
- Synchronizacja pozycji/rotacji przez `NetworkTransform` na prefabie `Player` — ruch jest **klient-autorytatywny**.

## Autorytet i sieć

Obecnie klient jest autorytetem swojej pozycji. Dla tej gry (towarzyska, znajomi przez lobby Steam friends-only) to akceptowalny kompromis MVP, ale serwer waliduje interakcje i strzały pozycją, którą raportuje klient — teleport-cheat pozwoliłby ominąć walidację dystansu w `PlayerInteractor`/`PlayerWeaponController`.

## Zależności

- Wejście dla `PlayerInteractor` (kamera to źródło raycastu interakcji).
- Wejście dla `PlayerWeaponController` (kierunek strzału z kamery).
- Fundament Głosu Przestrzennego — pozycja synchronizowana przez Mirror będzie źródłem pozycji mówcy dla Dissonance.

## Luki do domknięcia

1. **Blokada ruchu poza Rundą** — po Egzekucji / końcu Rundy oraz w menu/karcie Alibi ruch i kamera powinny być wyłączane centralnie (dziś nie ma żadnej bramki stanu gry).
2. **Widok własnego ciała** — wyłączanie wszystkich rendererów lokalnie oznacza brak widocznej broni w rękach z perspektywy FPP; trzymana broń jest childem prefabu gracza, więc lokalny gracz może jej nie widzieć (do weryfikacji: `RefreshHeldWeaponVisual` tworzy visual po `OnStartClient`, po tym jak `PlayerController` wyłączył renderery — nowe renderery broni nie są objęte wyłączeniem, ale warto to ujednolicić świadomą polityką „first-person rig").
3. **Brak biegu/kucania** — nie jest wymagane przez zatwierdzone reguły; dodawać tylko po decyzji użytkownika.

## Kryteria akceptacji

- Dwóch klientów (ParrelSync/KCP) widzi wzajemnie płynny ruch i rotację.
- Lokalny gracz nigdy nie słyszy podwójnego audio (jeden aktywny `AudioListener`).
- Ruch zablokowany, gdy Runda nie trwa (po wdrożeniu bramki stanu).
