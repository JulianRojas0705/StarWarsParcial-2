using UnityEngine;
using System.IO;

public class JsonManager : MonoBehaviour
{
    // Ruta para guardar el archivo JSON
    private string filePath;

    // Clase que representa los datos del conteo de clics
    [System.Serializable]
    public class UsageData
    {
        public int usageCount;
    }

    public UsageData usageData;

    void Start()
    {
        // Ruta donde se guarda el archivo JSON
        filePath = Application.persistentDataPath + "/usageData.json";
        Debug.Log("Ruta donde se guarda el archivo JSON: " + filePath);

        // Si no existe el archivo, se crea uno nuevo con el contador en 0
        if (!File.Exists(filePath))
        {
            usageData = new UsageData();  // Si el archivo no existe, inicializamos el contador
            SaveData();  // Guardamos el archivo con el valor inicial
        }
        else
        {
            // Si el archivo existe, cargamos los datos
            LoadData();
        }
    }

    // Método para guardar los datos
    public void SaveData()
    {
        string json = JsonUtility.ToJson(usageData);  // Convertimos los datos a formato JSON
        File.WriteAllText(filePath, json);  // Guardamos el archivo JSON en la ruta especificada
        Debug.Log("Datos guardados: " + json);
    }

    // Método para cargar los datos
    public void LoadData()
    {
        string json = File.ReadAllText(filePath);  // Leemos el archivo JSON
        usageData = JsonUtility.FromJson<UsageData>(json);  // Convertimos el JSON a un objeto de datos
        Debug.Log("Datos cargados: " + json);
    }

    // Método que se llama cuando el jugador hace clic en el botón de "Iniciar"
    public void IncrementUsage()
    {
        usageData.usageCount++;  // Aumentamos el contador de uso
        SaveData();  // Guardamos los datos actualizados
    }

    // Método para mostrar el número de veces que se ha iniciado la aplicación
    public void ShowUsage()
    {
        Debug.Log("La aplicación ha sido iniciada " + usageData.usageCount + " veces.");
    }

    public void ShowUsageOnButtonClick()
    {
        usageData.usageCount++;  // Aumentamos el contador de uso
        SaveData();  // Guardamos los datos actualizados
        ShowUsage();  // Mostramos el uso actual en la consola
    }
}
