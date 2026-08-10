namespace BrunoMikoski.Core.DevelopmentTools
{
    /// <summary>
    /// Implement on a MonoBehaviour in a prefab to validate or block saving.
    /// Called before the prefab is written to disk.
    /// </summary>
    public interface IOnSavePrefab
    {
        SaveValidationResult CanSavePrefab(string prefabPath);
    }
}
