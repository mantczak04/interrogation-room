# Silnik Rundy (`RoundEngine`)

**Status:** ❌ Do zaimplementowania — **najważniejsza brakująca mechanika**
**Priorytet:** Must-have (MVP) — krok 1 kolejności implementacji z [MVP-ARCHITECTURE.md](../../architecture/MVP-ARCHITECTURE.md)
**Docelowy kod:** `Assets/Scripts/Game/Domain/` (osobne asmdef bez Unity/Mirror)

## Cel

Czysty moduł C# będący jedynym źródłem reguł Rundy: fazy, role, dostęp do Alibi, Limit Rundy, Egzekucja, rozstrzygnięcie wyników. Wszystko inne (sieć, UI, broń, głos) jest adapterem wokół niego. Bez tego modułu żadna mechanika „rundowa" nie ma się do czego podpiąć.

## Zasada działania

### Interfejs (zatwierdzony w architekturze)

```csharp
RoundTransition Handle(RoundCommand command);
PlayerRoundView ViewFor(PlayerId viewer);
```

- `Handle` przyjmuje wyłącznie intencje i zwraca nowy stan publiczny + zdarzenia do rozesłania; **zero efektów ubocznych** wewnątrz modułu.
- `ViewFor` to jedyna droga odczytu — filtruje informacje wg roli i fazy (ADR-0011).

### Maszyna stanów

```
Lobby → Przygotowanie → Runda → Zakończona
```

- **Przygotowanie**: Podejrzani widzą swoje wersje Alibi; Detektyw nie widzi nic z Alibi. Kończy się komendą (host/timer) — po tym Alibi znika bezpowrotnie (ADR-0007).
- **Runda**: swobodna rozgrywka; działa Limit Rundy; dozwolona dokładnie jedna Egzekucja.
- **Zakończona**: po Egzekucji albo upływie Limitu Rundy; wszystkie komendy poza odczytem wyników są odrzucane.

### Komendy MVP (`RoundCommand`)

| Komenda | Dozwolona w | Efekt |
|---|---|---|
| `StartRound(CaseDefinition, skład, seed)` | Lobby | walidacja Składu Rundy, przydział ról, wejście w Przygotowanie |
| `EndPreparation` | Przygotowanie | wejście w Rundę, start Limitu Rundy |
| `Execute(PlayerId cel)` | Runda | rozstrzygnięcie i koniec Rundy |
| `TimeExpired` | Runda | koniec Rundy, przegrana Detektywa |

Komenda niedozwolona w bieżącym stanie zwraca odrzucenie (bez wyjątku, bez zmiany stanu).

### Reguły egzekwowane przez moduł (niezmienniki)

1. Skład Rundy: 4–6 graczy, dokładnie 1 Detektyw, 1 Winny, 2–4 Niewinnych (ADR-0001).
2. Alibi: pełne dla Niewinnych, zredagowane dla Winnego, żadne dla Detektywa (ADR-0006).
3. Alibi niedostępne po Przygotowaniu — `ViewFor` po prostu przestaje je zwracać (ADR-0007).
4. Najwyżej jedna Egzekucja; druga jest odrzucana (ADR-0003).
5. Egzekucja Winnego → wygrana Detektywa; Niewinnego → przegrana Detektywa; upływ Limitu → przegrana Detektywa (ADR-0003/0004).
6. Wyniki Niewinnych są indywidualne: Przetrwanie per gracz; przy włączonym Sekretnym Celu — przetrwanie właściciela **i** eliminacja Celu (ADR-0002).
7. Detektyw nie może być celem Egzekucji; egzekucja samego siebie odrzucana.

### Determinizm i czas

- Moduł **nie czyta zegara** — upływ czasu wchodzi wyłącznie komendą `TimeExpired` od adaptera sieciowego. Dzięki temu całość jest testowalna Edit Mode bez czasu rzeczywistego.
- Losowość (przydział ról, wybór ukrytych faktów) przez seed przekazany w `StartRound` — powtarzalne testy.

## Zależności

- Wejście: `CaseDefinition` ([content-sprawy.md](./content-sprawy.md)).
- Konsument: `NetworkRoundCoordinator` ([prywatne-widoki-sieciowe.md](./prywatne-widoki-sieciowe.md)).
- Konsument widoków: `RoundPresenter` ([ui-rundy.md](./ui-rundy.md)).

## Testy (Edit Mode, przez publiczny interfejs — lista z architektury)

1. Poprawny Skład Rundy zawsze ma jednego Detektywa i jednego Winnego.
2. Każdy gracz otrzymuje wyłącznie dozwolone informacje.
3. Winny widzi dokładnie skonfigurowane braki w Alibi.
4. Po Przygotowaniu żaden Podejrzany nie otrzymuje treści Alibi.
5. Egzekucja Winnego daje zwycięstwo Detektywowi.
6. Egzekucja Niewinnego daje przegraną Detektywowi i kończy Rundę.
7. Druga Egzekucja oraz komendy po zakończeniu Rundy są odrzucane.
8. Upływ Limitu Rundy bez Egzekucji kończy Rundę przegraną Detektywa.

## Otwarte pytania (nie blokują implementacji)

- Sekretne Cele: liczba domyślna nierozstrzygnięta ([OPEN-QUESTIONS.md](../OPEN-QUESTIONS.md)) — moduł powinien przyjmować konfigurację `0..N`, z `0` jako bezpiecznym MVP.
- Bunt — poza zakresem; nie projektować pod niego seamów na zapas.
