using System;
using FlameBlame.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FlameBlame.UI
{
    public sealed class DiaryView : MonoBehaviour
    {
        public CanvasGroup dim, sheetGroup;
        public RectTransform sheet;
        public TMP_Text body;
        public Sprite buttonSprite;
        public TextAsset content;
        LoopEndingTuning tuning;
        readonly UITweenRunner tween=new UITweenRunner(12);
        public void Initialize(LoopEndingTuning settings,Action close)
        {
            tuning=settings; body.SetText(content.text);
            var button=MakeButton(sheet,"Закрити",buttonSprite,body.font,close); button.anchoredPosition=new Vector2(0,-272);
            gameObject.SetActive(false);
        }
        public void Tick(float dt) { tween.Tick(dt); }
        public void Show()
        {
            gameObject.SetActive(true); sheetGroup.alpha=1;
            tween.Move(sheet,new Vector2(0,-800),Vector2.zero,tuning.openSeconds,Ease.OutBack);
            tween.Rotate(sheet,-8,-2.5f,tuning.openSeconds,Ease.OutBack);
            tween.Fade(dim,0,1,.25f,Ease.OutCubic);
        }
        public void Close()
        {
            tween.Move(sheet,sheet.anchoredPosition,new Vector2(0,-800),tuning.closeSeconds,Ease.InCubic);
            tween.Fade(dim,dim.alpha,0,tuning.closeSeconds,Ease.Linear);
        }
        public void Hide() { gameObject.SetActive(false); }
        public static TMP_Text MakeText(Transform parent,string text,TMP_FontAsset font,float size)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            var label=go.GetComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=size; label.text=text; label.raycastTarget=false; return label;
        }
        public static RectTransform MakeButton(Transform parent,string label,Sprite sprite,TMP_FontAsset font,Action action)
        {
            var go=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.sizeDelta=new Vector2(210,48);
            var image=go.GetComponent<Image>(); image.sprite=sprite; image.type=Image.Type.Sliced; image.color=new Color(.65f,.68f,.7f);
            var button=go.GetComponent<Button>(); button.targetGraphic=image; button.onClick.AddListener(()=>action());
            var text=MakeText(rect,label,font,28); text.color=new Color(.1f,.12f,.14f); text.alignment=TextAlignmentOptions.Center;
            return rect;
        }
    }
}
