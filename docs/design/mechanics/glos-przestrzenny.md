# Głos Przestrzenny (voice chat + akustyka)

**Status:** ❌ Do zaimplementowania (research zakończony, wybrane narzędzie: Dissonance)
**Priorytet:** Must-have — bez głosu gra praktycznie nie istnieje (cała rozgrywka to rozmowa)
**Docelowy kod:** `Assets/Scripts/Game/Voice/` (`VoiceRuntime`, `VoiceOcclusion.cs`)
**Research:** [proximity-voice-tools.md](../../research/proximity-voice-tools.md)

## Cel

Jedyny kanał rozmowy podczas Rundy: słyszalność zależy wyłącznie od odległości, pomieszczeń i stanu drzwi (ADR-0009). Prywatne Przesłuchanie nie tworzy osobnego kanału — prywatność daje zamknięcie drzwi, a podsłuchiwanie wymaga fizycznego podejścia. To mechanika definiująca grę.

## Zasada działania

### Stack (decyzja z researchu)

- **Dissonance Voice Chat 9.0.9** (Asset Store, jednorazowa licencja) z integracją Mirror — jedzie po istniejącym transporcie (KCP lokalnie, FizzySteamworks na Steam).
- Wyłącznie **positional proximity channel** z VAD; bez kanału globalnego i bez kanałów prywatnych.
- Tracker pozycji (`IDissonancePlayer`) na prefabie `Player` — pozycje już synchronizuje Mirror.

### Własna warstwa akustyczna (per słuchacz-mówca)

Dissonance daje 3D i tłumienie dystansem; ściany/drzwi wymagają własnego małego modułu `VoiceOcclusion`:

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

1. Import Dissonance + demo na KCP, rozmowa dwóch klientów.
2. Tracker na prefabie gracza, sam proximity channel.
3. `VoiceOcclusion` z modelem portalowym i strojeniem na jednym pokoju + drzwiach.
4. Test na dwóch kontach Steam przez FizzySteamworks (wymóg z AGENTS.md).
5. Spike zapasowy Vivox tylko, jeśli Dissonance+Fizzy okaże się niestabilne.

## Zależności

- Drzwi i pomieszczenia (stan drzwi wpływa na tłumienie), transport Mirror (istnieje), prefab gracza.

## Kryteria akceptacji

- Dwóch graczy w jednym pomieszczeniu rozmawia czysto; po zamknięciu drzwi trzeci gracz za drzwiami słyszy stłumiony, nieczytelny pomruk; pod drzwiami — częściowo zrozumiały.
- Brak echa u hosta (lokalny głos nie wraca).
- Działa na KCP i na FizzySteamworks (dwa konta, dwie maszyny).

## Otwarte pytania

- Host migration a sesja voice — wymaga osobnego testu (research, sekcja pytań).
- Dokładne krzywe tłumienia — wyłącznie playtest.
