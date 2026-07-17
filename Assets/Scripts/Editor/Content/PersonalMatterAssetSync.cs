using System;
using System.Collections.Generic;
using InterrogationRoom.Content;
using UnityEditor;
using UnityEngine;

namespace InterrogationRoom.Editor.Content
{
    public static class PersonalMatterAssetSync
    {
        private const string OutputFolder = "Assets/Content/PersonalMatters";

        [MenuItem("Interrogation Room/Content/Sync Personal Matter Assets")]
        public static void Sync()
        {
            EnsureFolder("Assets/Content");
            EnsureFolder(OutputFolder);

            Sync(CreateOs01());
            Sync(CreateOs02());
            Sync(CreateOs03());
            Sync(CreateOs04());
            Sync(CreateOs05());
            Sync(CreateOs06());
            Sync(CreateOs07());
            Sync(CreateOs08());
            Sync(CreateOs09());
            Sync(CreateOs10());
            Sync(CreateOs11());
            Sync(CreateOs12());
            Sync(CreateOs13());
            Sync(CreateOs14());
            Sync(CreateOs15());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PersonalMatterAssetSync] Synchronized OS-01 through OS-15.");
        }

        private static void Sync(Definition source)
        {
            var path = $"{OutputFolder}/{source.Id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PersonalMatterAsset>(path);
            var isNew = asset == null;
            if (isNew)
                asset = ScriptableObject.CreateInstance<PersonalMatterAsset>();

            asset.stableId = source.Id;
            asset.title = source.Title;
            asset.motive = source.Motive;
            asset.steps = new List<PersonalMatterAsset.AuthoredStep>();
            foreach (var step in source.Steps)
            {
                asset.steps.Add(new PersonalMatterAsset.AuthoredStep
                {
                    stableStepId = step.Id,
                    description = step.Description,
                    locationHint = step.LocationHint,
                    anchorActionId = step.AnchorActionId,
                    createsIncident = step.CreatesIncident
                });
            }
            asset.reservedItemIds = new List<string>(source.ReservedItemIds);

            var errors = asset.Validate();
            if (errors.Count > 0)
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(asset);
                throw new InvalidOperationException(
                    $"PersonalMatterAsset '{source.Id}' is invalid: {string.Join(" | ", errors)}");
            }

