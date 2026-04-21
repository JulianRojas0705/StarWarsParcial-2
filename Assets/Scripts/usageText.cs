using UnityEngine;
using TMPro;  // Asegúrate de importar el paquete de TextMeshPro

public class DisplayUsage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usageText;  // Usamos TextMeshProUGUI

    private int usageCount = 0;

    private void Start()
    {
        // Verificamos que la referencia haya sido asignada
        if (usageText == null)
        {
            Debug.LogError("usageText no ha sido asignado en el Inspector!");
            return;  // Si no está asignado, salimos del método para evitar errores
        }

        // Cargar el conteo guardado desde PlayerPrefs (si existe)
        usageCount = PlayerPrefs.GetInt("usageCount", 0);
        UpdateUsageText();  // Actualizamos el texto con el valor guardado
    }

    // Método para incrementar el contador y actualizar el texto
    public void IncrementUsage()
    {
        usageCount++;
        PlayerPrefs.SetInt("usageCount", usageCount);  // Guardamos el valor en PlayerPrefs
        PlayerPrefs.Save();  // Aseguramos que se guarde el valor
        UpdateUsageText();  // Actualizamos el texto con el nuevo valor
    }

    // Método para actualizar el texto en el panel
    public void UpdateUsageText()
    {
        if (usageText != null)
        {
            usageText.text = "Usos: " + usageCount.ToString();  // Actualizamos el texto
        }
    }
}