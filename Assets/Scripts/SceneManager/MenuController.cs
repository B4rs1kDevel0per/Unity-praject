using UnityEngine;

public class MenuController : MonoBehaviour
{
    public void OnPlayPressed()
    {
        SceneTransitionManager.Instance.LoadScene("World");
    }

    public void OnSettingsPressed()
    {
        SceneTransitionManager.Instance.LoadScene("Settings");
    }

    public void OnExitPressed()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}