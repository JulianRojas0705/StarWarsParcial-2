using UnityEngine;
using Vuforia;

/// <summary>
/// TapToAttack v2 — Sin raycast.
/// Detecta si el toque en pantalla está cerca del Image Target proyectado.
/// </summary>
public class TapToAttack : MonoBehaviour
{
    [Header("Animator del personaje hijo")]
    public Animator characterAnimator;

    [Header("Nombre del trigger de ataque")]
    public string attackTrigger = "Attack";

    [Tooltip("Radio en píxeles de pantalla para detectar el toque sobre la tarjeta")]
    public float tapRadius = 150f;

    private Camera arCamera;
    private bool isTracked = false;

    void Start()
    {
        arCamera = Camera.main;

        if (arCamera == null)
        {
            var vuforia = FindObjectOfType<VuforiaBehaviour>();
            if (vuforia != null)
                arCamera = vuforia.GetComponent<Camera>();
        }

        var observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += (behaviour, status) =>
            {
                isTracked = (status.Status == Status.TRACKED ||
                             status.Status == Status.EXTENDED_TRACKED);
            };
        }
        else
        {
            Debug.LogWarning($"TapToAttack: No se encontró ObserverBehaviour en {gameObject.name}");
        }
    }

    void Update()
    {
        if (!isTracked || arCamera == null) return;

        // Dispositivo — toque
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            CheckTap(Input.GetTouch(0).position);
        }

        // Editor — mouse
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            CheckTap(Input.mousePosition);
        }
#endif
    }

    private void CheckTap(Vector2 screenPos)
    {
        // Proyectar la posición del Image Target en pantalla
        Vector3 targetScreenPos = arCamera.WorldToScreenPoint(transform.position);

        // Si el target está detrás de la cámara ignorar
        if (targetScreenPos.z < 0) return;

        // Calcular distancia en píxeles entre el toque y el centro del target
        float distance = Vector2.Distance(screenPos, new Vector2(targetScreenPos.x, targetScreenPos.y));

        Debug.Log($"{gameObject.name} — distancia toque: {distance}px | radio: {tapRadius}px");

        if (distance <= tapRadius)
        {
            TriggerAttack();
        }
    }

    private void TriggerAttack()
    {
        if (characterAnimator == null)
        {
            Debug.LogWarning($"TapToAttack: characterAnimator no asignado en {gameObject.name}");
            return;
        }

        characterAnimator.SetTrigger(attackTrigger);
        Debug.Log($"{gameObject.name} → ataque activado por toque");
    }
}