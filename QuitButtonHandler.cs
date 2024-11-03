using UnityEngine;

public class QuitButtonHandler : MonoBehaviour
{
    // Function to be called when the Quit button is clicked
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

