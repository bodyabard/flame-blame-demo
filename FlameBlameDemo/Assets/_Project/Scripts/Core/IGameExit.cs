using UnityEngine;
namespace FlameBlame.Core
{
    public interface IGameExit { void Exit(System.Action browserEnding); }
    public sealed class PlatformGameExit : IGameExit
    {
        public void Exit(System.Action browserEnding)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#elif UNITY_WEBGL
            browserEnding();
#else
            Application.Quit();
#endif
        }
    }
}
