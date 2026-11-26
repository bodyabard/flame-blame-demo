using UnityEngine;
namespace FlameBlame.Core
{
    public enum LoopState { Normal, DiaryOpen, LoopRevealed, LoopBroken }
    public sealed class LoopSession : MonoBehaviour { public bool DiaryRead { get; set; } }
}
