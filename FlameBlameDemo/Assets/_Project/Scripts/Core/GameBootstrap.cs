using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Player;
using FlameBlame.Water;
using FlameBlame.Electric;
using FlameBlame.Npc;
using FlameBlame.UI;
using UnityEngine;

namespace FlameBlame.Core
{
    /// Composition root: the only place that builds services and wires dependencies. No singletons.
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Scene components")]
        [SerializeField] LegacyInput input;
        [SerializeField] HoseController hose;
        [SerializeField] HoseRenderer hoseRenderer;
        [SerializeField] JetController jet;
        [SerializeField] PlayerLook playerLook;
        [SerializeField] PlayerMotor playerMotor;
        [SerializeField] CameraFeedback cameraFeedback;
        [SerializeField] GameAudio gameAudio;
        [SerializeField] FirstPersonHands hands;
        [SerializeField] WindowEntryGate entryGate;

        [Header("VFX templates (inactive scene objects)")]
        [SerializeField] Transform effectsRoot;
        [SerializeField] ParticleSystem flameTemplate;
        [SerializeField] ParticleSystem steamTemplate;
        [SerializeField] ParticleSystem splashTemplate;
        [SerializeField] Transform puddleTemplate;

        [Header("Fire")]
        [SerializeField] int maxActiveFires = 16;
        [SerializeField] ChaosSettings chaosSettings;
        [SerializeField] HudRoot hud;
        [SerializeField] PhotoMode photo;
        [SerializeField] ParticleSystem sparkTemplate;

        [SerializeField] LoopRevealController loop;
        LoopSession session;
        GameEvents events;
        FireSystem fire;
        EffectFactory effects;
        PuddleFactory puddles;
        EffectsPresenter presenter;
        AdaptiveEffectBudget visualBudget;
        HudContextPublisher hudContext;
        FlameBlame.Ladder.PlayerLadderInteraction ladderInteraction;
        PlayerLocomotionStateMachine locomotion;
        LevelScenario scenario;
        int lastSecond = -1;
        bool roundStarted;
        ElectricitySystem electricity;
        DamageLedger ledger;
        GameStateMachine game;
        PlayerShockReceiver playerShock;
        HoseImpactSystem hoseImpacts;
        NpcActor[] npcs;
        SparkCable[] cables;
        Flammable[] flammables;

        void Awake()
        {
            Time.timeScale = 1f;
            events = new GameEvents();
            if(gameAudio!=null) gameAudio.Initialize(events);
            ledger = new DamageLedger(events);
            scenario=FindFirstObjectByType<LevelScenario>();
            game = new GameStateMachine(scenario!=null ? scenario.roundSeconds : chaosSettings.roundSeconds, scenario!=null ? int.MaxValue : chaosSettings.damageLimit,
                chaosSettings.minimumRoundSeconds, chaosSettings.firesOutDelay);
            game.Changed += OnPhase;
            electricity = new ElectricitySystem();
            fire = new FireSystem(events, Mathf.Clamp(maxActiveFires, 1, Mathf.Clamp(chaosSettings.flamePoolSize, 1, 16)));
            effects = new EffectFactory(effectsRoot, flameTemplate, steamTemplate, splashTemplate, sparkTemplate,
                chaosSettings.flamePoolSize, chaosSettings.steamPoolSize, chaosSettings.splashPoolSize, chaosSettings.sparkPoolSize);
            puddles = new PuddleFactory(effectsRoot, puddleTemplate, electricity, chaosSettings.maxPuddles, chaosSettings.evaporationSeconds);
            presenter = new EffectsPresenter(events, effects, puddles);
            var jetFeedback=hose.GetComponent<JetFeedback>();
            if(jetFeedback!=null) jetFeedback.Initialize(events,effectsRoot);
            visualBudget = new AdaptiveEffectBudget(chaosSettings, effects, jet, playerLook.transform);
            hud.Initialize(events, ledger, game, playerLook.GetComponentInChildren<Camera>(), Mathf.RoundToInt(scenario!=null ? scenario.roundSeconds : chaosSettings.roundSeconds));

            hose.Initialize(input);
            hoseRenderer.Initialize(hose);
            jet.Initialize(input, events);
            playerLook.Initialize(input);
            if(hands!=null) hands.Initialize(input);
            playerMotor.Initialize(input, hose, events);
            cameraFeedback.Initialize(playerLook, events);
            var ladders=FindObjectsByType<FlameBlame.Ladder.LadderStateMachine>(FindObjectsSortMode.None);
            if(ladders.Length>0)
            {
                foreach(var ladder in ladders) ladder.Initialize(events);
                var interaction=ladderInteraction=playerMotor.gameObject.AddComponent<FlameBlame.Ladder.PlayerLadderInteraction>();
                locomotion=playerMotor.gameObject.AddComponent<PlayerLocomotionStateMachine>();
                interaction.view=playerLook.GetComponentInChildren<Camera>().transform;
                interaction.hose=hose; interaction.jet=jet; interaction.motor=playerMotor; interaction.locomotion=locomotion;
                interaction.Initialize(); locomotion.interaction=interaction; locomotion.view=interaction.view; locomotion.feedback=cameraFeedback;
                locomotion.Initialize(ladders); playerMotor.Locomotion=locomotion;
                locomotion.PlayerFellOffLadder += OnFellOffLadder;
            }
            hudContext = new HudContextPublisher(events, hose, ladderInteraction, locomotion);
            playerShock = new PlayerShockReceiver(playerMotor.transform, events, chaosSettings);
            hoseImpacts = new HoseImpactSystem(hose.Points, chaosSettings);
            npcs = FindObjectsByType<NpcActor>(FindObjectsSortMode.None);
            cables = FindObjectsByType<SparkCable>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++) npcs[i].Initialize(events);
            for (int i = 0; i < cables.Length; i++) electricity.Register(cables[i]);
            foreach (var property in FindObjectsByType<PropertyDamage>(FindObjectsSortMode.None)) property.Initialize(events);

