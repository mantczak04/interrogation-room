# Dokumentacja projektu

Punktem wejścia do reguł produktu i kanonicznego słownika jest
[`CONTEXT.md`](../CONTEXT.md). Dokumenty w tym katalogu rozwijają decyzje
projektowe, architekturę oraz procedury weryfikacji.

## Architektura

- [`MVP-ARCHITECTURE.md`](architecture/MVP-ARCHITECTURE.md) — granice modułów,
  przepływ Rundy i kolejność implementacji pionowego wycinka.
- [`STEAM-NETWORKING.md`](architecture/STEAM-NETWORKING.md) — lobby Steam,
  FizzySteamworks i awaryjny transport KCP.
- [`adr/`](adr/) — trwałe uzasadnienia zatwierdzonych decyzji.

## Projekt gry

- [`MECHANICS-OVERVIEW.md`](design/MECHANICS-OVERVIEW.md) — indeks mechanik.
- [`design/mechanics/`](design/mechanics/) — szczegółowe kontrakty mechanik.
- [`OPEN-QUESTIONS.md`](design/OPEN-QUESTIONS.md) — decyzje nadal odłożone.
- [`MAP-MVP.md`](design/MAP-MVP.md) — zakres i kontrakt mapy MVP.
- [`PLAYTEST-CONTENT-CATALOG.md`](design/PLAYTEST-CONTENT-CATALOG.md) — katalog
  treści do testów.

## Grafika i UI

- [`GRAPHICS-ROADMAP.md`](design/GRAPHICS-ROADMAP.md) — plan prac graficznych.
- [`design/graphics/README.md`](design/graphics/README.md) — indeks dokumentów
  grafiki.
- [`UI-STYLE-GUIDE.md`](design/ui/UI-STYLE-GUIDE.md) — zasady interfejsu.
- [`map-polish/`](map-polish/) — raporty kolejnych przebiegów dopracowania mapy.

## Integracja, testy i badania

- [`integration/`](integration/) — handoffy integracyjne mechanik fizycznych.
- [`playtests/`](playtests/) — procedury i raporty testów.
- [`proximity-voice-tools.md`](research/proximity-voice-tools.md) — historyczne
  badanie narzędzi voice; aktualna decyzja znajduje się w
  [`glos-przestrzenny.md`](design/mechanics/glos-przestrzenny.md).
