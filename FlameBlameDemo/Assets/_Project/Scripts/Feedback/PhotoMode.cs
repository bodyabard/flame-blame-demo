using System.Collections;
using System.IO;
using FlameBlame.Hose;
using FlameBlame.Player;
using UnityEngine;

namespace FlameBlame.Feedback
{
    /// F12: photo mode (time frozen, free slow camera, HUD hidden, save a frame in a chosen format).
    /// F11: hide / show the whole HUD at any time (for recording).
    /// Keys in photo mode: WASD move, Space/C up/down, Shift fast, mouse look, wheel FOV, 1-3 format, H HUD, , . bookmarks, Enter save.
    public sealed class PhotoMode : MonoBehaviour
    {
        [SerializeField] ShowcaseShots shots;
        [SerializeField] Camera gameCamera;
        [SerializeField] Transform hudRoot;
        [SerializeField] float moveSpeed = 1.6f;
        [SerializeField] float fastMultiplier = 5f;
        [SerializeField] float lookSensitivity = 1.4f;

        PlayerLook look;
        PlayerMotor motor;
        CameraFeedback feedback;
        HoseController hose;
        JetController jet;
        FirstPersonHands hands;
        Canvas[] canvases;

        CharacterController playerController;
        Vector3 savedPlayerPosition;
        Transform cameraTransform, savedParent;
        Vector3 savedLocalPosition;
        Quaternion savedLocalRotation;
        float savedTimeScale = 1f, savedFov;
        CursorLockMode savedCursor;
        bool savedCursorVisible, hudVisible = true, photoHud, capturing;
        int formatIndex, shotIndex = -1;
        float yaw, pitch;
        string status = "";

        public bool Active { get; private set; }
        public bool Busy => capturing;
        public Camera Camera => gameCamera;

        public void Initialize(PlayerLook playerLook, PlayerMotor playerMotor, CameraFeedback cameraFeedback,
            HoseController hoseController, JetController jetController, FirstPersonHands firstPersonHands)
        {
            look = playerLook; motor = playerMotor; feedback = cameraFeedback;
            hose = hoseController; jet = jetController; hands = firstPersonHands;
            cameraTransform = gameCamera.transform;
            playerController = playerMotor.GetComponent<CharacterController>();
            canvases = hudRoot != null ? hudRoot.GetComponentsInChildren<Canvas>(true) : new Canvas[0];
        }

        // ------------------------------------------------------------ public API (also used by the editor capture tool)

        public void SetHud(bool visible)
        {
            hudVisible = visible;
            for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = visible;
        }

        public void Enter(bool freezeTime)
        {
            if (Active) return;
            Active = true;
            savedTimeScale = Time.timeScale;
            savedCursor = Cursor.lockState; savedCursorVisible = Cursor.visible;
            savedParent = cameraTransform.parent;
            savedLocalPosition = cameraTransform.localPosition; savedLocalRotation = cameraTransform.localRotation;
            savedFov = gameCamera.fieldOfView;
            cameraTransform.SetParent(null, true);
            savedPlayerPosition = motor.transform.position;
            playerController.enabled = false;      // the player body follows the camera so distance-based effect culling keeps working
            var e = cameraTransform.rotation.eulerAngles;
            yaw = e.y; pitch = e.x > 180f ? e.x - 360f : e.x;

            look.enabled = false; motor.enabled = false; feedback.enabled = false;
            if (freezeTime) { hose.enabled = false; jet.enabled = false; Time.timeScale = 0f; }
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            photoHud = false;
            SetHud(false);
            status = "PHOTO MODE";
        }

        public void Exit()
        {
            if (!Active) return;
            Active = false;
            cameraTransform.SetParent(savedParent, false);
            cameraTransform.localPosition = savedLocalPosition; cameraTransform.localRotation = savedLocalRotation;
            gameCamera.fieldOfView = savedFov;
            motor.transform.position = savedPlayerPosition;
            playerController.enabled = true;
            look.enabled = true; motor.enabled = true; feedback.enabled = true;
            hose.enabled = true; jet.enabled = true;
            Time.timeScale = savedTimeScale;
            Cursor.lockState = savedCursor; Cursor.visible = savedCursorVisible;
            SetHud(true);
        }