            foreach (var surface in FindObjectsByType<WaterSurface>(FindObjectsSortMode.None))
                surface.Initialize(events);
            flammables = FindObjectsByType<Flammable>(FindObjectsSortMode.None);
            foreach (var flammable in flammables)
                flammable.Initialize(fire, events);
            if(scenario!=null) scenario.Initialize(events,fire,hose,playerMotor.transform,playerLook.GetComponentInChildren<Camera>());
            foreach(var can in FindObjectsByType<WateringCanNpc>(FindObjectsSortMode.None))
                can.Initialize(playerMotor.transform, scenario != null ? scenario.atticVolume : null);
            if(loop!=null)
            {
                session=FindFirstObjectByType<LoopSession>();
                if(session==null) { session=new GameObject("Loop Session").AddComponent<LoopSession>(); DontDestroyOnLoad(session.gameObject); }
                loop.Initialize(events,session,fire,scenario,hose,hoseRenderer,gameAudio,hud,playerLook.GetComponentInChildren<Camera>(),new Behaviour[]{input,playerLook,playerMotor,hose,jet,hands,photo},jet);
            }
            if(photo!=null) photo.Initialize(playerLook, playerMotor, cameraFeedback, hose, jet, hands);
        }

        void Update()
        {
            if (game.Phase != GamePhase.Playing) return;
            if(loop!=null) { loop.Tick(); hudContext.DiaryFocused=loop.BookFocused; if(loop.Paused) return; }
            visualBudget.Tick(Time.unscaledDeltaTime);
            for (int i = 0; i < flammables.Length; i++) flammables[i].SyncPosition();
            if(scenario==null || scenario.HasStarted || loop!=null && loop.Revealed) fire.Tick(Time.deltaTime);
            effects.Tick(Time.time);
            puddles.Tick(Time.time);
            electricity.Tick();
            for (int i = 0; i < cables.Length; i++) cables[i].Tick(Time.time, effects);
            playerShock.Tick(electricity);
            presenter.Tick();
            hudContext.Tick();
            scenario?.Tick();
            bool entered = scenario!=null ? scenario.HasStarted : entryGate == null || entryGate.Entered;
            if (entered && !roundStarted && (loop==null || !loop.Revealed)) { roundStarted = true; events.RaiseRoundStarted(Mathf.RoundToInt(scenario!=null ? scenario.roundSeconds : chaosSettings.roundSeconds)); }
            if (entered && (loop==null || !loop.Revealed)) game.Tick(Time.deltaTime, scenario!=null ? 1 : fire.ActiveCount, ledger.Total);
            if(scenario!=null && scenario.Complete) game.Finish(RoundEnding.FiresOut);
            int second = Mathf.CeilToInt(game.Remaining);
            if (entered && (loop==null || !loop.Revealed) && second != lastSecond) { lastSecond = second; events.RaiseTimerTick(second); }
        }

        void FixedUpdate()
        {
            if (game == null || game.Phase != GamePhase.Playing) return;
            for (int i = 0; i < npcs.Length; i++) npcs[i].Tick(Time.fixedDeltaTime, fire, electricity, chaosSettings);
            if(!hose.LoopDetached) hoseImpacts.Tick(hose.Points, Time.fixedDeltaTime,hose.Segments,hose.ReelChanged);
        }

        void OnFellOffLadder() { events.RaiseHardImpact(0.6f); }

        void OnPhase(GamePhase phase)
        {
            if (phase != GamePhase.Result) return;
            events.RaiseRoundEnded(game.Ending);
            ledger.Closed = true;
            if(gameAudio!=null) gameAudio.Finish();
            input.enabled = false;
            hose.enabled = false;
            jet.enabled = false;
            jet.StopFlow();
            playerLook.enabled = false;
            playerMotor.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
        }

        void OnDestroy()
        {
            presenter?.Dispose();
            hudContext?.Dispose();
            if (locomotion != null) locomotion.PlayerFellOffLadder -= OnFellOffLadder;
            ledger?.Dispose();
            if (game != null) game.Changed -= OnPhase;
            Time.timeScale = 1f;
        }
    }
}
