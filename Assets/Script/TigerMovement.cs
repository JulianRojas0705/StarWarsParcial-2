using UnityEngine;
using Vuforia;

/// <summary>
/// TigerMovement: Mueve lateralmente al Tiger con deslizamiento táctil.
/// 
public class TigerMovement : MonoBehaviour
{
    [Header("Configuración de movimiento")]
    [Tooltip("Velocidad de movimiento lateral")]
    public float moveSpeed = 0.002f;

    [Tooltip("Límite lateral máximo (unidades locales del Image Target)")]
    public float horizontalLimit = 0.08f;

    [Tooltip("Suavizado del movimiento (0 = instantáneo, 1 = muy suave)")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.15f;

    [Header("Animación")]
    [Tooltip("Animator del Tiger (opcional — activa Walk al moverse)")]
    public Animator tigerAnimator;

    [Tooltip("Nombre del bool en el Animator para caminar")]
    public string walkBoolName = "IsWalking";

    [Header("Referencias Vuforia")]
    [Tooltip("El Image Target padre (Kellog2). Si se pierde tracking, Tiger no se mueve.")]
    public ObserverBehaviour imageTarget;

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private Vector3  targetLocalPos;
    private Vector2  touchStartPos;
    private bool     isTouching      = false;
    private bool     isTracked       = false;
    private float    currentVelocity = 0f;

    void Start()
    {
        targetLocalPos = transform.localPosition;

        if (imageTarget != null)
            imageTarget.OnTargetStatusChanged += OnTargetStatusChanged;
        else
            Debug.LogWarning("[TigerMovement] Asigna el Image Target en el Inspector.");
    }

    void OnDestroy()
    {
        if (imageTarget != null)
            imageTarget.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        isTracked = (status.Status == Status.TRACKED ||
                     status.Status == Status.EXTENDED_TRACKED);
    }

    // ─── UPDATE ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (!isTracked) return;

        HandleTouch();
        ApplyMovement();
        UpdateAnimation();
    }

    // ─── INPUT TÁCTIL ─────────────────────────────────────────────────────────

    private void HandleTouch()
    {
        // Soporte táctil
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    isTouching    = true;
                    break;

                case TouchPhase.Moved:
                    if (isTouching)
                    {
                        float deltaX = touch.deltaPosition.x * moveSpeed;
                        MoveHorizontal(deltaX);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    isTouching = false;
                    break;
            }
        }

#if UNITY_EDITOR
        // Mouse para testing en editor
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isTouching    = true;
        }
        if (Input.GetMouseButton(0) && isTouching)
        {
            float deltaX = Input.GetAxis("Mouse X") * moveSpeed * 10f;
            MoveHorizontal(deltaX);
        }
        if (Input.GetMouseButtonUp(0))
            isTouching = false;
#endif
    }

    private void MoveHorizontal(float delta)
    {
        float newX = Mathf.Clamp(
            targetLocalPos.x + delta,
            -horizontalLimit,
             horizontalLimit);

        targetLocalPos = new Vector3(newX, targetLocalPos.y, targetLocalPos.z);

        // Rotar Tiger según dirección
        if (Mathf.Abs(delta) > 0.0001f)
            transform.localRotation = Quaternion.Euler(
                0,
                delta > 0 ? 90f : -90f,
                0);
    }

    // ─── MOVIMIENTO SUAVIZADO ─────────────────────────────────────────────────

    private void ApplyMovement()
    {
        Vector3 current = transform.localPosition;
        float smoothX   = Mathf.SmoothDamp(
            current.x,
            targetLocalPos.x,
            ref currentVelocity,
            smoothing);

        transform.localPosition = new Vector3(smoothX, current.y, current.z);
    }

    // ─── ANIMACIÓN ────────────────────────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (tigerAnimator == null) return;

        bool isMoving = Mathf.Abs(currentVelocity) > 0.001f;
        tigerAnimator.SetBool(walkBoolName, isMoving);
    }

    // ─── GIZMOS ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 left  = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(-horizontalLimit, 0, 0))
            : transform.position + Vector3.left * horizontalLimit;
        Vector3 right = transform.parent != null
            ? transform.parent.TransformPoint(new Vector3(horizontalLimit, 0, 0))
            : transform.position + Vector3.right * horizontalLimit;

        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left,  0.01f);
        Gizmos.DrawWireSphere(right, 0.01f);
    }
}
