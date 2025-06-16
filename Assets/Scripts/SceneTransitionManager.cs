// --- START OF FILE SceneTransitionManager.cs ---

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [SerializeField]
    private GameObject loadingCanvas; // Unity 에디터에서 연결할 로딩 캔버스

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // 로딩 캔버스 활성화하여 화면 전체를 덮고 입력을 막음
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
        }

        // 비동기적으로 씬 로드 시작
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 씬 로드가 완료될 때까지 대기
        while (!operation.isDone)
        {
            // 여기서 로딩 진행률을 UI에 표시할 수 있습니다. (예: operation.progress)
            yield return null;
        }

        // 씬 로드가 완료되면 로딩 캔버스 비활성화
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
        }
    }
}