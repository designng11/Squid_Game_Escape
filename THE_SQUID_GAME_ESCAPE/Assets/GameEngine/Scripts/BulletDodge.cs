using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BulletDodge : MonoBehaviour
{
    public static BulletDodge Instance;
    
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab; // 총알 프리팹
    [SerializeField] private float bulletSpeed = 10f; // 총알 속도
    [SerializeField] private float minSpawnInterval = 1f; // 최소 생성 간격 (초)
    [SerializeField] private float maxSpawnInterval = 3f; // 최대 생성 간격 (초)
    
    [Header("Bullet Spawn Position Settings")]
    [SerializeField] private Transform spawnPoint; // 총알 생성 위치 (지정하면 이 위치 사용, 우선순위 높음)
    [SerializeField] private Vector3 fixedSpawnPosition; // 고정 생성 위치 (spawnPoint가 없을 때 사용)
    [SerializeField] private bool useFixedPosition = false; // 고정 위치 사용 여부
    [SerializeField] private float spawnYMin = -3f; // 총알 생성 Y 위치 최소값 (자동 계산 모드)
    [SerializeField] private float spawnYMax = 3f; // 총알 생성 Y 위치 최대값 (자동 계산 모드)
    [SerializeField] private float spawnXOffset = 15f; // 오른쪽에서 생성할 X 오프셋 (카메라 기준, 자동 계산 모드)
    
    [Header("Game Settings")]
    [SerializeField] private Transform finishLine; // 결승선
    [SerializeField] private float finishLineDistance = 1f; // 결승선 도착 거리
    [SerializeField] private bool startOnStart = true; // 시작 시 자동 시작
    [SerializeField] private string deadlyGroundTag = "DeadlyGround"; // 바닥 태그 (닿으면 게임오버)
    
    [Header("Game Over Settings")]
    [SerializeField] private GameObject gameOverPanel; // 게임오버 패널
    [SerializeField] private string gameOverMessage = "총알에 맞았습니다!";
    [SerializeField] private float gameOverDelay = 2f; // 게임오버 후 씬 전환 대기 시간
    [SerializeField] private string gameOverSceneName = ""; // 게임오버 시 이동할 씬
    
    [Header("Success Settings")]
    [SerializeField] private string successSceneName = ""; // 성공 시 이동할 씬
    
    [Header("Sound (Optional)")]
    [SerializeField] private AudioClip bulletSpawnSound; // 총알 생성 사운드
    [SerializeField] private AudioClip hitSound; // 맞았을 때 사운드
    [SerializeField] private AudioClip successSound; // 성공 사운드
    
    private bool isGameActive = false;
    private bool isGameOver = false;
    private PlayerController playerController;
    private Camera targetCamera;
    private AudioSource audioSource;
    private List<GameObject> activeBullets = new List<GameObject>(); // 활성 총알 리스트
    private Canvas gameOverCanvas; // 게임오버 UI의 Canvas
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
        
        // 게임오버 패널 숨기기
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // 게임오버 UI Canvas 설정
        SetupGameOverCanvas();
        
        // 자동 시작
        if (startOnStart)
        {
            StartGame();
        }
    }
    
    void Update()
    {
        if (!isGameActive || isGameOver)
        {
            return;
        }
        
        // 결승선 도착 확인
        if (finishLine != null)
        {
            Vector3 playerPosition = Vector3.zero;
            bool playerFound = false;
            
            if (playerController != null)
            {
                playerPosition = playerController.transform.position;
                playerFound = true;
            }
            else
            {
                // PlayerController02를 사용하는 경우
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerPosition = player.transform.position;
                    playerFound = true;
                }
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
    }
    
    // 플레이어 찾기
    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // PlayerController 먼저 찾기
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("BulletDodge: PlayerController를 통해 플레이어를 찾았습니다.");
            }
            else
            {
                // PlayerController02 찾기
                PlayerController02 playerController02 = FindFirstObjectByType<PlayerController02>();
                if (playerController02 != null)
                {
                    // PlayerController02를 PlayerController로 캐스팅할 수 없으므로
                    // null로 두고 플레이어 GameObject를 직접 사용
                    Debug.Log("BulletDodge: PlayerController02를 통해 플레이어를 찾았습니다.");
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
                    Debug.Log("BulletDodge: PlayerController02를 통해 플레이어를 찾았습니다.");
                }
                else
                {
                    Debug.LogWarning("BulletDodge: 플레이어 오브젝트에 PlayerController 또는 PlayerController02가 없습니다!");
                }
            }
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
                        Debug.Log($"BulletDodge: 게임오버 Canvas 초기 거리 저장 - {initialGameOverCanvasDistance}");
                    }
                }
            }
        }
    }
    
    // 게임 시작
    public void StartGame()
    {
        if (isGameActive)
        {
            return;
        }
        
        isGameActive = true;
        isGameOver = false;
        
        // 총알 생성 시작
        StartCoroutine(SpawnBullets());
        
        Debug.Log("BulletDodge: 게임 시작!");
    }
    
    // 게임 정지
    public void StopGame()
    {
        isGameActive = false;
        StopAllCoroutines();
        
        // 모든 총알 제거
        ClearAllBullets();
        
        Debug.Log("BulletDodge: 게임 정지!");
    }
    
    // 총알 생성 코루틴
    IEnumerator SpawnBullets()
    {
        while (isGameActive && !isGameOver)
        {
            // 생성 간격 랜덤
            float spawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(spawnInterval);
            
            // 게임이 활성화되어 있을 때만 총알 생성
            if (isGameActive && !isGameOver)
            {
                SpawnBullet();
            }
        }
    }
    
    // 총알 생성
    void SpawnBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("BulletDodge: 총알 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        Vector3 spawnPosition;
        
        // 1순위: Transform으로 지정된 생성 위치 사용
        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
            
            // Y 위치에 랜덤성 추가 (선택사항)
            if (spawnYMin != spawnYMax)
            {
                float randomY = Random.Range(spawnYMin, spawnYMax);
                spawnPosition = new Vector3(spawnPosition.x, spawnPosition.y + randomY, spawnPosition.z);
            }
        }
        // 2순위: 고정 위치 사용
        else if (useFixedPosition)
        {
            spawnPosition = fixedSpawnPosition;
            
            // Y 위치에 랜덤성 추가 (선택사항)
            if (spawnYMin != spawnYMax)
            {
                float randomY = Random.Range(spawnYMin, spawnYMax);
                spawnPosition = new Vector3(spawnPosition.x, spawnPosition.y + randomY, spawnPosition.z);
            }
        }
        // 3순위: 자동 계산 (카메라/플레이어 기준)
        else
        {
            // 카메라 위치 기준으로 오른쪽에서 생성
            float spawnX = 0f;
            if (targetCamera != null)
            {
                // 카메라 오른쪽 끝 위치 계산
                Vector3 cameraRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, targetCamera.nearClipPlane));
                spawnX = cameraRight.x + spawnXOffset;
            }
            else
            {
                // 카메라가 없으면 플레이어 기준으로 생성
                if (playerController != null)
                {
                    spawnX = playerController.transform.position.x + spawnXOffset;
                }
            }
            
            // Y 위치 랜덤
            float spawnY = Random.Range(spawnYMin, spawnYMax);
            spawnPosition = new Vector3(spawnX, spawnY, 0f);
        }
        
        // 총알 생성
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        activeBullets.Add(bullet);
        
        // 총알에 Bullet 컴포넌트 추가 (없으면)
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<Bullet>();
        }
        
        // 총알 설정
        bulletScript.Initialize(bulletSpeed, this);
        
        // 총알 경로 시각화
        DrawBulletPath(bullet, spawnPosition);
        
        // 사운드 재생
        PlaySound(bulletSpawnSound);
        
        Debug.Log($"BulletDodge: 총알 생성! 위치: {spawnPosition}");
    }

    // 총알 경로 표시 (LineRenderer)
    void DrawBulletPath(GameObject bullet, Vector3 spawnPosition)
    {
        if (bullet == null)
        {
            return;
        }

        // 경로 길이 계산 (카메라 왼쪽 영역까지)
        // 기본 길이
        float pathLength = 50f;
        if (targetCamera != null)
        {
            // 화면 왼쪽을 훨씬 넘어가도록 여유를 둠 (-0.5f)
            Vector3 leftBound = targetCamera.ViewportToWorldPoint(new Vector3(-0.5f, 0.5f, targetCamera.nearClipPlane));
            pathLength = Mathf.Abs(spawnPosition.x - leftBound.x) + 20f;
        }

        Vector3 endPos = spawnPosition + Vector3.left * pathLength;
        endPos.z = spawnPosition.z;

        // LineRenderer 설정
        LineRenderer line = bullet.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = bullet.AddComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;

        // 머티리얼/색상 설정
        if (line.material == null)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }
        line.startColor = new Color(1f, 1f, 1f, 0.8f);
        line.endColor = new Color(1f, 1f, 1f, 0.2f);

        line.SetPosition(0, spawnPosition);
        line.SetPosition(1, endPos);

        // 라인 빠른 페이드아웃
        StartCoroutine(FadeAndDisableLine(line, .5f));
    }

    // LineRenderer를 빠르게 페이드아웃 후 숨김
    IEnumerator FadeAndDisableLine(LineRenderer line, float duration)
    {
        if (line == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Color startColor = line.startColor;
        Color endColor = line.endColor;

        while (elapsed < duration && line != null)
        {
            float t = elapsed / duration;
            Color cStart = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), t);
            Color cEnd = Color.Lerp(endColor, new Color(endColor.r, endColor.g, endColor.b, 0f), t);
            line.startColor = cStart;
            line.endColor = cEnd;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (line != null)
        {
            line.startColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            line.endColor = new Color(endColor.r, endColor.g, endColor.b, 0f);
            line.enabled = false;
        }
    }
    
    // 총알이 플레이어와 충돌
    public void OnBulletHitPlayer(GameObject bullet)
    {
        if (isGameOver)
        {
            return;
        }
        
        Debug.Log("BulletDodge: 플레이어가 총알에 맞았습니다!");
        isGameOver = true;
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(hitSound);
        
        // 게임오버 UI 표시
        ShowGameOver();
        
        // 플레이어 이동 비활성화 및 Dead 파라미터 설정
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetDead(true);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 playerController02 = FindFirstObjectByType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
                playerController02.SetDead(true);
            }
        }
        
        // 모든 총알 제거
        ClearAllBullets();
        
        // 게임오버 Canvas 찾기 (아직 찾지 못했으면)
        if (gameOverCanvas == null)
        {
            SetupGameOverCanvas();
        }
        
        // 일정 시간 후 씬 전환
        StartCoroutine(GameOverSequence());
    }

    // 플레이어가 DeadlyGround에 닿았을 때 게임오버
    public void OnPlayerHitDeadlyGround()
    {
        if (isGameOver)
        {
            return;
        }

        Debug.Log("BulletDodge: 플레이어가 DeadlyGround에 닿았습니다! 게임오버!");
        isGameOver = true;
        isGameActive = false;

        // 사운드 재생
        PlaySound(hitSound);

        // 플레이어 이동 비활성화 및 Dead 파라미터 설정
        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetDead(true);
        }
        else
        {
            PlayerController02 playerController02 = FindFirstObjectByType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
                playerController02.SetDead(true);
            }
        }

        // 게임오버 UI 표시
        ShowGameOver();

        // 모든 총알 제거
        ClearAllBullets();

        // 게임오버 Canvas 찾기 (아직 찾지 못했으면)
        if (gameOverCanvas == null)
        {
            SetupGameOverCanvas();
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
        
        Debug.Log("BulletDodge: 결승선 도착! 성공!");
        isGameActive = false;
        
        // 사운드 재생
        PlaySound(successSound);
        
        // 플레이어 이동 비활성화
        if (playerController != null)
        {
            playerController.SetCanMove(false);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 playerController02 = FindFirstObjectByType<PlayerController02>();
            if (playerController02 != null)
            {
                playerController02.SetCanMove(false);
            }
        }
        
        // 모든 총알 제거
        ClearAllBullets();
        
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
    }
    
    // 게임오버 UI 표시
    void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
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
    
    // 모든 총알 제거
    void ClearAllBullets()
    {
        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null)
            {
                Destroy(bullet);
            }
        }
        activeBullets.Clear();
    }
    
    // 총알 제거 (총알이 화면 밖으로 나갔을 때)
    public void RemoveBullet(GameObject bullet)
    {
        if (activeBullets.Contains(bullet))
        {
            activeBullets.Remove(bullet);
        }
        if (bullet != null)
        {
            Destroy(bullet);
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
    
    // 게임이 활성화되어 있는지 확인
    public bool IsGameActive()
    {
        return isGameActive;
    }
    
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
    }
}

