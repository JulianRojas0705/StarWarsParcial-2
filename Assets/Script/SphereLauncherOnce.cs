using System.Collections;
using UnityEngine;

/// <summary>
/// SphereLauncherOnce — lanzamiento de esferas completamente independiente.
///

public class SphereLauncherOnce : MonoBehaviour
{
    [Header("Prefab y origen")]
    [Tooltip("Prefab de esfera del proyecto")]
    public GameObject spherePrefab;

    [Tooltip("Transform vacío hijo de Once — punto de salida de las esferas.\n" +
             "Créalo: clic derecho sobre Once en Hierarchy → Create Empty → llámalo SpawnPoint")]
    public Transform sphereSpawnPoint;

    [Header("Objetivo")]
    [Tooltip("Transform del Demogorgon (Fighter 1)")]
    public Transform demogorgonTarget;

    [Header("Parámetros de vuelo")]
    [Tooltip("Velocidad de vuelo de la esfera (m/s)")]
    public float sphereSpeed = 3.0f;

    [Tooltip("Segundos hasta que la esfera se destruye sola")]
    public float sphereLifetime = 4.0f;

    [Tooltip("Homing: la esfera persigue al Demogorgon en lugar de volar en línea recta")]
    public bool homingMode = false;

    [Header("Ráfaga")]
    [Tooltip("Cuántas esferas se lanzan por llamada a Launch()")]
    public int spheresPerShot = 1;

    [Tooltip("Intervalo en segundos entre esferas si spheresPerShot > 1")]
    public float timeBetweenSpheres = 0.15f;

    [Tooltip("Retardo inicial antes de lanzar (para sincronizar con la animación de ataque)")]
    public float launchDelay = 0.2f;

    // ─── INIT ─────────────────────────────────────────────────────────────────

    void Start()
    {
        if (spherePrefab == null)
            Debug.LogWarning("[SphereLauncher] Asigna el Sphere Prefab en el Inspector.");

        if (demogorgonTarget == null)
            Debug.LogWarning("[SphereLauncher] Asigna el Transform del Demogorgon.");

        // Auto-crear SpawnPoint si no está asignado
        if (sphereSpawnPoint == null)
        {
            GameObject sp = new GameObject("SpawnPoint_Auto");
            sp.transform.SetParent(transform);
            sp.transform.localPosition = new Vector3(0f, 0.5f, 0.2f);
            sphereSpawnPoint = sp.transform;
            Debug.Log("[SphereLauncher] SpawnPoint creado automáticamente.");
        }
    }

    // ─── API PÚBLICA ──────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia el lanzamiento de la ráfaga. Llámalo desde TouchZoneManager o cualquier otro script.
    /// </summary>
    public void Launch()
    {
        StartCoroutine(LaunchCoroutine());
    }

    // ─── LANZAMIENTO ──────────────────────────────────────────────────────────

    private IEnumerator LaunchCoroutine()
    {
        yield return new WaitForSeconds(launchDelay);

        for (int i = 0; i < spheresPerShot; i++)
        {
            LaunchOneSphere();
            if (i < spheresPerShot - 1)
                yield return new WaitForSeconds(timeBetweenSpheres);
        }
    }

    private void LaunchOneSphere()
    {
        if (spherePrefab == null || demogorgonTarget == null) return;

        Vector3 spawnPos = sphereSpawnPoint != null
            ? sphereSpawnPoint.position
            : transform.position + Vector3.up * 0.3f;

        GameObject sphere = Instantiate(spherePrefab, spawnPos, Quaternion.identity);

        if (homingMode)
        {
            HomingSphere homing = sphere.AddComponent<HomingSphere>();
            homing.Initialize(demogorgonTarget, sphereSpeed, sphereLifetime);
        }
        else
        {
            Vector3 direction = (demogorgonTarget.position - spawnPos).normalized;

            Rigidbody rb = sphere.GetComponent<Rigidbody>();
            if (rb == null) rb = sphere.AddComponent<Rigidbody>();

            rb.useGravity = false;
            rb.linearVelocity   = direction * sphereSpeed;

            sphere.transform.rotation = Quaternion.LookRotation(direction);
            Destroy(sphere, sphereLifetime);
        }

        Debug.Log($"[SphereLauncher] Esfera lanzada → Demogorgon | Origen: {spawnPos}");
    }

    // ─── GIZMOS ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (sphereSpawnPoint == null || demogorgonTarget == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(sphereSpawnPoint.position, demogorgonTarget.position);
        Gizmos.DrawWireSphere(sphereSpawnPoint.position, 0.02f);
        Gizmos.DrawWireSphere(demogorgonTarget.position, 0.03f);
    }
}

// ─── COMPONENTE AUXILIAR: HomingSphere ────────────────────────────────────────

/// <summary>
/// Se adjunta automáticamente a la esfera cuando homingMode = true.
/// Persigue suavemente al Demogorgon hasta impactar o expirar.
/// </summary>
public class HomingSphere : MonoBehaviour
{
    private Transform target;
    private float     speed;
    private float     lifetime;
    private float     timer;

    public void Initialize(Transform target, float speed, float lifetime)
    {
        this.target   = target;
        this.speed    = speed;
        this.lifetime = lifetime;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime || target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * speed * 3f);

        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
