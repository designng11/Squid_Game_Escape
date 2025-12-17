using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    
    [Header("UI 설정")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text dialogueText; // 기존 Text (선택사항)
    [SerializeField] private TextMeshProUGUI dialogueTextTMP; // TextMeshPro (선택사항)
    [SerializeField] private Text speakerNameText; // 기존 Text (선택사항)
    [SerializeField] private TextMeshProUGUI speakerNameTextTMP; // TextMeshPro (선택사항)
    [SerializeField] private Button nextButton;
    
    [Header("대화 설정")]
    [SerializeField] private float typingSpeed = 0.05f; // 텍스트 타이핑 속도
    
    private string[] currentDialogue;
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private string nextSceneName = ""; // 대화 후 이동할 씬 이름
    
    void Awake()
    {
        // 이미 인스턴스가 있고 그것이 파괴되지 않았는지 확인
        if (Instance != null && Instance != this)
        {
            // 기존 인스턴스가 유효하면 새 인스턴스 파괴
            Debug.LogWarning("DialogueManager: 이미 인스턴스가 존재합니다. 중복 인스턴스를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
        
        // 인스턴스가 없거나 현재 인스턴스인 경우
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
            Debug.Log("DialogueManager: 인스턴스가 생성되고 DontDestroyOnLoad로 설정되었습니다.");
        }
    }
    
    void Start()
    {
        // dialoguePanel 찾기
        if (dialoguePanel == null)
        {
            FindDialoguePanelInScene();
        }
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // 씬이 로드될 때 호출되는 메서드
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Instance가 유효한지 확인 (파괴되지 않았는지)
        if (Instance == null)
        {
            Debug.LogWarning("DialogueManager: Instance가 null입니다. 씬 로드 이벤트를 무시합니다.");
            return;
        }
        
        // 현재 인스턴스가 아닌 경우 무시
        if (Instance != this)
        {
            return;
        }
        
        // 기존 dialoguePanel이 있으면 먼저 닫기
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        // 씬이 바뀌면 dialoguePanel 참조 초기화 (새 씬의 오브젝트를 참조하도록)
        dialoguePanel = null;
        dialogueText = null;
        dialogueTextTMP = null;
        speakerNameText = null;
        speakerNameTextTMP = null;
        nextButton = null;
        
        // 씬이 완전히 로드된 후 대화창 닫기 (코루틴으로 지연)
        StartCoroutine(CloseDialogueOnSceneLoad());
    }
    
    // 씬 로드 후 대화창 닫기 (코루틴)
    IEnumerator CloseDialogueOnSceneLoad()
    {
        // 여러 프레임 대기하여 씬이 완전히 로드되도록 함
        yield return null;
        yield return null;
        
        // 현재 씬 이름 확인
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        
        // 모든 씬에서 dialoguePanel 찾기 (대화를 열기 위해 필요)
        FindDialoguePanelInScene();
        
        // Stage_01 씬에서만 대화창을 닫기
        if (sceneName.Contains("Stage_01") || sceneName.Contains("stage_01"))
        {
            Debug.Log($"DialogueManager: {sceneName} 씬에서 대화창을 닫습니다.");
            
            // 모든 활성화된 대화창 비활성화 (강제로)
            ForceCloseAllDialoguePanels();
            
            // 패널을 닫은 후 다시 찾기 (비활성화된 것도 찾을 수 있도록)
            FindDialoguePanelInScene();
            
            // 대화창 닫기
            CloseDialogue();
            
            // 한 번 더 확인 (혹시 모를 경우를 위해)
            yield return new WaitForSeconds(0.1f);
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(false);
                Debug.Log("DialogueManager: 추가 확인 후 대화창을 닫았습니다.");
            }
        }
        else
        {
            Debug.Log($"DialogueManager: {sceneName} 씬이므로 대화창을 닫지 않습니다.");
        }
    }
    
    // 모든 대화창 강제로 닫기
    void ForceCloseAllDialoguePanels()
    {
        // 1. "DialoguePanel" 이름으로 직접 찾기
        GameObject foundPanel = GameObject.Find("DialoguePanel");
        if (foundPanel != null && foundPanel.activeSelf)
        {
            foundPanel.SetActive(false);
            Debug.Log("DialogueManager: DialoguePanel을 찾아서 닫았습니다.");
        }
        
        // 2. 태그로 찾기 (태그가 정의되어 있지 않을 수 있으므로 try-catch 사용)
        try
        {
            GameObject foundPanelByTag = GameObject.FindGameObjectWithTag("DialoguePanel");
            if (foundPanelByTag != null && foundPanelByTag.activeSelf)
            {
                foundPanelByTag.SetActive(false);
                Debug.Log("DialogueManager: 태그로 DialoguePanel을 찾아서 닫았습니다.");
            }
        }
        catch (UnityException)
        {
            // 태그가 정의되어 있지 않으면 무시
        }
        
        // 3. Canvas의 직접 자식에서 Button과 Text/TextMeshProUGUI를 가진 활성화된 패널 찾기
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            // Canvas의 직접 자식만 검색 (패널 레벨)
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                Transform child = canvas.transform.GetChild(i);
                GameObject panel = child.gameObject;
                
                // 활성화되어 있는 패널만 확인
                if (panel.activeSelf)
                {
                    // Button과 Text/TextMeshProUGUI를 가진 패널인지 확인
                    Button btn = panel.GetComponentInChildren<Button>(true);
                    TextMeshProUGUI tmp = panel.GetComponentInChildren<TextMeshProUGUI>(true);
                    Text txt = panel.GetComponentInChildren<Text>(true);
                    
                    // Button이 있고, Text나 TextMeshProUGUI가 있는 패널 찾기
                    if (btn != null && (tmp != null || txt != null))
                    {
                        // 이름에 대화 관련 키워드가 있거나, Button이 직접 자식인 경우
                        string panelName = panel.name.ToLower();
                        bool isDialogueRelated = panelName.Contains("dialogue") || 
                                                panelName.Contains("dialog") || 
                                                panelName.Contains("대화");
                        
                        // Button이 이 패널의 직접 자식인 경우 (일반적인 대화창 구조)
                        bool hasButtonAsDirectChild = btn.transform.parent == panel.transform;
                        
                        if (isDialogueRelated || hasButtonAsDirectChild)
                        {
                            panel.SetActive(false);
                            Debug.Log($"DialogueManager: 대화창 패널을 강제로 닫았습니다: {panel.name}");
                        }
                    }
                }
            }
        }
        
        // 4. 추가로 모든 활성화된 GameObject 중에서 Button과 Text를 가진 패널 찾기 (최후의 수단)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.activeSelf && obj.transform.parent != null && obj.transform.parent.GetComponent<Canvas>() != null)
            {
                Button btn = obj.GetComponentInChildren<Button>(true);
                TextMeshProUGUI tmp = obj.GetComponentInChildren<TextMeshProUGUI>(true);
                Text txt = obj.GetComponentInChildren<Text>(true);
                
                if (btn != null && (tmp != null || txt != null))
                {
                    // "NEW TEXT" 같은 텍스트가 보이는 경우 대화창으로 간주
                    string textContent = "";
                    if (tmp != null) textContent = tmp.text.ToLower();
                    else if (txt != null) textContent = txt.text.ToLower();
                    
                    if (textContent.Contains("new text") || textContent.Length > 0)
                    {
                        obj.SetActive(false);
                        Debug.Log($"DialogueManager: 대화창 패널을 강제로 닫았습니다 (텍스트 기반): {obj.name}");
                    }
                }
            }
        }
    }
    
    // 현재 씬에서 dialoguePanel 찾기
    void FindDialoguePanelInScene()
    {
        // 이미 찾았으면 다시 찾지 않음
        if (dialoguePanel != null)
        {
            return;
        }
        
        GameObject foundPanel = null;
        Scene currentScene = SceneManager.GetActiveScene();
        
        // 1. 씬에서 "DialoguePanel" 이름으로 찾기 (활성화된 것만)
        foundPanel = GameObject.Find("DialoguePanel");
        
        // 2. 태그로 찾기 시도 (태그가 정의되어 있지 않을 수 있으므로 try-catch 사용)
        if (foundPanel == null)
        {
            try
            {
                foundPanel = GameObject.FindGameObjectWithTag("DialoguePanel");
            }
            catch (UnityException)
            {
                // 태그가 정의되어 있지 않으면 무시
            }
        }
        
        // 3. 이름으로 찾지 못하면 Canvas에서 찾기 (비활성화된 것도 찾을 수 있음)
        if (foundPanel == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in allCanvases)
            {
                // Canvas의 모든 자식 검색 (비활성화된 것도 포함)
                Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    // "DialoguePanel" 이름을 가진 자식 찾기
                    if (child.name == "DialoguePanel" || child.name.Contains("Dialogue") || child.name.Contains("대화"))
                    {
                        foundPanel = child.gameObject;
                        break;
                    }
                }
                if (foundPanel != null) break;
                
                // 이름으로 찾지 못하면 구조로 찾기
                Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    TextMeshProUGUI tmp = btn.GetComponentInParent<TextMeshProUGUI>();
                    Text txt = btn.GetComponentInParent<Text>();
                    if (tmp != null || txt != null)
                    {
                        foundPanel = btn.transform.parent != null ? btn.transform.parent.gameObject : btn.gameObject;
                        break;
                    }
                }
                if (foundPanel != null) break;
            }
        }
        
        if (foundPanel != null)
        {
            dialoguePanel = foundPanel;
            
            // 하위에서 UI 컴포넌트 찾기 (모든 컴포넌트를 가져와서 적절히 할당)
            TextMeshProUGUI[] tmpComponents = foundPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            Text[] textComponents = foundPanel.GetComponentsInChildren<Text>(true);
            
            // TextMeshPro 컴포넌트 할당 (첫 번째는 대화 텍스트, 두 번째는 화자 이름)
            if (tmpComponents.Length > 0)
            {
                dialogueTextTMP = tmpComponents[0];
            }
            if (tmpComponents.Length > 1)
            {
                speakerNameTextTMP = tmpComponents[1];
            }
            
            // Text 컴포넌트 할당 (첫 번째는 대화 텍스트, 두 번째는 화자 이름)
            if (textComponents.Length > 0)
            {
                dialogueText = textComponents[0];
            }
            if (textComponents.Length > 1)
            {
                speakerNameText = textComponents[1];
            }
            
            // Button 찾기
            nextButton = foundPanel.GetComponentInChildren<Button>(true);
            
            Debug.Log($"DialogueManager: 새 씬에서 dialoguePanel을 찾았습니다: {foundPanel.name} (활성화: {foundPanel.activeSelf})");
        }
        else
        {
            Debug.LogWarning("DialogueManager: 씬에서 dialoguePanel을 찾을 수 없습니다!");
        }
    }
    
    // 대화창 강제 닫기 (외부에서도 호출 가능)
    public void CloseDialogue()
    {
        // dialoguePanel이 null이면 씬에서 찾기
        if (dialoguePanel == null)
        {
            FindDialoguePanelInScene();
        }
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        // 타이핑 중이면 중지
        StopAllCoroutines();
        isTyping = false;
        
        // 플레이어 이동 활성화
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetCanMove(true);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 player02 = FindFirstObjectByType<PlayerController02>();
            if (player02 != null)
            {
                player02.SetCanMove(true);
            }
        }
        
        // 대화 상태 초기화
        currentDialogue = null;
        currentDialogueIndex = 0;
        nextSceneName = "";
    }
    
    // 대화 시작
    public void StartDialogue(string[] dialogues, string speakerName = "", string sceneToLoad = "")
    {
        // dialoguePanel이 null이면 씬에서 찾기
        if (dialoguePanel == null)
        {
            FindDialoguePanelInScene();
        }
        
        if (dialoguePanel == null || (dialogueText == null && dialogueTextTMP == null))
        {
            Debug.LogError("Dialogue UI가 설정되지 않았습니다! dialogueText 또는 dialogueTextTMP를 설정해주세요.");
            return;
        }
        
        currentDialogue = dialogues;
        currentDialogueIndex = 0;
        nextSceneName = sceneToLoad;
        
        dialoguePanel.SetActive(true);
        
        // 화자 이름 설정 (Text 또는 TextMeshPro 중 하나 사용)
        SetSpeakerName(speakerName);
        
        // 플레이어 이동 비활성화
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetCanMove(false);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 player02 = FindFirstObjectByType<PlayerController02>();
            if (player02 != null)
            {
                player02.SetCanMove(false);
            }
        }
        
        DisplayNextDialogue();
    }
    
    // 화자 이름 설정 (Text 또는 TextMeshPro 지원)
    private void SetSpeakerName(string name)
    {
        if (speakerNameTextTMP != null)
        {
            speakerNameTextTMP.text = name;
        }
        else if (speakerNameText != null)
        {
            speakerNameText.text = name;
        }
    }
    
    // 대화 텍스트 설정 (Text 또는 TextMeshPro 지원)
    private void SetDialogueText(string text)
    {
        if (dialogueTextTMP != null)
        {
            dialogueTextTMP.text = text;
        }
        else if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }
    
    // 현재 대화 텍스트 가져오기
    private string GetDialogueText()
    {
        if (dialogueTextTMP != null)
        {
            return dialogueTextTMP.text;
        }
        else if (dialogueText != null)
        {
            return dialogueText.text;
        }
        return "";
    }
    
    // 다음 대화 표시
    void DisplayNextDialogue()
    {
        if (currentDialogueIndex >= currentDialogue.Length)
        {
            EndDialogue();
            return;
        }
        
        StartCoroutine(TypeDialogue(currentDialogue[currentDialogueIndex]));
    }
    
    // 타이핑 효과로 대화 표시
    IEnumerator TypeDialogue(string dialogue)
    {
        isTyping = true;
        SetDialogueText("");
        
        foreach (char letter in dialogue.ToCharArray())
        {
            SetDialogueText(GetDialogueText() + letter);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
    }
    
    // 다음 버튼 클릭 또는 스페이스바 입력
    void OnNextButtonClicked()
    {
        if (isTyping)
        {
            // 타이핑 중이면 즉시 완성
            StopAllCoroutines();
            SetDialogueText(currentDialogue[currentDialogueIndex]);
            isTyping = false;
        }
        else
        {
            currentDialogueIndex++;
            DisplayNextDialogue();
        }
    }
    
    void Update()
    {
        // 스페이스바 또는 엔터로 다음 대화 진행
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                OnNextButtonClicked();
            }
        }
    }
    
    // 대화 종료
    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        
        // 플레이어 이동 활성화
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetCanMove(true);
        }
        else
        {
            // PlayerController02 찾기
            PlayerController02 player02 = FindFirstObjectByType<PlayerController02>();
            if (player02 != null)
            {
                player02.SetCanMove(true);
            }
        }
        
        // 다음 씬으로 이동
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(nextSceneName);
            }
            else
            {
                SceneLoader sceneLoader = FindFirstObjectByType<SceneLoader>();
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
    }
}

