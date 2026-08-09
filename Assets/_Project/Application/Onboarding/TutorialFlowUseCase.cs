#nullable enable

using System;

namespace HorseParking.Application.Onboarding
{
    /// <summary>
    /// Ordered first-minutes flow. It contains no Unity input or UI knowledge and can
    /// therefore be saved, restored and presented by any adapter.
    /// </summary>
    public enum TutorialStep
    {
        Controls = 0,
        CollectParkingPayment = 1,
        OpenExitGate = 2,
        SendCartToStore = 3,
        PurchaseMaterials = 4,
        ReturnCartToWarehouse = 5,
        UnloadCart = 6,
        StartConstruction = 7,
        HitConstructionWorker = 8,
        WaitForConstruction = 9,
        OpenEconomy = 10,
        Completed = 11
    }

    public sealed class TutorialFlowUseCase
    {
        public TutorialStep CurrentStep { get; private set; } = TutorialStep.Controls;

        public event Action<TutorialStep>? StepChanged;

        public bool TryAdvance(TutorialStep expectedStep)
        {
            if (CurrentStep != expectedStep || CurrentStep == TutorialStep.Completed)
                return false;

            CurrentStep++;
            StepChanged?.Invoke(CurrentStep);
            return true;
        }

        public bool CanRestore(TutorialStep step) => Enum.IsDefined(typeof(TutorialStep), step);

        public bool TryRestore(TutorialStep step)
        {
            if (!CanRestore(step)) return false;
            CurrentStep = step;
            StepChanged?.Invoke(CurrentStep);
            return true;
        }
    }
}
