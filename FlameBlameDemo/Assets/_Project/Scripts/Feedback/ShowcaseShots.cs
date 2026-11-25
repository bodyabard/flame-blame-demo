using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// What the Capture Showcase tool prepares in the level before taking a shot.
    [Flags]
    public enum ShotSetup
    {
        None = 0,
        ConnectHose = 1,        // hose taken and connected to the fire cabinet
        AtticFire = 2,          // attic fire switched on as well
        Spray = 4,              // valve open
        Thrust = 8,             // jet mode + valve open
        ThrowHose = 16,         // hose dropped while water flows (whip)
        Shock = 32,             // electric shock effects at the player's feet
        KickProps = 64,         // impulse on loose props near the camera
        PlaceLadder = 128,      // ladder leaning under the window
        IgniteLivingRoom = 256, // every burnable object in the second floor is already on fire
    }

    [Serializable]
    public sealed class ShowcaseShot
    {
        public string name = "Shot";
        public Vector3 position;
        public Vector3 eulerAngles;
        [Range(20f, 100f)] public float fieldOfView = 62f;
        public ShotSetup setup;
        [Tooltip("Seconds the simulation runs after setup before the frame is taken (0 = frozen).")]
        [Min(0f)] public float settleSeconds = 1.5f;
        [TextArea] public string notes;
    }

    [Serializable]
    public struct ShowcaseFormat
    {
        public string name;
        public int width, height;
        public ShowcaseFormat(string name, int width, int height) { this.name = name; this.width = width; this.height = height; }
    }

    /// Saved camera bookmarks for LinkedIn / trailer frames. Used by PhotoMode (keys) and the Capture Showcase tool.
    [CreateAssetMenu(menuName = "FlameBlame/Showcase Shots", fileName = "ShowcaseShots")]
    public sealed class ShowcaseShots : ScriptableObject
    {
        public List<ShowcaseShot> shots = new List<ShowcaseShot>();
        public List<ShowcaseFormat> formats = new List<ShowcaseFormat>
        {
            new ShowcaseFormat("1200x627", 1200, 627),
            new ShowcaseFormat("1080x1350", 1080, 1350),
            new ShowcaseFormat("1920x1080", 1920, 1080),
        };
    }
}
