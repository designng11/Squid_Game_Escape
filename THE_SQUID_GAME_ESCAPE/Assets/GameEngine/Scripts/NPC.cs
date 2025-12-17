using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPC 설정")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private string[] dialogues; // 대화 내용 배열
    [SerializeField] private string nextSceneName = ""; // 대화 후 이동할 씬 이름
    
    [Header("상호작용 설정")]
    [SerializeField] private float interactionRange = 2f; // 상호작용 가능 거리
    [SerializeField] private KeyCode interactionKey = KeyCode.E; // 상호작용 키
    [SerializeField] private GameObject interactionHint; // 상호작용 힌트 UI (예: "E키를 눌러 대화하기")
    
    private bool isPlayerNearby = false;
    private Transform playerTransform;
    
    void Start()
    {
        if (interactionHint != null)
        {
            interactionHint.SetActive(false);
        }
        
        // 플레이어 찾기
        FindPlayer();
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
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                    Debug.Log("NPC: PlayerController를 통해 플레이어를 찾았습니다.");
                }
                else
                {
                    // PlayerController02 찾기
                    PlayerController02 playerController02 = FindFirstObjectByType<PlayerController02>();
                    if (playerController02 != null)
                    {
                        playerTransform = playerController02.transform;
                        Debug.Log("NPC: PlayerController02를 통해 플레이어를 찾았습니다.");
                    }
                    else
                    {
                        Debug.LogWarning("NPC: 플레이어를 찾을 수 없습니다! 플레이어 오브젝트에 'Player' 태그를 설정하거나 PlayerController/PlayerController02를 추가해주세요.");
                    }
                }
            }
            else
            {
                playerTransform = player.transform;
                Debug.Log("NPC: 플레이어를 찾았습니다.");
            }
        }
    }
    
    void Update()
    {
        // 플레이어가 없으면 계속 찾기
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // 플레이어가 근처에 있는지 확인
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        isPlayerNearby = distance <= interactionRange;
        
        // 힌트 표시/숨김
        if (interactionHint != null)
        {
            interactionHint.SetActive(isPlayerNearby);
        }
        
        // 상호작용 키 입력 확인
        if (isPlayerNearby && Input.GetKeyDown(interactionKey))
        {
            Debug.Log("NPC: 상호작용 키가 눌렸습니다!");
            StartDialogue();
        }
    }
    
    // 대화 시작
    void StartDialogue()
    {
        // 대화 내용 확인
        if (dialogues == null || dialogues.Length == 0)
        {
            Debug.LogError("NPC: 대화 내용이 설정되지 않았습니다! Inspector에서 Dialogues 배열을 설정해주세요.");
            return;
        }
        
        // DialogueManager 확인
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("NPC: DialogueManager를 찾을 수 없습니다! 씬에 DialogueManager가 있는지 확인해주세요.");
            return;
        }
        
        Debug.Log($"NPC: 대화를 시작합니다. (대화 개수: {dialogues.Length})");
        DialogueManager.Instance.StartDialogue(dialogues, npcName, nextSceneName);
    }
    
    // Gizmos로 상호작용 범위 시각화 (에디터에서만 보임)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

