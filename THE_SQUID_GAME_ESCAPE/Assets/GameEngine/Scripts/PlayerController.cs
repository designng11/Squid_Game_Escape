using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5.0f;
    
    [Header("점프 설정")]
    public float jumpForce = 10.0f;
    
    [Header("물리 설정")]
    public float gravityScale = 2.0f; 
    
    private Animator animator;
    private SpriteRenderer spriterenderer;
    private Rigidbody2D rb;
    private bool isGrounded = false;
    private bool canMove = true; // 이동 가능 여부 (대화 중에는 false)
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriterenderer = GetComponent<SpriteRenderer>();
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody2D가 없습니다!");
        }
        else
        {
            // 중력 강도 설정
            rb.gravityScale = gravityScale;
        }
    }
    
    void Update()
    {
        // 이동이 비활성화되어 있으면 입력 무시
        if (!canMove)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (animator != null)
            {
                animator.SetFloat("Speed", 0);
            }
            return;
        }
        
        // 좌우 이동 입력
        float moveX = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveX = -1f;
            spriterenderer.flipX = true;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveX = 1f;
            spriterenderer.flipX = false;
        }

        // 점프 입력 (여러 키 지원: Space 또는 W)
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
        }
        
        // 물리 기반 이동 (중요: 이게 없으면 캐릭터가 안 움직임!)
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);
        
        // 애니메이션
        if (animator != null)
        {
            float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
            animator.SetFloat("Speed", currentSpeed);
        }
    }
    
    // 이동 가능 여부 설정 (대화 시스템에서 사용)
    public void SetCanMove(bool value)
    {
        canMove = value;
    }
    
   void OnCollisionEnter2D(Collision2D collision)
		{
		    // 충돌한 오브젝트가 "Ground" Tag를 가지고 있는지 확인
		    if (collision.gameObject.CompareTag("Ground"))
		    {
		        
		        isGrounded = true;
		    }
		}

	 
	 void OnCollisionExit2D(Collision2D collision)
		{
		    if (collision.gameObject.CompareTag("Ground"))
		    {
		        
		        isGrounded = false;
		    }
		}
}