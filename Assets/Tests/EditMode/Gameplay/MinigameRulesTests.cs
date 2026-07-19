using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Minigames;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class MinigameRulesTests
    {
        [Test]
        public void LaunchSequenceProducesDifferentSeedsForConsecutiveMinigames()
        {
            var sequence = new MinigameLaunchSequence(authoredSeed: 73, runtimeEntropy: 912);
            var seeds = new HashSet<int>();

            for (int launch = 0; launch < 40; launch++)
                seeds.Add(sequence.NextSeed());

            Assert.That(seeds.Count, Is.EqualTo(40));
        }

        [Test]
        public void FileSearchRequiresInspectionAndKeepsTargetAfterShufflePenalty()
        {
            FileSearchSession session = FileSearchSession.Create(seed: 147, folderCount: 10, targetYear: 1998);
            string targetSignature = session.TargetSignature;
            int wrongIndex = Enumerable.Range(0, session.Folders.Count)
                .First(index => session.Folders[index].Signature != targetSignature);

            Assert.That(session.Folders.Count, Is.EqualTo(10));
            Assert.That(session.Folders.Count(folder => folder.Signature == targetSignature), Is.EqualTo(1));
            Assert.That(session.Folders.Count(folder => folder.Year == session.TargetYear),
                Is.GreaterThanOrEqualTo(3));
            Assert.That(session.Folders.Count(folder =>
                    folder.Year == session.TargetYear &&
                    folder.NumberSuffix == session.TargetNumberSuffix &&
                    folder.DigitSum == session.TargetDigitSum),
                Is.EqualTo(1), "The authored clues must identify exactly one folder.");

            Assert.That(session.Inspect(wrongIndex), Is.True);
            Assert.That(session.ConfirmInspected(), Is.EqualTo(MinigameAttemptResult.Incorrect));
            Assert.That(session.PenaltySeconds, Is.EqualTo(2));
            Assert.That(session.Folders.Count(folder => folder.Signature == targetSignature), Is.EqualTo(1));
            Assert.That(session.Inspect(session.TargetIndex), Is.True);
            Assert.That(session.ConfirmInspected(), Is.EqualTo(MinigameAttemptResult.Success));
        }

        [Test]
        public void CodeLockPoolContainsFiftyDistinctCodesWithConsistentClues()
        {
            CodeLockSession[] sessions = Enumerable.Range(0, CodeLockSession.AvailableCodeCount)
                .Select(seed => CodeLockSession.Create(seed, maximumAttempts: 3))
                .ToArray();

            Assert.That(CodeLockSession.AvailableCodeCount, Is.EqualTo(50));
            Assert.That(sessions.Select(session => session.Code).Distinct().Count(), Is.EqualTo(50));

            foreach (CodeLockSession session in sessions)
            {
                int first = (session.FirstPairSum + session.OuterPairSum - session.LastPairSum) / 2;
                int middle = session.FirstPairSum - first;
                int last = session.OuterPairSum - first;
                int reconstructedCode = first * 100 + middle * 10 + last;

                Assert.That(reconstructedCode, Is.EqualTo(session.Code));
                Assert.That(session.Enter(reconstructedCode), Is.EqualTo(MinigameAttemptResult.Success));
            }
        }

        [Test]
        public void ShuffledCodeBagUsesEveryCodeBeforeRepeating()
        {
            CodeLockBag bag = CodeLockSession.CreateBag(seed: 4101);
            int[] firstCycle = Enumerable.Range(0, CodeLockSession.AvailableCodeCount)
                .Select(_ => bag.DrawNext())
                .ToArray();

            Assert.That(firstCycle.Distinct().Count(), Is.EqualTo(CodeLockSession.AvailableCodeCount));

            int firstCodeOfSecondCycle = bag.DrawNext();
            Assert.That(firstCodeOfSecondCycle, Is.Not.EqualTo(firstCycle[firstCycle.Length - 1]));

            int[] secondCycle = new[] { firstCodeOfSecondCycle }
                .Concat(Enumerable.Range(1, CodeLockSession.AvailableCodeCount - 1)
                    .Select(_ => bag.DrawNext()))
                .ToArray();
            Assert.That(secondCycle.Distinct().Count(), Is.EqualTo(CodeLockSession.AvailableCodeCount));
        }

        [Test]
        public void CodeLockRestartsAfterLimitedAttemptsWithoutBecomingBlocked()
        {
            var session = new CodeLockSession(code: 417, maximumAttempts: 2);

            Assert.That(session.FirstPairSum, Is.EqualTo(5));
            Assert.That(session.LastPairSum, Is.EqualTo(8));
            Assert.That(session.OuterPairSum, Is.EqualTo(11));

            Assert.That(session.Enter(111), Is.EqualTo(MinigameAttemptResult.Incorrect));
            Assert.That(session.Enter(222), Is.EqualTo(MinigameAttemptResult.Restarted));
            Assert.That(session.AttemptsInCurrentRun, Is.Zero);
            Assert.That(session.Enter(417), Is.EqualTo(MinigameAttemptResult.Success));
        }

        [Test]
        public void RecordsTerminalRequiresFiltersInspectionAndConfirmation()
        {
            RecordsTerminalSession session = RecordsTerminalSession.Create(seed: 912, recordCount: 6);

            Assert.That(session.Records.Count, Is.EqualTo(60));
            Assert.That(session.Records.Count(record =>
                record.Surname == session.TargetSurname &&
                record.Year == session.TargetYear &&
                record.Unit == session.TargetUnit), Is.EqualTo(1));
            Assert.That(session.OpenRecord(session.TargetIndex), Is.False,
                "A record cannot be opened before both filters are applied.");

            session.SetUnitFilter(session.TargetUnit);
            session.SetYearBandFilter(session.TargetYearBandStart);

            Assert.That(session.VisibleRecordIndices, Does.Contain(session.TargetIndex));
            Assert.That(session.VisibleRecordIndices.Count, Is.GreaterThanOrEqualTo(5),
                "Filtering should narrow the list without directly revealing the answer.");
            Assert.That(session.OpenRecord(session.TargetIndex), Is.True);
            Assert.That(session.ConfirmOpenedRecord(), Is.EqualTo(MinigameAttemptResult.Success));

            foreach (string unit in session.UnitOptions)
            {
                foreach (int yearBand in session.YearBandOptions)
                {
                    session.SetUnitFilter(unit);
                    session.SetYearBandFilter(yearBand);
                    Assert.That(session.VisibleRecordIndices.Count, Is.GreaterThanOrEqualTo(5),
                        $"Filter {unit}, {yearBand}-{yearBand + 4} should contain five records.");
                }
            }
        }

        [Test]
        public void SameSeedProducesSameAuthoredPuzzleLayout()
        {
            FileSearchSession first = FileSearchSession.Create(73, 9, 2004);
            FileSearchSession second = FileSearchSession.Create(73, 9, 2004);
            RecordsTerminalSession firstTerminal = RecordsTerminalSession.Create(73, 7);
            RecordsTerminalSession secondTerminal = RecordsTerminalSession.Create(73, 7);

            Assert.That(first.Folders.Select(folder => folder.Label),
                Is.EqualTo(second.Folders.Select(folder => folder.Label)));
            Assert.That(firstTerminal.Records.Select(record => record.Label),
                Is.EqualTo(secondTerminal.Records.Select(record => record.Label)));
        }

        [Test]
        public void AuthoredCluesRemainSolvableAcrossManySeeds()
        {
            for (int seed = 1; seed <= 128; seed++)
            {
                FileSearchSession files = FileSearchSession.Create(seed, 10, 1998);
                Assert.That(files.Folders.Count(folder =>
                        folder.Year == files.TargetYear &&
                        folder.NumberSuffix == files.TargetNumberSuffix &&
                        folder.DigitSum == files.TargetDigitSum),
                    Is.EqualTo(1), $"File clues are ambiguous for seed {seed}.");
                Assert.That(files.Folders.Count(folder => folder.Year == files.TargetYear),
                    Is.GreaterThanOrEqualTo(3), $"File search lacks same-year decoys for seed {seed}.");
                Assert.That(files.TargetIndex, Is.InRange(0, files.Folders.Count - 1));

                RecordsTerminalSession records = RecordsTerminalSession.Create(seed, 7);
                records.SetUnitFilter(records.TargetUnit);
                records.SetYearBandFilter(records.TargetYearBandStart);
                Assert.That(records.VisibleRecordIndices, Does.Contain(records.TargetIndex));
                Assert.That(records.VisibleRecordIndices.Count, Is.GreaterThanOrEqualTo(5));
                Assert.That(records.VisibleRecordIndices.Count(index =>
                        records.Records[index].Surname[0] == records.TargetSurnameInitial),
                    Is.EqualTo(1), $"Record clues are ambiguous for seed {seed}.");
            }
        }
    }
}