            if (isNew)
                AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
        }

        private static Definition CreateOs01() => new Definition(
            "OS-01",
            "Odzyskaj skonfiskowane papierosy",
            "Paczka trafiła do depozytu podczas zatrzymania. Chcesz ją odzyskać, zanim ktoś powiąże ją z Tobą lub wyrzuci.",
            new[]
            {
                Step(
                    "OS-01-klucz",
                    "Zabierz właściwy klucz z listwy depozytowej.",
                    "magazyn dowodów — listwa kluczy",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-01-papierosy",
                    "Otwórz oznaczony depozyt i zabierz swoją paczkę papierosów.",
                    "magazyn dowodów — szafki depozytowe",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "klucz-depozytu-papierosy", "papierosy-skonfiskowane" });

        private static Definition CreateOs02() => new Definition(
            "OS-02",
            "Usuń kompromitujący list",
            "W kartotece znajduje się prywatny list, który może zniszczyć ważną relację. Musisz wyjąć go z akt i schować w neutralnym miejscu.",
            new[]
            {
                Step(
                    "OS-02-koperta",
                    "Odszukaj opisaną kopertę w kartotece.",
                    "archiwum — szafki na literę K",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-02-skrytka",
                    "Wyjmij list z koperty i ukryj go w neutralnej skrytce poza swoją teczką.",
                    "archiwum — wolna skrytka dokumentowa",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "kompromitujacy-list", "koperta-kartoteki" });

        private static Definition CreateOs03() => new Definition(
            "OS-03",
            "Odzyskaj prywatny telefon",
            "Telefon zawiera wiadomości, których policja nie powinna czytać. Najpierw ustal numer depozytu, potem odzyskaj urządzenie.",
            new[]
            {
                Step(
                    "OS-03-numer",
                    "Znajdź żeton lub numer przypisany do Twojego telefonu w rejestrze depozytu.",
                    "magazyn dowodów — rejestr depozytowy",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-03-telefon",
                    "Otwórz odpowiadającą numerowi szafkę i zabierz telefon.",
                    "magazyn dowodów — szafki na elektronikę",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "prywatny-telefon", "zeton-depozytowy-telefon" });

        private static Definition CreateOs04() => new Definition(
            "OS-04",
            "Usuń wpis o długu",
            "W księdze pozostał wpis o długu, który może ujawnić Twój motyw do kłamstwa. Chcesz go dyskretnie zmienić.",
            new[]
            {
                Step(
                    "OS-04-korektor",
                    "Zabierz pieczątkę albo korektor potrzebny do poprawienia wpisu.",
                    "biuro dyżurnego — przybory na biurku",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-04-ksiega",
                    "Odszukaj wskazany wpis i zmień go tak, by dług nie prowadził do Ciebie.",
                    "archiwum — księga zobowiązań",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "pieczatka-biurkowa", "ksiega-zobowiazan" });

        private static Definition CreateOs05() => new Definition(
            "OS-05",
            "Odzyskaj rodzinne zdjęcie",
            "Wśród zabezpieczonych materiałów jest jedyna odbitka ważnego rodzinnego zdjęcia. Chcesz ją odzyskać, zanim trafi do akt.",
            new[]
            {
                Step(
                    "OS-05-pudelko",
                    "Odszukaj oznaczone pudełko dowodowe i narusz jego plombę.",
                    "magazyn dowodów — półka z pudełkami fotograficznymi",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-05-fotografia",
                    "Wyjmij rodzinną fotografię i zostaw pustą przekładkę.",
                    "magazyn dowodów — stół przeglądowy",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "rodzinna-fotografia", "pudelko-fotograficzne" });

        private static Definition CreateOs06() => new Definition(
            "OS-06",
            "Zniszcz kupon bukmacherski",
            "Kupon dokumentuje zakład, o którym nie może dowiedzieć się rodzina ani policja. Musisz odnaleźć go w aktach i wyrzucić.",
            new[]
            {
                Step(
                    "OS-06-sygnatura",
                    "Odczytaj sygnaturę właściwej teczki z indeksu archiwum.",
                    "archiwum — indeks rzeczowy",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-06-kupon",
                    "Odszukaj teczkę i wyrzuć kupon bukmacherski do kosza.",
                    "archiwum — regał teczek i kosz na dokumenty",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "kupon-bukmacherski", "teczka-kuponu" });

        private static Definition CreateOs07() => new Definition(
            "OS-07",
            "Odzyskaj pierścionek",
            "Pierścionek został zdeponowany razem z rzeczami osobistymi. Nie chcesz ryzykować, że zniknie w policyjnym magazynie.",
            new[]
            {
                Step(
                    "OS-07-klucz",
                    "Zabierz mały klucz z opisanej koperty depozytowej.",
                    "magazyn dowodów — przegródki z kluczami",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-07-pierscionek",
                    "Otwórz właściwą kasetkę i zabierz pierścionek.",
                    "magazyn dowodów — kasetki rzeczy osobistych",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "klucz-kasetki-pierscionek", "pierscionek-osobisty" });

        private static Definition CreateOs08() => new Definition(
            "OS-08",
            "Nadpisz kompromitujące nagranie",
            "Na zabezpieczonej kasecie słychać rozmowę, która może Ci zaszkodzić. Chcesz zniszczyć tylko jej najbardziej kompromitujący fragment.",
            new[]
            {
                Step(
                    "OS-08-kaseta",
                    "Zabierz właściwą kasetę z opisanej półki.",
                    "archiwum nagrań — półka kaset",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-08-nadpisanie",
                    "Włóż kasetę do magnetofonu i nadpisz wskazany fragment.",
                    "pokój odsłuchowy — magnetofon",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "kaseta-kompromitujaca", "magnetofon-archiwum" });

        private static Definition CreateOs09() => new Definition(
            "OS-09",
            "Odzyskaj leki",
            "Potrzebujesz własnych leków, które trafiły do depozytu. Recepta pozwoli Ci odnaleźć właściwy, oznaczony schowek.",
            new[]
            {
                Step(
                    "OS-09-recepta",
                    "Znajdź receptę w swojej teczce osobowej.",
                    "archiwum — teczki osobowe",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-09-leki",
                    "Użyj danych z recepty, aby otworzyć oznaczoną szafkę i zabrać butelkę leków.",
                    "magazyn dowodów — szafki medyczne",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "recepta-osobista", "butelka-lekow" });

        private static Definition CreateOs10() => new Definition(
            "OS-10",
            "Ukryj sfałszowane zwolnienie",
            "Przy Tobie znaleziono niedokończone fałszywe zwolnienie. Chcesz nadać mu pozory autentyczności i schować je w swojej teczce.",
            new[]
            {
                Step(
                    "OS-10-pieczatka",
                    "Zabierz pieczątkę z biurka dyżurnego.",
                    "biuro dyżurnego — przybory na biurku",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-10-zwolnienie",
                    "Ostempluj zwolnienie i włóż je do swojej teczki.",
                    "archiwum — teczka osobowa",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "pieczatka-biurkowa", "sfalszowane-zwolnienie" });

        private static Definition CreateOs11() => new Definition(
            "OS-11",
            "Usuń niewygodne zeznanie",
            "W protokole zapisano wypowiedź, która może obciążyć bliską Ci osobę. Chcesz zastąpić tę stronę neutralną kopią.",
            new[]
            {
                Step(
                    "OS-11-klucz",
                    "Zdobądź klucz do zamkniętej szafy archiwum.",
                    "biuro dyżurnego — listwa kluczy",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-11-protokol",
                    "Otwórz szafę i podmień wskazaną stronę protokołu.",
                    "archiwum — zamknięta szafa protokołów",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "klucz-szafy-archiwum", "strona-niewygodnego-zeznania" });

        private static Definition CreateOs12() => new Definition(
            "OS-12",
            "Odzyskaj szczęśliwą monetę",
            "Stara moneta jest dla Ciebie talizmanem, ale została zapakowana do anonimowego pakietu depozytowego.",
            new[]
            {
                Step(
                    "OS-12-numer",
                    "Sprawdź numer właściwego pakietu w księdze depozytu.",
                    "magazyn dowodów — księga depozytowa",
                    "osobista-sprawa-przygotuj",
                    false),
                Step(
                    "OS-12-moneta",
                    "Otwórz odpowiadającą numerowi kopertę i zabierz monetę.",
                    "magazyn dowodów — koperty drobnych przedmiotów",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "szczesliwa-moneta", "koperta-monety" });

        private static Definition CreateOs13() => new Definition(
            "OS-13",
            "Usuń numer telefonu bliskiej osoby",
            "Karta wiadomości łączy prywatny numer z Twoim nazwiskiem. Chcesz zastąpić ją kartą, która nie prowadzi do bliskiej osoby.",
            new[]
            {
                Step(
                    "OS-13-karta",
                    "Zabierz oryginalną kartę wiadomości z biurka.",
                    "biuro dyżurnego — tacka wiadomości",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-13-podmiana",
                    "Włóż do teczki neutralną kartę zamiast oryginału.",
                    "archiwum — teczka korespondencji",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "karta-prywatnego-numeru", "neutralna-karta-wiadomosci" });

        private static Definition CreateOs14() => new Definition(
            "OS-14",
            "Odzyskaj kwit z lombardu",
            "Kwit może ujawnić, że zastawiony przedmiot nie należał do Ciebie. Chcesz wyjąć go z depozytu i schować poza aktami.",
            new[]
            {
                Step(
                    "OS-14-szafka",
                    "Przeszukaj oznaczoną szafkę z rzeczami osobistymi.",
                    "magazyn dowodów — szafki rzeczy osobistych",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-14-kwit",
                    "Wyjmij kwit z lombardu i przenieś go do książki pełniącej skrytkę.",
                    "archiwum — półka książek i formularzy",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "kwit-z-lombardu", "ksiazka-skrytka" });

        private static Definition CreateOs15() => new Definition(
            "OS-15",
            "Ukryj kompromitującą fotografię",
            "Jedna fotografia może wywołać skandal niezwiązany ze Sprawą. Chcesz wyjąć ją z materiałów i pozostawić neutralną odbitkę.",
            new[]
            {
                Step(
                    "OS-15-fotografia",
                    "Zabierz kompromitujące zdjęcie z koperty dowodowej.",
                    "magazyn dowodów — koperty fotograficzne",
                    "osobista-sprawa-przygotuj",
                    true),
                Step(
                    "OS-15-podmiana",
                    "Włóż neutralną odbitkę do koperty zamiast oryginału.",
                    "magazyn dowodów — stół przeglądowy",
                    "osobista-sprawa-zakoncz",
                    true)
            },
            new[] { "kompromitujaca-fotografia", "neutralna-odbitka" });

        private static StepDefinition Step(
            string id,
            string description,
            string locationHint,
            string anchorActionId,
            bool createsIncident) =>
            new StepDefinition(id, description, locationHint, anchorActionId, createsIncident);

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
            public string Title { get; }
            public string Motive { get; }
            public IReadOnlyList<StepDefinition> Steps { get; }
            public IReadOnlyList<string> ReservedItemIds { get; }

            public Definition(
                string id,
                string title,
                string motive,
                IReadOnlyList<StepDefinition> steps,
                IReadOnlyList<string> reservedItemIds)
            {
                Id = id;
                Title = title;
                Motive = motive;
                Steps = steps;
                ReservedItemIds = reservedItemIds;
            }
        }

        private sealed class StepDefinition
        {
            public string Id { get; }
            public string Description { get; }
            public string LocationHint { get; }
            public string AnchorActionId { get; }
            public bool CreatesIncident { get; }

            public StepDefinition(
                string id,
                string description,
                string locationHint,
                string anchorActionId,
                bool createsIncident)
            {
                Id = id;
                Description = description;
                LocationHint = locationHint;
                AnchorActionId = anchorActionId;
                CreatesIncident = createsIncident;
            }
        }
    }
}
