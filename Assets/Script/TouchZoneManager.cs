using UnityEngine;
using Vuforia;

/// <summary>
/// TouchZoneManager: Divide el área del Image Target en zonas táctiles.
/// Al tocar cada zona se activa una animación o mecánica diferente en el personaje 3D.
///
/// CÓMO FUNCIONA:
/// - Convierte la posición del toque en coordenadas locales del Image Target.
/// - Determina en qué zona cayó el toque (izquierda / centro / derecha).
/// - Activa el trigger de animación correspondiente.
/// </summary>
public class TouchZoneManager : MonoBehaviour
{
    [Header("Referencia al personaje")]
    [Tooltip("Animator del personaje 3D hijo de este Image Target")]
    public Animator characterAnimator;

    [Header("Referencia al BattleManager (opcional)")]
    [Tooltip("Si está asignado, las zonas también pueden llamar ataques del BattleManager")]
    public BattleManager battleManager;

    [Header("Triggers de animación por zona")]
    [Tooltip("Trigger del Animator para la Zona Izquierda (ej: Attack, Punch)")]
    public string zoneLeftTrigger = "Attack";

    [Tooltip("Trigger del Animator para la Zona Central (ej: Special, Spin)")]
    public string zoneCenterTrigger = "Special";

    [Tooltip("Trigger del Animator para la Zona Derecha (ej: Defend, Block)")]
    public string zoneRightTrigger = "Defend";

    [Header("Configuración de zonas")]
    [Tooltip("Ancho del Image Target en unidades de mundo (revisa el Scale de tu target)")]
    public float targetWidth = 0.22f; // Coincide con el Size.X de tu BoxCollider

    [Tooltip("Alto del Image Target en unidades de mundo")]
    public float targetHeight = 0.22f; // Coincide con el Size.Z de tu BoxCollider

    [Tooltip("Activa para ver las zonas en la Scene View (Gizmos)")]
    public bool showDebugGizmos = true;

    // Camera de AR (se obtiene automáticamente)
    private Camera arCamera;

    // Collider del Image Target (usado para raycast)
    private BoxCollider targetCollider;

    void Start()
    {
        // Obtener la cámara AR
        arCamera = Camera.main;
        if (arCamera == null)
        {
            
            var vuforiaCamera = FindObjectOfType<VuforiaBehaviour>();
            if (vuforiaCamera != null)
                arCamera = vuforiaCamera.GetComponent<Camera>();
        }

        // Obtener el BoxCollider 
        targetCollider = GetComponent<BoxCollider>();
        if (targetCollider == null)
            targetCollider = GetComponentInParent<BoxCollider>();

        if (targetCollider == null)
            Debug.LogWarning("TouchZoneManager: No se encontró BoxCollider. " +
                             "Agrega un BoxCollider con Is Trigger = true al Image Target.");
    }

    void Update()
    {
        // Soporte para múltiples toques simultáneos 
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Solo procesar cuando el dedo toca la pantalla por primera vez
            if (touch.phase == TouchPhase.Began)
            {
                ProcessTouch(touch.position);
            }
        }

        // Soporte para editor (mouse click para testing)
        #if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            ProcessTouch(Input.mousePosition);
        }
        #endif
    }

    // ─── Procesamiento del toque ─────────────────────────────────────────────

    private void ProcessTouch(Vector2 screenPosition)
    {
        if (arCamera == null || targetCollider == null) return;

        // Raycast desde la pantalla hacia el mundo 3D
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Verificar que el raycast golpeó ESTE collider (o el padre)
            if (hit.collider == targetCollider)
            {
                // Convertir punto de impacto a coordenadas locales del Image Target
                Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);

                // Determinar la zona tocada y activar la animación
                TouchZone zone = DetermineZone(localHitPoint);
                ActivateZone(zone, localHitPoint);

                Debug.Log($"Toque detectado en zona: {zone} | Posición local: {localHitPoint}");
            }
        }
    }

    // ─── Determinación de zona ───────────────────────────────────────────────

    public enum TouchZone
    {
        Left,
        Center,
        Right
    }

    private TouchZone DetermineZone(Vector3 localPoint)
    {
        // Normalizar la posición X entre -0.5 y 0.5
        float normalizedX = localPoint.x / targetWidth;

        if (normalizedX < -0.166f)
            return TouchZone.Left;
        else if (normalizedX > 0.166f)
            return TouchZone.Right;
        else
            return TouchZone.Center;
    }

    private void ActivateZone(TouchZone zone, Vector3 localPoint)
    {
        if (characterAnimator == null) return;

        // Resetear triggers anteriores para evitar conflictos
        characterAnimator.ResetTrigger(zoneLeftTrigger);
        characterAnimator.ResetTrigger(zoneCenterTrigger);
        characterAnimator.ResetTrigger(zoneRightTrigger);

        switch (zone)
        {
            case TouchZone.Left:
                characterAnimator.SetTrigger(zoneLeftTrigger);
                // Opcionalmente llamar al BattleManager
                battleManager?.ManualAttackFighter1();
                Debug.Log("Zona izquierda: activando " + zoneLeftTrigger);
                break;

            case TouchZone.Center:
                characterAnimator.SetTrigger(zoneCenterTrigger);
                Debug.Log("Zona central: activando " + zoneCenterTrigger);
                break;

            case TouchZone.Right:
                characterAnimator.SetTrigger(zoneRightTrigger);
                // Opcionalmente llamar al BattleManager
                battleManager?.ManualAttackFighter2();
                Debug.Log("Zona derecha: activando " + zoneRightTrigger);
                break;
        }

        // Efecto visual de feedback (opcional — puedes instanciar un particle aquí)
        SpawnTouchFeedback(localPoint);
    }

    // ─── Feedback visual (personalizable) ────────────────────────────────────

    [Header("Feedback visual opcional")]
    [Tooltip("Prefab de partícula o efecto a instanciar al tocar (puede quedar vacío)")]
    public GameObject touchFeedbackPrefab;

    private void SpawnTouchFeedback(Vector3 localPoint)
    {
        if (touchFeedbackPrefab == null) return;

        Vector3 worldPoint = transform.TransformPoint(localPoint);
        Instantiate(touchFeedbackPrefab, worldPoint, Quaternion.identity);
    }

    // ─── Gizmos para debug en Scene View ─────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Zona izquierda (rojo)
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Vector3 leftCenter = transform.TransformPoint(new Vector3(-targetWidth * 0.33f, 0, 0));
        Gizmos.DrawCube(leftCenter, new Vector3(targetWidth * 0.33f, 0.01f, targetHeight));

        // Zona central (verde)
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.4f);
        Vector3 centerCenter = transform.TransformPoint(Vector3.zero);
        Gizmos.DrawCube(centerCenter, new Vector3(targetWidth * 0.33f, 0.01f, targetHeight));

        // Zona derecha (azul)
        Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.4f);
        Vector3 rightCenter = transform.TransformPoint(new Vector3(targetWidth * 0.33f, 0, 0));
        Gizmos.DrawCube(rightCenter, new Vector3(targetWidth * 0.33f, 0.01f, targetHeight));
    }
}
