using System;
using System.Collections.Generic;
using InterrogationRoom.Settings;

namespace InterrogationRoom.UI
{
    /// <summary>
    /// Small UI-only localization catalog. Authored case content deliberately
    /// bypasses this catalog until story localization is introduced separately.
    /// </summary>
    public static class UiText
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Host Game"] = "Host Game",
                ["Join Server"] = "Join Server",
                ["Settings"] = "Settings",
                ["Quit"] = "Quit",
                ["Gospodarz gry"] = "Host Game",
                ["Dołącz do serwera"] = "Join Server",
                ["Ustawienia"] = "Settings",
                ["Wyjdź"] = "Quit",
                ["KARTA USTAWIEŃ • 01"] = "SETTINGS FILE • 01",
                ["USTAWIENIA"] = "SETTINGS",
                ["Zmiany ustawień działają natychmiast."] = "Settings apply immediately.",
                ["Runda trwa — zmiany ustawień działają natychmiast."] = "The Round is in progress — settings apply immediately.",
                ["Czułość myszy"] = "Mouse sensitivity",
                ["Język"] = "Language",
                ["Polski"] = "Polish",
                ["Angielski"] = "English",
                ["V — wycisz / włącz mikrofon"] = "V — mute / unmute microphone",
                ["Wróć do menu"] = "Back to menu",
                ["Wróć do gry"] = "Back to game",
                ["Opuść Rundę"] = "Leave Round",
                ["PRZESŁUCHANIE"] = "INTERROGATION",
                ["Gracze w lobby: 0/8"] = "Players in lobby: 0/8",
                ["Wybierz postać"] = "Choose character",
                ["Włącz Sekretny Cel"] = "Enable Secret Objective",
                ["Sekretny Cel jest dostępny od 5 graczy."] = "Secret Objective is available from 5 players.",
                ["Sekretny Cel będzie użyty w tej Rundzie."] = "Secret Objective will be used in this Round.",
                ["Sekretny Cel jest wyłączony przez hosta."] = "Secret Objective was disabled by the host.",
                ["Zaproś znajomych"] = "Invite friends",
                ["Wyjdź z lobby"] = "Leave lobby",
                ["Start Rundy"] = "Start Round",
                ["Rundę można rozpocząć dla 3–8 graczy."] = "A Round can be started with 3–8 players.",
                ["Wygląd postaci nie zdradza roli."] = "A character's appearance does not reveal their role.",
                ["AKTA RUNDY"] = "ROUND FILE",
                ["Przygotowanie"] = "Preparation",
                ["Przestępstwo"] = "Crime",
                ["Twoje Alibi"] = "Your Alibi",
                ["Gotowy"] = "Ready",
                ["GOTOWY"] = "READY",
                ["Zwiń [I]"] = "Collapse [I]",
                ["Rejestr [I]"] = "Registry [I]",
                ["Cel [I]"] = "Objective [I]",
                ["KONIEC RUNDY"] = "ROUND OVER",
                ["Wróć do lobby"] = "Return to lobby",
                ["Rola"] = "Role",
                ["Gracz"] = "Player",
                ["Gracze w lobby"] = "Players in lobby",
                ["Gotowi"] = "Ready",
                ["Detektyw"] = "Detective",
                ["Winny"] = "Guilty",
                ["Niewinny"] = "Innocent",
                ["Małpa"] = "Monkey",
                ["Wieprz"] = "Boar",
                ["Jak"] = "Yak",
                ["Karton"] = "Box",
                ["Ptaku"] = "Bird",
                ["Niesiesz"] = "Carrying",
                ["[G] Upuść"] = "[G] Drop",
                ["Wstań"] = "Stand up",
                ["Usiądź"] = "Sit down",
                ["Otwórz drzwi"] = "Open door",
                ["Zamknij drzwi"] = "Close door",
                ["Podnieś"] = "Pick up",
                ["Odłóż przedmiot"] = "Put item down",
                ["Istotny przedmiot"] = "Relevant item",
                ["Intencja została odrzucona."] = "The action was rejected.",
                ["Brak w Twojej wersji Alibi"] = "Missing from your version of the Alibi",
                ["Zapamiętaj swoją wersję Alibi. Po Przygotowaniu nie będzie można jej ponownie otworzyć."] = "Memorize your version of the Alibi. You cannot open it again after Preparation.",
                ["1. Przesłuchaj każdego Podejrzanego.\n2. Porównuj zeznania z tym, co widzisz, oraz z Rejestrem Incydentów.\n3. Masz jedną Egzekucję — pierwsze trafienie żywego Podejrzanego kończy Rundę."] = "1. Question every Suspect.\n2. Compare testimony with what you observe and with the Incident Registry.\n3. You have one Execution — the first hit on a living Suspect ends the Round.",
                ["AKTA SYSTEMOWE • MENU"] = "SYSTEM FILES • MENU",
                ["WYBIERZ TRYB"] = "CHOOSE MODE",
                ["Uruchom lobby, przejdź do Rundy albo otwórz narzędzia testowe."] = "Start a lobby, enter a Round, or open the test tools.",
                ["SIEĆ / HOST"] = "NETWORK / HOST",
                ["ZWYKŁA RUNDA"] = "NORMAL ROUND",
                ["TRYB DEVELOPERSKI"] = "DEVELOPER MODE",
                ["ZAMKNIJ MENU"] = "CLOSE MENU",
                ["← Menu"] = "← Menu",
                ["AKTA TRYBU"] = "MODE FILE",
                ["Graj z panelem"] = "Play with panel",
                ["Graj"] = "Play",
                ["Utwórz lobby"] = "Create lobby",
                ["Dołącz"] = "Join",
                ["Łączenie…"] = "Connecting…",
                ["Anuluj"] = "Cancel",
                ["Łączenie przez Steam…"] = "Connecting through Steam…",
                ["Tworzenie lobby Steam…"] = "Creating Steam lobby…",
                ["Utwórz lobby dla znajomych"] = "Create friends lobby",
                ["Znajomi dołączają przez nakładkę Steam: Znajomi → Dołącz do gry"] = "Friends join through the Steam overlay: Friends → Join Game",
                ["Lobby działa — jesteś hostem."] = "Lobby is running — you are the host.",
                ["Serwer działa."] = "Server is running.",
                ["Połączono z lobby."] = "Connected to the lobby.",
                ["Nakładka Steam jest niedostępna — użyj zaproszenia poniżej."] = "Steam Overlay is unavailable — use an invitation below.",
                ["Brak znajomych Steam online."] = "No Steam friends online.",
                ["Opuść grę"] = "Leave game",
                ["Zamknij lobby"] = "Close lobby",
                ["Rozłącz"] = "Disconnect",
                ["Zatrzymaj serwer"] = "Stop server",
                ["Klient gotowy"] = "Client Ready",
                ["Tylko serwer"] = "Server Only",
                ["Zaproś"] = "Invite",
                ["AKTA SPRAWY"] = "CASE FILE",
                ["Gra sieciowa dla 3–8 graczy."] = "An online game for 3–8 players."
            };

        private static readonly IReadOnlyDictionary<string, string> Polish =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Host Game"] = "Gospodarz gry",
                ["Join Server"] = "Dołącz do serwera",
                ["Settings"] = "Ustawienia",
                ["Quit"] = "Wyjdź",
                ["Stand up"] = "Wstań",
                ["Sit down"] = "Usiądź",
                ["Open door"] = "Otwórz drzwi",
                ["Close door"] = "Zamknij drzwi",
                ["Pick up gun"] = "Podnieś pistolet",
                ["Interact"] = "Wejdź w interakcję",
                ["Search receipt tray"] = "Przeszukaj tackę z paragonami",
                ["Take suspicious item"] = "Weź podejrzany przedmiot",
                ["Hide personal document"] = "Ukryj osobisty dokument",
                ["Store personal document"] = "Odłóż osobisty dokument",
                ["Search evidence shelf"] = "Przeszukaj półkę z dowodami",
                ["Search records cabinet"] = "Przeszukaj szafkę z aktami",
                ["Force loading gate"] = "Sforsuj bramę załadunkową",
                ["Force service vent"] = "Sforsuj otwór serwisowy",
                ["Trigger archive alarm"] = "Uruchom alarm archiwum",
                ["Unlatch loading gate"] = "Odblokuj bramę załadunkową",
                ["Loosen vent cover"] = "Poluzuj osłonę otworu",
                ["Search maintenance cabinet"] = "Przeszukaj szafkę techniczną",
                ["Plant suspicious item"] = "Podłóż podejrzany przedmiot",
                ["Inspect service route"] = "Sprawdź drogę serwisową"
            };

        public static UiLanguage CurrentLanguage => GameSettingsService.Current.Language;

        public static string Get(string polish, UiLanguage language)
        {
            if (string.IsNullOrEmpty(polish))
                return polish;
            if (language == UiLanguage.English && English.TryGetValue(polish, out string translated))
                return translated;
            if (language == UiLanguage.Polish && Polish.TryGetValue(polish, out string localized))
                return localized;
            return polish;
        }

        public static string Get(string polish) => Get(polish, CurrentLanguage);

        public static string Format(string polishFormat, UiLanguage language, params object[] args) =>
            string.Format(Get(polishFormat, language), args);

        public static string Format(string polishFormat, params object[] args) =>
            Format(polishFormat, CurrentLanguage, args);
    }
}