        public void Place(Vector3 position, Vector3 euler, float fov)
        {
            cameraTransform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            yaw = euler.y; pitch = euler.x > 180f ? euler.x - 360f : euler.x;
            FollowPlayer();
            gameCamera.fieldOfView = fov;
        }

        void FollowPlayer()
        {
            if (Active) motor.transform.position = cameraTransform.position - Vector3.up * 1.6f;
        }

        public void GoTo(int index)
        {
            if (shots == null || shots.shots.Count == 0) return;
            shotIndex = (index % shots.shots.Count + shots.shots.Count) % shots.shots.Count;
            var s = shots.shots[shotIndex];
            Place(s.position, s.eulerAngles, s.fieldOfView);
            status = "Bookmark " + (shotIndex + 1) + ": " + s.name;
        }

        /// Renders the camera into a texture of the requested size (with or without HUD) and writes a PNG.
        public void Capture(string path, int width, int height, bool withHud)
        {
            if (!capturing) StartCoroutine(CaptureRoutine(path, width, height, withHud));
        }

        IEnumerator CaptureRoutine(string path, int width, int height, bool withHud)
        {
            capturing = true;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
            var modes = new RenderMode[canvases.Length];
            var enabledFlags = new bool[canvases.Length];
            var cameras = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; enabledFlags[i] = canvases[i].enabled; cameras[i] = canvases[i].worldCamera;
                canvases[i].enabled = withHud;
                if (withHud) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = gameCamera; canvases[i].planeDistance = 1f; }
            }
            gameCamera.targetTexture = target;
            yield return null;
            yield return new WaitForEndOfFrame();

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false);
            RenderTexture.active = previous;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());

            gameCamera.targetTexture = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; canvases[i].enabled = enabledFlags[i];
            }
            Destroy(image); target.Release(); Destroy(target);
            status = "Saved " + Path.GetFileName(path);
            capturing = false;
        }

        // ------------------------------------------------------------ input

        static string OutputDirectory => Application.isEditor
            ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures", "Showcase")
            : Path.Combine(Application.persistentDataPath, "Showcase");

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) SetHud(!hudVisible);
            if (Input.GetKeyDown(KeyCode.F12))
            {
                if (Active) Exit(); else Enter(true);
            }
            if (!Active || capturing) return;

            float dt = Time.unscaledDeltaTime;
            yaw += Input.GetAxis("Mouse X") * lookSensitivity * 2f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * lookSensitivity * 2f, -89f, 89f);
            cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
            Vector3 move = cameraTransform.right * Input.GetAxisRaw("Horizontal") + cameraTransform.forward * Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.C)) move += Vector3.down;
            cameraTransform.position += move * speed * dt;
            FollowPlayer();
            gameCamera.fieldOfView = Mathf.Clamp(gameCamera.fieldOfView - Input.mouseScrollDelta.y * 2f, 20f, 100f);

            if (shots != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) formatIndex = 0;
                if (Input.GetKeyDown(KeyCode.Alpha2)) formatIndex = Mathf.Min(1, shots.formats.Count - 1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) formatIndex = Mathf.Min(2, shots.formats.Count - 1);
                if (Input.GetKeyDown(KeyCode.Comma)) GoTo(shotIndex - 1);
                if (Input.GetKeyDown(KeyCode.Period)) GoTo(shotIndex + 1);
            }
            if (Input.GetKeyDown(KeyCode.H)) { photoHud = !photoHud; SetHud(photoHud); }
            if (Input.GetKeyDown(KeyCode.Return) && shots != null && shots.formats.Count > 0)
            {
                var f = shots.formats[Mathf.Clamp(formatIndex, 0, shots.formats.Count - 1)];
                string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                Capture(Path.Combine(OutputDirectory, "photo-" + stamp + "-" + f.name + ".png"), f.width, f.height, photoHud);
            }
        }

        void OnGUI()
        {
            if (!Active || capturing) return;
            string format = shots != null && shots.formats.Count > 0 ? shots.formats[Mathf.Clamp(formatIndex, 0, shots.formats.Count - 1)].name : "-";
            GUI.Label(new Rect(12, 12, 700, 24), status + "   |   format " + format + (photoHud ? "   |   HUD on" : "   |   HUD off")
                + "   |   Enter save, 1-3 format, H HUD, , . bookmarks, F12 exit");
        }
    }
}
