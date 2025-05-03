using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using unityroom.Api;

public class CatController : MonoBehaviour
{
    Rigidbody2D rigid2D; //リジッドボディの宣言
    private Animator animator; //アニメーターの宣言

    public float jumpVelocity = 5f;
    private bool wasGrounded = true;
    public float snakeJump = 2f;

    private Vector2 initialPosition; // 初期位置を保存
    private float moveStartTime = -1f; // 動き出した時刻を記録（未開始は -1）
    private bool hasMoved = false; // 初期位置から動いたか
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 2.0f;
    [SerializeField] float returnSpeed = 2f; // 初期位置に戻るスピード

    public bool isEnd = false; //ゲーム終了フラグ
    public bool isEnded = false; //ゲーム終了処理
    private float deadLineX = -10f;
    private float deadLineY = -7f;

    public GrundController grundController; //足場のスクリプト参照
    public GameObject gameOverText; //ゲームオーバーテキスト
    public GameObject nextText;

    private int count = 0;
    public TMP_Text countText;

    private AudioSource audioSource;      // 音を鳴らすためのAudioSource
    public AudioClip jumpSound;           // ジャンプ時に鳴らす音素材

    private GameManager gameManager;

    private bool justBounced = false; // 跳ねた直後かどうか
    private float bounceTimer = 0f;
    private float bounceGraceTime = 0.05f; // 跳ねた後この秒数だけジャンプOK

    void Start()
    {
        rigid2D = GetComponent<Rigidbody2D>();  //ゲーム開始時に物理を取得
        gameManager = FindObjectOfType<GameManager>(); //ゲーマネスクリプトを取得
        animator = GetComponent<Animator>(); //アニメーターを取得
        audioSource = GetComponent<AudioSource>(); //サウンドを取得

        // 初期位置を保存
        initialPosition = transform.position;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && isEnd)
        {
            SceneManager.LoadScene("SampleScene");
        }

        if (this.isEnd)
        {
            if (gameOverText != null && nextText != null)
            {
                gameOverText.SetActive(true);
                nextText.SetActive(true);
            }
            animator.SetBool("Dead", true);
            return;
        }

        Count();
        UnityroomApiClient.Instance.SendScore(1, count, ScoreboardWriteMode.HighScoreDesc);

        Vector2 origin = transform.position;
        Vector2 direction = Vector2.down;
        float distance = 0.85f;
        LayerMask groundLayer = LayerMask.GetMask("Ground");

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, groundLayer);
        bool isGround = hit.collider != null;
        Debug.DrawRay(origin, direction * distance, Color.red);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGround || justBounced)
            {
                float currentJumpVelocity = Mathf.Min(jumpVelocity * gameManager.speedMultiplier, 12f);
                rigid2D.velocity = new Vector2(0, currentJumpVelocity);

                animator.SetBool("Jump", true);
                audioSource.PlayOneShot(jumpSound);
                justBounced = false;
            }
        }

        if (!wasGrounded && isGround)
        {
            animator.SetBool("Jump", false);
        }

        wasGrounded = isGround;

        if (rigid2D.velocity.y < 0)
        {
            float currentFallMultiplier = fallMultiplier * gameManager.speedMultiplier;
            rigid2D.velocity += Vector2.up * Physics2D.gravity.y * (currentFallMultiplier - 1) * Time.deltaTime;
        }
        else if (rigid2D.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            float currentLowJumpMultiplier = lowJumpMultiplier * gameManager.speedMultiplier;
            rigid2D.velocity += Vector2.up * Physics2D.gravity.y * (currentLowJumpMultiplier - 1) * Time.deltaTime;
        }

        if (!hasMoved && Mathf.Abs(transform.position.x - initialPosition.x) > 0.01f)
        {
            hasMoved = true;
            moveStartTime = Time.time;
        }

        if (hasMoved && Time.time - moveStartTime > 3f)
        {
            float newX = Mathf.MoveTowards(transform.position.x, initialPosition.x, returnSpeed * Time.deltaTime);
            transform.position = new Vector2(newX, transform.position.y);
        }

        if (transform.position.x < deadLineX || transform.position.y < deadLineY)
        {
            this.isEnd = true;
        }

        if (justBounced)
        {
            bounceTimer += Time.deltaTime;
            if (bounceTimer > bounceGraceTime)
            {
                justBounced = false;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "snake")
        {
            ContactPoint2D contact = other.contacts[0];
            Vector2 normal = contact.normal;
            Debug.Log("normal.y = " + normal.y);
            if (normal.y > 0.5f)
            {
                Destroy(other.gameObject);
                rigid2D.velocity = new Vector2(0, snakeJump);
                justBounced = true;
                bounceTimer = 0f;
                count += 2;
                Count();
            }
            else
            {
                this.isEnd = true;
            }
        }

        if (other.gameObject.tag == "nezumiB")
        {
            Destroy(other.gameObject);
            count--;
            Count();
        }

        if (other.gameObject.tag == "nezumi")
        {
            Destroy(other.gameObject);
            count++;
            Count();
        }
    }

    public void Count()
    {
        countText.text = count.ToString();
    }
}
