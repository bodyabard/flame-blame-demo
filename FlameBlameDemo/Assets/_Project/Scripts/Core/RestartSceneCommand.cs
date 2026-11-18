using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlameBlame.Core
{
    public sealed class RestartSceneCommand
    {
        bool requested;
        public void Execute()
        {
            if (requested) return;
            requested = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
