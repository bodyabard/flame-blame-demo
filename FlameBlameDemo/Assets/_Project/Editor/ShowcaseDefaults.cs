using FlameBlame.Feedback;
using UnityEditor;
using UnityEngine;

namespace FlameBlame.EditorTools
{
    /// Creates Settings/ShowcaseShots.asset with the five LinkedIn bookmarks (editable afterwards, never overwritten).
    public static class ShowcaseDefaults
    {
        public const string Path = "Assets/_Project/Settings/ShowcaseShots.asset";

        public static ShowcaseShots LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ShowcaseShots>(Path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<ShowcaseShots>();
            asset.shots.AddRange(new[]
            {
                new ShowcaseShot
                {
                    name = "01_Hero", position = new Vector3(-14f, 5.2f, -13f), eulerAngles = new Vector3(4f, 35f, 0), fieldOfView = 60f,
                    setup = ShotSetup.PlaceLadder | ShotSetup.IgniteLivingRoom | ShotSetup.AtticFire, settleSeconds = 7f,
                    notes = "Street, fire engine, long house, smoke from the windows, ladder under the window."
                },
                new ShowcaseShot
                {
                    name = "02_FirstPerson_Room", position = new Vector3(-0.4f, 4.8f, 3.45f), eulerAngles = new Vector3(9f, -22f, 0), fieldOfView = 70f,
                    setup = ShotSetup.ConnectHose | ShotSetup.IgniteLivingRoom | ShotSetup.Spray, settleSeconds = 4f,
                    notes = "Hands with the nozzle, jet into the fire, puddles, panicking neighbours."
                },
                new ShowcaseShot
                {
                    name = "03_Thrust_Hatch", position = new Vector3(-0.2f, 5.0f, 3.2f), eulerAngles = new Vector3(-52f, 0f, 0), fieldOfView = 72f,
                    setup = ShotSetup.ConnectHose | ShotSetup.IgniteLivingRoom | ShotSetup.Thrust, settleSeconds = 1.2f,
                    notes = "Flying up through the ceiling hatch; broken ladder hangs from the rim."
                },
                new ShowcaseShot
                {
                    name = "04_Attic", position = new Vector3(1.6f, 7.7f, 4.9f), eulerAngles = new Vector3(9.5f, 215f, 0), fieldOfView = 66f,
                    setup = ShotSetup.ConnectHose | ShotSetup.AtticFire | ShotSetup.Spray, settleSeconds = 5f,
                    notes = "Pillar, the watering-can neighbour, burning boxes, player's jet."
                },
                new ShowcaseShot
                {
                    name = "05_Chaos", position = new Vector3(-1.6f, 4.9f, 4.0f), eulerAngles = new Vector3(8f, 22f, 0), fieldOfView = 72f,
                    setup = ShotSetup.ConnectHose | ShotSetup.IgniteLivingRoom | ShotSetup.ThrowHose | ShotSetup.KickProps | ShotSetup.Shock, settleSeconds = 1.6f,
                    notes = "Dropped nozzle whipping, props flying, shock sparks."
                },
            });
            AssetDatabase.CreateAsset(asset, Path);
            return asset;
        }
    }
}
