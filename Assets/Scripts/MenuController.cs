using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelComoFunciona;

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

        ActualizarColorBotonMute();
    }

    public void IniciarApp()
    {
        SceneManager.LoadScene("RVprimero");
    }

    public void MostrarComoFunciona()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(true);
        }
    }

    public void VolverAlMenu()
    {
        if (panelComoFunciona != null)
        {
            panelComoFunciona.SetActive(false);
        }
    }

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