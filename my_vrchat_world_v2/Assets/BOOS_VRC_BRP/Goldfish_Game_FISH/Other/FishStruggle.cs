using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class FishStruggle : UdonSharpBehaviour
{
    private Animator animator;

    [Header("アニメーション速度設定")]
    [Tooltip("通常時のアニメーション速度")]
    [SerializeField] private float minAnimSpeed = 1.0f;
    [Tooltip("跳ねた瞬間（最大時）のアニメーション速度")]
    [SerializeField] private float maxAnimSpeed = 4.0f;

    [Header("跳ねる方向と強さ (Impulse)")]
    [Tooltip("指定した方向に一瞬で加わる力。Z軸に0.05など。")]
    [SerializeField] private Vector3 jumpImpulse = new Vector3(0f, 0f, 0.05f);

    [Header("物理パラメーター")]
    [Tooltip("戻る力の強さ（推奨: 100-300）")]
    [SerializeField] private float springStiffness = 250.0f;
    [Tooltip("空気抵抗（推奨: 10-20）")]
    [SerializeField] private float damping = 15.0f;

    [Header("跳ねる間隔")]
    [SerializeField] private float interval = 0.5f;

    private Vector3 initialLocalPosition;
    private Vector3 currentLocalPosition;
    private Vector3 velocity;
    private float timer;

    void Start()
    {
        animator = GetComponent<Animator>();
        initialLocalPosition = transform.localPosition;
        currentLocalPosition = initialLocalPosition;
        timer = interval;
    }

    void Update()
    {
        if (animator == null) return;

        float dt = Time.deltaTime;

        timer -= dt;
        if (timer <= 0)
        {
            velocity += jumpImpulse;
            animator.speed = maxAnimSpeed;
            timer = interval;
        }

        Vector3 force = (initialLocalPosition - currentLocalPosition) * springStiffness;
        velocity += force * dt;

        velocity *= Mathf.Clamp01(1.0f - damping * dt);

        currentLocalPosition += velocity * dt;
        transform.localPosition = currentLocalPosition;

        if (animator.speed > minAnimSpeed)
        {
            animator.speed = Mathf.Lerp(animator.speed, minAnimSpeed, dt * 10.0f);
        }
    }

    private void OnDisable()
    {
        if (animator != null) animator.speed = 1.0f;
        transform.localPosition = initialLocalPosition;
        velocity = Vector3.zero;
        currentLocalPosition = initialLocalPosition;
    }
}