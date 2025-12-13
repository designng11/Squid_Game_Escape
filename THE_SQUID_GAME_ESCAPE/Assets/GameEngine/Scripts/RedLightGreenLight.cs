using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class RedLightGreenLight : MonoBehaviour
{
    public static RedLightGreenLight Instance;
    
    public enum GameState
    {
        GreenLight,  // 파란불 (움직여도 됨)
        RedLight    // 빨간불 (움직이면 게임오버)
    }
    
    [Header("게임 설정")]
    [SerializeField] private float minGreenLightTime = 2f; // 최소 파란불 시간
    [SerializeField] private float maxGreenLightTime = 5f; // 최대 파란불 시간
    [SerializeField] private float minRedLightTime = 1f;   // 최소 빨간불 시간
    [SerializeField] private float maxRedLightTime = 3f;   // 최대 빨간불 시간
    [SerializeField] private float velocityThreshold = 0.1f; // 속도 감지 임계값 (이 값 이상이면 게임오버)
    [SerializeField] private float redLightDetectionDelay = 0.5f; // 빨간불로 변한 후 움직임 감지 시작 딜레이 (초)
    
    [Header("UI 설정")]
    [SerializeField] private GameObject statusPanel; // 상태 표시 패널
    [SerializeField] private Text statusText; // 기존 Text
    [SerializeField] private TextMeshProUGUI statusTextTMP; // TextMeshPro
    [SerializeField] private Image statusBackground; // 상태 배경 (색상 변경용)
    [SerializeField] private Image greenLightSprite; // 초록불 스프라이트 이미지
    [SerializeField] private Image redLightSprite; // 빨간불 스프라이트 이미지
    [SerializeField] private Camera targetCamera; // UI가 따라갈 카메라 (비어있으면 Main Camera 자동 찾기)
    [SerializeField] private bool followCamera = true; // 카메라를 따라갈지 여부
    [SerializeField] private bool followPlayerX = false; // 플레이어의 X축을 따라갈지 여부
    [SerializeField] private bool followCameraX = true; // 카메라의 X축을 따라갈지 여부
    [SerializeField] private float spriteYOffset = 0f; // 스프라이트 Y축 오프셋 (화면 상단에서의 거리)
    
    [Header("게임오버 설정")]
    [SerializeField] private GameObject gameOverPanel; // 게임오버 패널
    [SerializeField] private Text gameOverText; // 기존 Text
    [SerializeField] private TextMeshProUGUI gameOverTextTMP; // TextMeshPro
    [SerializeField] private string gameOverMessage = "움직임이 감지되었습니다!";
    [SerializeField] private string timeUpMessage = "시간 초과!"; // 시간초과 메시지
    [SerializeField] private float gameOverDelay = 2f; // 게임오버 후 씬 전환 대기 시간
    
    [Header("씬 설정")]
    [SerializeField] private string gameOverSceneName = ""; // 게임오버 시 이동할 씬 (비어있으면 현재 씬 재시작)
    [SerializeField] private string successSceneName = ""; // 성공 시 이동할 씬
    [SerializeField] private Transform finishLine; // 결승선 (도착하면 성공)
    
    [Header("사운드 (선택사항)")]
    [SerializeField] private AudioClip greenLightSound; // 파란불 사운드
    [SerializeField] private AudioClip redLightSound;   // 빨간불 사운드
    [SerializeField] private AudioClip gameOverSound;    // 게임오버 사운드
    [SerializeField] private bool adjustGreenLightSoundLength = true; // GreenLight 시간에 맞춰 오디오 길이 조절
    [SerializeField] private float minPitch = 0.5f; // 최소 pitch (너무 느리게 재생 방지)
    [SerializeField] private float maxPitch = 2.0f; // 최대 pitch (너무 빠르게 재생 방지)
    
    private GameState currentState = GameState.GreenLight;
    private float stateTimer = 0f;
    private float nextStateChangeTime = 0f;
    private bool isGameActive = true;
    private bool isGameOver = false;
    private float redLightStartTime = 0f; // 빨간불로 변한 시간
    private bool canDetectMovement = false; // 움직임 감지 가능 여부
    
    private PlayerController playerController;
    private Vector3 lastPlayerPosition;
    private Rigidbody2D playerRigidbody;
    private AudioSource audioSource;
    private Canvas statusCanvas; // 상태 UI의 Canvas
    private Canvas gameOverCanvas; // 게임오버 UI의 Canvas
    private Vector2 initialCanvasSize; // Canvas 초기 크기
    private Vector3 initialCanvasScale; // Canvas 초기 Scale
    private bool canvasSizeInitialized = false; // Canvas 크기 초기화 여부
    private Vector3 initialGreenSpritePos; // 초록불 스프라이트 초기 위치
    private Vector3 initialRedSpritePos; // 빨간불 스프라이트 초기 위치
    private Vector3 initialStatusTextPos; // Status 텍스트 초기 위치
    private bool initialPositionsSaved = false; // 초기 위치 저장 여부
    private float initialCanvasDistance = 10f; // Canvas와 카메라 간 초기 거리
    private bool canvasDistanceInitialized = false; // Canvas 거리 초기화 여부
    private float initialGameOverCanvasDistance = 10f; // 게임오버 Canvas와 카메라 간 초기 거리
    private bool gameOverCanvasDistanceInitialized = false; // 게임오버 Canvas 거리 초기화 여부
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 플레이어 찾기
        FindPlayer();
        
        // 카메라 찾기
        FindCamera();
        
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // UI 초기화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 스프라이트 초기화
        if (greenLightSprite != null)
        {
            greenLightSprite.gameObject.SetActive(false);
        }
        if (redLightSprite != null)
        {
            redLightSprite.gameObject.SetActive(false);
        }
        
        // 상태 UI Canvas 설정
        SetupStatusCanvas();
        
        // 게임오버 UI Canvas 설정
        SetupGameOverCanvas();
        
        // 초기 위치 저장 (한 번만)
        if (!initialPositionsSaved)
        {
            SaveInitialPositions();
        }
        
        // 첫 상태 설정
        ChangeState(GameState.GreenLight);
        
        // 플레이어 초기 위치 저장
        if (playerController != null)
        {
            lastPlayerPosition = playerController.transform.position;
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
        }
    }
    
    // 카메라 찾기
    void FindCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }
    }
    
    // 상태 UI Canvas 설정
    void SetupStatusCanvas()
    {
        if (followCamera && targetCamera != null)
        {
            Canvas foundCanvas = null;
            
            // statusPanel에서 Canvas 찾기
            if (statusPanel != null)
            {
                foundCanvas = statusPanel.GetComponent<Canvas>();
                if (foundCanvas == null)
                {
                    foundCanvas = statusPanel.GetComponentInParent<Canvas>();
                }
            }
            
            // 스프라이트에서 Canvas 찾기 (statusPanel이 없는 경우)
            if (foundCanvas == null)
            {
                if (greenLightSprite != null)
                {
                    foundCanvas = greenLightSprite.GetComponentInParent<Canvas>();
                }
                if (foundCanvas == null && redLightSprite != null)
                {
                    foundCanvas = redLightSprite.GetComponentInParent<Canvas>();
                }
            }
            
            // Canvas 설정만 변경 (부모는 변경하지 않음)
            if (foundCanvas != null)
            {
                statusCanvas = foundCanvas;
                
                // Canvas 초기 크기와 Scale 저장 (한 번만)
                if (!canvasSizeInitialized)
                {
                    RectTransform canvasRect = statusCanvas.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        initialCanvasSize = canvasRect.sizeDelta;
                        initialCanvasScale = canvasRect.localScale;
                        canvasSizeInitialized = true;
                        Debug.Log($"RedLightGreenLight: Canvas 초기 크기 저장 - {initialCanvasSize}, Scale - {initialCanvasScale}");
                    }
                }
                
                // Canvas를 카메라에 맞춰 설정
                if (statusCanvas.renderMode != RenderMode.WorldSpace)
                {
                    statusCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    statusCanvas.worldCamera = targetCamera;
                }
                else
                {
                    // World Space 모드: World Camera 설정 및 초기 거리 저장
                    statusCanvas.worldCamera = targetCamera;
                    
                    if (!canvasDistanceInitialized)
                    {
                        RectTransform canvasRect = statusCanvas.GetComponent<RectTransform>();
                        if (canvasRect != null && targetCamera != null)
                        {
                            Vector3 canvasPos = canvasRect.position;
                            Vector3 cameraPos = targetCamera.transform.position;
                            Vector3 cameraForward = targetCamera.transform.forward;
                            
                            // Canvas가 카메라 앞에 있는지 확인하고 거리 계산
                            Vector3 toCanvas = canvasPos - cameraPos;
                            float distance = Vector3.Dot(toCanvas, cameraForward);
                            
                            if (distance > 0.01f)
                            {
                                initialCanvasDistance = distance;
                            }
                            else
                            {
                                // Canvas가 카메라 뒤에 있거나 거리가 너무 가까우면 기본값 사용
                                initialCanvasDistance = 10f;
                            }
                            
                            canvasDistanceInitialized = true;
                            Debug.Log($"RedLightGreenLight: Canvas 초기 거리 저장 - {initialCanvasDistance}");
                        }
                    }
                }
            }
            else
            {
                // Canvas가 없으면 새로 만들기 (필요한 경우에만)
                Debug.LogWarning("RedLightGreenLight: Canvas를 찾을 수 없습니다. UI 요소들이 Canvas의 자식인지 확인해주세요.");
            }
        }
    }
    
    // 게임오버 UI Canvas 설정
    void SetupGameOverCanvas()
    {
        if (followCamera && targetCamera != null)
        {
            Canvas foundCanvas = null;
            
            // gameOverPanel에서 Canvas 찾기
            if (gameOverPanel != null)
            {
                foundCanvas = gameOverPanel.GetComponent<Canvas>();
                if (foundCanvas == null)
                {
                    foundCanvas = gameOverPanel.GetComponentInParent<Canvas>();
                }
            }
            
            // 게임오버 텍스트에서 Canvas 찾기 (gameOverPanel이 없는 경우)
            if (foundCanvas == null)
            {
                if (gameOverTextTMP != null)
                {
                    foundCanvas = gameOverTextTMP.GetComponentInParent<Canvas>();
                }
                if (foundCanvas == null && gameOverText != null)
                {
                    foundCanvas = gameOverText.GetComponentInParent<Canvas>();
                }
            }
            
            // Canvas 설정
            if (foundCanvas != null)
            {
                gameOverCanvas = foundCanvas;
                
                // Canvas를 카메라에 맞춰 설정
                if (gameOverCanvas.renderMode != RenderMode.WorldSpace)
                {
                    gameOverCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                    gameOverCanvas.worldCamera = targetCamera;
                }
                else
                {
                    // World Space 모드: World Camera 설정 및 초기 거리 저장
                    gameOverCanvas.worldCamera = targetCamera;
                    
                    if (!gameOverCanvasDistanceInitialized)
                    {
                        RectTransform canvasRect = gameOverCanvas.GetComponent<RectTransform>();
                        if (canvasRect != null && targetCamera != null)
                        {
                            Vector3 canvasPos = canvasRect.position;
                            Vector3 cameraPos = targetCamera.transform.position;
                            Vector3 cameraForward = targetCamera.transform.forward;
                            
                            // Canvas가 카메라 앞에 있는지 확인하고 거리 계산
                            Vector3 toCanvas = canvasPos - cameraPos;
                            float distance = Vector3.Dot(toCanvas, cameraForward);
                            
                            if (distance > 0.01f)
                            {
                                initialGameOverCanvasDistance = distance;
                            }
                            else
                            {
                                // Canvas가 카메라 뒤에 있거나 거리가 너무 가까우면 기본값 사용
                                initialGameOverCanvasDistance = 10f;
                            }
                            
                            gameOverCanvasDistanceInitialized = true;
                            Debug.Log($"RedLightGreenLight: 게임오버 Canvas 초기 거리 저장 - {initialGameOverCanvasDistance}");
                        }
                    }
                }
            }
        }
    }
    
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // PlayerController 먼저 찾기
            playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("RedLightGreenLight: PlayerController를 통해 플레이어를 찾았습니다.");
            }
            else
            {
                // PlayerController02 찾기
                PlayerController02 playerController02 = FindObjectOfType<PlayerController02>();
                if (playerController02 != null)
                {
                    // PlayerController02를 PlayerController로 캐스팅할 수 없으므로
                    // MonoBehaviour를 통해 접근하거나 별도 처리 필요
                    // 일단 Rigidbody2D는 직접 찾을 수 있으므로 playerController는 null로 두고
                    // playerRigidbody만 찾기
                    playerRigidbody = playerController02.GetComponent<Rigidbody2D>();
                    Debug.Log("RedLightGreenLight: PlayerController02를 통해 플레이어를 찾았습니다.");
                }
            }
        }
        else
        {
            // PlayerController 먼저 찾기
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                // PlayerController02 찾기
                PlayerController02 playerController02 = player.GetComponent<PlayerController02>();
                if (playerController02 != null)
                {
                    playerRigidbody = playerController02.GetComponent<Rigidbody2D>();
                    Debug.Log("RedLightGreenLight: PlayerController02를 통해 플레이어를 찾았습니다.");
                }
                else
                {
                    Debug.LogWarning("RedLightGreenLight: 플레이어 오브젝트에 PlayerController 또는 PlayerController02가 없습니다!");
                }
            }
        }
        
        // Rigidbody2D 찾기 (playerController가 있으면)
        if (playerController != null && playerRigidbody == null)
        {
            playerRigidbody = playerController.GetComponent<Rigidbody2D>();
        }
        
        // Rigidbody2D를 직접 찾기 (위에서 못 찾았으면)
        if (playerRigidbody == null)
        {
            if (player != null)
            {
                playerRigidbody = player.GetComponent<Rigidbody2D>();
            }
            else
            {
                playerRigidbody = FindObjectOfType<Rigidbody2D>();
            }
        }
    }
    
    // 초기 위치 저장
    void SaveInitialPositions()
    {
        if (greenLightSprite != null)
        {
            initialGreenSpritePos = greenLightSprite.transform.position;
        }
        if (redLightSprite != null)
        {
            initialRedSpritePos = redLightSprite.transform.position;
        }
        if (statusTextTMP != null)
        {
            initialStatusTextPos = statusTextTMP.transform.position;
        }
        else if (statusText != null)
        {
            initialStatusTextPos = statusText.transform.position;
        }
        initialPositionsSaved = true;
        Debug.Log("RedLightGreenLight: 초기 위치 저장 완료");
    }
    
    void Update()
    {
        if (!isGameActive || isGameOver)
        {
            return;
        }
        
        // 상태 타이머 업데이트
        stateTimer += Time.deltaTime;
        
        // GreenLight 오디오가 재생 중이고 시간이 끝나면 정지
        if (currentState == GameState.GreenLight && adjustGreenLightSoundLength && audioSource != null && audioSource.isPlaying)
        {
            if (stateTimer >= nextStateChangeTime)
            {
                audioSource.Stop();
            }
        }
        
        if (stateTimer >= nextStateChangeTime)
        {
            // 상태 전환
            if (currentState == GameState.GreenLight)
            {
                ChangeState(GameState.RedLight);
            }
            else
            {
                ChangeState(GameState.GreenLight);
            }
        }
        
        // 빨간불일 때 움직임 감지 (딜레이 후)
        if (currentState == GameState.RedLight)
        {
            // 빨간불로 변한 후 딜레이 시간이 지났는지 확인
            if (canDetectMovement)
            {
                CheckPlayerMovement();
            }
            else
            {
                // 딜레이 시간 체크
                if (Time.time - redLightStartTime >= redLightDetectionDelay)
                {
                    canDetectMovement = true;
                    // 딜레이가 끝나면 플레이어 위치 저장
                    if (playerController != null)
                    {
                        lastPlayerPosition = playerController.transform.position;
                    }
                    else
                    {
                        // PlayerController02를 사용하는 경우
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            lastPlayerPosition = player.transform.position;
                        }
                    }
                }
            }
        }
        
        // 결승선 도착 확인
        if (finishLine != null)
        {
            Vector3 playerPos = Vector3.zero;
            if (playerController != null)
            {
                playerPos = playerController.transform.position;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerPos = player.transform.position;
                }
                else
                {
                    return; // 플레이어를 찾을 수 없으면 리턴
                }
            }
            
            float distanceToFinish = Vector2.Distance(playerPos, finishLine.position);
            
            if (distanceToFinish < 1f) // 결승선 도착
            {
                OnGameSuccess();
            }
        }
    }
    
    void LateUpdate()
    {
        // UI가 카메라를 따라가도록 설정
        if (followCamera && targetCamera != null && statusCanvas != null)
        {
            // World Space 모드: Canvas 자체를 카메라 앞에 위치시킴
            if (statusCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransform canvasRect = statusCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Canvas의 World Camera 설정 (매 프레임 확인)
                    if (statusCanvas.worldCamera != targetCamera)
                    {
                        statusCanvas.worldCamera = targetCamera;
                    }
                    
                    // Canvas를 카메라 앞에 위치시킴 (카메라의 forward 방향)
                    Vector3 cameraForward = targetCamera.transform.forward;
                    Vector3 cameraPosition = targetCamera.transform.position;
                    
                    // 초기 거리 사용 (초기화되지 않았으면 기본값 사용)
                    float distanceFromCamera = canvasDistanceInitialized ? initialCanvasDistance : 10f;
                    
                    // Canvas 위치를 카메라 앞에 고정
                    canvasRect.position = cameraPosition + cameraForward * distanceFromCamera;
                    
                    // Canvas가 카메라를 바라보도록 회전
                    canvasRect.LookAt(canvasRect.position + cameraForward);
                }
            }
            else
            {
                // Screen Space 모드: Canvas의 카메라를 업데이트
                if (statusCanvas.worldCamera != targetCamera)
                {
                    statusCanvas.worldCamera = targetCamera;
                }
            }
        }
        
        // 게임오버 UI가 카메라를 따라가도록 설정
        if (isGameOver && followCamera && targetCamera != null && gameOverCanvas != null)
        {
            // World Space 모드: Canvas 자체를 카메라 앞에 위치시킴
            if (gameOverCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransform canvasRect = gameOverCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Canvas의 World Camera 설정 (매 프레임 확인)
                    if (gameOverCanvas.worldCamera != targetCamera)
                    {
                        gameOverCanvas.worldCamera = targetCamera;
                    }
                    
                    // Canvas를 카메라 앞에 위치시킴 (카메라의 forward 방향)
                    Vector3 cameraForward = targetCamera.transform.forward;
                    Vector3 cameraPosition = targetCamera.transform.position;
                    
                    // 초기 거리 사용 (초기화되지 않았으면 기본값 사용)
                    float distanceFromCamera = gameOverCanvasDistanceInitialized ? initialGameOverCanvasDistance : 10f;
                    
                    // Canvas 위치를 카메라 앞에 고정
                    canvasRect.position = cameraPosition + cameraForward * distanceFromCamera;
                    
                    // Canvas가 카메라를 바라보도록 회전
                    canvasRect.LookAt(canvasRect.position + cameraForward);
                }
            }
            else
            {
                // Screen Space 모드: Canvas의 카메라를 업데이트
                if (gameOverCanvas.worldCamera != targetCamera)
                {
                    gameOverCanvas.worldCamera = targetCamera;
                }
            }
        }
        
        // Canvas 크기와 Scale 유지
        if (statusCanvas != null && canvasSizeInitialized)
        {
            RectTransform canvasRect = statusCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                // 크기 유지
                if (canvasRect.sizeDelta != initialCanvasSize)
                {
                    canvasRect.sizeDelta = initialCanvasSize;
                }
                // Scale 유지
                if (canvasRect.localScale != initialCanvasScale)
                {
                    canvasRect.localScale = initialCanvasScale;
                }
            }
        }
        
        // 위치 추적 (게임이 활성화되어 있을 때만)
        if (isGameActive && statusCanvas != null)
        {
            if (followPlayerX)
            {
                UpdateSpritePosition();
            }
            else if (followCameraX && targetCamera != null)
            {
                UpdateSpritePosition();
            }
        }
        else if (!isGameOver)
        {
            // 게임이 비활성화되어 있고 게임오버가 아닐 때만 초기 위치로 복원
            // (게임오버일 때는 현재 위치 유지)
            RestoreInitialPositions();
        }
    }
    
    // 초기 위치로 복원
    void RestoreInitialPositions()
    {
        if (!initialPositionsSaved)
        {
            return;
        }
        
        if (greenLightSprite != null)
        {
            greenLightSprite.transform.position = initialGreenSpritePos;
        }
        if (redLightSprite != null)
        {
            redLightSprite.transform.position = initialRedSpritePos;
        }
        if (statusTextTMP != null)
        {
            statusTextTMP.transform.position = initialStatusTextPos;
        }
        else if (statusText != null)
        {
            statusText.transform.position = initialStatusTextPos;
        }
    }
    
    // 스프라이트 위치를 플레이어 또는 카메라 X축에 맞춰 업데이트
    void UpdateSpritePosition()
    {
        float targetWorldX = 0f;
        
        // 추적할 대상 결정
        if (followPlayerX)
        {
            if (playerController != null)
            {
                targetWorldX = playerController.transform.position.x;
            }
            else
            {
                // PlayerController02를 사용하는 경우
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetWorldX = player.transform.position.x;
                }
                else
                {
                    return; // 플레이어를 찾을 수 없으면 리턴
                }
            }
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
        
        // Canvas 확인
        if (statusCanvas == null)
        {
            // Canvas를 다시 찾기 시도
            SetupStatusCanvas();
            if (statusCanvas == null)
            {
                // Canvas가 없으면 조용히 return (경고는 SetupStatusCanvas에서 이미 출력됨)
                return;
            }
        }
        
        // World Space 모드: 월드 좌표를 직접 사용 (가장 확실한 방법)
        if (statusCanvas.renderMode == RenderMode.WorldSpace)
        {
            // 초록불 스프라이트 위치 업데이트 (Y, Z는 현재 위치 유지)
            if (greenLightSprite != null)
            {
                Transform greenTransform = greenLightSprite.transform;
                Vector3 currentPos = greenTransform.position;
                greenTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
            }
            
            // 빨간불 스프라이트 위치 업데이트 (Y, Z는 현재 위치 유지)
            if (redLightSprite != null)
            {
                Transform redTransform = redLightSprite.transform;
                Vector3 currentPos = redTransform.position;
                redTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
            }
            
            // Status 텍스트 위치 업데이트 (Y, Z는 현재 위치 유지)
            if (statusTextTMP != null)
            {
                Transform textTransform = statusTextTMP.transform;
                Vector3 currentPos = textTransform.position;
                textTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
            }
            else if (statusText != null)
            {
                Transform textTransform = statusText.transform;
                Vector3 currentPos = textTransform.position;
                textTransform.position = new Vector3(targetWorldX, currentPos.y, currentPos.z);
            }
        }
        else
        {
            // Screen Space 모드: 스크린 좌표로 변환
            if (targetCamera == null)
            {
                FindCamera();
                if (targetCamera == null)
                {
                    Debug.LogWarning("RedLightGreenLight: 카메라를 찾을 수 없습니다!");
                    return;
                }
            }
            
            // 추적할 대상의 월드 위치
            float targetWorldY = 0f;
            if (followPlayerX)
            {
                if (playerController != null)
                {
                    targetWorldY = playerController.transform.position.y;
                }
                else
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        targetWorldY = player.transform.position.y;
                    }
                    else
                    {
                        targetWorldY = targetCamera.transform.position.y;
                    }
                }
            }
            else
            {
                targetWorldY = targetCamera.transform.position.y;
            }
            Vector3 targetWorldPos = new Vector3(targetWorldX, targetWorldY, 0);
            Vector3 screenPoint = targetCamera.WorldToScreenPoint(targetWorldPos);
            
            Vector2 canvasPos = Vector2.zero;
            
            RectTransform canvasRect = statusCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, 
                    screenPoint, 
                    statusCanvas.worldCamera != null ? statusCanvas.worldCamera : targetCamera, 
                    out canvasPos
                );
                
                if (!success)
                {
                    // 변환 실패 시 직접 계산
                    canvasPos = new Vector2(screenPoint.x - Screen.width / 2f, screenPoint.y - Screen.height / 2f);
                }
            }
            else
            {
                canvasPos = new Vector2(screenPoint.x, screenPoint.y);
            }
            
            float uiY = (canvasRect != null ? canvasRect.rect.height : Screen.height) - spriteYOffset;
            
            // 초록불 스프라이트 위치 업데이트
            if (greenLightSprite != null)
            {
                RectTransform greenRect = greenLightSprite.GetComponent<RectTransform>();
                if (greenRect != null)
                {
                    greenRect.anchoredPosition = new Vector2(canvasPos.x, uiY);
                }
            }
            
            // 빨간불 스프라이트 위치 업데이트
            if (redLightSprite != null)
            {
                RectTransform redRect = redLightSprite.GetComponent<RectTransform>();
                if (redRect != null)
                {
                    redRect.anchoredPosition = new Vector2(canvasPos.x, uiY);
                }
            }
            
            // Status 텍스트 위치 업데이트
            if (statusTextTMP != null)
            {
                RectTransform textRect = statusTextTMP.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchoredPosition = new Vector2(canvasPos.x, uiY);
                }
            }
            else if (statusText != null)
            {
                RectTransform textRect = statusText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchoredPosition = new Vector2(canvasPos.x, uiY);
                }
            }
        }
    }
    
    // 상태 변경
    void ChangeState(GameState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        
        // 다음 상태 변경 시간 설정
        if (newState == GameState.GreenLight)
        {
            // 랜덤 시간 먼저 결정
            nextStateChangeTime = Random.Range(minGreenLightTime, maxGreenLightTime);
            
            // GreenLight 시간에 맞춰서 오디오 길이 조절
            if (adjustGreenLightSoundLength && greenLightSound != null)
            {
                // 결정된 랜덤 시간에 맞춰서 오디오 pitch 조절
                PlaySoundWithDuration(greenLightSound, nextStateChangeTime);
            }
            else
            {
                PlaySound(greenLightSound);
            }
            
            // 파란불일 때 플레이어 이동 허용 및 움직임 감지 비활성화
            canDetectMovement = false;
            if (playerController != null)
            {
                playerController.SetCanMove(true);
            }
            else
            {
                // PlayerController02 찾기
                PlayerController02 playerController02 = FindObjectOfType<PlayerController02>();
                if (playerController02 != null)
                {
                    playerController02.SetCanMove(true);
                }
            }
        }
        else
        {
            // GreenLight 오디오 정지 및 pitch 원래대로
            if (adjustGreenLightSoundLength && audioSource != null)
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                audioSource.pitch = 1.0f; // pitch를 원래대로 되돌림
            }
            
            nextStateChangeTime = Random.Range(minRedLightTime, maxRedLightTime);
            PlaySound(redLightSound);
            
            // 빨간불로 바뀔 때 시간 기록 및 움직임 감지 딜레이 시작
            redLightStartTime = Time.time;
            canDetectMovement = false; // 딜레이 동안은 움직임 감지 안 함
            
            // 빨간불로 바뀔 때 플레이어 위치 저장
            if (playerController != null)
            {
                lastPlayerPosition = playerController.transform.position;
                // 이동을 완전히 막지는 않고 감지만 함 (게임의 재미를 위해)
                // playerController.SetCanMove(false); // 이 줄을 활성화하면 빨간불일 때 완전히 움직임을 막음
            }
            else
            {
                // PlayerController02를 사용하는 경우
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    lastPlayerPosition = player.transform.position;
                }
            }
        }
        
        // UI 업데이트
        UpdateUI();
    }
    
    // UI 업데이트
    void UpdateUI()
    {
        string statusMessage = "";
        Color statusColor = Color.white;
        
        if (currentState == GameState.GreenLight)
        {
            statusMessage = "Green Light!";
            statusColor = Color.green;
            
            // 초록불 스프라이트 표시, 빨간불 스프라이트 숨김
            if (greenLightSprite != null)
            {
                greenLightSprite.gameObject.SetActive(true);
            }
            if (redLightSprite != null)
            {
                redLightSprite.gameObject.SetActive(false);
            }
        }
        else
        {
            statusMessage = "Red Light!";
            statusColor = Color.red;
            
            // 빨간불 스프라이트 표시, 초록불 스프라이트 숨김
            if (redLightSprite != null)
            {
                redLightSprite.gameObject.SetActive(true);
            }
            if (greenLightSprite != null)
            {
                greenLightSprite.gameObject.SetActive(false);
            }
        }
        
        // TextMeshPro 우선 사용
        if (statusTextTMP != null)
        {
            statusTextTMP.text = statusMessage;
            statusTextTMP.color = statusColor;
        }
        else if (statusText != null)
        {
            statusText.text = statusMessage;
            statusText.color = statusColor;
        }
        
        // 배경 색상 변경
        if (statusBackground != null)
        {
            statusBackground.color = new Color(statusColor.r, statusColor.g, statusColor.b, 0.3f);
        }
    }
    
    // 플레이어 움직임 감지 (속도 기반)
    void CheckPlayerMovement()
    {
        // Rigidbody2D를 통해 속도 확인
        if (playerRigidbody != null)
        {
            // 수평 속도만 확인 (점프는 허용)
            float horizontalVelocity = Mathf.Abs(playerRigidbody.linearVelocity.x);
            
            // 디버그 로그 (필요시 주석 해제)
            // Debug.Log($"RedLightGreenLight: 현재 수평 속도 = {horizontalVelocity}, 임계값 = {velocityThreshold}");
            
            // 수평 속도가 임계값을 넘으면 게임오버
            if (horizontalVelocity > velocityThreshold)
            {
                Debug.Log($"RedLightGreenLight: 속도 감지! 수평 속도 = {horizontalVelocity}, 임계값 = {velocityThreshold}");
                OnPlayerMoved();
            }
        }
        else
        {
            // Rigidbody2D를 다시 찾기 시도
            if (playerController != null)
            {
                playerRigidbody = playerController.GetComponent<Rigidbody2D>();
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerRigidbody = player.GetComponent<Rigidbody2D>();
                }
            }
            
            if (playerRigidbody == null)
            {
                Debug.LogWarning("RedLightGreenLight: Rigidbody2D를 찾을 수 없어 속도 기반 감지를 할 수 없습니다!");
            }
        }
    }
    
    // 플레이어가 움직임
    void OnPlayerMoved()
    {
        if (isGameOver)
        {
            return;
        }
        
        Debug.Log("RedLightGreenLight: 움직임 감지! 게임오버!");
        isGameOver = true;
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(gameOverSound);
        
        // 게임오버 UI 표시
        ShowGameOver();
        
        // 플레이어 이동 비활성화
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 playerController02 = FindObjectOfType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
            }
        }
        
        // 일정 시간 후 씬 전환
        StartCoroutine(GameOverSequence());
    }
    
    // 게임오버 UI 표시
    void ShowGameOver(string message = null)
    {
        // Status UI 숨기기
        if (statusPanel != null)
        {
            statusPanel.SetActive(false);
        }
        if (greenLightSprite != null)
        {
            greenLightSprite.gameObject.SetActive(false);
        }
        if (redLightSprite != null)
        {
            redLightSprite.gameObject.SetActive(false);
        }
        if (statusTextTMP != null)
        {
            statusTextTMP.gameObject.SetActive(false);
        }
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
        if (statusBackground != null)
        {
            statusBackground.gameObject.SetActive(false);
        }
        
        // Timer UI 숨기기
        RedLightGreenLightTimer timer = FindObjectOfType<RedLightGreenLightTimer>();
        if (timer != null)
        {
            timer.HideTimer();
        }
        
        // 게임오버 Canvas 찾기 (아직 찾지 못했으면)
        if (gameOverCanvas == null)
        {
            SetupGameOverCanvas();
        }
        
        // 게임오버 패널 표시
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 메시지가 지정되지 않았으면 기본 메시지 사용
        string displayMessage = message != null ? message : gameOverMessage;
        
        // TextMeshPro 우선 사용
        if (gameOverTextTMP != null)
        {
            gameOverTextTMP.text = displayMessage;
        }
        else if (gameOverText != null)
        {
            gameOverText.text = displayMessage;
        }
    }
    
    // 시간초과로 인한 게임오버 (외부에서 호출 가능)
    public void OnTimeUp()
    {
        if (isGameOver)
        {
            return;
        }
        
        Debug.Log("RedLightGreenLight: 시간 초과! 게임오버!");
        isGameOver = true;
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(gameOverSound);
        
        // 게임오버 UI 표시 (시간초과 메시지)
        ShowGameOver(timeUpMessage);
        
        // 플레이어 이동 비활성화
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 playerController02 = FindObjectOfType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
            }
        }
        
        // 일정 시간 후 씬 전환
        StartCoroutine(GameOverSequence());
    }
    
    // 게임 성공
    void OnGameSuccess()
    {
        if (isGameOver)
        {
            return;
        }
        
        Debug.Log("RedLightGreenLight: 결승선 도착! 성공!");
        isGameActive = false;
        
        // 플레이어 이동 비활성화
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 playerController02 = FindObjectOfType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
            }
        }
        
        // 다음 씬으로 이동
        if (!string.IsNullOrEmpty(successSceneName))
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(successSceneName);
            }
            else
            {
                SceneManager.LoadScene(successSceneName);
            }
        }
    }
    
    // 게임오버 시퀀스
    IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        // 씬 전환
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(gameOverSceneName);
            }
            else
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
        }
        else
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
    }
    
    // 사운드 재생
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // 지정된 시간에 맞춰서 오디오 재생 (pitch 조절)
    void PlaySoundWithDuration(AudioClip clip, float targetDuration)
    {
        if (audioSource != null && clip != null)
        {
            // 오디오 클립의 원본 길이
            float clipLength = clip.length;
            
            // 목표 시간에 맞춰서 pitch 계산
            // pitch = 원본길이 / 목표길이
            // 예: 오디오가 3초인데 5초에 맞추려면 pitch = 3/5 = 0.6 (느리게 재생)
            // 예: 오디오가 3초인데 2초에 맞추려면 pitch = 3/2 = 1.5 (빠르게 재생)
            float calculatedPitch = clipLength / targetDuration;
            
            // pitch 범위 제한
            float finalPitch = Mathf.Clamp(calculatedPitch, minPitch, maxPitch);
            
            // 기존 재생 중인 오디오 정지
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // pitch 설정 및 재생
            audioSource.pitch = finalPitch;
            audioSource.clip = clip;
            audioSource.Play();
            
            Debug.Log($"RedLightGreenLight: 오디오 재생 - 원본 길이: {clipLength}초, 목표 길이: {targetDuration}초, Pitch: {finalPitch}");
        }
    }
    
    // 현재 상태 가져오기
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    // 게임이 활성화되어 있는지 확인
    public bool IsGameActive()
    {
        return isGameActive;
    }
}

