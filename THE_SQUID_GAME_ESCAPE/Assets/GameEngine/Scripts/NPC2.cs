using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NPC2 : MonoBehaviour
{
    [Header("NPC 설정")]
    [SerializeField] private string npcName = "NPC2";
    [SerializeField] private string[] dialogues; // 대화 내용 배열
    [SerializeField] private string nextSceneName = ""; // 대화 후 이동할 씬 이름
    
    [Header("제한시간 설정")]
    [SerializeField] private float timeLimit = 30f; // 제한시간 (초)
    [SerializeField] private bool startTimerOnStart = true; // 시작 시 타이머 자동 시작
    
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRange = 2f; // 상호작용 가능 거리
    [SerializeField] private KeyCode interactionKey = KeyCode.E; // 상호작용 키
    [SerializeField] private GameObject interactionHint; // 상호작용 힌트 UI
    
    [Header("타이머 UI (선택사항)")]
    [SerializeField] private Text timerText; // 기존 Text
    [SerializeField] private TextMeshProUGUI timerTextTMP; // TextMeshPro
    [SerializeField] private GameObject timerPanel; // 타이머 패널 (선택사항)
    
    [Header("실패 처리")]
    [SerializeField] private string failSceneName = ""; // 시간 초과 시 이동할 씬 (비어있으면 현재 씬 재시작)
    [SerializeField] private bool restartOnFail = true; // 실패 시 현재 씬 재시작
    
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private bool hasInteracted = false; // 상호작용 완료 여부
    
    void Start()
    {
        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }
        
        // 타이머 패널 초기화
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
        }
        
        // 플레이어 찾기
        FindPlayer();
        
        // 타이머 시작
        if (startTimerOnStart)
        {
            StartTimer();
        }
    }
    
    // 플레이어 찾기 (지속적으로 시도)
    void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Player 태그가 없으면 PlayerController를 가진 오브젝트 찾기
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                    Debug.Log("NPC2: PlayerController를 통해 플레이어를 찾았습니다.");
                }
                else
                {
                    Debug.LogWarning("NPC2: 플레이어를 찾을 수 없습니다!");
                }
            }
            else
            {
                playerTransform = player.transform;
                Debug.Log("NPC2: 플레이어를 찾았습니다.");
            }
        }
    }
    
    void Update()
    {
        // 이미 상호작용했으면 더 이상 처리하지 않음
        if (hasInteracted)
        {
            return;
        }
        
        // 플레이어가 없으면 계속 찾기
        if (playerTransform == null)
        {
            FindPlayer();
        }
        else
        {
            // 플레이어가 근처에 있는지 확인
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            isPlayerNearby = distance <= interactionRange;
            
            // 힌트 표시/숨김
            if (interactionHint != null)
            {
                interactionHint.SetActive(isPlayerNearby && isTimerRunning);
            }
            
            // 상호작용 키 입력 확인
            if (isPlayerNearby && Input.GetKeyDown(interactionKey) && isTimerRunning)
            {
                Debug.Log("NPC2: 상호작용 키가 눌렸습니다!");
                OnInteractionSuccess();
            }
        }
        
        // 타이머 업데이트
        if (isTimerRunning)
        {
            UpdateTimer();
        }
    }
    
    // 타이머 시작
    public void StartTimer()
    {
        currentTime = timeLimit;
        isTimerRunning = true;
        hasInteracted = false;
        Debug.Log($"NPC2: 타이머 시작! 제한시간: {timeLimit}초");
    }
    
    // 타이머 업데이트
    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;
        
        // UI 업데이트
        UpdateTimerUI();
        
        // 시간 초과 체크
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            OnTimeUp();
        }
    }
    
    // 타이머 UI 업데이트
    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        // TextMeshPro 우선 사용
        if (timerTextTMP != null)
        {
            timerTextTMP.text = timeString;
            
            // 시간이 부족하면 빨간색으로 표시
            if (currentTime <= 10f)
            {
                timerTextTMP.color = Color.red;
            }
            else
            {
                timerTextTMP.color = Color.white;
            }
        }
        else if (timerText != null)
        {
            timerText.text = timeString;
            
            // 시간이 부족하면 빨간색으로 표시
            if (currentTime <= 10f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    
    // 상호작용 성공 (제한시간 안에 상호작용)
    void OnInteractionSuccess()
    {
        if (hasInteracted)
        {
            return;
        }
        
        hasInteracted = true;
        isTimerRunning = false;
        
        Debug.Log($"NPC2: 성공! 남은 시간: {currentTime:F2}초");
        
        // 대화 시작 또는 바로 다음 씬으로 이동
        if (dialogues != null && dialogues.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(dialogues, npcName, nextSceneName);
        }
        else
        {
            // 대화 없이 바로 다음 씬으로 이동
            LoadNextScene();
        }
    }
    
    // 시간 초과 처리
    void OnTimeUp()
    {
        if (hasInteracted)
        {
            return;
        }
        
        isTimerRunning = false;
        Debug.Log("NPC2: 시간 초과! 실패!");
        
        // 실패 처리
        if (restartOnFail)
        {
            // 현재 씬 재시작
            Scene currentScene = SceneManager.GetActiveScene();
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(currentScene.name);
            }
            else
            {
                SceneManager.LoadScene(currentScene.name);
            }
        }
        else if (!string.IsNullOrEmpty(failSceneName))
        {
            // 실패 씬으로 이동
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(failSceneName);
            }
            else
            {
                SceneManager.LoadScene(failSceneName);
            }
        }
    }
    
    // 다음 씬으로 이동
    void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("NPC2: 다음 씬 이름이 설정되지 않았습니다!");
            return;
        }
        
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(nextSceneName);
        }
        else
        {
            SceneLoader sceneLoader = FindObjectOfType<SceneLoader>();
            if (sceneLoader != null)
            {
                sceneLoader.LoadSceneWithFade(nextSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
    
    // 타이머 정지
    public void StopTimer()
    {
        isTimerRunning = false;
    }
    
    // 남은 시간 가져오기
    public float GetRemainingTime()
    {
        return currentTime;
    }
    
    // 타이머가 실행 중인지 확인
    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }
    
    // Gizmos로 상호작용 범위 시각화 (에디터에서만 보임)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

