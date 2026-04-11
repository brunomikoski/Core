namespace BrunoMikoski.Core
{
    /// <summary>
    /// Result of a save validation. Use Success to allow save, Fail to block and show a message.
    /// </summary>
    public struct SaveValidationResult
    {
        public bool CanSave { get; private set; }
        public string Message { get; private set; }

        public static SaveValidationResult Success => new()
        {
            CanSave = true,
            Message = null
        };

        public static SaveValidationResult Fail(string message) => new()
        {
            CanSave = false,
            Message = message ?? "Save was blocked."
        };
    }
}
