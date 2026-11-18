using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;                 // дочірня камера
    public HoseSystem hose;               // для обмеження довжини рукава

    [Header("Move")]
    public float moveSpeed = 3.5f;
    public float lookSensitivity = 2f;
    public float gravity = -20f;
    public float impulseDamping = 6f;

    [Header("Shake")]
    public float shakeMaxAngle = 3f;
    public float shakeDecay = 4f;

    CharacterController cc;
    float pitch;
    float yVel;
    Vector3 impulse;
    float shake;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Look
        float mx = Input.GetAxis("Mouse X") * lookSensitivity;
        float my = Input.GetAxis("Mouse Y") * lookSensitivity;
        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);

        // Shake (камера)
        shake = Mathf.MoveTowards(shake, 0f, shakeDecay * Time.deltaTime);
        float t = Time.time * 25f;
        float sx = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * shake * shakeMaxAngle;
        float sy = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * shake * shakeMaxAngle;
        cam.localRotation = Quaternion.Euler(pitch + sx, sy, 0f);

        // Move
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        input = Vector3.ClampMagnitude(input, 1f);
        Vector3 move = transform.TransformDirection(input) * moveSpeed;

        if (cc.isGrounded && yVel < 0f) yVel = -2f;
        yVel += gravity * Time.deltaTime;

        impulse = Vector3.Lerp(impulse, Vector3.zero, impulseDamping * Time.deltaTime);
        cc.Move((move + impulse + Vector3.up * yVel) * Time.deltaTime);

        // Прив'язка до гідранта: не відходимо далі довжини рукава
        if (hose != null && hose.Held)
        {
            Vector3 off = transform.position - hose.anchor.position;
            off.y = 0f;
            float max = hose.MaxReach;
            if (off.magnitude > max)
                cc.Move(-off.normalized * (off.magnitude - max));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// Віддача від струменя (м/с, додається до швидкості).
    public void AddRecoil(Vector3 velocityDelta) { impulse += velocityDelta; }

    public void AddShake(float amount) { shake = Mathf.Max(shake, Mathf.Clamp01(amount)); }
}
