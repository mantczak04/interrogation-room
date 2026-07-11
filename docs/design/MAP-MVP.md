# Mapa pierwszego slice'a — propozycja (niezatwierdzona)

Status: **propozycja do dyskusji**, nie ADR. Wynika z [ADR-0005](../adr/0005-continuous-free-roaming-rounds.md) (zwarta lokacja, swobodny ruch), [ADR-0009](../adr/0009-voice-privacy-comes-from-space.md) (prywatność z przestrzeni) i [researchu voice](../research/proximity-voice-tools.md) (portalowy model akustyki: pokoje + drzwi).

## Wymagania wynikające z wizji

1. **Zwarta lokacja** — Detektyw musi w Limicie Rundy zdążyć z kilkoma Prywatnymi Przesłuchaniami; przejście przez całą mapę nie powinno trwać dłużej niż ~6–8 s (przy 5 m/s z `PlayerController`).
2. **Pokój Przesłuchań** — odizolowane pomieszczenie z jednymi drzwiami; pod drzwiami musi istnieć miejsce, z którego podsłuch jest możliwy, ale widoczny dla wchodzących.
3. **Przestrzeń wspólna** — sala, w której Podejrzani naturalnie się mijają i prowadzą równoległe rozmowy.
4. **Pokoje boczne** — co najmniej dwa małe pomieszczenia na prywatne rozmowy Podejrzanych (zmowy, Sekretne Cele), żeby prywatność nie była monopolem Detektywa.
5. **Drzwi jako jedyne portale dźwięku** — pomieszczenia wypukłe, pełne ściany i sufit; akustyka steruje głośnością i low-passem per portal, więc geometria musi jednoznacznie wyznaczać graf pokoi.
6. **Czytelność podsłuchu** — układ musi wymuszać ryzyko: podsłuchujący pod drzwiami stoi w korytarzu, którym chodzą inni.

## Proponowany układ: „Posterunek” (jedna kondygnacja)

```text
+--------------------+   +-------------+
|                    |   |   POKÓJ     |
|    SALA WSPÓLNA    | D |PRZESŁUCHAŃ  |
|     ~12 × 9 m      |   |  ~5 × 4 m   |
|  (spawn 6 graczy)  |   +------D------+
|                    |          |
+---------D----------+   KORYTARZ ~2.5 m
          |                     |
          +----------+----------+
          |          |          |
     +----D---+  +---D----+     |
     | POKÓJ  |  | ARCHIWUM|    |
     |SOCJALNY|  | ~4 × 3 m|    |
     | ~4×3 m |  +---------+    |
     +--------+                 |
```

- `D` — drzwi (szerokość 1.2 m, wysokość 2.1 m); ściany 3 m, zamknięty sufit.
- Sala Wspólna i Pokój Przesłuchań rozdzielone tak, by droga z każdego punktu sali pod drzwi przesłuchań prowadziła przez korytarz — podsłuchujący jest widoczny.
- Najdłuższa trasa (róg Sali Wspólnej → Archiwum) ≈ 30 m ≈ 6 s biegu.

## Zawartość sceny graybox

- Podłoga, ściany i sufit z ProBuildera/prymitywów z colliderami; bez tekstur, kolory rozróżniające pomieszczenia.
- **Otwory drzwiowe** z placeholderem skrzydła (pivot na zawiasie) — na razie statycznie otwarte; sieciowy stan drzwi to osobne zadanie `VoiceRuntime`.
- **Strefy pokoi** (`BoxCollider` trigger na pomieszczenie) — przyszłe wejście dla `VoiceOcclusion` do wyznaczania grafu pokoi.
- **6 punktów spawnu** (`NetworkStartPosition`) w Sali Wspólnej.
- Jedno światło kierunkowe + proste światła punktowe w pokojach; bez bakingu.

## Zależności i otwarte kwestie

- Krzywe zasięgu głosu i presety drzwi (otwarte/uchylone/zamknięte) wyjdą dopiero ze spike'a Dissonance — wymiary pokoi trzeba będzie iterować po pierwszym teście dwuosobowym.
- Czy cienkie ściany przepuszczają ton głosu, czy tylko drzwi są portalem — nierozstrzygnięte (research §otwarte pytania); graybox zakłada wariant „tylko drzwi”.
- Temat/setting (posterunek vs inne wnętrze) — do decyzji zespołu; układ pomieszczeń jest od niego niezależny.
- Miejsce Egzekucji nie wymaga dedykowanej przestrzeni w MVP (Egzekucja to decyzja w UI) — nie dodajemy celi ani pokoju egzekucji bez decyzji.
