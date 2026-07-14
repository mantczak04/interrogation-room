# Silnik Rundy (`RoundEngine`)

**Status:** ✅ Bazowy vertical slice zaimplementowany; rozszerzenie o Prywatne Cele, Incydenty i Ucieczkę zatwierdzone do implementacji
**Priorytet:** Must-have (MVP) dla obecnego rdzenia; następny filar po slice dla [rozszerzenia](./prywatne-cele-incydenty-i-ucieczka.md)
**Docelowy kod:** `Assets/Scripts/Game/Domain/` (osobne asmdef bez Unity/Mirror)

## Cel

Czysty moduł C# będący jedynym źródłem reguł Rundy: fazy, role, dostęp do Alibi, Limit Rundy, Egzekucja i rozstrzygnięcie wyników. Zatwierdzone rozszerzenie doda do tej samej domeny przypisanie i postęp Prywatnych Celów, Tropy do Alibi oraz Ucieczkę. Wszystko inne (sieć, UI, broń, głos i interakcje scenowe) pozostaje adapterem wokół niego.

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
- **Runda**: swobodna rozgrywka; działa Limit Rundy; dozwolona jest jedna Egzekucja, a po rozszerzeniu także wykonywanie Celów i próba Ucieczki.
- **Zakończona**: po Egzekucji, skutecznej Ucieczce albo upływie Limitu Rundy; wszystkie komendy poza odczytem wyników są odrzucane.

### Obecne komendy bazowego slice (`RoundCommand`)

| Komenda | Dozwolona w | Efekt |
|---|---|---|
| `StartRound(CaseDefinition, skład, seed)` | Lobby | walidacja Składu Rundy, przydział ról, wejście w Przygotowanie |
| `EndPreparation` | Przygotowanie | wejście w Rundę, start Limitu Rundy |
| `Execute(PlayerId cel)` | Runda | rozstrzygnięcie i koniec Rundy |
| `TimeExpired` | Runda | koniec Rundy, przegrana Detektywa |

Komenda niedozwolona w bieżącym stanie zwraca odrzucenie (bez wyjątku, bez zmiany stanu).

Komendy postępu Celów, odkrycia Incydentu i Ucieczki zostaną zaprojektowane przy implementacji zatwierdzonego rozszerzenia. Ich dokładne nazwy nie są jeszcze publicznym API; reguły opisuje [osobna specyfikacja](./prywatne-cele-incydenty-i-ucieczka.md).

### Reguły egzekwowane przez moduł (niezmienniki)

1. Skład Rundy: 3–6 graczy, dokładnie 1 Detektyw, 1 Winny, 1–4 Niewinnych (ADR-0001).
2. Alibi: pełne dla Niewinnych, zredagowane dla Winnego, żadne dla Detektywa (ADR-0006).
3. Alibi niedostępne po Przygotowaniu — `ViewFor` po prostu przestaje je zwracać (ADR-0007).
4. Najwyżej jedna Egzekucja; druga jest odrzucana (ADR-0003).
5. Egzekucja Winnego → wygrana Detektywa; Niewinnego → przegrana Detektywa; upływ Limitu → przegrana Detektywa (ADR-0003/0004).
6. Wyniki Niewinnych są indywidualne: każdy potrzebuje ukończenia własnego Prywatnego Celu i Przetrwania. Sekretny Cel dodatkowo wymaga ukończonego Wrobienia i Egzekucji wskazanego Celu (ADR-0013).
7. Detektyw nie może być celem Egzekucji; egzekucja samego siebie odrzucana.
8. Skuteczna Ucieczka Winnego kończy Rundę jego zwycięstwem i przegraną Detektywa; nie odbiera zwycięstwa Niewinnym, którzy ukończyli Cel i Przetrwali.

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

## Następne rozszerzenie

- Każdy Niewinny otrzymuje dokładnie jeden Prywatny Cel. Przy 3–4 graczach liczba Sekretnych Celów jest wymuszona na `0`; przy 5–6 domyślnie wynosi `1`, a host może wybrać `0`.
- Bunt nie otrzymuje osobnych komend ani stanu. Jest emergentnym skutkiem indywidualnych wyników i Ucieczki.
- Dokładne typy komend oraz definicji contentu należy ustalić przy implementacji bez naruszania istniejących publicznych seamów `Handle` i `ViewFor`.
