using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// CerealRain: Cuando el Image Target es detectado, hace caer prefabs desde arriba en posiciones
/// aleatorias. Primero caen lento, luego rápido. Al colisionar con Tiger,
/// desaparecen con un efecto.
public class CerealRain : MonoBehaviour
{
    

    [Header("Vuforia")]
    [Tooltip("El Image Target Kellog2")]
    public ObserverBehaviour imageTarget;

    [Header("Prefabs de cereal")]
    [Tooltip("Prefab1:")]
    public GameObject chocolateBoxPrefab;

    [Tooltip("Prefab2:")]
    public GameObject kellogsCerealPrefab;

    [Header("Tiger")]
    [Tooltip("Transform del Tiger")]
    public Transform tigerTransform;

    [Tooltip("Radio de colisión con Tiger")]
    public float collisionRadius = 0.05f;

    //
    [Header("Configuración de lluvia")]
    [Tooltip("Área horizontal donde aparecen los cereales (relativa al Image Target)")]
    public float spawnAreaWidth  = 0.15f;

    [Tooltip("Altura desde la que caen los cereales")]
    public float spawnHeight = 0.7f;

    [Tooltip("Tiempo entre cada spawn ")]
    public float spawnInterval = 1.2f;

    [Tooltip("Máximo de cereales en pantalla a la vez")]
    public int maxCerealsOnScreen = 10;

    [Header("Velocidades de caída")]
    [Tooltip("Velocidad inicial (lenta)")]
    public float slowFallSpeed = 0.02f;

    [Tooltip("Velocidad final (rápida)")]
    public float fastFallSpeed = 2.0f;

    [Tooltip("Porcentaje del recorrido que cae lento (0-1)")]
    [Range(0.1f, 0.9f)]
    public float slowPhaseRatio = 0.35f;

    [Header("Efecto al desaparecer Animacion")]
    [Tooltip("Prefab de efecto al colisionar con Tiger")]
    public GameObject destroyEffectPrefab;

    [Tooltip("Escala del efecto de destrucción")]
    public float destroyEffectScale = 0.05f;

    // ─── PRIVADOS

    private bool isTracked = false;
    private bool isRaining = false;
    private List<CerealInstance> activeCereals = new List<CerealInstance>();
    private Coroutine spawnCoroutine;

    // Clase interna para trackear cada cereal activo
    private class CerealInstance
    {
        public GameObject obj;
        public float      startY;
        public float      targetY;
        public float      progress;  
        public float      randomSpeed;
        public bool       isDead;
    }

    // ─── INIT ─────────────────────────────────────────────────────────────────

    void Start()
    {
        if (imageTarget != null)
            imageTarget.OnTargetStatusChanged += OnTargetStatusChanged;
        else
            Debug.LogWarning("[CerealRain] Asigna el Image Target en el Inspector.");

        if (chocolateBoxPrefab == null || kellogsCerealPrefab == null)
            Debug.LogWarning("[CerealRain] Asigna ambos prefabs de cereal.");
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

        if (isTracked && !isRaining)
            StartRain();
        else if (!isTracked && isRaining)
            StopRain();
    }

    // ─── LLUVIA

    private void StartRain()
    {
        isRaining      = true;
        spawnCoroutine = StartCoroutine(SpawnLoop());
        Debug.Log("[CerealRain] ¡Lluvia de cereales iniciada!");
    }

    private void StopRain()
    {
        isRaining = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        // Destruir cereales activos
        foreach (var c in activeCereals)
            if (c.obj != null) Destroy(c.obj);
        activeCereals.Clear();
        Debug.Log("[CerealRain] Lluvia detenida.");
    }

    // ─── SPAWN LOOP ───────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        // Pequeña espera inicial para que el target se estabilice
        yield return new WaitForSeconds(0.5f);

        while (isRaining)
        {
            if (activeCereals.Count < maxCerealsOnScreen)
                SpawnCereal();

            // Intervalo aleatorio entre spawns para que se vea natural
            float waitTime = spawnInterval + Random.Range(-0.3f, 0.5f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SpawnCereal()
    {
        if (imageTarget == null) return;

        // Elegir prefab aleatoriamente entre los dos tipos
        GameObject prefab = Random.value > 0.5f
            ? chocolateBoxPrefab
            : kellogsCerealPrefab;

        if (prefab == null) return;

        // Posición aleatoria en el área del Image Target
        float randomX = Random.Range(-spawnAreaWidth, spawnAreaWidth);
        float randomZ = Random.Range(-spawnAreaWidth * 0.5f, spawnAreaWidth * 0.5f);

        // Posición en espacio mundo relativa al Image Target
        Transform targetTf = imageTarget.transform;
        Vector3 spawnWorldPos = targetTf.TransformPoint(
            new Vector3(randomX, spawnHeight, randomZ));

        Vector3 landWorldPos = targetTf.TransformPoint(
            new Vector3(randomX, 0.02f, randomZ));

        // Rotación aleatoria para variedad visual
        Quaternion randomRot = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f));

        GameObject cereal = Instantiate(prefab, spawnWorldPos, randomRot);

        // Escala pequeña para AR (ajusta según tu prefab)
        cereal.transform.localScale = Vector3.one * Random.Range(0.03f, 0.05f);

        // Deshabilitar física propia — nosotros controlamos la caída
        Rigidbody rb = cereal.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity  = false;
            rb.isKinematic = true;
        }

