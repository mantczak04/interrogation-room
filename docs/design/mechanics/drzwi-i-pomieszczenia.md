# Drzwi i pomieszczenia

**Status:** ❌ Do zaimplementowania
**Priorytet:** Must-have — nośnik prywatności głosu i Prywatnego Przesłuchania
**Docelowy kod:** `Assets/Scripts/Gameplay/Interaction/NetworkDoor.cs` (nowy `INetworkInteractable`) + definicje pomieszczeń dla `VoiceOcclusion`

## Cel

Prywatność w tej grze pochodzi z przestrzeni, nie z kanałów (ADR-0009). Drzwi i pomieszczenia to fizyczna mechanika, która realizuje Prywatne Przesłuchanie: Detektyw zamyka się z Podejrzanym w pokoju, a reszta może co najwyżej podsłuchiwać pod drzwiami. Bez drzwi Głos Przestrzenny nie ma czego tłumić.

## Zasada działania

### Drzwi (`NetworkDoor : NetworkBehaviour, INetworkInteractable`)

1. Interakcja E przez istniejący framework ([interakcja-z-obiektami.md](./interakcja-z-obiektami.md)) — walidacja zasięgu i line-of-sight już działa.
2. Stan `SyncVar bool isOpen` (hook animuje skrzydło u wszystkich klientów). Stan drzwi jest **jawny** — nie jest sekretem, więc `SyncVar` jest tu poprawny.
3. Serwer przełącza stan w `TryInteractServer`; krótki cooldown (~0,3 s) przeciw trzepaniu drzwiami.
4. Fizyka: zamknięte drzwi mają collider blokujący ruch i pociski; otwarte — nie blokują (obrót skrzydła z colliderem).
5. Dźwięk otwarcia/zamknięcia jako 3D one-shot — sam w sobie jest informacją („ktoś wszedł").
6. Każdy może otwierać każde drzwi (MVP). Zamykanie na klucz — poza zakresem, dopóki użytkownik nie zdecyduje inaczej.

### Pomieszczenia (dane dla akustyki)

- Pokoje zdefiniowane jako wolumeny (trigger box / komponent `RoomVolume` z identyfikatorem), drzwi jako **portale** łączące dwa pokoje (referencje w komponencie drzwi).
- Graf pokoi i portali konsumuje `VoiceOcclusion` ([glos-przestrzenny.md](./glos-przestrzenny.md)): kategoria ścieżki słuchacz→mówca zależy od wspólnego pokoju i stanu drzwi po drodze.
- Przynależność gracza do pokoju: śledzona lokalnie (trigger enter/exit lub test punktu w wolumenie) — to dane czysto kliencko-akustyczne, nie domenowe.

### Układ posterunku (level design pod mechaniki)

- Minimum dla slice'a: 1 przestrzeń wspólna + 2–3 pokoje przesłuchań z drzwiami.
- Pokój przesłuchań musi być na tyle mały, że rozmowa wewnątrz jest czytelna, a na tyle odizolowany, że przez zamknięte drzwi słychać tylko pomruk.
- Drzwi z „cieniem akustycznym" pod progiem: podsłuchiwanie pod drzwiami ma być realną, ryzykowną taktyką.

## Zależności

- Framework interakcji (istnieje), `VoiceOcclusion` (konsument stanu), pociski (zamknięte drzwi zatrzymują raycast strzału — collider zwykły, nie trigger).

## Kryteria akceptacji

- Dwóch klientów: otwarcie drzwi u jednego natychmiast widoczne i słyszalne u drugiego; late-joiner widzi poprawny stan.
- Zamknięte drzwi blokują ruch, pociski i (po integracji voice) tłumią głos.
- Spam E nie desynchronizuje animacji ze stanem.
