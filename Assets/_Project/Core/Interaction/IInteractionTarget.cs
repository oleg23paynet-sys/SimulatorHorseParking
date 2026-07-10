namespace HorseParking.Core.Interaction
{

/// <summary>
/// Domain contract for anything the player can interact with.
/// Unity colliders and UI stay outside this interface.
/// </summary>
public interface IInteractionTarget
{
    string Id { get; }

    InteractionAvailability Availability { get; }

    InteractionPrompt Prompt { get; }

    InteractionResult Interact();
}
}
