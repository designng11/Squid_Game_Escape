using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GlassBridgeGame : MonoBehaviour
{
    [Header("플레이트 설정")]
    [SerializeField] private GameObject[] plates = new GameObject[9]; // 총 9개의 플레이트
    [SerializeField] private int[] correctChoices = new int[9]; // 각 플레이트의 정답 (1 또는 2)
    
    [Header("게임 설정")]
    [SerializeField] private KeyCode choice1Key = KeyCode.Alpha1; // 1번 키
    [SerializeField] private KeyCode choice2Key = KeyCode.Alpha2; // 2번 키
    [SerializeField] private string plateTag = "Plate"; // 플레이트 태그
    [SerializeField] private string deadlyGroundTag = "DeadlyGround"; // 바닥 태그 (바닥에 닿으면 죽음)
    [SerializeField] private Transform finishLine; // 결승선
    [SerializeField] private float finishLineDistance = 1f; // 결승선 도착 거리
    
    [Header("게임오버 설정")]
    [SerializeField] private string gameOverSceneName = ""; // 게임오버 시 이동할 씬
    [SerializeField] private float gameOverDelay = 2f; // 게임오버 후 씬 전환 대기 시간
    [SerializeField] private AudioClip wrongChoiceSound; // 잘못된 선택 사운드
    [SerializeField] private AudioClip correctChoiceSound; // 올바른 선택 사운드
    
    [Header("성공 설정")]
    [SerializeField] private string successSceneName = ""; // 성공 시 이동할 씬
    
    [Header("UI 설정")]
    [SerializeField] private GameObject gameOverPanel; // 게임오버 패널
    [SerializeField] private string gameOverMessage = "잘못된 선택입니다!";
    [SerializeField] private GameObject selectionHint; // 선택 힌트 UI (예: "1 또는 2를 눌러 선택하세요")
    [SerializeField] private GameObject hintTextPanel; // 힌트 텍스트 패널 (GameOver와 같은 방식)
    [SerializeField] private string selectionHintText = "1 또는 2를 눌러 선택하세요"; // 선택 힌트 텍스트
    
    private Transform playerTransform;
    private PlayerController playerController;
    private PlayerController02 playerController02;
    private Camera targetCamera;
    private int currentPlateIndex = 0; // 현재 플레이어가 있는 플레이트 인덱스
    private bool isGameActive = true;
    private bool isGameOver = false;
    private bool canSelect = false; // 현재 선택 가능한 상태인지
    private GameObject currentPlateOn = null; // 현재 플레이어가 올라가 있는 플레이트
    private AudioSource audioSource;
    private Canvas gameOverCanvas; // 게임오버 UI의 Canvas
    private float initialGameOverCanvasDistance = 10f; // 게임오버 Canvas와 카메라 간 초기 거리
    private bool gameOverCanvasDistanceInitialized = false; // 게임오버 Canvas 거리 초기화 여부
    private Canvas hintTextCanvas = null; // 힌트 텍스트 Canvas
    private TextMeshProUGUI hintTextTMP = null; // 힌트 텍스트 (TextMeshPro)
    private Text hintTextLegacy = null; // 힌트 텍스트 (기존 Text)
    private float initialHintTextCanvasDistance = 10f; // 힌트 텍스트 Canvas와 카메라 간 초기 거리
    private bool hintTextCanvasDistanceInitialized = false; // 힌트 텍스트 Canvas 거리 초기화 여부
    
    void Start()
    {
        // 플레이어 찾기
        FindPlayer();
        
        // 카메라 찾기
        FindCamera();
        
        // AudioSource 초기화
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 플레이트 배열 초기화 확인
        if (plates == null || plates.Length != 9)
        {
            Debug.LogWarning("GlassBridgeGame: 플레이트가 9개 설정되지 않았습니다!");
        }
        
        // 정답 배열 초기화 확인
        if (correctChoices == null || correctChoices.Length != 9)
        {
            Debug.LogWarning("GlassBridgeGame: 정답 배열이 9개 설정되지 않았습니다!");
            // 기본값으로 초기화 (모두 1번)
            correctChoices = new int[9] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        }
        
        // 정답이 1 또는 2인지 확인
        for (int i = 0; i < correctChoices.Length; i++)
        {
            if (correctChoices[i] != 1 && correctChoices[i] != 2)
            {
                Debug.LogWarning($"GlassBridgeGame: 플레이트 {i}의 정답이 1 또는 2가 아닙니다! 기본값 1로 설정합니다.");
                correctChoices[i] = 1;
            }
        }
        
        // UI 초기화
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (selectionHint != null)
        {
            selectionHint.SetActive(false);
        }
        
        // 힌트 텍스트 패널 초기화
        if (hintTextPanel != null)
        {
            hintTextPanel.SetActive(false);
        }
        
        // 게임오버 UI Canvas 설정
        SetupGameOverCanvas();
        
        // 힌트 텍스트 Canvas 설정
        SetupHintTextCanvas();
    }
    
    void Update()
    {
        // 플레이어가 없으면 찾기
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // 게임이 종료되었으면 입력 무시
        if (!isGameActive || isGameOver)
        {
            return;
        }
        
        // 결승선 도착 확인 (모든 플레이트를 통과한 후에만)
        if (finishLine != null && currentPlateIndex >= plates.Length)
        {
            Vector3 playerPosition = Vector3.zero;
            bool playerFound = false;
            
            if (playerController != null)
            {
                playerPosition = playerController.transform.position;
                playerFound = true;
            }
            else if (playerController02 != null)
            {
                playerPosition = playerController02.transform.position;
                playerFound = true;
            }
            else if (playerTransform != null)
            {
                playerPosition = playerTransform.position;
                playerFound = true;
            }
            
            if (playerFound)
            {
                float distanceToFinish = Vector2.Distance(playerPosition, finishLine.position);
                
                if (distanceToFinish < finishLineDistance)
                {
                    OnGameSuccess();
                }
            }
        }
        
        // 선택 가능한 상태일 때만 키 입력 처리
        if (canSelect)
        {
            if (Input.GetKeyDown(choice1Key))
            {
                OnChoiceSelected(1);
            }
            else if (Input.GetKeyDown(choice2Key))
            {
                OnChoiceSelected(2);
            }
        }
    }
    
    // 플레이어 찾기
    void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // PlayerController 먼저 찾기
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                    Debug.Log("GlassBridgeGame: PlayerController를 통해 플레이어를 찾았습니다.");
                }
                else
                {
                    // PlayerController02 찾기
                    playerController02 = FindFirstObjectByType<PlayerController02>();
                    if (playerController02 != null)
                    {
                        playerTransform = playerController02.transform;
                        Debug.Log("GlassBridgeGame: PlayerController02를 통해 플레이어를 찾았습니다.");
                    }
                    else
                    {
                        Debug.LogWarning("GlassBridgeGame: 플레이어를 찾을 수 없습니다!");
                    }
                }
            }
            else
            {
                playerTransform = player.transform;
                playerController = player.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    playerController02 = player.GetComponent<PlayerController02>();
                }
            }
        }
    }
    
    // 플레이어가 플레이트 위에 올라갔을 때 호출 (플레이어 컨트롤러에서 호출)
    public void OnPlayerEnterPlate(GameObject plate)
    {
        if (isGameOver || !isGameActive)
        {
            return;
        }
        
        // 플레이트가 배열에 있는지 확인하고 인덱스 찾기
        int plateIndex = -1;
        for (int i = 0; i < plates.Length; i++)
        {
            if (plates[i] == plate)
            {
                plateIndex = i;
                break;
            }
        }
        
        // 현재 플레이트 인덱스와 일치하는지 확인
        if (plateIndex == currentPlateIndex && plateIndex >= 0)
        {
            currentPlateOn = plate;
            canSelect = true;
            
            // 플레이트 위에 있으면 이동 제한
            RestrictPlayerMovement();
            
            // UI 힌트 표시
            if (selectionHint != null)
            {
                selectionHint.SetActive(true);
            }
            
            // 힌트 텍스트 표시 (GameOver와 같은 방식)
            ShowHintText();
            
            Debug.Log($"GlassBridgeGame: 플레이어가 플레이트 {currentPlateIndex + 1} 위에 올라갔습니다.");
        }
        else if (plateIndex >= 0)
        {
            // 다른 플레이트 위에 올라간 경우 (이미 통과한 플레이트)
            Debug.Log($"GlassBridgeGame: 플레이어가 플레이트 {plateIndex + 1} 위에 있지만, 현재 진행 중인 플레이트는 {currentPlateIndex + 1}입니다.");
        }
    }
    
    // 플레이어가 플레이트에서 내려갔을 때 호출 (플레이어 컨트롤러에서 호출)
    public void OnPlayerExitPlate(GameObject plate)
    {
        if (currentPlateOn == plate)
        {
            currentPlateOn = null;
            canSelect = false;
            
            // 플레이트 위에 없으면 이동 가능
            RestorePlayerMovement();
            
            // UI 힌트 숨기기
            if (selectionHint != null)
            {
                selectionHint.SetActive(false);
            }
            
            // 힌트 텍스트 숨기기
            HideHintText();
            
            Debug.Log($"GlassBridgeGame: 플레이어가 플레이트에서 내려갔습니다.");
        }
    }
    
    // 힌트 텍스트 Canvas 설정 (GameOver와 같은 방식)
    void SetupHintTextCanvas()
    {
        if (targetCamera != null && hintTextPanel != null)
        {
            Canvas foundCanvas = null;
            
            // hintTextPanel에서 Canvas 찾기
            foundCanvas = hintTextPanel.GetComponent<Canvas>();
            if (foundCanvas == null)
            {
                foundCanvas = hintTextPanel.GetComponentInParent<Canvas>();
            }
            
            // Canvas 설정
            if (foundCanvas != null)
            {
                hintTextCanvas = foundCanvas;
                
                // Canvas를 World Space 모드로 설정
                hintTextCanvas.renderMode = RenderMode.WorldSpace;
                hintTextCanvas.worldCamera = targetCamera;
                
                // 초기 거리 저장
                if (!hintTextCanvasDistanceInitialized)
                {
                    RectTransform canvasRect = hintTextCanvas.GetComponent<RectTransform>();
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
                            initialHintTextCanvasDistance = distance;
                        }
                        else
                        {
                            // Canvas가 카메라 뒤에 있거나 거리가 너무 가까우면 기본값 사용
                            initialHintTextCanvasDistance = 10f;
                        }
                        
                        hintTextCanvasDistanceInitialized = true;
                        Debug.Log($"GlassBridgeGame: 힌트 텍스트 Canvas 초기 거리 저장 - {initialHintTextCanvasDistance}");
                    }
                }
                
                // TextMeshPro 또는 Text 컴포넌트 찾기
                hintTextTMP = hintTextPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                if (hintTextTMP == null)
                {
                    hintTextLegacy = hintTextPanel.GetComponentInChildren<Text>(true);
                }
            }
        }
    }
    
    // 힌트 텍스트 표시 (GameOver와 같은 방식)
    void ShowHintText()
    {
        if (hintTextPanel != null)
        {
            hintTextPanel.SetActive(true);
            
            // 텍스트 설정
            if (hintTextTMP != null)
            {
                hintTextTMP.text = selectionHintText;
            }
            else if (hintTextLegacy != null)
            {
                hintTextLegacy.text = selectionHintText;
            }
        }
    }
    
    // 힌트 텍스트 숨기기
    void HideHintText()
    {
        if (hintTextPanel != null)
        {
            hintTextPanel.SetActive(false);
        }
    }
    
    // 플레이어 이동 제한 (플레이트 위에 있을 때)
    void RestrictPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else if (playerController02 != null)
        {
            playerController02.SetCanMove(false);
        }
    }
    
    // 플레이어 이동 복원 (플레이트 위에 없을 때)
    void RestorePlayerMovement()
    {
        if (!isGameOver && isGameActive)
        {
            if (playerController != null)
            {
                playerController.SetCanMove(true);
            }
            else if (playerController02 != null)
            {
                playerController02.SetCanMove(true);
            }
        }
    }
    
    // 선택 처리
    void OnChoiceSelected(int choice)
    {
        if (!canSelect || currentPlateIndex >= plates.Length)
        {
            return;
        }
        
        int correctChoice = correctChoices[currentPlateIndex];
        
        if (choice == correctChoice)
        {
            // 올바른 선택
            OnCorrectChoice();
        }
        else
        {
            // 잘못된 선택
            OnWrongChoice();
        }
    }
    
    // 올바른 선택 처리
    void OnCorrectChoice()
    {
        Debug.Log($"GlassBridgeGame: 플레이트 {currentPlateIndex + 1}에서 올바른 선택을 했습니다!");
        
        // 사운드 재생
        PlaySound(correctChoiceSound);
        
        // 플레이어 이동 복원 (올바른 선택을 했으므로 다음 플레이트로 이동 가능)
        RestorePlayerMovement();
        
        // 현재 플레이트 참조 초기화
        currentPlateOn = null;
        canSelect = false;
        
        // 선택 힌트 숨기기
        if (selectionHint != null)
        {
            selectionHint.SetActive(false);
        }
        
        // 힌트 텍스트 숨기기
        HideHintText();
        
        // 다음 플레이트로 이동
        currentPlateIndex++;
        
        // 모든 플레이트를 통과했는지 확인 (성공 표시만, 씬 전환은 finishLine 도착 시)
        if (currentPlateIndex >= plates.Length)
        {
            Debug.Log("GlassBridgeGame: 모든 플레이트를 통과했습니다! 이제 finishLine으로 이동하세요!");
            // 씬 전환은 하지 않고, finishLine 도착 시 OnGameSuccess()가 호출됨
        }
    }
    
    // 잘못된 선택 처리
    void OnWrongChoice()
    {
        Debug.Log($"GlassBridgeGame: 플레이트 {currentPlateIndex + 1}에서 잘못된 선택을 했습니다!");
        
        // 현재 플레이트 삭제
        if (currentPlateIndex < plates.Length && plates[currentPlateIndex] != null)
        {
            Destroy(plates[currentPlateIndex]);
            Debug.Log($"GlassBridgeGame: 잘못된 플레이트 {currentPlateIndex + 1}를 삭제했습니다.");
        }
        
        // 사운드 재생
        PlaySound(wrongChoiceSound);
        
        // 플레이어 이동 복원 (플레이트가 삭제되어 떨어지도록)
        RestorePlayerMovement();
        
        // 현재 플레이트 참조 초기화
        currentPlateOn = null;
        canSelect = false;
        
        // 선택 힌트 숨기기
        if (selectionHint != null)
        {
            selectionHint.SetActive(false);
        }
        
        // 힌트 텍스트 숨기기
        HideHintText();
        
        // 게임오버는 하지 않고, 플레이어가 바닥에 닿으면 OnPlayerHitGround()에서 처리됨
        Debug.Log("GlassBridgeGame: 플레이트가 삭제되었습니다. 바닥에 닿으면 게임오버됩니다.");
    }
    
    // 게임 성공
    void OnGameSuccess()
    {
        if (isGameOver)
        {
            return;
        }
        
        Debug.Log("GlassBridgeGame: 결승선 도착! 성공!");
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(correctChoiceSound);
        
        // 플레이어 이동 비활성화
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else if (playerController02 != null)
        {
            playerController02.SetCanMove(false);
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(successSceneName);
            }
        }
        else
        {
            Debug.Log("GlassBridgeGame: 성공 씬 이름이 설정되지 않았습니다!");
        }
    }
    
    // 게임오버 UI 표시
    void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // 선택 힌트 숨기기
        if (selectionHint != null)
        {
            selectionHint.SetActive(false);
        }
    }
    
    // 게임오버 시퀀스
    System.Collections.IEnumerator GameOverSequence()
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
            }
        }
        else
        {
            // 현재 씬 재시작
            UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(currentScene.name);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.name);
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
    
    // 카메라 찾기
    void FindCamera()
    {
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    // 게임오버 UI Canvas 설정
    void SetupGameOverCanvas()
    {
        if (targetCamera != null && gameOverPanel != null)
        {
            Canvas foundCanvas = null;
            
            // gameOverPanel에서 Canvas 찾기
            foundCanvas = gameOverPanel.GetComponent<Canvas>();
            if (foundCanvas == null)
            {
                foundCanvas = gameOverPanel.GetComponentInParent<Canvas>();
            }
            
            // Canvas 설정
            if (foundCanvas != null)
            {
                gameOverCanvas = foundCanvas;
                
                // Canvas를 World Space 모드로 설정
                gameOverCanvas.renderMode = RenderMode.WorldSpace;
                gameOverCanvas.worldCamera = targetCamera;
                
                // 초기 거리 저장
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
                        Debug.Log($"GlassBridgeGame: 게임오버 Canvas 초기 거리 저장 - {initialGameOverCanvasDistance}");
                    }
                }
            }
        }
    }
    
    // 플레이어가 바닥에 닿았을 때 처리 (외부에서 호출 가능, 예: 플레이어 컨트롤러의 OnCollisionEnter2D에서)
    public void OnPlayerHitGround()
    {
        Debug.Log("GlassBridgeGame: 플레이어가 바닥에 닿았습니다! 게임오버!");
        
        isGameOver = true;
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(wrongChoiceSound);
        
        // 플레이어 이동 비활성화 및 Dead 파라미터 설정
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetDead(true);
        }
        else if (playerController02 != null)
        {
            playerController02.SetCanMove(false);
            playerController02.SetDead(true);
        }
        
        // 게임오버 UI 표시
        ShowGameOver();
        
        // 일정 시간 후 씬 전환
        StartCoroutine(GameOverSequence());
    }
    
    // 게임오버 UI와 힌트 텍스트가 카메라를 따라가도록 설정
    void LateUpdate()
    {
        // 게임오버 UI가 카메라의 X축을 따라가도록 설정 (World Space 모드)
        if (isGameOver && targetCamera != null && gameOverCanvas != null)
        {
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
                    
                    // 카메라의 X 위치 가져오기
                    float cameraX = targetCamera.transform.position.x;
                    Vector3 cameraForward = targetCamera.transform.forward;
                    Vector3 cameraPosition = targetCamera.transform.position;
                    
                    // 초기 거리 사용
                    float distanceFromCamera = gameOverCanvasDistanceInitialized ? initialGameOverCanvasDistance : 10f;
                    
                    // Canvas 위치를 카메라 앞에 고정 (X는 카메라 X, Y와 Z는 초기 위치 유지)
                    Vector3 currentPos = canvasRect.position;
                    Vector3 newPosition = cameraPosition + cameraForward * distanceFromCamera;
                    newPosition.x = cameraX; // 카메라의 X 위치로 설정
                    canvasRect.position = newPosition;
                    
                    // Canvas가 카메라를 바라보도록 회전
                    canvasRect.LookAt(canvasRect.position + cameraForward);
                }
            }
        }
        
        // 힌트 텍스트가 카메라의 X축을 따라가도록 설정 (World Space 모드)
        if (canSelect && targetCamera != null && hintTextCanvas != null && hintTextPanel != null && hintTextPanel.activeSelf)
        {
            if (hintTextCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransform canvasRect = hintTextCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Canvas의 World Camera 설정 (매 프레임 확인)
                    if (hintTextCanvas.worldCamera != targetCamera)
                    {
                        hintTextCanvas.worldCamera = targetCamera;
                    }
                    
                    // 카메라의 X 위치 가져오기
                    float cameraX = targetCamera.transform.position.x;
                    Vector3 cameraForward = targetCamera.transform.forward;
                    Vector3 cameraPosition = targetCamera.transform.position;
                    
                    // 초기 거리 사용
                    float distanceFromCamera = hintTextCanvasDistanceInitialized ? initialHintTextCanvasDistance : 10f;
                    
                    // Canvas 위치를 카메라 앞에 고정 (X는 카메라 X, Y와 Z는 초기 위치 유지)
                    Vector3 currentPos = canvasRect.position;
                    Vector3 newPosition = cameraPosition + cameraForward * distanceFromCamera;
                    newPosition.x = cameraX; // 카메라의 X 위치로 설정
                    canvasRect.position = newPosition;
                    
                    // Canvas가 카메라를 바라보도록 회전
                    canvasRect.LookAt(canvasRect.position + cameraForward);
                }
            }
        }
    }
    
    // 게임 재시작 (외부에서 호출 가능)
    public void RestartGame()
    {
        currentPlateIndex = 0;
        isGameActive = true;
        isGameOver = false;
        canSelect = false;
        currentPlateOn = null;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (selectionHint != null)
        {
            selectionHint.SetActive(false);
        }
        
        // 플레이어 이동 활성화
        if (playerController != null)
        {
            playerController.SetCanMove(true);
            playerController.SetDead(false);
        }
        else if (playerController02 != null)
        {
            playerController02.SetCanMove(true);
            playerController02.SetDead(false);
        }
    }
}

