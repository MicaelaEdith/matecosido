using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndUI : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Min(0f)] private float sceneLoadDelay = 0.12f;

    public void ReloadLevel()
    {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneAfterDelay(buildIndex));
    }

    public void LoadMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            StartCoroutine(LoadSceneAfterDelay(mainMenuSceneName));
    }

    private IEnumerator LoadSceneAfterDelay(int buildIndex)
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(buildIndex);
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }
}
