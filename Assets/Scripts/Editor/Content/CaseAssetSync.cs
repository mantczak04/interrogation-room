using System;
using System.Collections.Generic;
using InterrogationRoom.Content;
using UnityEditor;
using UnityEngine;

namespace InterrogationRoom.Editor.Content
{
    public static class CaseAssetSync
    {
        private const string OutputFolder = "Assets/Content/Cases";

        [MenuItem("Interrogation Room/Content/Sync Case Assets")]
        public static void Sync()
        {
            EnsureFolder("Assets/Content");
            EnsureFolder(OutputFolder);

            foreach (var definition in Definitions())
                Sync(definition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CaseAssetSync] Synchronized C01 through C15 as six-point CaseAssets.");
        }

        private static IReadOnlyList<Definition> Definitions() => new[]
        {
            new Definition(
                "C01",
                $"{OutputFolder}/SprawaRozowyPomnik.asset",
                "Różowy pomnik burmistrza",
                "Ktoś przemalował pomnik burmistrza na różowo i przykleił mu wąsy z waty cukrowej, gdy cała grupa była razem na kolacji.",
                new[]
                {
                    Fact("spotkanie-pizzeria", "O 19:00 wszyscy spotkali się w pizzerii „U Grubego” przy rynku.", false),
                    Fact("trzy-pizze", "Zamówili trzy pizze: hawajską, capricciosę i podwójny ser.", true),
                    Fact("bledny-rachunek", "Kelner pomylił rachunek i doliczył cztery kompoty, których nikt nie zamawiał.", true),
                    Fact("awaria-swiatla", "Około 19:40 na kilka minut zgasło światło w całym lokalu.", false),
                    Fact(
                        "dowcip-kucharza",
                        "Kucharz opowiedział dowcip o strażaku, z którego nikt się nie zaśmiał.",
                        true,
                        true,
                        "Kucharz opowiedział dowcip o strażaku, z którego nikt się nie zaśmiał.",
                        "Kucharz opowiedział dowcip o policjancie, z którego nikt się nie zaśmiał."),
                    Fact("wspolny-powrot", "Po 21:00 wszyscy wyszli razem, minęli patrol straży miejskiej i doszli do przystanku przy fontannie.", false)
                },
                new[]
                {
                    Clue("paragon-cztery-kompoty", "bledny-rachunek", "Paragon z czterema kompotami dopisanymi do rachunku, mimo że nikt ich nie zamawiał."),
                    Clue("paragon-trzy-pizze", "trzy-pizze", "Naderwany paragon z pozycjami HAW, CAP i 2×SER."),
                    Clue("kartka-kucharza", "dowcip-kucharza", "Kartka kucharza z hełmem strażackim i skreślonym zakończeniem żartu.")
                }),

            new Definition(
                "C02",
                $"{OutputFolder}/Case_Wesele.asset",
                "Wesele bez tortu",
                "Ktoś wystrzelił weselny tort z armatki konfetti na dach remizy.",
                new[]
                {
                    Fact("miejsca-przy-orkiestrze", "O 18:30 wszyscy zajęli miejsca przy stole obok orkiestry.", false),
                    Fact("rozlany-kompot", "Świadek pana młodego rozlał kompot z agrestu na biały obrus.", true),
                    Fact("pierwsza-polka", "Kapela zagrała polkę, gdy zegar nad barem wskazywał 18:47.", true),
                    Fact(
                        "papierowe-korony",
                        "Ciocia Grażyna rozdawała papierowe korony z napisem „Król Parkietu”.",
                        false,
                        true,
                        "Ciocia Grażyna rozdawała papierowe korony z napisem „Król Parkietu”.",
                        "Ciocia Grażyna rozdawała papierowe korony z napisem „Mistrz Wesela”."),
                    Fact("skrzypiacy-wozek", "Kelner wniósł tort na wózku z jednym skrzypiącym kołem.", true),
                    Fact("awaria-i-wyjscie", "Przez pięć minut nie było światła, ale muzyka grała dalej; o 19:05 grupa wyszła na zimne ognie, a potem razem czekała przy bramie na taksówkę.", false)
                },
                new[]
                {
                    Clue("fotografia-obrusa", "rozlany-kompot", "Fotografia białego obrusa z jasnozieloną plamą obok winietki świadka."),
                    Clue("setlista-1847", "pierwsza-polka", "Setlista kapeli z symbolem polki obok godziny 18:47."),
                    Clue("kwit-wozka", "skrzypiacy-wozek", "Kwit naprawy wózka z jednym przednim kołem opisanym jako piszczące pod obciążeniem.")
                }),

            new Definition(
                "C03",
                $"{OutputFolder}/Case_Obserwatorium.asset",
                "Incydent w obserwatorium",
                "Ktoś przykleił do teleskopu ogromne rzęsy i skierował go na komin piekarni.",
                new[]
                {
                    Fact(
                        "ochraniacze-na-buty",
                        "O 21:12 grupa otrzymała niebieskie ochraniacze na buty.",
                        false,
                        true,
                        "O 21:12 grupa otrzymała niebieskie ochraniacze na buty.",
                        "O 21:12 grupa otrzymała zielone ochraniacze na buty."),
                    Fact("termos-rakieta", "Astronom niósł termos w kształcie rakiety.", true),
                    Fact("wega-laser", "Pierwszą obserwowaną gwiazdą była Wega, wskazana zielonym laserem.", true),
                    Fact("blad-projektora", "Projektor zgasł po komunikacie o błędzie numer 17.", false),
                    Fact("srebrny-guzik", "Pani Marta znalazła srebrny guzik pod obrotowym krzesłem.", true),
                    Fact("drozdzowki-i-wyjscie", "W kopule pachniało drożdżówkami; o 21:46 wszyscy podpisali księgę gości i odjechali razem nocnym busem.", false)
                },
                new[]
                {
                    Clue("zdjecie-termosu", "termos-rakieta", "Fotografia metalowego pojemnika z płetwami i stożkową nakrętką."),
                    Clue("program-obserwacji", "wega-laser", "Zalany program, którego pierwsze pole zaczyna się literą W i ma zielony ślad wskaźnika."),
                    Clue("woreczek-guzik", "srebrny-guzik", "Woreczek ze srebrnym guzikiem i szkicem okrągłej podstawy mebla.")
                }),

            new Definition(
                "C04",
                $"{OutputFolder}/Case_Muzeum.asset",
                "Noc w Muzeum Osobliwości",
                "Ktoś zamienił woskową figurę burmistrza na gigantyczny ser.",
                new[]
                {
                    Fact("boczne-wejscie", "O 20:10 grupa weszła do muzeum bocznym wejściem przy szatni.", false),
                    Fact(
                        "parasol-przewodniczki",
                        "Przewodniczka miała czerwony parasol, mimo że nie padało.",
                        true,
                        true,
                        "Przewodniczka miała czerwony parasol, mimo że nie padało.",
                        "Przewodniczka miała turkusowy parasol, mimo że nie padało."),
                    Fact("alarm-zegarow", "W sali zegarów rozległ się alarm dokładnie o 20:24.", true),
                    Fact("bilet-przy-kaczce", "Pan Leon upuścił bilet obok gabloty z mechaniczną kaczką.", false),
                    Fact("zdjecie-wieloryba", "Grupa zrobiła zdjęcie przy szkielecie wieloryba w papierowej koronie.", true),
                    Fact("cukierki-i-wyjscie", "Kustosz poczęstował wszystkich miętowymi cukierkami; o 20:41 grupa wyszła przez sklep i razem wsiadła do autobusu naprzeciw muzeum.", false)
                },
                new[]
                {
                    Clue("kwit-parasola", "parasol-przewodniczki", "Kwit szatni z czerwonym parasolem i suchą pieczątką pogodową."),
                    Clue("log-alarmu", "alarm-zegarow", "Papierowy log z godziną 20:24, ikoną dzwonka i nieczytelnym kodem sali."),
                    Clue("negatyw-wieloryba", "zdjecie-wieloryba", "Negatyw z żebrami wielkiego szkieletu, papierowym zębem korony i sylwetkami bez twarzy.")
                }),

            new Definition(
                "C05",
                $"{OutputFolder}/Case_C05_GumoweKaczki.asset",
                "Poranek Stu Gumowych Kaczek",
                "Ktoś wrzucił sto gumowych kaczek do miejskiej fontanny, a największej założył policyjną czapkę i szarfę „Komendant Stawu”.",
                new[]
                {
                    Fact("zolw-w-lecznicy", "O 05:40 grupa przyniosła znalezionego żółwia do całodobowej lecznicy i zważyła go w niebieskiej misce sałatkowej.", false),
                    Fact(
                        "karta-z-lisem",
                        "Recepcjonistka dała im kartę kolejki z rysunkiem lisa.",
                        true,
                        true,
                        "Recepcjonistka dała im kartę kolejki z rysunkiem lisa.",
                        "Recepcjonistka dała im kartę kolejki z rysunkiem borsuka."),
                    Fact("ostrzezenie-o-mgle", "Radio w poczekalni ostrzegło przed gęstą poranną mgłą.", true),
                    Fact("terier-i-parasole", "Mały terier kichnął i przewrócił stojak na parasole.", false),
                    Fact("gwiazda-na-skorupie", "Weterynarz narysował na skorupie zmywalną białą gwiazdę.", true),
                    Fact("wyjscie-autobusem", "Wyszli drzwiami apteki, bo myto główne wejście, i o 06:05 razem wsiedli do autobusu numer 3.", false)
                },
                new[]
                {
                    Clue("karta-kolejki", "karta-z-lisem", "Karta z lisem, numerem 12 i odciskiem łapy, lecz bez nazwy placówki."),
                    Clue("wydruk-mgly", "ostrzezenie-o-mgle", "Wydruk z 05:47 z trzema symbolami mgły i ostrzeżeniem o bardzo małej widoczności."),
                    Clue("gaza-bialy-znak", "gwiazda-na-skorupie", "Gaza z fragmentem białego zmywalnego znaku i wzorem przypominającym łuski.")
                }),

            new Definition(
                "C06",
                $"{OutputFolder}/Case_C06_KogutWKajdankach.asset",
                "Kogut w kajdankach",
                "Ktoś ukradł pozłacanego koguta z szyldu hali drobiowej, skuł go kajdankami z miejską fontanną i odczytał mu prawa zatrzymanego.",
                new[]
                {
                    Fact("ulewa-i-plaszcze", "Podczas ulewy grupa weszła do pralni „Bąbel” i włożyła mokre płaszcze do kosza ze złamaną rączką.", false),
                    Fact("czerwona-skarpetka", "W pustej pralce znaleźli jedną czerwoną dziecięcą skarpetkę.", true),
                    Fact("podwojne-kakao", "Obsługująca rozmieniła banknot na mosiężne żetony, a automat wydał dwa kakao po jednym wyborze.", true),
                    Fact(
                        "karty-z-polki",
                        "Czekając, grali kartami z czerwonym rewersem znalezionymi na półce z czasopismami.",
                        false,
                        true,
                        "Czekając, grali kartami z czerwonym rewersem znalezionymi na półce z czasopismami.",
                        "Czekając, grali kartami z niebieskim rewersem znalezionymi na półce z czasopismami."),
                    Fact("trzy-sygnaly-suszarki", "Suszarka zapiszczała trzy razy, lecz pranie zostało wilgotne.", true),
                    Fact("zielony-parasol", "Wszyscy wyszli razem pod jednym zielonym parasolem reklamowym.", false)
                },
                new[]
                {
                    Clue("zdjecie-skarpetki", "czerwona-skarpetka", "Fotografia czerwonej prążkowanej tkaniny przy okrągłym stalowym otworze."),
                    Clue("reklamacja-kakao", "podwojne-kakao", "Reklamacja z jednym symbolem monety i dwiema obręczami po kubkach."),
                    Clue("wilgotny-filtr", "trzy-sygnaly-suszarki", "Woreczek z wilgotnym filtrem kłaczków i trzema stemplami kontroli.")
                }),

            new Definition(
                "C07",
                $"{OutputFolder}/Case_C07_KozaJejWysokosc.asset",
                "Koza Jej Wysokość",
                "Ktoś użył skradzionej miejskiej pieczęci, by podczas transmisji z rynku koronować kozę na tymczasową burmistrzynię.",
                new[]
                {
                    Fact(
                        "tor-borsuki",
                        "Grupa weszła na ostatnią godzinę do kręgielni „Meteor” i dostała tor szósty pod nazwą „Borsuki”.",
                        false,
                        true,
                        "Grupa weszła na ostatnią godzinę do kręgielni „Meteor” i dostała tor szósty pod nazwą „Borsuki”.",
                        "Grupa weszła na ostatnią godzinę do kręgielni „Meteor” i dostała tor szósty pod nazwą „Krety”."),
                    Fact("niedobrane-sznurowki", "Dostali biało-czerwone buty z niedobranymi sznurówkami.", true),
                    Fact("pierwszy-rzut-i-kula", "Pierwsza kula przewróciła tylko jeden kręgiel, a po awarii podajnika pracownik ręcznie wyciągnął zieloną kulę.", false),
                    Fact("ogorki-przy-awarii", "Gdy podajnik stanął, zamówili miskę kiszonych ogórków.", true),
                    Fact("wynik-77", "Tablica wyników zamarła na liczbie 77.", true),
                    Fact("koniec-kolorowych-swiatel", "Wszyscy wyszli razem, kiedy zgasły kolorowe światła nad torami.", false)
                },
                new[]
                {
                    Clue("kartonik-obuwia", "niedobrane-sznurowki", "Kartonik zwrotu obuwia z końcówkami białej i czerwonej sznurówki."),
                    Clue("podstawka-ogorki", "ogorki-przy-awarii", "Podstawka z zielonym słonym zaciekiem, rysunkiem kuli i pieczątką obsługi podajnika."),
                    Clue("zdjecie-wyniku", "wynik-77", "Zdjęcie dwóch czerwonych siódemek i odbicia numeru toru, bez graczy.")
                }),

            new Definition(
                "C08",
                $"{OutputFolder}/Case_C08_Galaretobus.asset",
                "Galaretobus",
                "Ktoś napełnił autobus miejski zieloną galaretą, zatopił w niej ogromną marchew i wystawił pojazd przed ratuszem jako „największą zimną nóżkę świata”.",
                new[]
                {
                    Fact(
                        "fartuchy-w-pracowni",
                        "Grupa przyszła na ostatnie zajęcia do pracowni ceramicznej, gdzie instruktorka rozdała im szare fartuchy.",
                        false,
                        true,
                        "Grupa przyszła na ostatnie zajęcia do pracowni ceramicznej, gdzie instruktorka rozdała im szare fartuchy.",
                        "Grupa przyszła na ostatnie zajęcia do pracowni ceramicznej, gdzie instruktorka rozdała im beżowe fartuchy."),
                    Fact("ryby-z-gliny", "Każdy ulepił z białej gliny rybę.", true),
                    Fact("prognoza-i-szkliwo", "Podczas prognozy pogody w radiu niebieskie szkliwo rozlało się obok stołu.", true),
                    Fact("zolta-gabka", "Wytarli rozlane szkliwo dużą żółtą gąbką.", false),
                    Fact("druga-polka-pieca", "Instruktorka ustawiła prace na drugiej półce pieca.", true),
                    Fact("podpisane-etykiety", "Przed wyjściem wszyscy podpisali papierowe etykiety inicjałami.", false)
                },
                new[]
                {
                    Clue("szkice-ryb", "ryby-z-gliny", "Kartka z podobnymi sylwetkami płetw i odciskami białej gliny."),
                    Clue("odcisk-w-szkliwie", "prognoza-i-szkliwo", "Nakładka z odciskiem podeszwy przeciętym kobaltową smugą."),
                    Clue("karta-zaladunku", "druga-polka-pieca", "Karta załadunku z glinianymi łuskami zaznaczonymi na drugim poziomie.")
                }),

            new Definition(
                "C09",
                $"{OutputFolder}/Case_C09_HejnalZKaczka.asset",
                "Hejnał z kaczką",
                "Ktoś podmienił nagranie ratuszowego hejnału, przez co w samo południe z wieży przez minutę rozlegało się uroczyste kwakanie.",
                new[]
                {
                    Fact("pokoj-karaoke", "Po odwołanym seansie grupa weszła do baru karaoke „Pod Fałszem” i wylosowała prywatny pokój numer trzy.", false),
                    Fact("duet-o-rozach", "Zaczęli od duetu o białych różach.", true),
                    Fact(
                        "lemoniada-i-mikrofon",
                        "Kelner przyniósł lemoniadę z plasterkami cytryny na metalowej tacy, a mikrofon zamilkł przed ostatnim refrenem.",
                        true,
                        true,
                        "Kelner przyniósł lemoniadę z plasterkami cytryny na metalowej tacy, a mikrofon zamilkł przed ostatnim refrenem.",
                        "Kelner przyniósł lemoniadę z plasterkami pomarańczy na metalowej tacy, a mikrofon zamilkł przed ostatnim refrenem."),
                    Fact("tamburyn", "Dokończyli piosenkę z tamburynem zdjętym ze ściany.", false),
                    Fact("zdjecia-w-budce", "Potem zrobili wspólne zdjęcia w budce.", true),
                    Fact("niebieskie-stemple", "Wszyscy wyszli razem z niebieskimi stemplami na dłoniach.", false)
                },
                new[]
                {
                    Clue("formularz-duetu", "duet-o-rozach", "Naderwany formularz z dwiema rubrykami wykonawców, dwiema różami i literą B."),
                    Clue("blister-baterii", "lemoniada-i-mikrofon", "Blister po bateriach z wykresem dźwięku urwanym przed trzecim refrenem."),
                    Clue("pasek-zdjec", "zdjecia-w-budce", "Tył paska zdjęć z cieniami głów i kolejnymi numerami klatek, bez twarzy.")
                }),

            new Definition(
                "C10",
                $"{OutputFolder}/Case_C10_PierogOdplywa.asset",
                "Pieróg odpływa",
                "Ktoś odwiązał ogromny dmuchany pieróg z festynu, założył perukę sędziego i popłynął na nim rzeką, wydając wyroki mijanym kaczkom.",
                new[]
                {
                    Fact("sektor-c", "Podczas deszczu grupa pomagała w schronisku i została wpisana do sektora boksów oznaczonego literą C.", true),
                    Fact("beagle-i-kot", "Nakarmili starego beagla z niebieskiej miski, po czym czarny kot uciekł do magazynu pościeli.", false),
                    Fact("zabawka-z-pior", "Próbowali wywabić kota zabawką z piór.", true),
                    Fact(
                        "skladanie-kocow",
                        "Czekając, składali koce w czerwono-białą kratę.",
                        false,
                        true,
                        "Czekając, składali koce w czerwono-białą kratę.",
                        "Czekając, składali koce w niebiesko-białe pasy."),
                    Fact("przewrocone-wiadro", "Beagle przewrócił wiadro z wodą.", true),
                    Fact("zolty-transporter", "Po złapaniu kota zamknęli go w żółtym transporterze i wyszli razem.", false)
                },
                new[]
                {
                    Clue("klips-sektora", "sektor-c", "Klips identyfikatora z wycięciem C, kreskami boksów i logo schroniska."),
                    Clue("polamane-piora", "zabawka-z-pior", "Zabawka z połamanymi piórami, czarnymi włosami i śladami ciągnięcia."),
                    Clue("zdjecie-mokrych-lap", "przewrocone-wiadro", "Fotografia mokrych psich łap, obrysu wiadra i rozlanej wody.")
                }),

            new Definition(
                "C11",
                $"{OutputFolder}/Case_C11_WirujacaPrzykrywka.asset",
                "Wirująca przykrywka",
                "Ktoś wypuścił podczas posiedzenia rady miasta trzysta nakręcanych kaczek, które zagłuszyły głosowanie nad budżetem.",
                new[]
                {
                    Fact("tabliczka-numer-6", "O 20:05 grupa weszła na nocną giełdę kwiatową „Irys” i dostała fioletową tabliczkę licytacyjną numer 6.", true),
                    Fact(
                        "pachnace-lilie",
                        "Wszyscy oglądali lilie pachnące cytryną.",
                        false,
                        true,
                        "Wszyscy oglądali lilie pachnące cytryną.",
                        "Wszyscy oglądali lilie pachnące wanilią."),
                    Fact("skrzynia-27", "Kurier w żółtym płaszczu przywiózł skrzynię oznaczoną numerem 27.", true),
                    Fact("polknieta-moneta", "Automat kasowy połknął monetę starszego klienta.", false),
                    Fact("tango-i-rekawiczka", "Z radia leciało tango, gdy z wózka spadła czerwona rękawiczka.", true),
                    Fact("papier-i-wyjscie", "Grupa złożyła trzy niebieskie arkusze papieru i o 20:43 wyszła razem za ciężarówką odbierającą szkło z wazonów.", false)
                },
                new[]
                {
                    Clue("fragment-tabliczki", "tabliczka-numer-6", "Fragment tabliczki z fioletową krawędzią, logo irysa i niepełnym numerem."),
                    Clue("kalka-dostawy", "skrzynia-27", "Kalka dostawy z symbolem skrzyni i numerem 27, lecz bez nazwiska kuriera."),
                    Clue("zdjecie-radia", "tango-i-rekawiczka", "Zdjęcie radia z napisem TANGO i czerwonym przedmiotem rozmazanym przy wózku.")
                }),

            new Definition(
                "C12",
                $"{OutputFolder}/Case_C12_ZupaUrzedowa.asset",
                "Zupa urzędowa",
                "Ktoś zastąpił wszystkie tabliczki z nazwiskami radnych szkliwionymi talerzami z napisem „ZUPA DNIA”.",
                new[]
                {
                    Fact(
                        "fartuchy-z-igla",
                        "O 17:20 grupa weszła na warsztaty introligatorskie i dostała pomarańczowe fartuchy z naszytą igłą.",
                        true,
                        true,
                        "O 17:20 grupa weszła na warsztaty introligatorskie i dostała pomarańczowe fartuchy z naszytą igłą.",
                        "O 17:20 grupa weszła na warsztaty introligatorskie i dostała musztardowe fartuchy z naszytą igłą."),
                    Fact("zlamana-korba", "Instruktorka złamała korbę pokazowej prasy do papieru.", false),
                    Fact("kobaltowe-plotno", "Grupa wybrała kobaltowe płótno na okładkę wspólnego albumu.", true),
                    Fact("slady-kota", "Czarny kot przeszedł po stole i zostawił ślady łap w kleju.", true),
                    Fact("precle-i-notesy", "Podczas przerwy wszyscy jedli słone precle, a cztery gotowe notesy ustawili na lewej półce.", false),
                    Fact("tramwaj-numer-4", "O 18:02 grupa wyszła razem na tramwaj numer 4.", false)
                },
                new[]
                {
                    Clue("fartuch-z-naszywka", "fartuchy-z-igla", "Fartuch z naszywką igły, śladem kleju i numerem garderoby."),
                    Clue("probka-plotna", "kobaltowe-plotno", "Próbka ciemnoniebieskiego płótna z kodem K-7 i odciskiem prostokątnej okładki."),
                    Clue("odciski-w-kleju", "slady-kota", "Zbliżenie małych odcisków w przezroczystym kleju i pojedynczego czarnego włosa.")
                }),

            new Definition(
                "C13",
                $"{OutputFolder}/Case_C13_RejsBezBiletu.asset",
                "Rejs bez biletu ulgowego",
                "Ktoś wprowadził furgonetkę z lodami do sali obrad i przez godzinę odtwarzał z niej hymn miasta wspak.",
                new[]
                {
                    Fact("dolny-poklad", "O 18:05 grupa przeszła przez bramkę przystani i usiadła na dolnym pokładzie obok pomarańczowego koła ratunkowego.", false),
                    Fact("kobieta-ze-slonecznikiem", "Naprzeciwko siedziała kobieta z dużym słonecznikiem.", true),
                    Fact("dwa-sygnaly", "Przy czerwonej boi prom zatrąbił dwa razy.", true),
                    Fact(
                        "herbata-z-kotwica",
                        "Wszyscy pili herbatę z papierowych kubków z granatową kotwicą.",
                        false,
                        true,
                        "Wszyscy pili herbatę z papierowych kubków z granatową kotwicą.",
                        "Wszyscy pili herbatę z papierowych kubków z czerwoną kotwicą."),
                    Fact("melodia-akordeonisty", "Akordeonista zagrał melodię o dziewczynie idącej przez las.", true),
                    Fact("lina-i-wyjscie", "Marynarz upuścił niebieską linę przy trapie, a o 18:38 grupa zeszła razem przy targu rybnym.", false)
                },
                new[]
                {
                    Clue("notes-slonecznik", "kobieta-ze-slonecznikiem", "Notes z zasuszonym płatkiem słonecznika, fragmentem biletu i szkicem przeciwległych ławek."),
                    Clue("karta-trasy", "dwa-sygnaly", "Karta trasy z czerwoną boją i dwoma wgłębieniami przy symbolu rogu."),
                    Clue("kartka-nutowa", "melodia-akordeonisty", "Kartka nutowa bez tytułu, z ilustracją dziewczyny w lesie i dopiskiem „dolny pokład”.")
                }),

            new Definition(
                "C14",
                $"{OutputFolder}/Case_C14_SmokZaKulisami.asset",
                "Smok za kulisami",
                "Ktoś podmienił młotek sędziego na gumowego kurczaka, który zapiszczał podczas publicznego ogłaszania wyroku.",
                new[]
                {
                    Fact("zielony-polksiezyc", "O 19:15 grupa weszła do teatru lalek wejściem dla aktorów, a bileter odbił każdemu na prawej dłoni zielony półksiężyc.", true),
                    Fact("brak-zeba-smoka", "Sceniczny smok nie miał lewego filcowego zęba.", true),
                    Fact(
                        "beben-i-golab",
                        "Próba zaczęła się od trzech uderzeń w bęben; potem gołąb usiadł po lewej stronie balkonu i wszyscy na chwilę zamilkli.",
                        false,
                        true,
                        "Próba zaczęła się od trzech uderzeń w bęben; potem gołąb usiadł po lewej stronie balkonu i wszyscy na chwilę zamilkli.",
                        "Próba zaczęła się od trzech uderzeń w bęben; potem gołąb usiadł po prawej stronie balkonu i wszyscy na chwilę zamilkli."),
                    Fact("herbata-jablkowa", "W przerwie podano herbatę jabłkową w metalowych kubkach.", true),
                    Fact("czerwona-kurtyna", "Grupa pomogła zwinąć czerwoną kurtynę.", false),
                    Fact("wyjscie-foyer", "O 19:52 wszyscy wyszli razem przez główne foyer.", false)
                },
                new[]
                {
                    Clue("bibula-polksiezyce", "zielony-polksiezyc", "Zielona bibuła z rozmazanymi półksiężycami i odciskami dłoni."),
                    Clue("karta-napraw-smoka", "brak-zeba-smoka", "Karta napraw z asymetryczną szczęką smoka, pustą kieszenią i próbką filcu."),
                    Clue("kupon-przerwa", "herbata-jablkowa", "Kupon z jabłkiem, metalowym kubkiem i pieczątką „przerwa”, bez nazwy napoju.")
                }),

            new Definition(
                "C15",
                $"{OutputFolder}/Case_C15_Dozynki.asset",
                "Dożynki pod podejrzeniem",
                "Ktoś ustawił na murawie stadionu czterdzieści ogrodowych krasnali i rozegrał nimi oficjalny rzut wolny przed pełnymi trybunami.",
                new[]
                {
                    Fact("taczka-numer-12", "O 16:40 grupa weszła na konkurs działkowy przez bramę z dyni i dostała czerwoną taczkę numer 12.", true),
                    Fact("dynia-14-kilogramow", "Wspólna dynia ważyła dokładnie czternaście kilogramów.", false),
                    Fact("nadgryziona-karta", "Koza odgryzła róg karty z wynikami.", true),
                    Fact("wstazka-na-porze", "Sędzia przywiązał niebieską wstążkę do najdłuższego pora.", false),
                    Fact("deszcz-i-altana", "Nagły deszcz zagonił wszystkich pod pasiastą altanę.", true),
                    Fact(
                        "lemoniada-i-wyjscie",
                        "Grupa piła lemoniadę koperkową ze słoików przez czerwone słomki i o 17:25 wyszła zachodnią bramą za zielonym traktorem.",
                        false,
                        true,
                        "Grupa piła lemoniadę koperkową ze słoików przez czerwone słomki i o 17:25 wyszła zachodnią bramą za zielonym traktorem.",
                        "Grupa piła lemoniadę koperkową ze słoików przez zielone słomki i o 17:25 wyszła zachodnią bramą za zielonym traktorem.")
                },
                new[]
                {
                    Clue("plakietka-taczki", "taczka-numer-12", "Plakietka z czerwonymi otarciami, symbolem jednego koła i niepełnym numerem 1_."),
                    Clue("fragment-karty", "nadgryziona-karta", "Fragment karty z półkolistymi śladami zębów, białą sierścią i urwanym wynikiem."),
                    Clue("zdjecie-altany", "deszcz-i-altana", "Zdjęcie kropli, pasiastych elementów dachu i mokrych sylwetek.")
                }),

            new Definition(
                "C16",
                $"{OutputFolder}/Case_C16_SyrenaWKapieli.asset",
                "Syrena w kąpieli",
                "Ktoś ubrał brązową syrenę z miejskiej fontanny w szlafrok, czepek kąpielowy i ogromne różowe kapcie.",
                new[]
                {
                    Fact("wejscie-do-apteki", "O 22:10 grupa weszła do całodobowej apteki po bandaż na skręconą kostkę.", false),
                    Fact("muszka-w-kaczki", "Farmaceuta miał granatową muszkę w żółte kaczki.", true),
                    Fact("numer-kolejki-8", "Dostali bilet kolejki numer 8, choć poza nimi nikogo nie było.", true),
                    Fact("termometr-dwa-sygnaly", "Gdy światło na chwilę przygasło, termometr przy kasie zapiszczał dwa razy.", true),
                    Fact(
                        "ziolowe-cukierki",
                        "Farmaceuta poczęstował wszystkich miętowymi cukierkami z zielonego słoja.",
                        false,
                        true,
                        "Farmaceuta poczęstował wszystkich miętowymi cukierkami z zielonego słoja.",
                        "Farmaceuta poczęstował wszystkich anyżowymi cukierkami z zielonego słoja."),
                    Fact("wspolna-taksowka", "Po opatrzeniu kostki o 22:36 wszyscy wyszli i wsiedli do tej samej taksówki.", false)
                },
                new[]
                {
                    Clue("zdjecie-muszki", "muszka-w-kaczki", "Zdjęcie ciemnej muszki z fragmentami żółtych ptasich sylwetek, bez twarzy właściciela."),
                    Clue("bilet-apteczny", "numer-kolejki-8", "Pognieciony bilet z cyfrą 8 i symbolem moździerza, bez adresu placówki."),
                    Clue("wydruk-termometru", "termometr-dwa-sygnaly", "Wydruk urządzenia z dwiema krótkimi przerwami pomiaru podczas spadku napięcia.")
                }),

            new Definition(
                "C17",
                $"{OutputFolder}/Case_C17_FontannaZeSniadaniem.asset",
                "Fontanna ze śniadaniem",
                "Ktoś napełnił fontannę przed sądem płatkami kukurydzianymi i ustawił w niej dwumetrową srebrną łyżkę.",
                new[]
                {
                    Fact("wejscie-repair-cafe", "O 18:20 grupa przyszła z zepsutym tosterem do społecznego warsztatu naprawczego.", false),
                    Fact("stanowisko-sowa-4", "Przydzielono im stanowisko numer 4 oznaczone rysunkiem sowy.", true),
                    Fact("zolte-nauszniki", "Wolontariusz zakładał żółte nauszniki przed każdym uruchomieniem tostera.", true),
                    Fact("jazz-i-bezpiecznik", "Podczas jazzowej audycji w radiu próbne grzanie wybiło bezpiecznik nad stołem.", true),
                    Fact(
                        "herbata-w-kubkach",
                        "Czekając na nowy przewód, wszyscy pili herbatę z kubków w czerwone kropki.",
                        false,
                        true,
                        "Czekając na nowy przewód, wszyscy pili herbatę z kubków w czerwone kropki.",
                        "Czekając na nowy przewód, wszyscy pili herbatę z kubków w niebieskie paski."),
                    Fact("naprawa-i-tramwaj", "Po udanej próbie tostera grupa wyszła razem i odjechała tramwajem numer 2.", false)
                },
                new[]
                {
                    Clue("karta-stanowiska", "stanowisko-sowa-4", "Karta pracy z sową i niepełnym numerem _4, bez nazwy warsztatu."),
                    Clue("pokrowiec-nausznikow", "zolte-nauszniki", "Pokrowiec z żółtymi włóknami i piktogramem ochrony słuchu."),
                    Clue("formularz-bezpiecznika", "jazz-i-bezpiecznik", "Formularz awarii z symbolem grzałki i notatką „audycja jazzowa w tle”.")
                }),

            new Definition(
                "C18",
                $"{OutputFolder}/Case_C18_CzekoladoweParkometry.asset",
                "Czekoladowe parkometry",
                "Ktoś zastąpił monety we wszystkich parkometrach przy ratuszu czekoladowymi krążkami w złotkach papierkach.",
                new[]
                {
                    Fact("boczna-brama-szklarni", "O 19:30 grupa weszła boczną bramą na wieczorne zwiedzanie miejskiej szklarni.", false),
                    Fact("konewka-zaba", "Przewodniczka niosła zieloną konewkę w kształcie żaby.", true),
                    Fact("etykieta-monstery", "Skroplona para zasłoniła etykietę monstery, więc przewodniczka przetarła ją rękawem.", true),
                    Fact("mgielka-1944", "O 19:44 automatyczne zraszacze uruchomiły gęstą mgiełkę na całej alejce.", true),
                    Fact(
                        "kot-ze-wstazka",
                        "Szklarniowy kot spał pod ławką w czerwonej wstążce.",
                        false,
                        true,
                        "Szklarniowy kot spał pod ławką w czerwonej wstążce.",
                        "Szklarniowy kot spał pod ławką w fioletowej wstążce."),
                    Fact("wyjscie-autobusem-7", "Po zamknięciu zraszaczy wszyscy wyszli główną bramą i odjechali autobusem numer 7.", false)
                },
                new[]
                {
                    Clue("zdjecie-konewki", "konewka-zaba", "Zdjęcie zielonego pojemnika z wypukłymi oczami i wylewką, bez osoby niosącej."),
                    Clue("wilgotna-etykieta", "etykieta-monstery", "Wilgotna etykieta z końcówką „-stera” i smugą tkaniny na laminacie."),
                    Clue("log-zraszaczy", "mgielka-1944", "Log instalacji z godziną 19:44 i uruchomieniem wszystkich dysz w jednej alejce.")
                })
        };

