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
        public FileFolderOption(string signature, int year)
        {
            Signature = signature;
            Year = year;
        }

        public string Signature { get; }
        public int Year { get; }
        public string Label => $"{Signature}  •  {Year}";
    }

    public sealed class FileSearchSession
    {
        private readonly List<FileFolderOption> folders;
        private int shuffleSeed;

        private FileSearchSession(
            int seed,
            List<FileFolderOption> folders,
            string targetSignature)
        {
            shuffleSeed = seed;
            this.folders = folders;
            TargetSignature = targetSignature;
            RefreshTargetIndex();
        }

        public IReadOnlyList<FileFolderOption> Folders => folders;
        public string TargetSignature { get; }
        public int TargetIndex { get; private set; }
        public int WrongChoiceCount { get; private set; }
        public int PenaltySeconds { get; private set; }

        public static FileSearchSession Create(int seed, int folderCount, int targetYear)
        {
            folderCount = Math.Max(8, Math.Min(12, folderCount));
            targetYear = Math.Max(1900, Math.Min(2100, targetYear));
            var random = new DeterministicRandom(seed);
            int targetNumber = 1000 + random.Next(9000);
            string targetSignature = $"KR-{targetNumber}/{targetYear}";
            var options = new List<FileFolderOption>(folderCount)
            {
                new FileFolderOption(targetSignature, targetYear)
            };

            while (options.Count < folderCount)
            {
                int year = targetYear + random.Next(-4, 5);
                string signature = $"KR-{1000 + random.Next(9000)}/{year}";
                bool duplicate = false;
                for (int index = 0; index < options.Count; index++)
                {
                    if (options[index].Signature == signature)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                    options.Add(new FileFolderOption(signature, year));
            }

            Shuffle(options, ref random);
            return new FileSearchSession(seed, options, targetSignature);
        }

        public MinigameAttemptResult Choose(int index)
        {
            if (index >= 0 && index < folders.Count &&
                folders[index].Signature == TargetSignature)
            {
                return MinigameAttemptResult.Success;
            }

            WrongChoiceCount++;
            PenaltySeconds += 2;
            var random = new DeterministicRandom(unchecked(shuffleSeed + WrongChoiceCount * 7919));
            Shuffle(folders, ref random);
            RefreshTargetIndex();
            return MinigameAttemptResult.Incorrect;
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
            int targetYear)
        {
            Records = records;
            TargetSurname = targetSurname;
            TargetYear = targetYear;
            TargetIndex = records.FindIndex(record =>
                record.Surname == targetSurname && record.Year == targetYear);
        }

        public IReadOnlyList<RecordsTerminalOption> Records { get; }
        public string TargetSurname { get; }
        public int TargetYear { get; }
        public int TargetIndex { get; }

        public static RecordsTerminalSession Create(int seed, int recordCount)
        {
            recordCount = Math.Max(4, Math.Min(9, recordCount));
            var random = new DeterministicRandom(seed);
            string targetSurname = Surnames[random.Next(Surnames.Length)];
            int targetYear = 1992 + random.Next(25);
            var records = new List<RecordsTerminalOption>(recordCount)
            {
                new RecordsTerminalOption(
                    targetSurname,
                    targetYear,
                    Units[random.Next(Units.Length)])
            };

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

                if (!duplicate)
                    records.Add(candidate);
            }

            for (int index = records.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                RecordsTerminalOption temporary = records[index];
                records[index] = records[other];
                records[other] = temporary;
            }

            return new RecordsTerminalSession(records, targetSurname, targetYear);
        }

        public MinigameAttemptResult Select(int index)
        {
            return index == TargetIndex
                ? MinigameAttemptResult.Success
                : MinigameAttemptResult.Incorrect;
        }
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
