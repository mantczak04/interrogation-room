# Narzędzia do przestrzennego voice chatu

Stan researchu: **2026-07-10**. Zakres: Unity 6, Mirror, FizzySteamworks i Steamworks.NET; rozmowa zawsze przestrzenna, z osłabieniem przez ściany i zamknięte drzwi oraz możliwością podsłuchiwania z bliska.

## Wniosek

Do pierwszego prototypu wybrałbym **Dissonance Voice Chat 9.0.9, z integracją Mirror działającą przez istniejący FizzySteamworks**, a tłumienie ścian i drzwi zrobił jako małą, własną warstwę akustyczną na każdym odtwarzanym głosie.

To jest najlepiej dopasowane do obecnej architektury: Dissonance ma gotowe śledzenie graczy, głos pozycyjny, proximity rooms, VAD, kodek i jitter handling, a jego integracja Mirror wysyła głos przez istniejącą sesję gry. Integracja wymaga transportu Mirror z kanałem unreliable; FizzySteamworks ma domyślnie kanał reliable oraz `k_EP2PSendUnreliableNoDelay`, a wariant oparty o nowe Steam Sockets mapuje kanał Mirror `Unreliable` na `k_nSteamNetworkingSend_Unreliable`. To znaczy, że warunek Dissonance jest spełniony na poziomie transportu, choć zgodność konkretnej wersji Dissonance z Mirror 96 należy potwierdzić krótkim spike'em przed zakupem/utrwaleniem rozwiązania. [Dissonance: integracja Mirror i wymóg unreliable](https://placeholder-software.co.uk/Dissonance/docs/Basics/Quick-Start-Mirror.html), [FizzySteamworks: skonfigurowane kanały](https://github.com/Chykary/FizzySteamworks/blob/master/com.mirror.steamworks.net/FizzySteamworks.cs), [FizzySteamworks: mapowanie nowych Steam Sockets](https://github.com/Chykary/FizzySteamworks/blob/master/com.mirror.steamworks.net/NextCommon.cs)

