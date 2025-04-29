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
    private Animator animator;　//アニメーターの宣言

    public float jumpVelocity = 5f;
    private bool wasGrounded = true;

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

    public GrundController grundController;　//足場のスクリプト参照
    public GameObject gameOverText; //ゲームオーバーテキスト
    public GameObject nextText;

    private int count = 0;
    public TMP_Text countText;

    private AudioSource audioSource;      // 音を鳴らすためのAudioSource
    public AudioClip jumpSound;           // ジャンプ時に鳴らす音素材


    private GameManager gameManager;

    void Start()
    {
        rigid2D = GetComponent<Rigidbody2D>();  //ゲーム開始時に物理を取得
        gameManager = FindObjectOfType<GameManager>();　//ゲーマネスクリプトを取得
        animator = GetComponent<Animator>();　//アニメーターを取得
        audioSource = GetComponent<AudioSource>();　//サウンドを取得

        // 初期位置を保存
        initialPosition = transform.position;
    }

    void Update()
    {
        // クリックされたらシーンをロードする（デバッグ用）
        if (Input.GetMouseButtonDown(0) && isEnd)
        {
            //SampleSceneを読み込む
            SceneManager.LoadScene("SampleScene");
        }


        if(this.isEnd)  //ゲームオーバー処理
        {
            if (gameOverText != null && nextText != null)
            {
                gameOverText.SetActive(true); // テキスト表示！
                nextText.SetActive(true);
            }
            animator.SetBool("Dead", true);
            return;
        }

        Count();

        UnityroomApiClient.Instance.SendScore(1, count, ScoreboardWriteMode.HighScoreDesc);

        // 地面判定用のRaycast
        Vector2 origin = transform.position;
        Vector2 direction = Vector2.down;
        float distance = 0.85f;
        LayerMask groundLayer = LayerMask.GetMask("Ground");

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, groundLayer);
        bool isGround = hit.collider != null;
        Debug.DrawRay(origin, direction * distance, Color.red);

        // スペースキーでジャンプ（地面にいるときのみ）
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            float currentJumpVelocity = Mathf.Min(jumpVelocity * gameManager.speedMultiplier, 12f);
            rigid2D.velocity = new Vector2(0, currentJumpVelocity);

            animator.SetBool("Jump", true) ;　//アニメーションを開始
            audioSource.PlayOneShot(jumpSound);　//ジャンプ音を再生
        }

        if (!wasGrounded && isGround)　//地面についたら
        {
            animator.SetBool("Jump", false); // 着地でアニメーションOFF
        }

        wasGrounded = isGround; // 次のフレームに備えて更新

        // ★ジャンプ中の落下を加速させる
        if (rigid2D.velocity.y < 0) // 落下中
        {
            float currentFallMultiplier = fallMultiplier * gameManager.speedMultiplier;
            rigid2D.velocity += Vector2.up * Physics2D.gravity.y * (currentFallMultiplier - 1) * Time.deltaTime;
        }
        else if (rigid2D.velocity.y > 0 && !Input.GetKey(KeyCode.Space)) // 早めにジャンプを離したとき
        {
            float currentLowJumpMultiplier = lowJumpMultiplier * gameManager.speedMultiplier;
            rigid2D.velocity += Vector2.up * Physics2D.gravity.y * (currentLowJumpMultiplier - 1) * Time.deltaTime;
        }

        // X軸で動いたかを判定
        if (!hasMoved && Mathf.Abs(transform.position.x - initialPosition.x) > 0.01f)
        {
            hasMoved = true;
            moveStartTime = Time.time; // 動いた瞬間の時間を記録
        }

        // 3秒経過したら徐々に初期X位置に戻す（Yはそのまま）
        if (hasMoved && Time.time - moveStartTime > 3f)
        {
            float newX = Mathf.MoveTowards(transform.position.x, initialPosition.x, returnSpeed * Time.deltaTime);
            transform.position = new Vector2(newX, transform.position.y);
        }

        //画面外に出たらゲームオーバーフラグ
        if(transform.position.x < deadLineX || transform.position.y < deadLineY)
        {
            this.isEnd = true;
        }
        
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        //蛇にぶつかったらゲーム終了
        if (other.gameObject.tag == "snake")
        {
            // 接触点の法線ベクトル（最初の1つ）を取得
            ContactPoint2D contact = other.contacts[0];
            Vector2 normal = contact.normal;
            Debug.Log("normal.y = " + normal.y);
            // normal.y が 0.5 より大きい時
            if (normal.y > 0.5f)
            {
                // 敵を倒す（削除）
                //animator.SetBool("Snake Dead", true);
                Destroy(other.gameObject);
                count += 2;
                Count();
            }
            else
            {
                // ゲームオーバー処理（例: フラグを立てる）
                this.isEnd = true;
            }
            
        }

        //ネズミは倒す
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