        // Registrar instancia
        var instance = new CerealInstance
        {
            obj         = cereal,
            startY      = spawnWorldPos.y,
            targetY     = landWorldPos.y,
            progress    = 0f,
            randomSpeed = Random.Range(0.8f, 1.3f), // variación de velocidad por cereal
            isDead      = false
        };

        activeCereals.Add(instance);
    }

    // ─── UPDATE: mover y detectar colisiones 

    void Update()
    {
        if (!isTracked || !isRaining) return;

        List<CerealInstance> toRemove = new List<CerealInstance>();

        foreach (var cereal in activeCereals)
        {
            if (cereal.isDead || cereal.obj == null)
            {
                toRemove.Add(cereal);
                continue;
            }

            // ── Movimiento de caída ──────────────────────────────────────────
            float speed;
            if (cereal.progress < slowPhaseRatio)
            {
                // Fase lenta
                speed = slowFallSpeed * cereal.randomSpeed;
            }
            else
            {
                // Fase rápida — acelera progresivamente
                float t = (cereal.progress - slowPhaseRatio) / (1f - slowPhaseRatio);
                speed = Mathf.Lerp(slowFallSpeed, fastFallSpeed, t) * cereal.randomSpeed;
            }

            cereal.progress += speed * Time.deltaTime;
            cereal.progress  = Mathf.Clamp01(cereal.progress);

            // Curva de caída: EaseIn para efecto más dramático
            float easedProgress = cereal.progress * cereal.progress;
            float newY = Mathf.Lerp(cereal.startY, cereal.targetY, easedProgress);

            cereal.obj.transform.position = new Vector3(
                cereal.obj.transform.position.x,
                newY,
                cereal.obj.transform.position.z);

            // Rotación durante la caída (efecto tumbling)
            cereal.obj.transform.Rotate(
                Vector3.forward * 60f * Time.deltaTime * cereal.randomSpeed);

            // ── Colisión con Personaje ───────────────────────────────────────────
            if (tigerTransform != null)
            {
                float dist = Vector3.Distance(
                    cereal.obj.transform.position,
                    tigerTransform.position);

                if (dist < collisionRadius)
                {
                    DestroyCereal(cereal);
                    toRemove.Add(cereal);
                    continue;
                }
            }

            // ── Llegó al suelo sin colisionar ────────────────────────────────
            if (cereal.progress >= 1f)
            {
                // Desaparecer suavemente en el suelo
                StartCoroutine(FadeAndDestroy(cereal));
                toRemove.Add(cereal);
            }
        }

        // Limpiar lista
        foreach (var dead in toRemove)
            activeCereals.Remove(dead);
    }

    // ─── DESTRUCCIÓN ──────────────────────────────────────────────────────────

    private void DestroyCereal(CerealInstance cereal)
    {
        if (cereal.isDead) return;
        cereal.isDead = true;

        // Efecto de destrucción
        if (destroyEffectPrefab != null && cereal.obj != null)
        {
            GameObject fx = Instantiate(
                destroyEffectPrefab,
                cereal.obj.transform.position,
                Quaternion.identity);
            fx.transform.localScale = Vector3.one * destroyEffectScale;
            Destroy(fx, 2f);
        }

        if (cereal.obj != null)
            Destroy(cereal.obj);

        Debug.Log("[CerealRain] ¡Cereal destruido por Tiger!");
    }

    private IEnumerator FadeAndDestroy(CerealInstance cereal)
    {
        if (cereal.isDead || cereal.obj == null) yield break;
        cereal.isDead = true;

        // Escalar a cero suavemente
        float duration = 0.3f;
        float timer    = 0f;
        Vector3 originalScale = cereal.obj.transform.localScale;

        while (timer < duration && cereal.obj != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            cereal.obj.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        if (cereal.obj != null)
            Destroy(cereal.obj);
    }

    // ─── GIZMOS ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (imageTarget == null) return;

        // Área de spawn
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Vector3 center = imageTarget.transform.TransformPoint(
            new Vector3(0, spawnHeight * 0.5f, 0));
        Gizmos.DrawCube(center,
            imageTarget.transform.TransformVector(
                new Vector3(spawnAreaWidth * 2f, spawnHeight, spawnAreaWidth)));

        // Radio de colisión del Tiger
        if (tigerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tigerTransform.position, collisionRadius);
        }
    }
}
