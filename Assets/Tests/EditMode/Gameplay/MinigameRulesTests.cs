using System.Linq;
using InterrogationRoom.Minigames;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class MinigameRulesTests
    {
        [Test]
        public void FileSearchRequiresInspectionAndKeepsTargetAfterShufflePenalty()
        {
            FileSearchSession session = FileSearchSession.Create(seed: 147, folderCount: 10, targetYear: 1998);
            string targetSignature = session.TargetSignature;
            int wrongIndex = Enumerable.Range(0, session.Folders.Count)
                .First(index => session.Folders[index].Signature != targetSignature);

            Assert.That(session.Folders.Count, Is.EqualTo(10));
            Assert.That(session.Folders.Count(folder => folder.Signature == targetSignature), Is.EqualTo(1));
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

            Assert.That(session.Records.Count, Is.EqualTo(6));
            Assert.That(session.Records.Count(record =>
                record.Surname == session.TargetSurname &&
                record.Year == session.TargetYear &&
                record.Unit == session.TargetUnit), Is.EqualTo(1));
            Assert.That(session.OpenRecord(session.TargetIndex), Is.False,
                "A record cannot be opened before both filters are applied.");

            session.SetUnitFilter(session.TargetUnit);
            session.SetYearBandFilter(session.TargetYearBandStart);

            Assert.That(session.VisibleRecordIndices, Does.Contain(session.TargetIndex));
            Assert.That(session.VisibleRecordIndices.Count, Is.GreaterThanOrEqualTo(2),
                "Filtering should narrow the list without directly revealing the answer.");
            Assert.That(session.OpenRecord(session.TargetIndex), Is.True);
            Assert.That(session.ConfirmOpenedRecord(), Is.EqualTo(MinigameAttemptResult.Success));
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

                RecordsTerminalSession records = RecordsTerminalSession.Create(seed, 7);
                records.SetUnitFilter(records.TargetUnit);
                records.SetYearBandFilter(records.TargetYearBandStart);
                Assert.That(records.VisibleRecordIndices, Does.Contain(records.TargetIndex));
                Assert.That(records.VisibleRecordIndices.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(records.VisibleRecordIndices.Count(index =>
                        records.Records[index].Surname[0] == records.TargetSurnameInitial),
                    Is.EqualTo(1), $"Record clues are ambiguous for seed {seed}.");
            }
        }
    }
}
