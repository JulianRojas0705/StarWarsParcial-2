using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// StatsPanel — Agrega este script a cada Image Target.
/// Muestra el panel de stats cuando solo esta tarjeta está visible.
/// Lo oculta cuando hay otra tarjeta cerca (batalla).
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("Canvas con la imagen de stats")]
    public GameObject statsCanvas;

    // Lista estática compartida entre todos los StatsPanel
    private static List<StatsPanel> allPanels = new List<StatsPanel>();

    private bool isTracked = false;

    void Start()
    {
        allPanels.Add(this);

        if (statsCanvas != null)
            statsCanvas.SetActive(false);

        var observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += (behaviour, status) =>
            {
                isTracked = (status.Status == Status.TRACKED ||
                             status.Status == Status.EXTENDED_TRACKED);
                EvaluateAllPanels();
            };
        }
    }

    void OnDestroy()
    {
        allPanels.Remove(this);
    }

    private static void EvaluateAllPanels()
    {
        int trackedCount = 0;
        foreach (var panel in allPanels)
            if (panel.isTracked) trackedCount++;

        foreach (var panel in allPanels)
        {
            if (panel.statsCanvas == null) continue;

            // Mostrar solo si esta tarjeta es la única visible
            bool shouldShow = panel.isTracked && trackedCount == 1;
            panel.statsCanvas.SetActive(shouldShow);
        }
    }
}