using System;
using System.Linq;
using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public struct PrivateObjectiveMessage
    {
        public string Id;
        public PrivateObjectiveKind Kind;
        public bool HasCurrentStep;
        public string CurrentStepId;
        public int CompletedStepCount;
        public int TotalStepCount;
        public bool IsCompleted;
        public bool HasTarget;
        public int TargetPlayerId;

        public static PrivateObjectiveMessage FromView(PrivateObjectiveView view) =>
            new PrivateObjectiveMessage
            {
                Id = view.Id.Value,
                Kind = view.Kind,
                HasCurrentStep = view.CurrentStep.HasValue,
                CurrentStepId = view.CurrentStep?.Value,
                CompletedStepCount = view.CompletedStepCount,
                TotalStepCount = view.TotalStepCount,
                IsCompleted = view.IsCompleted,
                HasTarget = view.Target.HasValue,
                TargetPlayerId = view.Target?.Value ?? 0
            };

        public PrivateObjectiveView ToView() => new PrivateObjectiveView(
            new PrivateObjectiveId(Id),
            Kind,
            HasCurrentStep ? new PrivateObjectiveStepId(CurrentStepId) : (PrivateObjectiveStepId?)null,
            CompletedStepCount,
            TotalStepCount,
            IsCompleted,
            HasTarget ? new PlayerId(TargetPlayerId) : (PlayerId?)null);
    }

    public struct IncidentRegistryEntryMessage
    {
        public string Id;
        public IncidentKind Kind;
        public string EffectId;
        public string LocationId;
        public long ReportedAtMilliseconds;

        public static IncidentRegistryEntryMessage FromView(IncidentRegistryEntryView view) =>
            new IncidentRegistryEntryMessage
            {
                Id = view.Id.Value,
                Kind = view.Kind,
                EffectId = view.Effect.Value,
                LocationId = view.Location.Value,
                ReportedAtMilliseconds = view.ReportedAt.MillisecondsSinceRoundStart
            };

        public IncidentRegistryEntryView ToView() => new IncidentRegistryEntryView(
            new IncidentId(Id),
            Kind,
            new IncidentEffectId(EffectId),
            new IncidentLocationId(LocationId),
            new IncidentTimestamp(ReportedAtMilliseconds));
    }

    public struct IncidentRevealMessage
    {
        public string Id;
        public IncidentKind Kind;
        public string EffectId;
        public string LocationId;
        public int AuthorPlayerId;

        public static IncidentRevealMessage FromView(IncidentRevealView view) =>
            new IncidentRevealMessage
            {
                Id = view.Id.Value,
                Kind = view.Kind,
                EffectId = view.Effect.Value,
                LocationId = view.Location.Value,
                AuthorPlayerId = view.Author.Value
            };

        public IncidentRevealView ToView() => new IncidentRevealView(
            new IncidentId(Id),
            Kind,
            new IncidentEffectId(EffectId),
            new IncidentLocationId(LocationId),
            new PlayerId(AuthorPlayerId));
    }

    public struct AlibiClueMessage
    {
        public string Id;
        public string Content;

        public static AlibiClueMessage FromView(AlibiClueView view) =>
            new AlibiClueMessage { Id = view.Id.Value, Content = view.Content };

        public AlibiClueView ToView() => new AlibiClueView(new AlibiClueId(Id), Content);
    }

    public struct EscapeExitOptionMessage
    {
        public string Id;
        public string PreparationStepId;
        public string LocationId;
        public bool IsPrepared;

        public static EscapeExitOptionMessage FromView(EscapeExitOptionView view) =>
            new EscapeExitOptionMessage
            {
                Id = view.Id.Value,
                PreparationStepId = view.PreparationStepId.Value,
                LocationId = view.Location.Value,
                IsPrepared = view.IsPrepared
            };

        public EscapeExitOptionView ToView() => new EscapeExitOptionView(
            new EscapeExitId(Id),
            new EscapeStepId(PreparationStepId),
            new IncidentLocationId(LocationId),
            IsPrepared);
    }

    public struct EscapePlanMessage
    {
        public string Id;
        public bool HasCurrentStep;
        public string CurrentStepId;
        public int CompletedCommonStepCount;
        public int TotalCommonStepCount;
        public bool IsPrepared;
        public bool HasActiveExit;
        public string ActiveExitId;
        public EscapeExitOptionMessage[] ExitOptions;

        public static EscapePlanMessage FromView(EscapePlanView view) =>
            new EscapePlanMessage
            {
                Id = view.Id.Value,
                HasCurrentStep = view.CurrentStep.HasValue,
                CurrentStepId = view.CurrentStep?.Value,
                CompletedCommonStepCount = view.CompletedCommonStepCount,
                TotalCommonStepCount = view.TotalCommonStepCount,
                IsPrepared = view.IsPrepared,
                HasActiveExit = view.ActiveExit.HasValue,
                ActiveExitId = view.ActiveExit?.Value,
                ExitOptions = view.ExitOptions.Select(EscapeExitOptionMessage.FromView).ToArray()
            };

        public EscapePlanView ToView() => new EscapePlanView(
            new EscapePlanId(Id),
            HasCurrentStep ? new EscapeStepId(CurrentStepId) : (EscapeStepId?)null,
            CompletedCommonStepCount,
            TotalCommonStepCount,
            IsPrepared,
            HasActiveExit ? new EscapeExitId(ActiveExitId) : (EscapeExitId?)null,
            (ExitOptions ?? Array.Empty<EscapeExitOptionMessage>()).Select(value => value.ToView()).ToArray());
    }

    public struct PlayerResultMessage
    {
        public bool Won;
        public bool Survived;
        public bool DetectiveWon;
        public RoundEndCause EndCause;
        public bool HasExecutedPlayer;
        public int ExecutedPlayerId;
        public bool PrivateObjectiveCompleted;
        public bool Escaped;

        public static PlayerResultMessage FromView(PlayerResultView view) =>
            new PlayerResultMessage
            {
                Won = view.Won,
                Survived = view.Survived,
                DetectiveWon = view.DetectiveWon,
                EndCause = view.EndCause,
                HasExecutedPlayer = view.ExecutedPlayer.HasValue,
                ExecutedPlayerId = view.ExecutedPlayer?.Value ?? 0,
                PrivateObjectiveCompleted = view.PrivateObjectiveCompleted,
                Escaped = view.Escaped
            };

        public PlayerResultView ToView() => new PlayerResultView(
            Won,
            Survived,
            DetectiveWon,
            EndCause,
            HasExecutedPlayer ? new PlayerId(ExecutedPlayerId) : (PlayerId?)null,
            PrivateObjectiveCompleted,
            Escaped);
    }

    public struct PlayerEndRevealMessage
    {
        public int PlayerId;
        public RoundRole Role;
        public bool HasPrivateObjective;
        public PrivateObjectiveMessage PrivateObjective;
        public PlayerResultMessage Result;

        public static PlayerEndRevealMessage FromView(PlayerEndRevealView view) =>
            new PlayerEndRevealMessage
            {
                PlayerId = view.Player.Value,
                Role = view.Role,
                HasPrivateObjective = view.PrivateObjective != null,
                PrivateObjective = view.PrivateObjective == null
                    ? default
                    : PrivateObjectiveMessage.FromView(view.PrivateObjective),
                Result = PlayerResultMessage.FromView(view.Result)
            };

        public PlayerEndRevealView ToView() => new PlayerEndRevealView(
            new PlayerId(PlayerId),
            Role,
            HasPrivateObjective ? PrivateObjective.ToView() : null,
            Result.ToView());
    }

    public struct AlibiClueRevealMessage
    {
        public string Id;
        public string LinkedFactId;
        public string Content;

        public static AlibiClueRevealMessage FromView(AlibiClueRevealView view) =>
            new AlibiClueRevealMessage
            {
                Id = view.Id.Value,
                LinkedFactId = view.LinkedFactId,
                Content = view.Content
            };

        public AlibiClueRevealView ToView() => new AlibiClueRevealView(
            new AlibiClueId(Id),
            LinkedFactId,
            Content);
    }

    public struct EscapeActionRevealMessage
    {
        public EscapeActionKind Kind;
        public bool HasStep;
        public string StepId;
        public bool HasExit;
        public string ExitId;

        public static EscapeActionRevealMessage FromView(EscapeActionRevealView view) =>
            new EscapeActionRevealMessage
            {
                Kind = view.Kind,
                HasStep = view.StepId.HasValue,
                StepId = view.StepId?.Value,
                HasExit = view.ExitId.HasValue,
                ExitId = view.ExitId?.Value
            };

        public EscapeActionRevealView ToView() => new EscapeActionRevealView(
            Kind,
            HasStep ? new EscapeStepId(StepId) : (EscapeStepId?)null,
            HasExit ? new EscapeExitId(ExitId) : (EscapeExitId?)null);
    }

    public struct EscapePlanRevealMessage
    {
        public string Id;
        public EscapeActionRevealMessage[] Actions;
        public bool HasSuccessfulExit;
        public string SuccessfulExitId;

        public static EscapePlanRevealMessage FromView(EscapePlanRevealView view) =>
            new EscapePlanRevealMessage
            {
                Id = view.Id.Value,
                Actions = view.Actions.Select(EscapeActionRevealMessage.FromView).ToArray(),
                HasSuccessfulExit = view.SuccessfulExit.HasValue,
                SuccessfulExitId = view.SuccessfulExit?.Value
            };

        public EscapePlanRevealView ToView() => new EscapePlanRevealView(
            new EscapePlanId(Id),
            (Actions ?? Array.Empty<EscapeActionRevealMessage>()).Select(value => value.ToView()).ToArray(),
            HasSuccessfulExit ? new EscapeExitId(SuccessfulExitId) : (EscapeExitId?)null);
    }

    public struct RoundRevealMessage
    {
        public PlayerEndRevealMessage[] Players;
        public AlibiClueRevealMessage[] AcquiredAlibiClues;
        public EscapePlanRevealMessage EscapePlan;
        public IncidentRevealMessage[] Incidents;

        public static RoundRevealMessage FromView(RoundRevealView view) =>
            new RoundRevealMessage
            {
                Players = view.Players.Select(PlayerEndRevealMessage.FromView).ToArray(),
                AcquiredAlibiClues = view.AcquiredAlibiClues.Select(AlibiClueRevealMessage.FromView).ToArray(),
                EscapePlan = EscapePlanRevealMessage.FromView(view.EscapePlan),
                Incidents = view.Incidents.Select(IncidentRevealMessage.FromView).ToArray()
            };

        public RoundRevealView ToView() => new RoundRevealView(
            (Players ?? Array.Empty<PlayerEndRevealMessage>()).Select(value => value.ToView()).ToArray(),
            (AcquiredAlibiClues ?? Array.Empty<AlibiClueRevealMessage>()).Select(value => value.ToView()).ToArray(),
            EscapePlan.ToView(),
            (Incidents ?? Array.Empty<IncidentRevealMessage>()).Select(value => value.ToView()).ToArray());
    }
}
