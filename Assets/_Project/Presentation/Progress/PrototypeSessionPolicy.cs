namespace HorseParking.Presentation.Progress
{
    /// <summary>Temporary clean-session mode while construction and parking are redesigned.</summary>
    public static class PrototypeSessionPolicy
    {
        // Existing disk saves are retained; no repository is constructed in this mode.
        public static bool PersistenceEnabled => false;
    }
}
