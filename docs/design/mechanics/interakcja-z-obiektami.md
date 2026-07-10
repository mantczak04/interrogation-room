# Interakcja z obiektami (framework „E")

**Status:** ✅ Zaimplementowana
**Priorytet:** Must-have (MVP)
**Kod:** `Assets/Scripts/Gameplay/Interaction/PlayerInteractor.cs`, `INetworkInteractable.cs`

## Cel

Jeden wspólny, serwerowo walidowany kanał interakcji gracza ze światem: podnoszenie przedmiotów, otwieranie drzwi, przyszłe użycia (włączniki, Notatki Detektywa na tablicy itd.). Każda nowa interakcja to tylko implementacja `INetworkInteractable` — bez dotykania kodu gracza.

## Zasada działania (stan obecny)

1. Lokalny gracz wciska **E**; raycast z kamery (`RaycastAll`, pierwsze trafienie nie-własne, `QueryTriggerInteraction.Collide`) w zasięgu `interactionRange + serverRangeTolerance + offset kamery`.
2. Jeśli trafiony collider należy do `NetworkIdentity` z komponentem `INetworkInteractable`, klient wysyła `CmdTryInteract(netId)`.
3. **Serwer waliduje niezależnie**: cel istnieje w `NetworkServer.spawned`, nie jest samym graczem, dystans do `InteractionPosition` ≤ `interactionRange + tolerance`, oraz line-of-sight z wysokości oczu (`serverViewHeight`) — pierwszy nie-własny hit musi być celem lub jego dzieckiem.
4. Po walidacji serwer woła `interactable.TryInteractServer(interactor)` — cała logika efektu żyje w obiekcie interaktywnym.

## Autorytet i sieć

W pełni server-authoritative: klient wysyła tylko intencję („chcę użyć obiektu X"), serwer podejmuje decyzję. To wzorzec zgodny z ADR-0011 (serwer jest właścicielem prawdy) i należy go stosować do wszystkich przyszłych interakcji.

## Interfejs

```csharp
public interface INetworkInteractable
{
    Vector3 InteractionPosition { get; }
    bool TryInteractServer(NetworkIdentity interactor);
}
```

## Istniejące implementacje

- `NetworkWeaponPickup` — podnoszenie pistoletu ([podnoszenie-przedmiotow.md](./podnoszenie-przedmiotow.md)).

## Planowane implementacje

- **Drzwi** ([drzwi-i-pomieszczenia.md](./drzwi-i-pomieszczenia.md)) — kluczowe dla Głosu Przestrzennego i Prywatnego Przesłuchania.
- Ewentualne przyszłe: tablica Notatek Detektywa, włączniki światła, przedmioty-rekwizyty sprawy.

## Przypadki brzegowe (obsłużone)

- Interakcja z samym sobą — odrzucana po obu stronach.
- Cel zniszczony między klikiem a komendą — `NetworkServer.spawned` nie zawiera netId, komenda ignorowana.
- Interakcja przez ścianę — odrzucana serwerowym testem line-of-sight.

## Luki do domknięcia

1. **Brak feedbacku UI** — nie ma podświetlenia/promptu „[E] Podnieś" gdy celownik wskazuje obiekt interaktywny. Do dodania w `RoundPresenter`/HUD (czysto kliencki odczyt tego samego raycastu).
2. **Interakcja przytrzymywana / progresywna** — obecny model jest wyłącznie „single press". Jeśli jakaś mechanika będzie wymagać przytrzymania (np. podnoszenie ciała), interfejs trzeba będzie rozszerzyć — nie robić tego na zapas.

## Kryteria akceptacji

- Interakcja działa u hosta i klienta zdalnego (KCP, dwie instancje).
- Nie da się użyć obiektu przez ścianę ani spoza zasięgu, nawet modyfikując klienta.
