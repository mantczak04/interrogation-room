# Głos Przestrzenny (voice chat + akustyka)

**Status:** 🔶 Implementacja B6 w toku (zatwierdzony provider: Vivox)
**Priorytet:** Must-have — bez głosu gra praktycznie nie istnieje (cała rozgrywka to rozmowa)
**Docelowy kod:** `Assets/Scripts/Game/Voice/` (`VoiceRuntime`, `VoiceOcclusion.cs`)
**Research:** [proximity-voice-tools.md](../../research/proximity-voice-tools.md). Pierwotna rekomendacja Dissonance została zastąpiona decyzją właściciela projektu z 2026-07-14: Vivox jest już obecny w repo i pozostaje bezpłatny do 5000 PCU.

## Cel

Jedyny kanał rozmowy podczas Rundy: słyszalność zależy wyłącznie od odległości, pomieszczeń i stanu drzwi (ADR-0009). Prywatne Przesłuchanie nie tworzy osobnego kanału — prywatność daje zamknięcie drzwi, a podsłuchiwanie wymaga fizycznego podejścia. To mechanika definiująca grę.

## Zasada działania

### Stack (zatwierdzona decyzja B6)

- **Vivox Unity SDK 16.11.0** przez Unity Gaming Services, niezależnie od transportu gameplayowego Mirror.
- Wyłącznie **positional channel** z VAD; bez kanału globalnego i bez kanałów prywatnych.
- Pozycja lokalnego gracza jest raportowana do Vivox 2–4 razy na sekundę, a zdalny głos trafia do per-participant Audio Tap przypiętego do zreplikowanego obiektu `Player`.

### Własna warstwa akustyczna (per słuchacz-mówca)

Vivox daje kanał 3D i tłumienie dystansem; ściany/drzwi obsługuje własny mały moduł `VivoxVoiceOcclusion`:

1. Model **portalowy**: pomieszczenia jako wolumeny + drzwi jako portale między nimi ([drzwi-i-pomieszczenia.md](./drzwi-i-pomieszczenia.md)).
2. Dla każdej pary (lokalny słuchacz, zdalny mówca) co ~0,1–0,2 s licz kategorię ścieżki: to samo pomieszczenie / przez otwarte drzwi / przez zamknięte drzwi / przez ścianę.
3. Na `AudioSource`/głosie mówcy ustawiaj per kategoria: mnożnik głośności + filtr dolnoprzepustowy (low-pass). Orientacyjny start do strojenia: otwarte drzwi ≈ lekkie stłumienie; zamknięte drzwi ≈ mocny low-pass + duży spadek głośności (zrozumienie rozmowy wymaga przyłożenia ucha); ściana ≈ praktycznie niesłyszalne.
4. Płynne przejścia (interpolacja parametrów), żeby otwarcie drzwi nie „strzelało" głośnością.
5. Kalibrować pod **czytelność dialogu**, nie realizm (wniosek researchu; Steam Audio odrzucone na ten etap).

### Zasady

- Voice jest niezależny od `RoundEngine` — działa też w Lobby i po zakończeniu Rundy (rozmowa przy wynikach), chyba że użytkownik zdecyduje inaczej.
- Martwi gracze: w MVP nieistotne (Egzekucja kończy Rundę natychmiast).
- Mikrofon: VAD domyślnie; push-to-talk jako opcja ustawień (nice-to-have).

### Plan wdrożenia (z researchu, skrót)

1. Włączyć istniejący runtime Vivox i potwierdzić logowanie UGS oraz positional channel.
2. Powiązać participant Audio Tap ze zreplikowanym obiektem `Player`.
3. `VivoxVoiceOcclusion` z grafem `RoomVolume` / `IRoomPortalState` oraz płynnym strojeniem głośności i low-pass.
4. Test host + minimum dwa klienty na KCP/ParrelSync; transport Steam nie jest wymagany przez Vivox i pozostaje osobnym testem gameplayu.

## Zależności

- Drzwi i pomieszczenia (stan drzwi wpływa na tłumienie), transport Mirror (istnieje), prefab gracza.

## Kryteria akceptacji

- Dwóch graczy w jednym pomieszczeniu rozmawia czysto; po zamknięciu drzwi trzeci gracz za drzwiami słyszy stłumiony, nieczytelny pomruk.
- Brak echa u hosta (lokalny głos nie wraca).
- Działa podczas sesji gameplayowej na KCP; późniejsza walidacja Steam dotyczy współistnienia z FizzySteamworks, nie transportu pakietów Vivox.

## Otwarte pytania

- Host migration a sesja voice — wymaga osobnego testu (research, sekcja pytań).
- Dokładne krzywe tłumienia — wyłącznie playtest.