Nie używałbym osobnych voice rooms do symulacji zamkniętych drzwi. Rooms dają twarde „słyszy/nie słyszy”, natomiast projekt potrzebuje ciągłego przejścia: wyraźnie w pokoju, cicho i niewyraźnie w korytarzu, lepiej przy samych drzwiach, ponownie wyraźnie po ich otwarciu. Najprostszy model to jedna przestrzenna transmisja oraz lokalna ocena drogi głosu dla każdej pary słuchacz–mówca: dystans + stan drzwi/portal pomieszczenia + ewentualny raycast. Wynik steruje głośnością i filtrem dolnoprzepustowym konkretnego `AudioSource`. Unity 6 obsługuje na `AudioSource` przestrzenność, krzywe głośności i krzywą low-pass po dodaniu filtra. [Unity 6: Audio Source i filtr low-pass](https://docs.unity3d.com/6000.0/Documentation/Manual/AudioSource-reference.html)

## Stan projektu

Repo używa Unity `6000.5.3f1`, Mirror `96.0.1` i FizzySteamworks `6.0.1`; Fizzy jest transportem Mirror opartym o Steamworks.NET i obsługuje stary oraz nowy stos Steam Networking. [ProjectVersion.txt](../../ProjectSettings/ProjectVersion.txt), [Mirror version](../../Assets/Mirror/version.txt), [pakiet FizzySteamworks](../../Packages/com.mirror.steamworks.net/package.json), [repozytorium FizzySteamworks](https://github.com/Chykary/FizzySteamworks)

Ważne ograniczenie testowe istniejącego transportu: jego dokumentacja wymaga dwóch maszyn, dwóch kont Steam i dwóch licencji aplikacji do testu P2P; na jednej maszynie zaleca inny transport. Prototyp Dissonance należy więc najpierw uruchomić lokalnie na KCP/innym transporcie unreliable, a potem powtórzyć na dwóch kontach przez FizzySteamworks. [FizzySteamworks: konfiguracja i testowanie](https://github.com/Chykary/FizzySteamworks#testing-your-game-locally)

## Porównanie

| Rozwiązanie | Zgodność z Mirror/Steam | Przestrzeń i occlusion | Koszt / zależności | Ocena dla projektu |
|---|---|---|---|---|
| **Dissonance 9.0.9** | Gotowa integracja Mirror; może użyć istniejącego transportu unreliable | Gotowe 3D i proximity; ściany/drzwi wymagają własnego filtra per głos | Jednorazowy Asset Store Extension Asset na stanowisko; brak usługi chmurowej | **Najlepszy wybór do prototypu** |
| **Unity Vivox 16.10** | Niezależne połączenie z usługą Unity; Mirror dalej prowadzi grę | Gotowe kanały pozycyjne; Participant Audio Taps pozwalają filtrować osobny głos | Hosted service, konto/projekt UGS i logowanie; do 5000 PCU bez opłat | Najlepsza alternatywa usługowa |
| **Photon Voice 2** | Działa standalone obok Mirror, ale tworzy drugą sesję Photon | `Speaker` używa `AudioSource`; własny filtr/occlusion jest możliwy | Photon Cloud; 20 CCU tylko development, płatny launch | Dobre technicznie, słabsze dopasowanie i cena |
| **Steam Voice API** | Pasuje do Steamworks.NET, ale transport i cały pipeline piszemy sami | Pełna kontrola po dekodowaniu PCM | Steamworks bez osobnej opłaty, najwyższy koszt implementacji i utrzymania | Nie na pierwszy prototyp |

## 1. Dissonance Voice Chat

### Dlaczego pasuje

Dissonance oferuje Opus, VAD/PTT, echo cancellation, positional audio i elastyczne rooms. Po włączeniu position tracking nie przesyła dodatkowej pozycji — odtwarza głos przy już zreplikowanym obiekcie gracza. Proximity trigger dzieli świat na wirtualne komórki, żeby nie wysyłać głosu do odległych osób, a custom playback prefab jest instancjonowany osobno dla każdego zdalnego gracza i można dołączyć do niego własny `AudioSource` oraz skrypt. To ostatnie jest dokładnie punktem zaczepienia dla tłumienia drzwi. [Funkcje Dissonance](https://placeholder-software.co.uk/dissonance/docs/), [position tracking](https://placeholder-software.co.uk/Dissonance/docs/Tutorials/Position-Tracking.html), [proximity chat](https://placeholder-software.co.uk/Dissonance/docs/Tutorials/Proximity-Chat.html), [custom playback prefab](https://placeholder-software.co.uk/dissonance/docs/Tutorials/Playback-Prefab.html)

Nie ma udokumentowanego, gotowego modelu ścian i drzwi. Co więcej, dokumentacja Dissonance mówi, że od wersji 7.0.2 zewnętrzne spatializer plugins nie są wspierane z powodu zachowania Unity. Dlatego nie zakładałbym bez spike'a, że można po prostu dołożyć Steam Audio do playback prefabu. Bezpieczny MVP to standardowe 3D Unity oraz własne sterowanie `volume` i `AudioLowPassFilter`; zaawansowane odbicia i transmisję materiałową można ocenić później. [Dissonance: ograniczenie zewnętrznych spatializerów](https://placeholder-software.co.uk/Dissonance/docs/Tutorials/Spatializer-Plugin.html)

### Sieć i utrzymanie

Integracja Mirror wysyła pakiety Dissonance przez sesję Mirror i wymaga backendu unreliable. FizzySteamworks spełnia ten warunek, ale Dissonance nazywa prefab integracji `MirrorIgnoranceCommsNetwork`, a dokumentacja pokazuje Ignorance jako przykład. To nie jest dowód certyfikowanej kombinacji Dissonance 9.0.9 + Mirror 96 + Fizzy 6.0.1, więc potrzebny jest test integracyjny, nie założenie. Alternatywna integracja Dissonance bezpośrednio ze Steamworks.NET istnieje, lecz jej dokumentacja odnosi się do Steamworks P2P i wymaga ręcznego powiadamiania o join/leave oraz wskazania serwera sesji; dla tego repo integracja Mirror jest prostsza i ma mniej podwójnego kodu cyklu życia. [Dissonance Mirror quick start](https://placeholder-software.co.uk/Dissonance/docs/Basics/Quick-Start-Mirror.html), [Dissonance Steamworks.NET P2P quick start](https://placeholder-software.co.uk/dissonance/docs/Basics/Quick-Start-Steamworks.Net-P2P.html)

Dissonance jest aktywnie aktualizowane: Asset Store pokazuje 9.0.9 z 27 kwietnia 2026 i bazową wersją Unity `6000.0.23`; release notes wprost wymieniają aktualizację do Unity 6000.0.23. [Asset Store](https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078), [release 9.0.9](https://placeholder-software.co.uk/dissonance/releases/9.0.9.html)

Licencja jest jednorazowym Extension Asset per seat w standardowej EULA Asset Store. W dniu researchu strona pokazywała cenę katalogową `€101.21` i promocję `€50.60`; ceny i VAT są dynamiczne. Dystrybucja wymaga też dołączenia plików licencji użytych bibliotek Opus/WebRTC/RNNoise. [Asset Store i EULA](https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078), [Dissonance: licensing](https://placeholder-software.co.uk/dissonance/docs/Basics/Licensing.html)

## 2. Unity Vivox

Vivox jest usługą hostowaną, niezależną od transportu gry. Klient dołącza do jednego positional channel i regularnie raportuje pozycję/orientację; Vivox wykonuje kierunkowość i tłumienie odległości według `AudibleDistance`, `ConversationalDistance` i wybranego fade model. Zalecana częstotliwość aktualizacji pozycji to 2–4 razy na sekundę. [Positional channels](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channels), [właściwości kanału 3D](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/positional-channel-properties), [aktualizacja pozycji](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/developer-guide/channels/positional-channel-configuration)

Sam kanał 3D nie modeluje drzwi. Aktualny SDK ma jednak `Vivox Participant Tap`, który udostępnia osobny głos konkretnego gracza na Unity `AudioSource`; opcja `Silence In Channel Audio Mix` pozwala wyciszyć oryginalną kopię i słyszeć tylko zmodyfikowany sygnał. To daje poprawny punkt do własnego low-pass/volume lub późniejszej integracji z silnikiem akustycznym. [Audio Tap components](https://docs.unity.com/en-us/vivox-unity/developer-guide/audio-taps/audio-tap-components), [parametry tapów](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/developer-guide/audio-taps/tap-component-parameters), [użycie AudioSource i Audio Mixer](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/developer-guide/audio-taps/use-audio-taps)

Kosztem jest osobny lifecycle i identity mapping: Steam lobby/ID trzeba skojarzyć z użytkownikiem Vivox, klient loguje się do UGS/Vivox i dołącza do kanału. Najprostsza ścieżka korzysta z Unity Authentication (dokumentacja pokazuje anonymous sign-in); własny bezpieczny provider tokenów wymaga serwera podpisującego tokeny i ochrony shared secret. [Logowanie przez Unity Authentication](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/developer-guide/vivox-unity-sdk-basics/sign-in-with-authentication-package), [tokeny Vivox](https://docs.unity.com/ugs/en-us/manual/vivox-core/manual/server-to-server-api-reference/access-tokens)

Vivox 16.10 wymaga co najmniej Unity 2022.3, więc Unity 6 jest w zakresie, a release notes są bieżące. Cennik obejmuje do 5000 PCU bez opłat; powyżej 5000 PCU zaczynają się progi od `$2000 / 5000 PCU`. To atrakcyjna opcja, jeśli później ważniejsze staną się cross-platform, hosted operations albo funkcje moderation, ale na Steam-only MVP dokłada niepotrzebną usługę. [Vivox release notes](https://docs.unity.com/ugs/en-us/manual/vivox-unity/manual/Unity/release-notes), [UGS pricing](https://unity.com/products/gaming-services/pricing)

## 3. Photon Voice 2

Photon Voice działa standalone z systemami firm trzecich, więc Mirror może nadal prowadzić stan gry. W praktyce klient utrzymuje osobne połączenie z Photon Realtime/Voice room, a projekt musi mapować Photon user/stream na właściwy obiekt gracza Mirror. `UnityVoiceClient` tworzy `Speaker` z prefabu dla każdego nadchodzącego strumienia; domyślny Speaker odtwarza przez Unity `AudioSource`, więc można dodać własny skrypt i filtr drzwi. SDK zapewnia Opus, jitter buffer, VAD i WebRTC DSP/AEC. [Photon Voice intro i standalone integration](https://doc.photonengine.com/voice/current/getting-started/voice-intro), [Recorder, VAD i DSP](https://doc.photonengine.com/voice/current/getting-started/recorder)

Interest groups mogą ograniczyć ruch do fragmentów świata, ale są twardymi subkanałami; same nie dadzą częściowego podsłuchu przez drzwi. Dla małej mapy lepiej najpierw wysyłać wszystkich w jednym Voice room i filtrować lokalnie. [Photon interest groups](https://doc.photonengine.com/realtime/current/gameplay/interestgroups)

Photon Cloud jest utrzymywane przez dostawcę, ale bezpłatny plan 20 CCU jest tylko development. Aktualny cennik Voice pokazuje `$95` za 100 CCU na 12 miesięcy lub `$95/mies.` za 500 CCU. Przy obecnym Mirror + Steam oznacza to drugi backend i opłatę bez istotnej przewagi nad Vivox lub Dissonance. [Photon Voice pricing](https://www.photonengine.com/voice/pricing)

## 4. Steam Voice API

Steam Voice daje capture i kompresję mikrofonu (`StartVoiceRecording`, `GetAvailableVoice`, `GetVoice`) oraz dekodowanie otrzymanych danych do mono 16-bit PCM (`DecompressVoice`). Nie wysyła danych po sieci — aplikacja musi sama zbudować routing, sekwencjonowanie, buffering/jitter handling, odtwarzanie per gracz, mute, wybór urządzeń i obsługę zmian sesji. [Steam Voice](https://partner.steamgames.com/doc/features/voice?language=english), [ISteamUser::DecompressVoice](https://partner.steamgames.com/doc/api/ISteamUser?language=english#DecompressVoice)

Po dekodowaniu PCM można uzyskać pełną kontrolę nad przestrzennością i occlusion, a pakiety da się wysłać kanałem unreliable Fizzy/Mirror lub bezpośrednio przez Steam Networking. To jednak własny system VoIP, nie gotowe narzędzie. Szczególnie istotne dla tej gry: Valve pisze, że always-on rzadko powinien być domyślny i „nigdy” nie jest rekomendowany powyżej czterech graczy. Można dobudować VAD, ale Steam Voice go nie dostarcza jako kompletnej warstwy gameplayowej. [Steam Voice: recording i zalecenie dotyczące always-on](https://partner.steamgames.com/doc/features/voice?language=english)

Steamworks jest bezpłatnym zestawem usług dla gier na Steam, a licencja SDK jest royalty-free, lecz rozwiązanie wiąże voice chat z działającym klientem Steam i Steam ID. Koszt finansowy jest mały, za to koszt implementacji, QA i utrzymania największy. [Steamworks](https://partner.steamgames.com/doc/home), [Steamworks SDK Access Agreement](https://partner.steamgames.com/documentation/sdk_access_agreement)

## Narzędzie uzupełniające: Steam Audio

Steam Audio nie przechwytuje ani nie transportuje głosu; jest silnikiem akustycznym dla już istniejących `AudioSource`. Potrafi modelować HRTF, occlusion, transmisję przez geometrię, reflections i materiały. Wersja 4.8.1 została wydana 11 lutego 2026, wspiera Unity i jest na Apache-2.0. [Steam Audio Unity](https://valvesoftware.github.io/steam-audio/doc/unity/index.html), [Steam Audio Source: occlusion i transmission](https://valvesoftware.github.io/steam-audio/doc/unity/source.html), [repo i release 4.8.1](https://github.com/ValveSoftware/steam-audio)

Nie dodawałbym go w pierwszym spike'u: dla kilku pokoi, drzwi i 4–8 graczy portalowy model + low-pass da się łatwiej stroić pod czytelność dialogu. Steam Audio warto ocenić później z Vivox Participant Taps, Photon Speaker albo własnym Steam Voice `AudioSource`. Z Dissonance obowiązuje wyżej opisane ostrzeżenie o braku wsparcia zewnętrznych spatializerów.

## Minimalny plan prototypu

1. Kupić/importować Dissonance i integrację Mirror. Uruchomić dostarczoną scenę demo bez Steam, na transporcie Mirror z unreliable, i potwierdzić rozmowę dwóch klientów.
2. Podłączyć `IDissonancePlayer`/tracker do istniejącego prefabu gracza. Włączyć tylko positional proximity channel z VAD; bez globalnego i bez prywatnych kanałów. [Dissonance: position tracking](https://placeholder-software.co.uk/Dissonance/docs/Tutorials/Position-Tracking.html)
3. Zrobić custom playback prefab: `VoicePlayback` + `AudioSource` 3D + `AudioLowPassFilter` + mały komponent `VoiceOcclusion`.
4. W jednym testowym układzie przygotować dwa pomieszczenia i drzwi. Dla każdej pary słuchacz–mówca testować trzy stany: ten sam pokój, inne pokoje/zamknięte drzwi, inne pokoje/otwarte drzwi. Płynnie interpolować volume i cutoff, żeby uniknąć skoków.
5. Test akceptacyjny: w pokoju każde słowo jest czytelne; za zamkniętymi drzwiami z daleka rozmowa jest niezrozumiała, przy drzwiach można wyłapać część słów; po otwarciu drzwi głos odzyskuje czytelność; poruszanie się nie powoduje cięć ani podwójnego audio.
6. Powtórzyć test na dwóch maszynach i kontach przez FizzySteamworks. Zmierzyć end-to-end latency, packet loss, host bandwidth i zachowanie po opuszczeniu/dołączeniu Steam lobby.
7. Dopiero jeśli integracja Dissonance + aktualny Mirror/Fizzy okaże się niestabilna, zrobić równoległy spike Vivox. Photon Voice i własny Steam Voice nie są uzasadnionymi drugimi wyborami na tym etapie.

## Decyzje, które nadal wymagają playtestu

- VAD jako domyślne wejście czy dodatkowa opcja push-to-talk; „zawsze przestrzenny” nie musi oznaczać ciągłego wysyłania szumu z mikrofonu.
- Dokładne krzywe zasięgu oraz trzy presety drzwi: otwarte, uchylone, zamknięte.
- Czy cienkie ściany pozwalają słyszeć ton głosu, czy wyłącznie drzwi są portalem podsłuchu.
- Czy host migration ma zachować sesję voice bez restartu; Dissonance Steamworks integration wyraźnie traktuje serwer jako centralny punkt sesji, więc ten przypadek wymaga osobnego testu. [Dissonance Steamworks.NET P2P](https://placeholder-software.co.uk/dissonance/docs/Basics/Quick-Start-Steamworks.Net-P2P.html)
