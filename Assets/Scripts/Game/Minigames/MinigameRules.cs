using System;
using System.Collections.Generic;

namespace InterrogationRoom.Minigames
{
    public enum MinigameKind
    {
        FileSearch,
        CodeLock,
        RecordsTerminal
    }

    public enum MinigameAttemptResult
    {
        Incorrect,
        Restarted,
        Success
    }

    public readonly struct FileFolderOption
    {
        public FileFolderOption(int number, int year)
        {
            Number = Math.Max(0, Math.Min(9999, number));
            Year = year;
        }

        public int Number { get; }
        public int Year { get; }
        public string Signature => $"KR-{Number:0000}/{Year}";
        public int NumberSuffix => Number % 100;
        public int DigitSum =>
            Number / 1000 +
            Number / 100 % 10 +
            Number / 10 % 10 +
            Number % 10;
        public string Label => $"{Signature}  •  {Year}";
    }

    public sealed class FileSearchSession
    {
        private readonly List<FileFolderOption> folders;
        private int shuffleSeed;
        private int inspectedIndex = -1;

        private FileSearchSession(
            int seed,
            List<FileFolderOption> folders,
            int targetNumber,
            int targetYear)
        {
            shuffleSeed = seed;
            this.folders = folders;
            TargetNumber = targetNumber;
            TargetYear = targetYear;
            RefreshTargetIndex();
        }

        public IReadOnlyList<FileFolderOption> Folders => folders;
        public int TargetNumber { get; }
        public int TargetYear { get; }
        public string TargetSignature => $"KR-{TargetNumber:0000}/{TargetYear}";
        public int TargetNumberSuffix => TargetNumber % 100;
        public int TargetDigitSum =>
            TargetNumber / 1000 +
            TargetNumber / 100 % 10 +
            TargetNumber / 10 % 10 +
            TargetNumber % 10;
        public int TargetIndex { get; private set; }
        public int InspectedIndex => inspectedIndex;
        public int WrongChoiceCount { get; private set; }
        public int PenaltySeconds { get; private set; }

        public static FileSearchSession Create(int seed, int folderCount, int targetYear)
        {
            folderCount = Math.Max(8, Math.Min(12, folderCount));
            targetYear = Math.Max(1900, Math.Min(2100, targetYear));
            var random = new DeterministicRandom(seed);
            int targetNumber = 1000 + random.Next(9000);
            var options = new List<FileFolderOption>(folderCount)
            {
                new FileFolderOption(targetNumber, targetYear)
            };

            while (options.Count < folderCount)
            {
                int year = targetYear + random.Next(-4, 5);
                int number = 1000 + random.Next(9000);
                var candidate = new FileFolderOption(number, year);
                bool duplicate = false;
                for (int index = 0; index < options.Count; index++)
                {
                    if (options[index].Signature == candidate.Signature)
                    {
                        duplicate = true;
                        break;
                    }
                }

                bool duplicatesTargetClues =
                    candidate.Year == targetYear &&
                    candidate.NumberSuffix == targetNumber % 100 &&
                    candidate.DigitSum == DigitSum(targetNumber);
                if (!duplicate && !duplicatesTargetClues)
                    options.Add(candidate);
            }

            Shuffle(options, ref random);
            return new FileSearchSession(seed, options, targetNumber, targetYear);
        }

        public bool Inspect(int index)
        {
            if (index < 0 || index >= folders.Count)
                return false;

            inspectedIndex = index;
            return true;
        }

        public MinigameAttemptResult ConfirmInspected()
        {
            if (inspectedIndex >= 0 &&
                inspectedIndex < folders.Count &&
                folders[inspectedIndex].Signature == TargetSignature)
                return MinigameAttemptResult.Success;

            WrongChoiceCount++;
            PenaltySeconds += 2;
            inspectedIndex = -1;
            var random = new DeterministicRandom(unchecked(shuffleSeed + WrongChoiceCount * 7919));
            Shuffle(folders, ref random);
            RefreshTargetIndex();
            return MinigameAttemptResult.Incorrect;
        }

        public MinigameAttemptResult Choose(int index)
        {
            return Inspect(index)
                ? ConfirmInspected()
                : MinigameAttemptResult.Incorrect;
        }

