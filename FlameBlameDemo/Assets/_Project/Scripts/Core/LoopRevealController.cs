using System;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Feedback;
using FlameBlame.UI;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.UI;
namespace FlameBlame.Core
{
    public sealed class LoopRevealController : MonoBehaviour
    {
        public SpecialBook book;
        public GameObject facade, openHouse;
        public GameObject[] oldRoofAndWindows;
        public EndingFlower flower;
        public Volume volume;
        public LoopEndingTuning tuning;
        public DiaryView diary;
        public AudioSource ambience, paperAudio;
        public LoopState State { get; private set; }
        public bool Revealed => session!=null && session.DiaryRead;
        public bool BookFocused { get; private set; }
        public bool Paused => State==LoopState.DiaryOpen || State==LoopState.LoopBroken;
        IGameEvents events;
        LoopSession session;
        FireSystem fire;
        LevelScenario scenario;
        HoseController hose;
        HoseRenderer rope;
        GameAudio audio;
        HudRoot hud;
        Camera cameraView;
        Behaviour[] controls;
        bool[] enabledBefore;
        JetController jet;
        readonly UITweenRunner tween=new UITweenRunner(16);
        Action<float> setWeight;
        float savedScale, closeAt=-1, endingAt=-1, revealAt=-1;
        CursorLockMode savedLock;
        bool savedCursor, closing, browserExit;
        ShutterView shutter;
        RectTransform exitButton;
        CanvasGroup black;
        TMP_Text endLabel;
        public void Initialize(IGameEvents hub,LoopSession state,FireSystem system,LevelScenario level,HoseController controller,HoseRenderer renderer,GameAudio gameAudio,HudRoot root,Camera view,Behaviour[] inputControls,JetController water)
        {
            events=hub; session=state; fire=system; scenario=level; hose=controller; rope=renderer; audio=gameAudio; hud=root; cameraView=view; controls=inputControls; jet=water;
            enabledBefore=new bool[controls.Length]; setWeight=v=>volume.weight=v;
            book.Initialize(this); diary.Initialize(tuning,CloseDiary); events.LoopBroken+=BreakLoop;
            if(session.DiaryRead) Reveal(false);
        }
        public void Tick()
        {
            tween.Tick(Time.unscaledDeltaTime); diary.Tick(Time.unscaledDeltaTime);
            if(browserExit) return;
            if(closeAt>=0 && Time.unscaledTime>=closeAt)
            {
                closeAt=-1; diary.Hide(); RestoreControls();
                if(!session.DiaryRead) { session.DiaryRead=true; events.RaiseDiaryRead(); Reveal(true); }
                State=LoopState.LoopRevealed; closing=false;
            }
            if(State==LoopState.DiaryOpen)
            {
                if(!closing && (Input.GetKeyDown(KeyCode.F)||Input.GetKeyDown(KeyCode.Escape))) CloseDiary();
                return;
            }
            if(State==LoopState.LoopBroken)
            {
                if(endingAt>=0 && Time.unscaledTime>=endingAt) { endingAt=-1; ShowEnding(); }
                return;
            }
            bool focus=Physics.Raycast(cameraView.transform.position,cameraView.transform.forward,out var hit,tuning.readRange,~(1<<cameraView.transform.root.gameObject.layer),QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<SpecialBook>()==book;
            BookFocused=focus; book.Focus(focus);
            if(focus && Input.GetKeyDown(KeyCode.F)) book.Interact(null);
            if(Revealed && flower.smokeVisual==null && flower.flammable.Node!=null && Time.unscaledTime>=revealAt)
            { revealAt=Time.unscaledTime+1.2f; events.RaiseSteamCreated(flower.transform.position+Vector3.up*4); }
        }
        void PauseControls()
        {
            savedScale=Time.timeScale; savedLock=Cursor.lockState; savedCursor=Cursor.visible;
            for(int i=0;i<controls.Length;i++) if(controls[i]!=null) { enabledBefore[i]=controls[i].enabled; controls[i].enabled=false; }
            jet.StopFlow(); Time.timeScale=0; Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
        void RestoreControls()
        {
            for(int i=0;i<controls.Length;i++) if(controls[i]!=null) controls[i].enabled=enabledBefore[i];
            Time.timeScale=savedScale; Cursor.lockState=savedLock; Cursor.visible=savedCursor;
        }
        public void OpenDiary()
        {
            if(Paused) return;
            State=LoopState.DiaryOpen; PauseControls(); events.RaiseDiaryOpened(); diary.Show();
            if(paperAudio!=null) paperAudio.Play();
        }
        public void CloseDiary()
        {
            if(State!=LoopState.DiaryOpen || closing) return;
            closing=true;
            if(!session.DiaryRead) { session.DiaryRead=true; events.RaiseDiaryRead(); Reveal(true); State=LoopState.DiaryOpen; }
            diary.Close(); closeAt=Time.unscaledTime+tuning.closeSeconds;
        }
        void Reveal(bool animate)
        {
            State=LoopState.LoopRevealed;
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if(data!=null && data.renderType==UnityEngine.Rendering.Universal.CameraRenderType.Overlay) data.renderPostProcessing=true;
            }
            facade.SetActive(false); foreach(var part in oldRoofAndWindows) part.SetActive(false); openHouse.SetActive(true);
            flower.Initialize(fire,events); scenario.RevealLoop(); hud.HideLoopTimer();
            audio.RevealLoop(tuning.musicFadeSeconds); ambience.Play();
            // Reach includes height needed to clear the walls, not only horizontal distance.
            if(Vector3.Distance(hose.AnchorPosition,flower.transform.position+Vector3.up*6)>hose.MaxReach)
            { hose.DetachForLoop(); rope.HideForLoop(); if(paperAudio!=null) paperAudio.Play(); }
            volume.weight=animate ? 0 : 1;
            if(animate) tween.Value(volume,0,setWeight,0,1,tuning.revealSeconds,Ease.InOutSine);
        }
        void BreakLoop()
        {
            if(State==LoopState.LoopBroken) return;
            State=LoopState.LoopBroken; BookFocused=false; book.Focus(false); PauseControls();
            ambience.Stop(); audio.Finish(); endingAt=Time.unscaledTime+tuning.silenceSeconds;
            if(tuning.restoreColorOnEnding) tween.Value(volume,0,setWeight,volume.weight,0,tuning.revealSeconds,Ease.InOutSine);
        }
        void ShowEnding()
        {
            shutter=hud.EndingShutter;
            foreach(var group in shutter.hudGroups) group.alpha=0;
            shutter.root.SetActive(true); shutter.plate.localScale=Vector3.one;
            shutter.plateLabel.SetText("Петлю розірвано.");
            var slats=shutter.slats; float h=shutter.curtain.rect.height;
            slats.sizeDelta=new Vector2(slats.sizeDelta.x,h+80); slats.anchoredPosition=new Vector2(0,40);
            shutter.bottomBar.anchoredPosition=new Vector2(0,-h+60); shutter.hazard.anchoredPosition=new Vector2(0,-h+20);
            shutter.plate.anchoredPosition=new Vector2(0,-h*.45f);
            var curtain=shutter.curtain;
            tween.Move(curtain,new Vector2(0,h+80),Vector2.zero,tuning.shutterSeconds,Ease.InOutSine);
            if(shutter.rattle!=null) shutter.audioSource.PlayOneShot(shutter.rattle,.12f);
            exitButton=DiaryView.MakeButton(shutter.plate,"Нарешті",diary.buttonSprite,diary.body.font,Exit);
            exitButton.anchoredPosition=new Vector2(0,-125);
            tween.Scale(exitButton,Vector3.zero,Vector3.one,.3f,Ease.OutCubic,tuning.shutterSeconds);
        }
        void Exit() { if(!browserExit) new PlatformGameExit().Exit(BrowserEnding); }
        public void BrowserEnding()
        {
            diary.GetComponent<Canvas>().sortingOrder=32760;
            browserExit=true; exitButton.gameObject.SetActive(false);
            var go=new GameObject("End",typeof(RectTransform),typeof(CanvasGroup),typeof(Image));
            var rect=(RectTransform)go.transform; rect.SetParent(diary.transform,false); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            go.GetComponent<Image>().color=Color.black; black=go.GetComponent<CanvasGroup>(); black.alpha=0;
            tween.Fade(black,0,1,tuning.exitFadeSeconds,Ease.InOutSine); AudioListener.volume=0; Time.timeScale=0;
            endLabel=DiaryView.MakeText(rect,"Кінець.",diary.body.font,22); endLabel.color=new Color(.5f,.5f,.5f); endLabel.alignment=TextAlignmentOptions.Center;
            diary.gameObject.SetActive(true);
        }
        void OnDestroy() { if(events!=null) events.LoopBroken-=BreakLoop; AudioListener.volume=1; }
    }
}
