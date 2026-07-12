# Graphics — tracker postępu i zasady pracy

Nadrzędny plan: [GRAPHICS-ROADMAP.md](../GRAPHICS-ROADMAP.md). Ten katalog rozbija roadmapę na wykonawcze dokumenty per faza. Każda faza ma własny branch i własny dokument z checklistą zadań.

## Tablica statusów

Statusy: `Open` → `In progress` → `Review` → `Approved` → `Done`. `Approved` oznacza fazę zaakceptowaną przez użytkownika i obecną na branchu integracyjnym; `Done` oznacza dopiero merge do `main`. Dodatkowo `Blocked` z powodem.

| Faza | Dokument | Branch | Status |
|---|---|---|---|
| 0. Fundamenty URP | [FAZA-0-fundamenty-urp.md](./FAZA-0-fundamenty-urp.md) | `gfx/faza-0-fundamenty-urp` | Approved |
| 1. Art direction | [FAZA-1-art-direction.md](./FAZA-1-art-direction.md) | `gfx/faza-1-art-direction` | Approved |
| 2. Oświetlenie | [FAZA-2-oswietlenie.md](./FAZA-2-oswietlenie.md) | `gfx/graphics-overhaul` | Review |
| 3. Materiały i kit | [FAZA-3-materialy-kit.md](./FAZA-3-materialy-kit.md) | `gfx/faza-3-materialy-kit` | Open |
| 4. Post-processing | [FAZA-4-postprocessing.md](./FAZA-4-postprocessing.md) | `gfx/graphics-overhaul` | Review |
| 5. Postacie | [FAZA-5-postacie.md](./FAZA-5-postacie.md) | `gfx/faza-5-postacie` | Open |
| 6. VFX i mikrodetale | [FAZA-6-vfx-detale.md](./FAZA-6-vfx-detale.md) | `gfx/faza-6-vfx-detale` | Open |
| 7. Optymalizacja | [FAZA-7-optymalizacja.md](./FAZA-7-optymalizacja.md) | `gfx/faza-7-optymalizacja` | Open |

## Kolejność i zależności

```text
Faza 0 ──► Faza 4 ──► Faza 2 ──► Faza 3 ──► Faza 5 ──► Faza 6 ──► Faza 7
             (Faza 1 równolegle — decyzje użytkownika, nie kod)
```

- Fazy 0, 4, 2 to „pakiet startowy" — wykonywane teraz, przed pełnym art passem.
- Faza 1 wymaga decyzji użytkownika; można ją prowadzić równolegle, ale Fazy 3 i 5 nie mogą się zacząć przed jej zamknięciem.
- Faza 7 częściowo ciągła: budżety wydajności z jej dokumentu obowiązują we wszystkich fazach.

## Zasady pracy dla agenta wykonującego fazę

1. **Przeczytaj najpierw:** `AGENTS.md` (całość, obowiązkowo), potem dokument swojej fazy. Nie czytaj dokumentów innych faz, jeśli nie są wskazane jako zależność.
2. **Branch:** standardowo utwórz branch fazy z aktualnego `main`. Dla zatwierdzonego pakietu grafiki opisanego w [AUTONOMOUS-GRAPHICS-RUN.md](./AUTONOMOUS-GRAPHICS-RUN.md) pracuj kolejno na `gfx/graphics-overhaul`; nie commituj do `main`.
3. **Statusy:** przy rozpoczęciu pracy zmień status fazy na `In progress` w tej tabeli ORAZ w nagłówku dokumentu fazy (jeden commit). Po ukończeniu wszystkich zadań i weryfikacji ustaw `Review`. Użytkownik ustawia `Approved`; `Done` dopiero po merge do `main`.
4. **Checklisty:** w dokumencie fazy odhaczaj zadania (`- [x]`) w miarę ukończenia i commituj razem ze zmianą, której dotyczą. Zadania oznaczone `[CZŁOWIEK]` wykonuje użytkownik — jeśli blokują dalsze kroki, ustaw status `Blocked` z opisem i zakończ pracę.
5. **Unity MCP:** wszystkie operacje w Unity Editor wykonuj przez Unity MCP zgodnie z zasadami w `AGENTS.md`. Obowiązuje bezwzględny STOP RULE: jeśli Unity MCP nie ma potrzebnej możliwości — zatrzymaj się i zgłoś, nie edytuj YAML scen/prefabów/assetów ręcznie.
6. **Commity:** małe, jednotematyczne, po polsku lub angielsku w stylu istniejącej historii (`fix:`, `feat:`, `chore:`). Bez stopki Co-Authored-By.
7. **Weryfikacja przed `Review`:** brak błędów kompilacji, brak błędów w Unity Console, scena zapisana przez Unity MCP, `git diff --check` czysty, zrzut ekranu „przed/po" zapisany do `Assets/Screenshots/` z prefiksem `gfx_fazaN_`.
8. **Nie ruszaj:** katalogów wymienionych w `AGENTS.md` (`Library/`, vendor itd.), plików `*.meta`, `Packages/packages-lock.json`, oraz niezwiązanych zmian użytkownika widocznych w `git status`.
9. **Budżety wydajności** (obowiązują zawsze): 60 FPS na sprzęcie średniej klasy, maks. ~4 realtime światła z cieniami w kadrze, pojedynczy pokój ≤ 150 tys. trisów po art passie.