        private void RefreshTargetIndex()
        {
            TargetIndex = folders.FindIndex(folder => folder.Signature == TargetSignature);
        }

        private static void Shuffle(List<FileFolderOption> values, ref DeterministicRandom random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                FileFolderOption temporary = values[index];
                values[index] = values[other];
                values[other] = temporary;
            }
        }

        private static int DigitSum(int number) =>
            number / 1000 +
            number / 100 % 10 +
            number / 10 % 10 +
            number % 10;
    }

    public sealed class CodeLockSession
    {
        public CodeLockSession(int code, int maximumAttempts)
        {
            Code = Math.Max(0, Math.Min(999, code));
            MaximumAttempts = Math.Max(1, maximumAttempts);
        }

        public int Code { get; }
        public int MaximumAttempts { get; }
        public int AttemptsInCurrentRun { get; private set; }
        public int FirstPairSum => Code / 100 + Code / 10 % 10;
        public int LastPairSum => Code / 10 % 10 + Code % 10;
        public int OuterPairSum => Code / 100 + Code % 10;

        public MinigameAttemptResult Enter(int candidate)
        {
            if (candidate == Code)
                return MinigameAttemptResult.Success;

            AttemptsInCurrentRun++;
            if (AttemptsInCurrentRun < MaximumAttempts)
                return MinigameAttemptResult.Incorrect;

            AttemptsInCurrentRun = 0;
            return MinigameAttemptResult.Restarted;
        }
    }

    public readonly struct RecordsTerminalOption
    {
        public RecordsTerminalOption(string surname, int year, string unit)
        {
            Surname = surname;
            Year = year;
            Unit = unit;
        }

        public string Surname { get; }
        public int Year { get; }
        public string Unit { get; }
        public string Label => $"{Surname}  •  {Year}  •  {Unit}";
    }

    public sealed class RecordsTerminalSession
    {
        private static readonly string[] Surnames =
        {
            "Borkowski", "Czarnecki", "Domańska", "Kowal", "Lis", "Majewska",
            "Nowak", "Ostrowski", "Pawlak", "Sikora", "Wrona", "Zielińska"
        };

        private static readonly string[] Units =
        {
            "Archiwum", "Patrol", "Ruch drogowy", "Dochodzeniówka"
        };

        private RecordsTerminalSession(
            List<RecordsTerminalOption> records,
            string targetSurname,
            int targetYear,
            string targetUnit,
            List<int> yearBandOptions)
        {
            Records = records;
            TargetSurname = targetSurname;
            TargetYear = targetYear;
            TargetUnit = targetUnit;
            TargetYearBandStart = YearBandStart(targetYear);
            YearBandOptions = yearBandOptions;
            TargetIndex = records.FindIndex(record =>
                record.Surname == targetSurname &&
                record.Year == targetYear &&
                record.Unit == targetUnit);
        }

        public IReadOnlyList<RecordsTerminalOption> Records { get; }
        public IReadOnlyList<string> UnitOptions => Units;
        public IReadOnlyList<int> YearBandOptions { get; }
        public string TargetSurname { get; }
        public char TargetSurnameInitial => TargetSurname[0];
        public int TargetYear { get; }
        public string TargetUnit { get; }
        public int TargetYearBandStart { get; }
        public int TargetIndex { get; }
        public IReadOnlyList<int> VisibleRecordIndices => visibleRecordIndices;
        public int OpenedRecordIndex { get; private set; } = -1;

        private readonly List<int> visibleRecordIndices = new List<int>();
        private string selectedUnit;
        private int? selectedYearBandStart;

        public static RecordsTerminalSession Create(int seed, int recordCount)
        {
            recordCount = Math.Max(4, Math.Min(9, recordCount));
            var random = new DeterministicRandom(seed);
            string targetSurname = Surnames[random.Next(Surnames.Length)];
            int targetYear = 1992 + random.Next(25);
            string targetUnit = Units[random.Next(Units.Length)];
            int targetBand = YearBandStart(targetYear);
            var records = new List<RecordsTerminalOption>(recordCount)
            {
                new RecordsTerminalOption(
                    targetSurname,
                    targetYear,
                    targetUnit)
            };

            string decoySurname = Surnames[0];
            for (int index = 0; index < Surnames.Length; index++)
            {
                if (Surnames[index][0] == targetSurname[0])
                    continue;

                decoySurname = Surnames[index];
                break;
            }
            records.Add(new RecordsTerminalOption(
                decoySurname,
                targetBand + random.Next(5),
                targetUnit));

            while (records.Count < recordCount)
            {
                string surname = Surnames[random.Next(Surnames.Length)];
                int year = 1992 + random.Next(25);
                if (surname == targetSurname && year == targetYear)
                    year++;

                var candidate = new RecordsTerminalOption(
                    surname,
                    year,
                    Units[random.Next(Units.Length)]);
                bool duplicate = false;
                for (int index = 0; index < records.Count; index++)
                {
                    RecordsTerminalOption existing = records[index];
                    if (existing.Surname == candidate.Surname &&
                        existing.Year == candidate.Year &&
                        existing.Unit == candidate.Unit)
                    {
                        duplicate = true;
                        break;
                    }
                }

                bool duplicatesTargetClue =
                    candidate.Unit == targetUnit &&
                    YearBandStart(candidate.Year) == targetBand &&
                    candidate.Surname[0] == targetSurname[0];
                if (!duplicate && !duplicatesTargetClue)
                    records.Add(candidate);
            }

            for (int index = records.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                RecordsTerminalOption temporary = records[index];
                records[index] = records[other];
                records[other] = temporary;
            }

            var yearBands = new List<int>
            {
                targetBand - 5,
                targetBand,
                targetBand + 5
            };
            for (int index = yearBands.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                int temporary = yearBands[index];
                yearBands[index] = yearBands[other];
                yearBands[other] = temporary;
            }

            return new RecordsTerminalSession(
                records,
                targetSurname,
                targetYear,
                targetUnit,
                yearBands);
        }

        public bool SetUnitFilter(string unit)
        {
            if (Array.IndexOf(Units, unit) < 0)
                return false;

            selectedUnit = unit;
            OpenedRecordIndex = -1;
            RefreshVisibleRecords();
            return true;
        }

        public bool SetYearBandFilter(int yearBandStart)
        {
            bool isAvailable = false;
            for (int index = 0; index < YearBandOptions.Count; index++)
            {
                if (YearBandOptions[index] != yearBandStart)
                    continue;

                isAvailable = true;
                break;
            }
            if (!isAvailable)
                return false;

            selectedYearBandStart = yearBandStart;
            OpenedRecordIndex = -1;
            RefreshVisibleRecords();
            return true;
        }

        public bool OpenRecord(int index)
        {
            if (!visibleRecordIndices.Contains(index))
                return false;

            OpenedRecordIndex = index;
            return true;
        }

        public MinigameAttemptResult ConfirmOpenedRecord()
        {
            bool success = OpenedRecordIndex == TargetIndex;
            OpenedRecordIndex = -1;
            return success
                ? MinigameAttemptResult.Success
                : MinigameAttemptResult.Incorrect;
        }

        public MinigameAttemptResult Select(int index)
        {
            return OpenRecord(index)
                ? ConfirmOpenedRecord()
                : MinigameAttemptResult.Incorrect;
        }

        private void RefreshVisibleRecords()
        {
            visibleRecordIndices.Clear();
            if (string.IsNullOrEmpty(selectedUnit) || !selectedYearBandStart.HasValue)
                return;

            for (int index = 0; index < Records.Count; index++)
            {
                RecordsTerminalOption record = Records[index];
                if (record.Unit == selectedUnit &&
                    YearBandStart(record.Year) == selectedYearBandStart.Value)
                {
                    visibleRecordIndices.Add(index);
                }
            }
        }

        private static int YearBandStart(int year) => year - year % 5;
    }

    internal struct DeterministicRandom
    {
        private uint state;

        public DeterministicRandom(int seed)
        {
            state = unchecked((uint)seed) ^ 0xA3C59AC3u;
            if (state == 0u)
                state = 1u;
        }

        public int Next(int maximumExclusive)
        {
            if (maximumExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));

            state = unchecked(state * 1664525u + 1013904223u);
            return (int)(state % (uint)maximumExclusive);
        }

        public int Next(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            return minimumInclusive + Next(maximumExclusive - minimumInclusive);
        }
    }
}
