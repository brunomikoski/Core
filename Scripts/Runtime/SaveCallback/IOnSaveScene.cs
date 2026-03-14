using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core.SaveCallback
{
    /// <summary>
    /// Implement on a MonoBehaviour in a scene to validate or block saving.
    /// Called before the scene is written to disk.
    /// </summary>
    public interface IOnSaveScene
    {
        SaveValidationResult CanSaveScene(Scene scene);
    }
}
