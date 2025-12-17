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
            
            // 점프 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
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
    
    // Dead 파라미터 설정 (GameOver 상태에서 사용)
    public void SetDead(bool value)
    {
        if (animator != null)
        {
            animator.SetBool("Dead", value);
        }
    }
    
   void OnCollisionEnter2D(Collision2D collision)
		{
		    // 충돌한 오브젝트가 "Ground" 또는 "Plate" Tag를 가지고 있는지 확인 (점프 가능)
		    if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Plate"))
		    {
		        
		        isGrounded = true;
		    }
		    
		    // 유리판 게임: 플레이트 위에 올라갔을 때
		    if (collision.gameObject.CompareTag("Plate"))
		    {
		        GlassBridgeGame glassBridgeGame = FindFirstObjectByType<GlassBridgeGame>();
		        if (glassBridgeGame != null)
		        {
		            glassBridgeGame.OnPlayerEnterPlate(collision.gameObject);
		        }
		    }
		    
		    // 유리판 게임: 바닥에 닿으면 죽음
		    if (collision.gameObject.CompareTag("DeadlyGround"))
		    {
		        GlassBridgeGame glassBridgeGame = FindFirstObjectByType<GlassBridgeGame>();
		        if (glassBridgeGame != null)
		        {
		            glassBridgeGame.OnPlayerHitGround();
		        }

		        // 총알 피하기 게임: DeadlyGround 게임오버
		        BulletDodge bulletDodge = FindFirstObjectByType<BulletDodge>();
		        if (bulletDodge != null)
		        {
		            bulletDodge.OnPlayerHitDeadlyGround();
		        }
		    }
		}

	 
	 void OnCollisionExit2D(Collision2D collision)
		{
		    // Ground 또는 Plate에서 떨어졌을 때 isGrounded 해제
		    if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Plate"))
		    {
		        
		        isGrounded = false;
		    }
		    
		    // 유리판 게임: 플레이트에서 내려갔을 때
		    if (collision.gameObject.CompareTag("Plate"))
		    {
		        GlassBridgeGame glassBridgeGame = FindFirstObjectByType<GlassBridgeGame>();
		        if (glassBridgeGame != null)
		        {
		            glassBridgeGame.OnPlayerExitPlate(collision.gameObject);
		        }
		    }
		}
}