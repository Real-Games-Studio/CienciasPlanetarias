using UnityEngine;

public class UIBackButton : MonoBehaviour
{
    public void OnBackButtonPressed()
    {
        ScreenCanvasController.instance.GoBack();
    }
}
