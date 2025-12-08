using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RedLightGreenLightTimer : MonoBehaviour
{
    [Header("타이머 설정")]
    [SerializeField] private float timeLimit = 60f; // 제한시간 (초)
    [SerializeField] private bool countdown = true; // 카운트다운 모드 (true) 또는 카운트업 모드 (false)
    [SerializeField] private bool startOnStart = true; // 시작 시 자동 시작
    [SerializeField] private bool pauseOnGameOver = true; // 게임오버 시 일시정지
    
    [Header("UI 설정")]
    [SerializeField] private Text timerText; // 기존 Text
    [SerializeField] private TextMeshProUGUI timerTextTMP; // TextMeshPro
    [SerializeField] private GameObject timerPanel; // 타이머 패널 (선택사항)
    
    [Header("위치 추적 설정")]
    [SerializeField] private bool followPlayerX = false; // 플레이어의 X축을 따라갈지 여부
    [SerializeField] private bool followCameraX = true; // 카메라의 X축을 따라갈지 여부
    [SerializeField] private float yOffset = 0f; // Y축 오프셋
    
    [Header("경고 설정")]
    [SerializeField] private float warningTime = 10f; // 경고 표시 시간 (초)
    [SerializeField] private Color normalColor = Color.white; // 일반 색상
    [SerializeField] private Color warningColor = Color.yellow; // 경고 색상
    [SerializeField] private Color dangerColor = Color.red; // 위험 색상
    
    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private bool isPaused = false;
    private bool isTimerHidden = false; // 타이머가 숨겨졌는지 여부 (게임오버 등)
    private Transform playerTransform;
    private Camera targetCamera;
    private Canvas timerCanvas; // Canvas 캐싱
    private RectTransform canvasRectTransform; // Canvas RectTransform 캐싱
    private bool canvasInitialized = false; // Canvas 초기화 여부
    private Vector2 lastValidPosition = Vector2.zero; // 마지막 유효한 위치 저장
    private bool hasValidPosition = false; // 유효한 위치가 있는지 여부
    private Vector2 lastAppliedPosition = Vector2.zero; // 마지막으로 적용된 위치
    private float lastCheckTime = 0f; // 마지막 UI 활성화 체크 시간
    private const float UI_CHECK_INTERVAL = 0.5f; // UI 활성화 체크 간격 (초)
    
    void Start()
    {
        // 플레이어 찾기
        FindPlayer();
        
        // 카메라 찾기
        FindCamera();
        
        // Canvas 초기화
        InitializeCanvas();
        
        // 타이머 패널 초기화
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
        }
        
        // 초기 시간 설정
        if (countdown)
        {
            currentTime = timeLimit;
        }
        else
        {
            currentTime = 0f;
        }
        
        // 자동 시작
        if (startOnStart)
        {
            StartTimer();
        }
        
        // 초기 UI 업데이트
        UpdateTimerUI();
        
        // 초기 위치 저장 (Screen Space 모드용)
        if (timerCanvas != null && timerCanvas.renderMode != RenderMode.WorldSpace)
        {
            SaveInitialPosition();
        }
    }
    
    // 초기 위치 저장
    void SaveInitialPosition()
    {
        if (timerTextTMP != null)
        {
            RectTransform textRect = timerTextTMP.GetComponent<RectTransform>();
            if (textRect != null)
            {
                lastValidPosition = textRect.anchoredPosition;
                hasValidPosition = true;
            }
        }
        else if (timerText != null)
        {
            RectTransform textRect = timerText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                lastValidPosition = textRect.anchoredPosition;
                hasValidPosition = true;
            }
        }
    }
    
    void Update()
    {
        // 플레이어가 없으면 계속 찾기
        if (playerTransform == null && followPlayerX)
        {
            FindPlayer();
        }
        
        // 카메라 찾기
        if (targetCamera == null && followCameraX)
        {
            FindCamera();
        }
        
        // Canvas가 null이면 다시 찾기 시도 (Canvas가 파괴되었을 수 있음)
        if (timerCanvas == null && (followPlayerX || followCameraX))
        {
            canvasInitialized = false;
            InitializeCanvas();
        }
        
        // 타이머 업데이트
        if (isTimerRunning && !isPaused)
        {
            UpdateTimer();
        }
        
        // 타이머 UI 요소가 항상 활성화되어 있는지 확인 (0.5초마다)
        if (Time.time - lastCheckTime >= UI_CHECK_INTERVAL)
        {
            EnsureTimerUIActive();
            lastCheckTime = Time.time;
        }
        
        // 위치 추적
        if (followPlayerX)
        {
            if (playerTransform != null)
            {
                UpdateTimerPosition();
            }
            else
            {
                // 플레이어를 찾지 못했으면 다시 시도
                FindPlayer();
            }
        }
        else if (followCameraX)
        {
            if (targetCamera != null)
            {
                UpdateTimerPosition();
            }
            else
            {
                // 카메라를 찾지 못했으면 다시 시도
                FindCamera();
            }
        }
    }
    
    // 플레이어 찾기
    void FindPlayer()
    {
        if (playerTransform != null)
        {
            return; // 이미 찾았으면 리턴
        }
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                Debug.Log("RedLightGreenLightTimer: PlayerController를 통해 플레이어를 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("RedLightGreenLightTimer: 플레이어를 찾을 수 없습니다!");
            }
        }
        else
        {
            playerTransform = player.transform;
            Debug.Log("RedLightGreenLightTimer: 플레이어를 찾았습니다.");
        }
    }
    
    // 카메라 찾기
    void FindCamera()
    {
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }
    }
    
    // Canvas 초기화 및 캐싱
    void InitializeCanvas()
    {
        if (canvasInitialized && timerCanvas != null)
        {
            return; // 이미 초기화되었으면 리턴
        }
        
        // Canvas 찾기
        if (timerTextTMP != null)
        {
            timerCanvas = timerTextTMP.GetComponentInParent<Canvas>();
        }
        else if (timerText != null)
        {
            timerCanvas = timerText.GetComponentInParent<Canvas>();
        }
        
        if (timerCanvas != null)
        {
            canvasRectTransform = timerCanvas.GetComponent<RectTransform>();
            canvasInitialized = true;
            Debug.Log("RedLightGreenLightTimer: Canvas 초기화 완료");
            
            // Screen Space 모드일 때 초기 위치 저장
            if (timerCanvas.renderMode != RenderMode.WorldSpace && !hasValidPosition)
            {
                SaveInitialPosition();
            }
        }
        else
        {
            canvasInitialized = false;
            Debug.LogWarning("RedLightGreenLightTimer: Canvas를 찾을 수 없습니다. 다시 시도합니다.");
        }
    }
    
    // 타이머 UI 요소가 항상 활성화되어 있는지 확인
    void EnsureTimerUIActive()
    {
        // 타이머가 숨겨진 상태면 UI를 다시 활성화하지 않음
        if (isTimerHidden)
        {
            return;
        }
        
        // 실제로 비활성화되었을 때만 활성화 (불필요한 SetActive 호출 방지)
        bool needUpdate = false;
        
        if (timerPanel != null && !timerPanel.activeSelf)
        {
            timerPanel.SetActive(true);
            needUpdate = true;
        }
        
        if (timerTextTMP != null && !timerTextTMP.gameObject.activeSelf)
        {
            timerTextTMP.gameObject.SetActive(true);
            needUpdate = true;
        }
        
        if (timerText != null && !timerText.gameObject.activeSelf)
        {
            timerText.gameObject.SetActive(true);
            needUpdate = true;
        }
        
        // UI가 활성화되었으면 위치 재적용
        if (needUpdate && hasValidPosition)
        {
            ApplyPositionToUI(lastValidPosition);
        }
    }
    
    // 타이머 업데이트
    void UpdateTimer()
    {
        if (countdown)
        {
            // 카운트다운
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                OnTimeUp();
            }
        }
        else
        {
            // 카운트업
            currentTime += Time.deltaTime;
        }
        
        UpdateTimerUI();
    }
    
    // 타이머 UI 업데이트
    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        // 색상 결정
        Color displayColor = normalColor;
        if (countdown)
        {
            if (currentTime <= warningTime / 2f)
            {
                displayColor = dangerColor;
            }
            else if (currentTime <= warningTime)
            {
                displayColor = warningColor;
            }
        }
        
        // TextMeshPro 우선 사용
        if (timerTextTMP != null)
        {
            timerTextTMP.text = timeString;
            timerTextTMP.color = displayColor;
        }
        else if (timerText != null)
        {
            timerText.text = timeString;
            timerText.color = displayColor;
        }
        else
        {
            // UI가 설정되지 않았으면 콘솔에 출력 (디버그용)
            if (isTimerRunning && !isPaused)
            {
                Debug.Log($"RedLightGreenLightTimer: {timeString} (UI가 설정되지 않았습니다)");
            }
        }
    }
    
    // 타이머 위치 업데이트 (플레이어 또는 카메라 추적)
    void UpdateTimerPosition()
    {
        // Canvas가 없으면 초기화 시도
        if (timerCanvas == null || !canvasInitialized)
        {
            InitializeCanvas();
            if (timerCanvas == null)
            {
                // Canvas를 찾지 못했으면 위치 업데이트 중단 (타이머는 계속 표시됨)
                return;
            }
        }
        
        float targetWorldX = 0f;
        
        // 추적할 대상 결정
        if (followPlayerX && playerTransform != null)
        {
            targetWorldX = playerTransform.position.x;
        }
        else if (followCameraX && targetCamera != null)
        {
            targetWorldX = targetCamera.transform.position.x;
        }
        else
        {
            // 추적할 대상이 없으면 리턴
            if (followCameraX && targetCamera == null)
            {
                FindCamera(); // 카메라를 다시 찾기 시도
            }
            return;
        }
        
        // World Space 모드: 월드 좌표 직접 사용 (가장 확실한 방법)
        if (timerCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (timerTextTMP != null)
            {
                Transform textTransform = timerTextTMP.transform;
                Vector3 currentPos = textTransform.position;
                // X 위치가 실제로 변경되었을 때만 업데이트 (깜빡임 방지)
                if (Mathf.Abs(currentPos.x - targetWorldX) > 0.01f)
                {
                    // Y와 Z는 유지하고 X만 업데이트 (yOffset은 Y에 적용하지 않음 - World Space에서는 월드 좌표 사용)
                    textTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
                }
            }
            else if (timerText != null)
            {
                Transform textTransform = timerText.transform;
                Vector3 currentPos = textTransform.position;
                // X 위치가 실제로 변경되었을 때만 업데이트 (깜빡임 방지)
                if (Mathf.Abs(currentPos.x - targetWorldX) > 0.01f)
                {
                    // Y와 Z는 유지하고 X만 업데이트 (yOffset은 Y에 적용하지 않음 - World Space에서는 월드 좌표 사용)
                    textTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
                }
            }
        }
        else if (targetCamera != null)
        {
            // Screen Space 모드: 스크린 좌표로 변환
            float targetY = followPlayerX && playerTransform != null ? playerTransform.position.y : targetCamera.transform.position.y;
            Vector3 targetWorldPos = new Vector3(targetWorldX, targetY, 0);
            Vector3 screenPoint = targetCamera.WorldToScreenPoint(targetWorldPos);
            
            // 카메라 앞에 있는지 확인 (z > 0)
            if (screenPoint.z <= 0)
            {
                // 카메라 뒤에 있으면 이전 위치 유지 (이미 적용되어 있으면 다시 적용하지 않음)
                return;
            }
            
            // 화면 범위를 벗어난 경우도 이전 위치 유지
            if (screenPoint.x < -100 || screenPoint.x > Screen.width + 100 || 
                screenPoint.y < -100 || screenPoint.y > Screen.height + 100)
            {
                // 화면 밖에 있으면 이전 위치 유지 (이미 적용되어 있으면 다시 적용하지 않음)
                return;
            }
            
            // 캐시된 RectTransform 사용
            if (canvasRectTransform == null && timerCanvas != null)
            {
                canvasRectTransform = timerCanvas.GetComponent<RectTransform>();
            }
            
            Vector2 canvasPos = Vector2.zero;
            bool positionValid = false;
            
            if (canvasRectTransform != null)
            {
                Camera canvasCamera = timerCanvas.worldCamera != null ? timerCanvas.worldCamera : targetCamera;
                bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    screenPoint,
                    canvasCamera,
                    out canvasPos
                );
                
                if (success)
                {
                    positionValid = true;
                }
                else
                {
                    // 변환 실패 시 더 안전한 계산
                    // Canvas의 크기와 스케일을 고려한 계산
                    float canvasWidth = canvasRectTransform.rect.width;
                    float canvasHeight = canvasRectTransform.rect.height;
                    
                    // 화면 좌표를 Canvas 좌표로 변환
                    float normalizedX = Mathf.Clamp01(screenPoint.x / Screen.width);
                    float normalizedY = Mathf.Clamp01(screenPoint.y / Screen.height);
                    
                    canvasPos = new Vector2(
                        (normalizedX - 0.5f) * canvasWidth,
                        (normalizedY - 0.5f) * canvasHeight
                    );
                    
                    // 계산된 위치가 Canvas 범위 내에 있는지 확인
                    if (Mathf.Abs(canvasPos.x) <= canvasWidth * 0.6f && Mathf.Abs(canvasPos.y) <= canvasHeight * 0.6f)
                    {
                        positionValid = true;
                    }
                }
            }
            else
            {
                // RectTransform이 없으면 기본 계산
                canvasPos = new Vector2(screenPoint.x - Screen.width / 2f, screenPoint.y - Screen.height / 2f);
                // 화면 범위 내에 있는지 확인
                if (screenPoint.x >= 0 && screenPoint.x <= Screen.width && 
                    screenPoint.y >= 0 && screenPoint.y <= Screen.height)
                {
                    positionValid = true;
                }
            }
            
            // 위치가 유효하면 적용, 아니면 이전 위치 유지
            if (positionValid)
            {
                float uiY = (canvasRectTransform != null ? canvasRectTransform.rect.height : Screen.height) - yOffset;
                Vector2 finalPosition = new Vector2(canvasPos.x, uiY);
                
                // 위치가 실제로 변경되었는지 확인 (깜빡임 방지)
                if (!hasValidPosition || Vector2.Distance(finalPosition, lastValidPosition) > 1f)
                {
                    // 위치 저장 및 적용
                    lastValidPosition = finalPosition;
                    hasValidPosition = true;
                    ApplyPositionToUI(finalPosition);
                }
            }
            // 유효하지 않은 위치면 이전 위치 유지 (이미 적용되어 있으면 다시 적용하지 않음)
        }
    }
    
    // UI에 위치 적용 (헬퍼 메서드)
    void ApplyPositionToUI(Vector2 position)
    {
        // 위치가 실제로 변경되었을 때만 업데이트 (깜빡임 방지)
        if (Vector2.Distance(position, lastAppliedPosition) < 0.1f)
        {
            return; // 위치 변경이 거의 없으면 업데이트하지 않음
        }
        
        lastAppliedPosition = position;
        
        if (timerTextTMP != null)
        {
            RectTransform textRect = timerTextTMP.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchoredPosition = position;
            }
        }
        else if (timerText != null)
        {
            RectTransform textRect = timerText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchoredPosition = position;
            }
        }
    }
    
    // 타이머 시작
    public void StartTimer()
    {
        isTimerRunning = true;
        isPaused = false;
        
        if (countdown)
        {
            currentTime = timeLimit;
        }
        else
        {
            currentTime = 0f;
        }
        
        // UI 업데이트
        UpdateTimerUI();
        
        Debug.Log($"RedLightGreenLightTimer: 타이머 시작! 모드: {(countdown ? "카운트다운" : "카운트업")}, 시간: {timeLimit}초, UI 설정: {(timerTextTMP != null || timerText != null ? "있음" : "없음")}");
    }
    
    // 타이머 정지
    public void StopTimer()
    {
        isTimerRunning = false;
        Debug.Log("RedLightGreenLightTimer: 타이머 정지");
    }
    
    // 타이머 일시정지
    public void PauseTimer()
    {
        isPaused = true;
        Debug.Log("RedLightGreenLightTimer: 타이머 일시정지");
    }
    
    // 타이머 재개
    public void ResumeTimer()
    {
        isPaused = false;
        Debug.Log("RedLightGreenLightTimer: 타이머 재개");
    }
    
    // 타이머 리셋
    public void ResetTimer()
    {
        if (countdown)
        {
            currentTime = timeLimit;
        }
        else
        {
            currentTime = 0f;
        }
        UpdateTimerUI();
        Debug.Log("RedLightGreenLightTimer: 타이머 리셋");
    }
    
    // 현재 시간 가져오기
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    // 남은 시간 가져오기 (카운트다운 모드일 때)
    public float GetRemainingTime()
    {
        if (countdown)
        {
            return currentTime;
        }
        return timeLimit - currentTime;
    }
    
    // 타이머가 실행 중인지 확인
    public bool IsTimerRunning()
    {
        return isTimerRunning && !isPaused;
    }
    
    // 타이머 UI 숨기기 (게임오버 시 호출)
    public void HideTimer()
    {
        isTimerHidden = true; // 타이머 숨김 상태로 설정
        
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
        }
        if (timerTextTMP != null)
        {
            timerTextTMP.gameObject.SetActive(false);
        }
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }
    
    // 타이머 UI 다시 표시하기 (필요한 경우)
    public void ShowTimer()
    {
        isTimerHidden = false; // 타이머 숨김 상태 해제
        
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
        }
        if (timerTextTMP != null)
        {
            timerTextTMP.gameObject.SetActive(true);
        }
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
    }
    
    // 시간 초과 이벤트
    void OnTimeUp()
    {
        if (pauseOnGameOver)
        {
            PauseTimer();
        }
        
        Debug.Log("RedLightGreenLightTimer: 시간 초과!");
        
        // RedLightGreenLight 게임에 시간 초과 알림
        if (RedLightGreenLight.Instance != null)
        {
            RedLightGreenLight.Instance.OnTimeUp();
        }
    }
}

