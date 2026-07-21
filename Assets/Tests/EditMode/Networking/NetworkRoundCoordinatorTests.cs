using System.Collections.Generic;
using InterrogationRoom.Content;
using InterrogationRoom.Domain;
using NUnit.Framework;
using UnityEngine;

namespace InterrogationRoom.Networking.Tests
{
    public sealed class NetworkRoundCoordinatorTests
    {
        [TestCase(RoundPhase.Round, false, 10d, 10d, true)]
        [TestCase(RoundPhase.Round, false, 9d, 10d, false)]
        [TestCase(RoundPhase.Round, true, 1000d, 10d, false)]
        [TestCase(RoundPhase.Preparation, false, 1000d, 10d, false)]
        public void ShouldExpireRound_SkipsAutomaticTimeoutForDeveloperScenario(
            RoundPhase phase,
            bool developerRoundUnlimited,
            double now,
            double deadline,
            bool expected)
        {
            Assert.That(
                NetworkRoundCoordinator.ShouldExpireRound(
                    phase,
                    developerRoundUnlimited,
                    now,
                    deadline),
                Is.EqualTo(expected));
        }

        [TestCase(5, 400d)]
        [TestCase(10, 700d)]
        [TestCase(15, 1000d)]
        [TestCase(20, 1300d)]
        public void CalculateRoundDeadline_UsesHostSelectedSharedLimit(
            int minutes,
            double expectedDeadline)
        {
            Assert.That(
                NetworkRoundCoordinator.CalculateRoundDeadline(100d, minutes),
                Is.EqualTo(expectedDeadline));
        }

        [Test]
        public void PreparationLimit_UsesApprovedThirtySecondMaximum()
        {
            Assert.That(NetworkRoundCoordinator.PreparationLimitSeconds, Is.EqualTo(30f));
            Assert.That(NetworkRoundCoordinator.AllReadyPreparationSeconds, Is.EqualTo(3f));
        }

        [TestCase(RoundPhase.Preparation, false, 130d, 130d, true)]
        [TestCase(RoundPhase.Preparation, false, 129.9d, 130d, false)]
        [TestCase(RoundPhase.Preparation, true, 1000d, 130d, false)]
        [TestCase(RoundPhase.Preparation, false, 1000d, 0d, false)]
        [TestCase(RoundPhase.Round, false, 1000d, 130d, false)]
        [TestCase(RoundPhase.Lobby, false, 1000d, 130d, false)]
        public void ShouldEndPreparation_ExpiresOnlyRealPreparationDeadlines(
            RoundPhase phase,
            bool developerRoundUnlimited,
            double now,
            double deadline,
            bool expected)
        {
            Assert.That(
                NetworkRoundCoordinator.ShouldEndPreparation(
                    phase,
                    developerRoundUnlimited,
                    now,
                    deadline),
                Is.EqualTo(expected));
        }

        [Test]
        public void ShortenedPreparationDeadline_CutsToThreeSecondsWhenAllReady()
        {
            Assert.That(
                NetworkRoundCoordinator.ShortenedPreparationDeadline(
                    deadline: 130d,
                    now: 110d,
                    NetworkRoundCoordinator.AllReadyPreparationSeconds),
                Is.EqualTo(113d));
        }

        [Test]
        public void ShortenedPreparationDeadline_NeverExtendsAShorterRemainder()
        {
            Assert.That(
                NetworkRoundCoordinator.ShortenedPreparationDeadline(
                    deadline: 111d,
                    now: 110d,
                    NetworkRoundCoordinator.AllReadyPreparationSeconds),
                Is.EqualTo(111d));
        }

        [Test]
        public void ShortenedPreparationDeadline_KeepsDeveloperPreparationUnlimited()
        {
            Assert.That(
                NetworkRoundCoordinator.ShortenedPreparationDeadline(
                    deadline: 0d,
                    now: 110d,
                    NetworkRoundCoordinator.AllReadyPreparationSeconds),
                Is.Zero);
        }

        [Test]
        public void SelectRandomValidCaseIndex_DrawsOnlyAmongValidCases()
        {
            var cases = new List<CaseAsset>
            {
                InvalidCase(),
                ValidCase("Sprawa A"),
                InvalidCase(),
                ValidCase("Sprawa B")
            };
            try
            {
                var drawn = new HashSet<int>();
                for (var pick = 0; pick < 2; pick++)
                {
                    var index = NetworkRoundCoordinator.SelectRandomValidCaseIndex(
                        cases, validCount => pick % validCount);
                    Assert.That(index, Is.EqualTo(pick == 0 ? 1 : 3));
                    Assert.That(cases[index].Validate(), Is.Empty);
                    drawn.Add(index);
                }

                Assert.That(drawn, Is.EquivalentTo(new[] { 1, 3 }),
                    "Both valid cases stay drawable while invalid ones are never picked.");
            }
            finally
            {
                foreach (var caseAsset in cases)
                    Object.DestroyImmediate(caseAsset);
            }
        }

        [Test]
        public void SelectRandomValidCaseIndex_ReturnsMinusOneWithoutAnyValidCase()
        {
            var invalid = InvalidCase();
            try
            {
                Assert.That(
                    NetworkRoundCoordinator.SelectRandomValidCaseIndex(
                        new List<CaseAsset> { null, invalid },
                        count => 0),
                    Is.EqualTo(-1));
            }
            finally
            {
                Object.DestroyImmediate(invalid);
            }
        }

        private static CaseAsset ValidCase(string title)
        {
            var caseAsset = ScriptableObject.CreateInstance<CaseAsset>();
            caseAsset.title = title;
            caseAsset.crimeDescription = "Ktoś pomalował ratuszowy zegar na różowo.";
            caseAsset.minHiddenFacts = 1;
            caseAsset.maxHiddenFacts = 1;
            caseAsset.alibiFacts = new List<CaseAsset.AuthoredFact>
            {
                new CaseAsset.AuthoredFact
                {
                    id = "f1",
                    text = "O 18:00 grupa spotkała się przy fontannie.",
                    canBeHidden = false
                },
                new CaseAsset.AuthoredFact
                {
                    id = "f2",
                    text = "Kelner wylał zupę na obrus.",
                    canBeHidden = true
                },
                new CaseAsset.AuthoredFact { id = "f3", text = "Orkiestra zagrała pierwszy walc.", canBeHidden = false },
                new CaseAsset.AuthoredFact { id = "f4", text = "Ktoś zgubił klucze pod stołem.", canBeHidden = true },
                new CaseAsset.AuthoredFact { id = "f5", text = "Grupa wróciła tramwajem numer 12.", canBeHidden = false },
                new CaseAsset.AuthoredFact
                {
                    id = "f6",
                    text = "Na przystanku padał deszcz.",
                    canBeHidden = false,
                    distinctiveDetail = true,
                    variantTexts = new List<string>
                    {
                        "Na przystanku padał deszcz.",
                        "Na przystanku padała drobna mżawka."
                    }
                }
            };
            return caseAsset;
        }

        private static CaseAsset InvalidCase()
        {
            var caseAsset = ScriptableObject.CreateInstance<CaseAsset>();
            caseAsset.title = "Zepsuta Sprawa";
            caseAsset.crimeDescription = string.Empty;
            return caseAsset;
        }
    }
}
