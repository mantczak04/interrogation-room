# B6 — konfiguracja i test Vivox

## Jednorazowe odblokowanie Unity Dashboard

Projekt Unity jest połączony z Unity Cloud i ma zainstalowany pakiet `com.unity.services.vivox` 16.11.0, ale usługa Vivox musi jeszcze dostać własne credentiale.

1. Otwórz Unity Dashboard dla projektu `interrogation-room`.
2. Wejdź w `Development > Products > Vivox Voice and Text Chat` i wykonaj guided setup.
3. W Unity otwórz `Edit > Project Settings > Services` i potwierdź poprawne połączenie projektu.
4. W `Edit > Project Settings > Vivox` ustaw środowisko na `Automatic`, aby Unity pobrało credentiale.
5. Uruchom scenę `Room`, host KCP i sprawdź w Console wpis `[Vivox] Joined positional channel`.

Brak konfiguracji jest jednoznacznie widoczny jako stan `Faulted` i błąd Vivox `'server' is null or empty`. Brak mikrofonu daje osobny stan `NoInputDevice` i nie blokuje odbierania głosu.

## Test KCP / ParrelSync

1. Uruchom hosta i minimum dwa klony jako klientów KCP.
   Każdy lokalny gracz używa osobnego profilu UGS wyprowadzonego z jego `netId`, aby klony ParrelSync nie współdzieliły anonimowej sesji Authentication.
2. Potwierdź na każdym kliencie stan `Ready` oraz dostępne urządzenie wejściowe.
3. W jednym pokoju sprawdź spadek głośności z dystansem i brak echa lokalnego głosu.
4. Umieść graczy po obu stronach portalu z otwartymi drzwiami: głos pozostaje czytelny z lekkim tłumieniem.
5. Zamknij drzwi: participant Audio Tap przechodzi do `ClosedPortalPath`, głośność spada, a low-pass wyraźnie tłumi mowę bez tworzenia prywatnego kanału.
6. Przejdź między pomieszczeniami i ponownie otwórz drzwi; parametry powinny przejść płynnie, bez utraty kanału.
7. Odłącz mikrofon jednego klienta i potwierdź `NoInputDevice`, podczas gdy pozostali nadal słyszą się nawzajem.
8. Dołącz klienta po rozpoczęciu sesji i potwierdź powstanie jego participant Audio Tap po zarejestrowaniu obiektu Mirror.

## Diagnostyka

- `WaitingForNetwork` — brak lokalnego gracza Mirror.
- `InitializingServices` — inicjalizacja UGS/Vivox.
- `JoiningChannel` — logowanie zakończone, trwa wejście do positional channel.
- `Ready` — voice i mikrofon są gotowe.
- `NoInputDevice` — brak mikrofonu; odbieranie voice pozostaje dostępne.
- `Recovering` — Vivox odzyskuje połączenie.
- `Disconnected` — sesja Mirror została zakończona.
- `Faulted` — konfiguracja lub połączenie Vivox nie pozwala uruchomić voice.

Skrót `V` przełącza mute wejścia. Ikona mikrofonu nie przechwytuje kliknięć UI: biała oznacza gotowość, zielona wykrytą mowę, a czerwona mute albo błąd inicjalizacji.
