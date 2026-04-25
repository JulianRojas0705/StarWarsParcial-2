using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;  // Importamos TextMeshPro

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelComoFunciona;
    public GameObject panelFormulario;    // Panel donde el usuario ingresa sus datos

    [Header("Campos de entrada")]
    public TMP_InputField nombreInput;        // Campo para ingresar nombre
    public TMP_InputField apellidoInput;      // Campo para ingresar apellido
    public TMP_InputField edadInput;          // Campo para ingresar edad
    public TMP_InputField correoInput;        // Campo para ingresar correo
    public TMP_InputField ciudadInput;        // Campo para ingresar ciudad

    [Header("Audio")]
    public AudioSource musicaMenu;
    public SpriteRenderer spriteBotonMute;

    [Header("Colores")]
    public Color colorActivo = Color.white;
    public Color colorMuteado = Color.gray;

    private bool estaMuteado = false;

    void Start()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(false);
        }

        if (panelFormulario != null)
        {
            panelFormulario.SetActive(false);  // Escondemos el formulario al inicio
        }

        ActualizarColorBotonMute();
    }

    // Función para iniciar la aplicación, cargar la escena principal
    public void IniciarApp()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(true);  // Mostrar el panel de cómo funciona la app
        }
    }

    // Función para mostrar el panel de "Cómo Funciona"
    public void MostrarComoFunciona()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(true);
        }
    }

    // Función para volver al menú principal (ocultar el panel de cómo funciona)
    public void VolverAlMenu()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(false);
        }
    }

    // Función que se llama al presionar el botón "Continuar" en el panel "Cómo Funciona"
    public void ContinuarConComoFunciona()
    {
        // Ocultar el panel de explicación
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(false);
        }

        // Mostrar el panel de formulario para que el usuario ingrese los datos
        if (panelFormulario != null)
        {
            panelFormulario.SetActive(true);
        }
    }

    // Función para guardar los datos de los usuarios en JSON y continuar
    public void ContinuarConFormulario()
    {
        // Validar que todos los campos estén completos antes de continuar
        if (ValidarFormulario())
        {
            // Guardar los datos en un archivo JSON
            GuardarDatos();

            // Llamada para iniciar la parte de RA o cargar la escena que sigue
            SceneManager.LoadScene("Juego");  // Cambia el nombre de la escena según tu configuración
        }
        else
        {
            // Mostrar un mensaje si no se han llenado los campos correctamente
            Debug.Log("Por favor, complete todos los campos.");
        }
    }

    // Función para validar que los campos del formulario están completos
    private bool ValidarFormulario()
    {
        // Verificar que todos los campos tienen texto
        return !string.IsNullOrEmpty(nombreInput.text) &&
               !string.IsNullOrEmpty(apellidoInput.text) &&
               !string.IsNullOrEmpty(edadInput.text) &&
               !string.IsNullOrEmpty(correoInput.text) &&
               !string.IsNullOrEmpty(ciudadInput.text);
    }

    // Función para guardar los datos de los usuarios en JSON
    private void GuardarDatos()
    {
        // Crear un objeto con los datos del formulario
        UsuarioData usuario = new UsuarioData
        {
            nombre = nombreInput.text,
            apellido = apellidoInput.text,
            edad = edadInput.text,
            correo = correoInput.text,
            ciudad = ciudadInput.text
        };

        // Leer los datos existentes si el archivo JSON ya existe
        string filePath = Application.persistentDataPath + "/usuarioDatos.json";
        string jsonData = "";

        if (System.IO.File.Exists(filePath))
        {
            jsonData = System.IO.File.ReadAllText(filePath);
            UsuarioListWrapper wrapper = JsonUtility.FromJson<UsuarioListWrapper>(jsonData);
            wrapper.usuarios.Add(usuario);
            jsonData = JsonUtility.ToJson(wrapper, true);
        }
        else
        {
            // Si el archivo no existe, creamos uno nuevo
            UsuarioListWrapper wrapper = new UsuarioListWrapper();
            wrapper.usuarios = new System.Collections.Generic.List<UsuarioData> { usuario };
            jsonData = JsonUtility.ToJson(wrapper, true);
        }

        // Guardar los datos en el archivo JSON
        System.IO.File.WriteAllText(filePath, jsonData);

        // Imprimir la ruta para asegurarse de que se está guardando correctamente
        Debug.Log("Datos guardados en: " + filePath);
    }

    // Estructura para los datos del usuario
    [System.Serializable]
    public class UsuarioData
    {
        public string nombre;
        public string apellido;
        public string edad;
        public string correo;
        public string ciudad;
    }

    // Clase auxiliar para envolver la lista de usuarios
    [System.Serializable]
    public class UsuarioListWrapper
    {
        public System.Collections.Generic.List<UsuarioData> usuarios;
    }

    // Función para activar/desactivar el sonido
    public void ToggleMute()
    {
        estaMuteado = !estaMuteado;

        if (musicaMenu != null)
        {
            musicaMenu.mute = estaMuteado;
        }

        ActualizarColorBotonMute();
    }

    private void ActualizarColorBotonMute()
    {
        if (spriteBotonMute != null)
        {
            spriteBotonMute.color = estaMuteado ? colorMuteado : colorActivo;
        }
    }
}