        private static void Sync(Definition source)
        {
            var asset = AssetDatabase.LoadAssetAtPath<CaseAsset>(source.Path);
            var isNew = asset == null;
            if (isNew)
                asset = ScriptableObject.CreateInstance<CaseAsset>();

            asset.title = source.Title;
            asset.crimeDescription = source.CrimeDescription;
            asset.alibiFacts = new List<CaseAsset.AuthoredFact>(source.Facts);
            asset.alibiClues = new List<CaseAsset.AuthoredAlibiClue>(source.Clues);
            asset.minHiddenFacts = 2;
            asset.maxHiddenFacts = 3;

            var errors = asset.Validate();
            if (errors.Count > 0)
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(asset);
                throw new InvalidOperationException($"CaseAsset {source.Id} at '{source.Path}' is invalid: {string.Join(" | ", errors)}");
            }

            if (isNew)
                AssetDatabase.CreateAsset(asset, source.Path);
            EditorUtility.SetDirty(asset);
        }

        private static CaseAsset.AuthoredFact Fact(
            string id,
            string text,
            bool canBeHidden,
            bool distinctiveDetail = false,
            params string[] variants) =>
            new CaseAsset.AuthoredFact
            {
                id = id,
                text = text,
                canBeHidden = canBeHidden,
                distinctiveDetail = distinctiveDetail,
                variantTexts = new List<string>(variants ?? Array.Empty<string>())
            };

        private static CaseAsset.AuthoredAlibiClue Clue(string id, string linkedFactId, string content) =>
            new CaseAsset.AuthoredAlibiClue
            {
                id = id,
                linkedFactId = linkedFactId,
                content = content
            };

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            var folder = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private sealed class Definition
        {
            public string Id { get; }
            public string Path { get; }
            public string Title { get; }
            public string CrimeDescription { get; }
            public IReadOnlyList<CaseAsset.AuthoredFact> Facts { get; }
            public IReadOnlyList<CaseAsset.AuthoredAlibiClue> Clues { get; }

            public Definition(
                string id,
                string path,
                string title,
                string crimeDescription,
                IReadOnlyList<CaseAsset.AuthoredFact> facts,
                IReadOnlyList<CaseAsset.AuthoredAlibiClue> clues)
            {
                Id = id;
                Path = path;
                Title = title;
                CrimeDescription = crimeDescription;
                Facts = facts;
                Clues = clues;
            }
        }
    }
}
