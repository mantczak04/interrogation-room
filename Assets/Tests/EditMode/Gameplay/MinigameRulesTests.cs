using System.Linq;
using InterrogationRoom.Minigames;
using NUnit.Framework;

namespace InterrogationRoom.Gameplay.Tests
{
    public sealed class MinigameRulesTests
    {
        [Test]
        public void FileSearchCreatesEightToTwelveFoldersAndKeepsTargetAfterShufflePenalty()
        {
            FileSearchSession session = FileSearchSession.Create(seed: 147, folderCount: 10, targetYear: 1998);
            string targetSignature = session.TargetSignature;
            int wrongIndex = Enumerable.Range(0, session.Folders.Count)
                .First(index => session.Folders[index].Signature != targetSignature);

            Assert.That(session.Folders.Count, Is.EqualTo(10));
            Assert.That(session.Folders.Count(folder => folder.Signature == targetSignature), Is.EqualTo(1));
            Assert.That(session.Choose(wrongIndex), Is.EqualTo(MinigameAttemptResult.Incorrect));
            Assert.That(session.PenaltySeconds, Is.EqualTo(2));
            Assert.That(session.Folders.Count(folder => folder.Signature == targetSignature), Is.EqualTo(1));
            Assert.That(session.Choose(session.TargetIndex), Is.EqualTo(MinigameAttemptResult.Success));
        }

        [Test]
        public void CodeLockRestartsAfterLimitedAttemptsWithoutBecomingBlocked()
        {
            var session = new CodeLockSession(code: 417, maximumAttempts: 2);

            Assert.That(session.Enter(111), Is.EqualTo(MinigameAttemptResult.Incorrect));
            Assert.That(session.Enter(222), Is.EqualTo(MinigameAttemptResult.Restarted));
            Assert.That(session.AttemptsInCurrentRun, Is.Zero);
            Assert.That(session.Enter(417), Is.EqualTo(MinigameAttemptResult.Success));
        }

        [Test]
        public void RecordsTerminalHasExactlyOneRecordMatchingBothCriteria()
        {
            RecordsTerminalSession session = RecordsTerminalSession.Create(seed: 912, recordCount: 6);

            Assert.That(session.Records.Count, Is.EqualTo(6));
            Assert.That(session.Records.Count(record =>
                record.Surname == session.TargetSurname && record.Year == session.TargetYear), Is.EqualTo(1));
            Assert.That(session.Select(session.TargetIndex), Is.EqualTo(MinigameAttemptResult.Success));
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
    }
}
