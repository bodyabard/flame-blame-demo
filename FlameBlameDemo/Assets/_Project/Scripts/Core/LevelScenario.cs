using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Ladder;
using TMPro;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Scene configuration and adapters for the six-step scenario.
    public sealed class LevelScenario : MonoBehaviour
    {
        public Transform reel, hydrant, ladderTarget, atticTarget;
        public BoxCollider atticVolume;
        public FireZone floor1, floor2, attic;
        public TMP_Text objective, marker, interaction;
        public AudioSource supplyAudio;
        public AudioClip couplingClick;
        public GameObject backpackCoil;
        public GameTuning tuning;
        public float roundSeconds => tuning.roundSeconds;
        float interactionRange => tuning.interactionRange;
        float connectionSeconds => tuning.connectionSeconds;
        public Vector3 truckPosition=new Vector3(-11,0,-4);
        readonly string[] goals={"Візьми шланг з машини", "Постав драбину під вікно 2-го поверху", "Підключи шланг до пожежного крана", "Загаси пожежу на 1-му та 2-му поверсі", "Злети на горище на реактивній тязі", "Загаси пожежу на горищі", "ПЕРЕМОГА!"};
        IGameEvents events;
        ObjectiveSequence sequence;
        FireSystem fire;
        HoseController hose;
        Transform player;
        Camera view;
        LadderStateMachine[] ladders;
        PlayerLadderInteraction interactor;
        HoseReelPickup pickup;
        HydrantConnector connector;
        float connection=-1;
        float nextSmoke;
        readonly string[] goalLabels=new string[7], completedLabels=new string[6];
        int shown=-1, pendingGoal;
        float goalTransition;
        bool lowerCleared;
        public bool Complete => sequence!=null && sequence.Complete;
        public bool HasStarted { get; private set; }
        public PromptKind SupplyPrompt { get; private set; }
        public void Initialize(IGameEvents hub, FireSystem system, HoseController controller, Transform playerTransform, Camera camera)
        {
            events=hub; fire=system; hose=controller; player=playerTransform; view=camera;
            for(int i=0;i<7;i++) {goalLabels[i]=$"{Mathf.Min(6,i+1)} з 6\n{goals[i]}"; if(i<6) completedLabels[i]=$"✓ <s>{goals[i]}</s>";}
            sequence=new ObjectiveSequence(events); events.ObjectiveChanged+=OnObjective;
            events.FireExtinguished+=OnExtinguished;
            events.FireBurnedOut+=OnExtinguished;
            ladders=FindObjectsByType<LadderStateMachine>(FindObjectsSortMode.None);
            interactor=player.GetComponent<PlayerLadderInteraction>();
            pickup=reel.GetComponent<HoseReelPickup>(); pickup.Initialize(this);
            connector=hydrant.GetComponent<HydrantConnector>(); connector.Initialize(this);
            hose.Stow(); OnObjective(0);
            if(backpackCoil!=null) {backpackCoil.transform.SetParent(player,false); backpackCoil.transform.localPosition=new Vector3(0,1,-.3f); backpackCoil.SetActive(false);}
        }
        void OnObjective(int current)
        {
            if(objective!=null)
            {
                bool transition=shown>=0 && current>shown && current<6;
                objective.SetText(transition ? completedLabels[shown] : goalLabels[current]);
                pendingGoal=current; goalTransition=transition ? .45f : 0; shown=current;
            }
            if(current==4) attic.Activate(fire);
        }
        public void TakeHose()
        {
            if(hose.Acquired) return;
            hose.Pickup(); if(backpackCoil!=null) backpackCoil.SetActive(true);
            events.RaiseScenarioSignal(ScenarioSignal.HosePickedUp);
        }
        public void BeginConnection() {if(hose.Acquired && !hose.Connected && connection<0) connection=connectionSeconds;}
        void OnExtinguished(FireNode node)
        {
            if(hose.Connected && floor1.Cleared && floor2.Cleared && !lowerCleared)
            { lowerCleared=true; events.RaiseScenarioSignal(ScenarioSignal.LowerFiresOut); }
            if(sequence.Current==5 && attic.Cleared) events.RaiseScenarioSignal(ScenarioSignal.AtticFiresOut);
        }
        public void Tick()
        {
            if(sequence==null || Complete) return;
            if(goalTransition>0) {goalTransition-=Time.deltaTime; if(goalTransition<=0 && objective!=null) objective.SetText(goalLabels[pendingGoal]);}
            if(Time.time>=nextSmoke)
            {
                nextSmoke=Time.time+tuning.smokeInterval;
                if(!floor1.Cleared) events.RaiseSteamCreated(floor1.members[0].Node.Position+Vector3.up);
                if(!floor2.Cleared) events.RaiseSteamCreated(ladderTarget.position);
                if(sequence.Current<4 || !attic.Cleared) events.RaiseSteamCreated(atticTarget.position);
            }
            HasStarted |= Input.GetAxisRaw("Horizontal")!=0 || Input.GetAxisRaw("Vertical")!=0 || Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
            bool nearReel=(player.position-reel.position).sqrMagnitude<interactionRange*interactionRange;
            bool nearHydrant=(player.position-hydrant.position).sqrMagnitude<interactionRange*interactionRange;
            bool canPickup=!hose.Acquired && nearReel;
            bool canConnect=hose.Acquired && !hose.Connected && nearHydrant;
            if(hose.Acquired && !hose.Connected && Input.GetMouseButtonDown(0) && supplyAudio!=null && couplingClick!=null) supplyAudio.PlayOneShot(couplingClick,.25f);
            SupplyPrompt=canPickup ? PromptKind.PickHose : canConnect ? PromptKind.ConnectHose : PromptKind.None;
            if(interaction!=null) interaction.SetText(connection>=0 ? "Підключення…" : "");
            if(Input.GetKeyDown(KeyCode.F) && canPickup) pickup.Interact(interactor);
            else if(Input.GetKeyDown(KeyCode.F) && canConnect) connector.Interact(interactor);
            if(connection>=0)
            {
                if(!nearHydrant) connection=-1;
                else { connection-=Time.deltaTime; if(connection<0) {hose.Connect(); if(backpackCoil!=null) backpackCoil.SetActive(false); if(supplyAudio!=null && couplingClick!=null) supplyAudio.PlayOneShot(couplingClick,.7f); events.RaiseScenarioSignal(ScenarioSignal.HoseConnected);} }
            }
            hose.ConnectionProgress=connection>=0 ? 1-connection/connectionSeconds : -1;
            if(hose.Connected && !lowerCleared) OnExtinguished(null);
            // Place() announces before its snap completes, so inspect the settled endpoints.
            for(int i=0;i<ladders.Length;i++)
                if(ladders[i].State==LadderState.Placed && (ladders[i].Top-ladderTarget.position).sqrMagnitude<2.25f)
                    events.RaiseScenarioSignal(ScenarioSignal.LadderPlaced);
            if(sequence.Current==4 && atticVolume.bounds.Contains(player.position)) events.RaiseScenarioSignal(ScenarioSignal.AtticEntered);
            if(sequence.Current==5 && attic.Cleared) events.RaiseScenarioSignal(ScenarioSignal.AtticFiresOut);
            UpdateMarker();
        }
        void UpdateMarker()
        {
            if(marker==null) return;
            Transform target=sequence.Current==0 ? reel : sequence.Current==1 ? ladderTarget : sequence.Current==2 ? hydrant : sequence.Current>=4 ? atticTarget : floor1.transform;
            bool visible=(target.position-player.position).sqrMagnitude>=9;
            marker.gameObject.SetActive(visible);
            if(!visible) return;
            Vector3 point=view.WorldToScreenPoint(target.position);
            bool behind=point.z<0;
            if(behind) {point.x=Screen.width-point.x; point.y=Screen.height-point.y;}
            bool edge=behind || point.x<40 || point.x>Screen.width-40 || point.y<40 || point.y>Screen.height-40;
            marker.SetText(edge ? ">" : "•");
            marker.transform.rotation=Quaternion.Euler(0,0,edge ? Mathf.Atan2(point.y-Screen.height*.5f,point.x-Screen.width*.5f)*Mathf.Rad2Deg : 0);
            marker.transform.position=new Vector3(Mathf.Clamp(point.x,40,Screen.width-40),Mathf.Clamp(point.y,40,Screen.height-40),0);
        }
        void OnDestroy()
        {
            sequence?.Dispose();
            if(events!=null) {events.ObjectiveChanged-=OnObjective; events.FireExtinguished-=OnExtinguished; events.FireBurnedOut-=OnExtinguished;}
        }
    }
}
