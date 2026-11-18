using FlameBlame.Feedback;
using FlameBlame.Fire;
using FlameBlame.Hose;
using FlameBlame.Player;
using FlameBlame.Water;
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

        [Header("VFX templates (inactive scene objects)")]
        [SerializeField] Transform effectsRoot;
        [SerializeField] ParticleSystem flameTemplate;
        [SerializeField] ParticleSystem steamTemplate;
        [SerializeField] ParticleSystem splashTemplate;
        [SerializeField] Transform puddleTemplate;

        [Header("Fire")]
        [SerializeField] int maxActiveFires = 16;

        GameEvents events;
        FireSystem fire;
        EffectFactory effects;
        PuddleFactory puddles;
        EffectsPresenter presenter;

        void Awake()
        {
            events = new GameEvents();
            fire = new FireSystem(events, maxActiveFires);
            effects = new EffectFactory(effectsRoot, flameTemplate, steamTemplate, splashTemplate);
            puddles = new PuddleFactory(effectsRoot, puddleTemplate);
            presenter = new EffectsPresenter(events, effects, puddles);

            hose.Initialize(input);
            hoseRenderer.Initialize(hose);
            jet.Initialize(input, events);
            playerLook.Initialize(input);
            playerMotor.Initialize(input, hose, events);
            cameraFeedback.Initialize(playerLook, events);

            foreach (var surface in FindObjectsByType<WaterSurface>(FindObjectsSortMode.None))
                surface.Initialize(events);
            foreach (var flammable in FindObjectsByType<Flammable>(FindObjectsSortMode.None))
                flammable.Initialize(fire, events);
        }

        void Update()
        {
            fire.Tick(Time.deltaTime);
            effects.Tick(Time.time);
        }

        void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
