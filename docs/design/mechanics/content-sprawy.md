# Content sprawy (`CaseAsset` → `CaseDefinition`)

**Status:** ❌ Do zaimplementowania
**Priorytet:** Must-have (MVP) — krok 2 kolejności implementacji
**Docelowy kod:** `Assets/Scripts/Game/Content/CaseAsset.cs`, `Assets/Scripts/Game/Domain/CaseDefinition.cs`

## Cel

Sprawy (Przestępstwo + Alibi) są **ręcznie autorowane** (ADR-0010) — żadnego generowania treści AI w runtime. Potrzebny jest pomost: wygodny w edytorze `ScriptableObject` do pisania spraw i niezmienny, czysty obiekt domenowy, który trafia do `RoundEngine`.

## Zasada działania

### `CaseAsset` (ScriptableObject, warstwa Unity)

- Pola autorskie: tytuł sprawy, treść Przestępstwa (jawna), lista faktów Alibi (tekst + flaga `możliwyDoUkrycia`), liczba/zakres faktów ukrywanych Winnemu.
- Walidacja w edytorze (`OnValidate`): min. liczba faktów, co najmniej tyle faktów `możliwyDoUkrycia`, ile wynosi maksimum ukrywanych, niepuste treści.
- Żadnej logiki gry — tylko dane i walidacja autorska.

### `CaseDefinition` (domena, immutable)

- Tworzony z `CaseAsset` przed `StartRound` (`caseAsset.ToDefinition()`); zwykłe typy C#, brak referencji do Unity.
- Testy Edit Mode budują `CaseDefinition` bezpośrednio w kodzie — bez assetów.
- Świadomie **bez** interfejsu katalogu spraw: przy jednej–kilku sprawach warstwa abstrakcji nie ma wartości (decyzja z MVP-ARCHITECTURE).

### Zawartość sprawy MVP

- Jedna testowa Sprawa wystarcza dla slice'a. Struktura przykładu:
  - Przestępstwo: absurdalny, publicznie znany czyn (rama wspólna, nie wskazuje Winnego).
  - Alibi: 6–10 krótkich faktów opisujących, co grupa robiła w czasie Przestępstwa.
  - 2–3 fakty oznaczone jako możliwe do ukrycia.

## Zasady

- Treść sprawy jest sekretem gameplayowym tak samo jak role: pełny `CaseDefinition` żyje tylko na hoście; klienci dostają fragmenty przez `PlayerRoundView`.
- Fakty muszą być pisane tak, by dały się relacjonować ustnie (gra dzieje się w rozmowie głosowej) — krótkie, konkretne, z detalami do przekręcenia.

## Zależności

- Konsument: `RoundEngine` ([silnik-rundy.md](./silnik-rundy.md)) i redagowanie ([alibi-i-redagowanie.md](./alibi-i-redagowanie.md)).
- Wybór sprawy przed startem: MVP — jedno pole na `NetworkRoundCoordinator`/hostowym UI.

## Kryteria akceptacji

- `CaseAsset.ToDefinition()` produkuje kompletny, niezmienny obiekt; mutacja assetu po konwersji nie zmienia definicji.
- Walidacja edytorowa blokuje sprawę, w której nie da się ukryć wymaganej liczby faktów.
- Testy domenowe nie referencjonują `CaseAsset` ani `UnityEngine`.
