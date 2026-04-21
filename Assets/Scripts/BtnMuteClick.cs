using UnityEngine;

public class BtnMuteClick : MonoBehaviour
{
    public MenuController menuController;

    private void OnMouseDown()
    {
        if (menuController != null)
        {
            menuController.ToggleMute();
        }
    }
}