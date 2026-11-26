using FlameBlame.Ladder;
using UnityEngine;
namespace FlameBlame.Core
{
    public sealed class SpecialBook : MonoBehaviour, IInteractable
    {
        LoopRevealController controller;
        Renderer surface;
        MaterialPropertyBlock block;
        public void Initialize(LoopRevealController owner) { controller=owner; surface=GetComponent<Renderer>(); block=new MaterialPropertyBlock(); }
        public void Focus(bool value)
        {
            if(surface==null) return;
            surface.GetPropertyBlock(block);
            Color emission=value ? new Color(.08f,.065f,.025f)*(1f+.2f*Mathf.Sin(Time.unscaledTime*3)) : Color.black;
            block.SetColor("_EmissionColor",emission); block.SetColor("_Emissive_Color",emission); surface.SetPropertyBlock(block);
        }
        public void Interact(PlayerLadderInteraction player) { controller.OpenDiary(); }
    }
}
