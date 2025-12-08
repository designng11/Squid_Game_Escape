using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed = 10f; // 총알 속도
    private BulletDodge bulletDodge; // BulletDodge 참조
    private bool isInitialized = false;
    
    [Header("충돌 설정")]
    [SerializeField] private string playerTag = "Player"; // 플레이어 태그
    [SerializeField] private float destroyDistance = 20f; // 화면 밖으로 나갔을 때 제거할 거리
    
    private Camera targetCamera;
    
    void Start()
    {
        // 카메라 찾기
        targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }
    }
    
    void Update()
    {
        if (!isInitialized)
        {
            return;
        }
        
        // 왼쪽으로 이동
        transform.position += Vector3.left * speed * Time.deltaTime;
        
        // 화면 밖으로 나갔는지 확인
        if (targetCamera != null)
        {
            Vector3 viewportPos = targetCamera.WorldToViewportPoint(transform.position);
            
            // 왼쪽 화면 밖으로 나갔으면 제거
            if (viewportPos.x < -0.1f)
            {
                if (bulletDodge != null)
                {
                    bulletDodge.RemoveBullet(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // 카메라가 없으면 거리 기준으로 제거
            if (bulletDodge != null && bulletDodge.IsGameActive())
            {
                // 플레이어와의 거리 확인
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    float distance = Vector2.Distance(transform.position, player.transform.position);
                    if (distance > destroyDistance)
                    {
                        bulletDodge.RemoveBullet(gameObject);
                    }
                }
            }
        }
    }
    
    // 총알 초기화
    public void Initialize(float bulletSpeed, BulletDodge manager)
    {
        speed = bulletSpeed;
        bulletDodge = manager;
        isInitialized = true;
    }
    
    // 충돌 감지
    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌
        if (other.CompareTag(playerTag))
        {
            if (bulletDodge != null)
            {
                bulletDodge.OnBulletHitPlayer(gameObject);
            }
            
            // 총알 제거
            Destroy(gameObject);
        }
    }
    
    // 충돌 감지 (Collider2D가 없을 때)
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어와 충돌
        if (collision.gameObject.CompareTag(playerTag))
        {
            if (bulletDodge != null)
            {
                bulletDodge.OnBulletHitPlayer(gameObject);
            }
            
            // 총알 제거
            Destroy(gameObject);
        }
    }
}

