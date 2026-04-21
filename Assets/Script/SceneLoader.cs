using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Vuforia;

/// <summary>
/// SceneLoader: Carga otra escena cuando el Image Target es detectado.

public class SceneLoader : MonoBehaviour
{
    // ─── REFERENCIAS ──────────────────────────────────────────────────────────

    [Header("Vuforia")]
    [Tooltip("El Image Target que dispara la carga de escena")]
    public ObserverBehaviour imageTarget;

    // ─── CONFIGURACIÓN DE ESCENA ──────────────────────────────────────────────

    [Header("Escena destino")]
    [Tooltip("Nombre EXACTO de la escena (debe estar en Build Settings)")]
    public string sceneToLoad = "NombreDeTuEscena";

    [Tooltip("Modo de carga de escena")]
    public LoadSceneMode loadMode = LoadSceneMode.Single;

    // ─── MODO DE ACTIVACIÓN ───────────────────────────────────────────────────

    [Header("Modo de activación")]
    [Tooltip("Segundos de espera después de detectar el target antes de cargar")]
    public float delayBeforeLoad = 1.5f;

    [Tooltip("Si está activo, el target debe mantenerse trackeado este tiempo para cargar")]
    public bool requireHoldTracking = false;

    [Tooltip("Segundos que debe mantenerse trackeado (solo si requireHoldTracking = true)")]
    public float holdDuration = 2.0f;

    // ─── UI FEEDBACK (OPCIONAL) ───────────────────────────────────────────────

    [Header("Feedback visual (opcional)")]
    [Tooltip("GameObject a activar mientras cuenta el hold (ej: barra de progreso)")]
    public GameObject loadingIndicator;

    [Tooltip("Efecto o panel a mostrar justo antes de cambiar de escena")]
    public GameObject transitionEffect;

    // ─── PRIVADOS ─────────────────────────────────────────────────────────────

    private bool      isTracked      = false;
    private bool      isLoading      = false;
    private float     holdTimer      = 0f;
    private Coroutine loadCoroutine;

    // ─── INIT ─────────────────────────────────────────────────────────────────

    void Start()
    {
        if (imageTarget != null)
            imageTarget.OnTargetStatusChanged += OnTargetStatusChanged;
        else
            Debug.LogError("[SceneLoader] Asigna el Image Target en el Inspector.");

        if (string.IsNullOrEmpty(sceneToLoad))
            Debug.LogError("[SceneLoader] Escribe el nombre de la escena en 'Scene To Load'.");

        // Ocultar indicador de carga al inicio
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    void OnDestroy()
    {
        if (imageTarget != null)
            imageTarget.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    // ─── TRACKING ─────────────────────────────────────────────────────────────

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        isTracked = (status.Status == Status.TRACKED ||
                     status.Status == Status.EXTENDED_TRACKED);

        if (isTracked)
        {
            Debug.Log($"[SceneLoader] Target detectado → cargando '{sceneToLoad}'");
            OnTargetFound();
        }
        else
        {
            Debug.Log("[SceneLoader] Target perdido.");
            OnTargetLost();
        }
    }

    private void OnTargetFound()
    {
        if (isLoading) return;

        if (requireHoldTracking)
        {
            // Modo hold: mostrar indicador y empezar a contar en Update
            holdTimer = 0f;
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);
        }
        else
        {
            // Modo simple: cargar después del delay
            loadCoroutine = StartCoroutine(LoadWithDelay());
        }
    }

    private void OnTargetLost()
    {
        if (isLoading) return;

        // Cancelar carga si se pierde el tracking antes de completar
        if (loadCoroutine != null)
        {
            StopCoroutine(loadCoroutine);
            loadCoroutine = null;
        }

        holdTimer = 0f;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    // ─── UPDATE: modo hold ────────────────────────────────────────────────────

    void Update()
    {
        if (!requireHoldTracking || isLoading || !isTracked) return;

        holdTimer += Time.deltaTime;

        // Progreso visual (si tienes un slider o barra)
        float progress = Mathf.Clamp01(holdTimer / holdDuration);
        // Puedes usar: loadingSlider.value = progress;
        Debug.Log($"[SceneLoader] Hold progress: {(int)(progress * 100)}%");

        if (holdTimer >= holdDuration)
        {
            isLoading     = true;
            loadCoroutine = StartCoroutine(LoadWithDelay());
        }
    }

    // ─── CARGA DE ESCENA ──────────────────────────────────────────────────────

    private IEnumerator LoadWithDelay()
    {
        isLoading = true;

        // Mostrar efecto de transición si existe
        if (transitionEffect != null)
            transitionEffect.SetActive(true);

        // Esperar el delay configurado
        yield return new WaitForSeconds(delayBeforeLoad);

        // Verificar que la escena existe en Build Settings
        if (!SceneExists(sceneToLoad))
        {
            Debug.LogError($"[SceneLoader] La escena '{sceneToLoad}' no existe en Build Settings.\n" +
                           "Ve a File → Build Settings y agrégala.");
            isLoading = false;
            yield break;
        }

        Debug.Log($"[SceneLoader] Cargando escena: '{sceneToLoad}'");
        SceneManager.LoadScene(sceneToLoad, loadMode);
    }

    // ─── UTILIDAD ─────────────────────────────────────────────────────────────

    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }

    /// <summary>
    /// Llamar desde un botón UI o desde otro script para forzar la carga.
    /// </summary>
    public void ForceLoadScene()
    {
        if (!isLoading)
            loadCoroutine = StartCoroutine(LoadWithDelay());
    }

    // ─── GIZMOS ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (imageTarget == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(imageTarget.transform.position, Vector3.one * 0.1f);
    }
}
