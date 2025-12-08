using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    public static string nextScene;  // 로드할 씬 이름
    

    [SerializeField] private float autoLoadDelay = 2.0f;  // 자동 로드 대기 시간 (초)
    
    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }
    
    IEnumerator LoadSceneAsync()
    {
        // 비동기 씬 로드 시작
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextScene);
        
        // 로딩 완료 전까지 자동 활성화 방지
        operation.allowSceneActivation = false;
        
        float timer = 0f;
        
        while (!operation.isDone)
        {
            // 로딩 완료 (90%)
            if (operation.progress >= 0.9f)
            {
                // 특정 시간 대기 후 자동으로 씬 활성화
                timer += Time.deltaTime;
                if (timer >= autoLoadDelay)
                {
                    operation.allowSceneActivation = true;
                }
            }
            
            yield return null;
        }
    }
}