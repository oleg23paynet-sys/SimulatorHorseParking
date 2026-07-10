using HorseParking.Core.Interaction;
using HorseParking.Core.Localization;

namespace HorseParking.Application.Interaction
{

/// <summary>
/// Application boundary for player interaction. Presentation calls this use case instead of calling domain targets directly.
/// </summary>
public sealed class InteractWithTargetUseCase
{
    public InteractionResult Execute(IInteractionTarget target)
    {
        if (target.Availability != InteractionAvailability.Available)
        {
            return InteractionResult.Failure(new LocalizationKey("interaction.unavailable"));
        }

        return target.Interact();
    }
}
}
