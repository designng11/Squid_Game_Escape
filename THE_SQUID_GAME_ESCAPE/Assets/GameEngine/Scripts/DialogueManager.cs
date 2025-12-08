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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }
    
    // 대화 시작
    public void StartDialogue(string[] dialogues, string speakerName = "", string sceneToLoad = "")
    {
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
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetCanMove(false);
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
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetCanMove(true);
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
    }
}

