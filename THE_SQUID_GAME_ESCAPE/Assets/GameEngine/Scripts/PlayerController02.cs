using UnityEngine;

public class PlayerController02 : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5.0f;
    
    [Header("점프 설정")]
    public float jumpForce = 10.0f;
    
    [Header("물리 설정")]
    public float gravityScale = 2.0f;
    
    [Header("관성 설정")]
    public float acceleration = 20.0f; // 가속도
    [Range(0f, 1f)]
    public float friction = 0.85f; // 마찰력 (0~1, 높을수록 더 오래 미끄러짐)
    public float minVelocityThreshold = 0.01f; // 최소 속도 임계값 (이 값 이하면 완전히 멈춤)
    
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
        bool isMoving = false;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveX = -1f;
            spriterenderer.flipX = true;
            isMoving = true;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveX = 1f;
            spriterenderer.flipX = false;
            isMoving = true;
        }

        // 점프 입력 (여러 키 지원: UpArrow 또는 W)
        if ((Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        
        // 관성 기반 이동
        float targetVelocityX = moveX * moveSpeed;
        float currentVelocityX = rb.linearVelocity.x;
        
        if (isMoving)
        {
            // 입력이 있을 때: 가속도 적용
            float velocityChange = targetVelocityX - currentVelocityX;
            float force = velocityChange * acceleration;
            rb.linearVelocity = new Vector2(currentVelocityX + force * Time.deltaTime, rb.linearVelocity.y);
        }
        else
        {
            // 입력이 없을 때: 마찰력 적용 (관성 - 미끄러지듯이 감소)
            if (Mathf.Abs(currentVelocityX) > minVelocityThreshold)
            {
                // 마찰력을 적용하여 속도를 점진적으로 감소시킴
                // friction 값이 높을수록 (1에 가까울수록) 더 오래 미끄러짐
                float newVelocityX = currentVelocityX * friction;
                
                // 속도가 매우 작아지면 완전히 멈춤
                if (Mathf.Abs(newVelocityX) < minVelocityThreshold)
                {
                    newVelocityX = 0f;
                }
                
                rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
            }
            else
            {
                // 속도가 임계값 이하면 바로 0으로 설정
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        
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

