using System.Collections.Generic;
using UnityEngine;

public class GrundController : MonoBehaviour
{
    public CatController catController; //猫のスクリプト参照
    [Header("足場プレハブ")]
    public GameObject groundPrefab;

    [Header("足場生成設定")]
    [SerializeField] private float groundY = -5f; //足場の高さ
    [SerializeField] private int initialCount = 5;　//最初に生成される足場の数
    [SerializeField] private float gap = 2f;　//足場と足場の幅
    [SerializeField] private float fixedWidth = 2f;　
    [SerializeField] private float minWidth = 1f;　//足場の最低幅
    [SerializeField] private float maxWidth = 3f;　//足場の最高幅

    private float currentGroundY = -5f; // 最初の高さ（地面の高さ）
    [SerializeField] private float groundMaxY = 0f; // 足場の最大高さ
    [SerializeField] private float groundMinY = -5f; // 最低高さ
    [SerializeField] private float groundStep = 5f;  // 高低差の最大値（±）

    [Header("追加の足場設定")]
    [SerializeField] private GameObject smallPlatformPrefab; // 上に乗せる足場プレハブ
    [SerializeField] private float stackedPlatformHeight = 5f; // 上に置く高さ差
    [SerializeField] private float stackedTriggerWidth = 2f;  // 出現条件の最低幅
    [SerializeField] private float stackedPlatformScaleX = 1f; // 幅
    [SerializeField] private float stackedPlatformScaleY = 1f; // 高さ
    private List<GameObject> stackedPlatforms = new List<GameObject>();


    [Header("追加足場の出現確率（0〜1）")]
    [SerializeField] private float stackedSpawnChance = 0.5f; // 50%の確率で出す
    [Header("追加足場の位置オフセット")]
    [SerializeField] private Vector2 stackedOffset = new Vector2(0f, 2f); // X: 左右, Y: 上下

    [Header("スクロール設定")]
    [SerializeField] private float scrollSpeed = 0.02f;　//左に動く速さ
    [SerializeField] private float destroyX = -15f;　//足場が消える位置

    [Header("敵プレハブ（3種類）")]
    [SerializeField] private GameObject ratPrefab;　//ネズミのプレハブ
    [SerializeField] private GameObject snakePrefab; //蛇のプレハブ
    [SerializeField] private GameObject rockPrefab;　// 岩のプレハブ

    [Header("敵の出現確率（0〜1）")]
    [SerializeField] private float enemySpawnChance = 0.4f;　//敵の出現頻度

    private float nextRightEdgeX = -15f;
    private List<GameObject> platforms = new List<GameObject>();
    
    public float baseSpeed = 2f;  //ゲームの基本速度
    private GameManager gameManager;

    void Start()
    {
        //最初の足場の出現
        currentGroundY = -5f;
        SpawnPlatform(fixedWidth, currentGroundY);

        for (int i = 0; i < initialCount; i++)
        {
            float width = fixedWidth;
            SpawnPlatform(fixedWidth);
        }
        gameManager = FindObjectOfType<GameManager>();
        
    }

    void Update()
    {
        if (catController != null && catController.isEnd)  // 猫がゲームオーバー状態になったらここにくる！
        {
            return;
        }
        
        //ゲーム速度アップ
        float currentSpeed = baseSpeed * gameManager.speedMultiplier;　　
        transform.position += Vector3.left * currentSpeed * Time.deltaTime;

        ScrollPlatforms();
        CleanupAndRespawn();
    }

    //足場の生成（調整用）
    void SpawnPlatform(float width, float y)
    {
        float leftX = nextRightEdgeX + gap;
        float centerX = leftX + (width / 2f);

        GameObject platform = Instantiate(groundPrefab, new Vector2(centerX, y), Quaternion.identity);

        Vector3 scale = platform.transform.localScale;
        scale.x = width;
        platform.transform.localScale = scale;

        platforms.Add(platform);
        nextRightEdgeX = leftX + width;

        currentGroundY = y; // 高さを保存（次に使う）
    }

    //足場の生成関係
    void SpawnPlatform(float width)
    {

        float offset = Random.Range(-groundStep, groundStep);
        float newY = currentGroundY + offset;

        // 最低高さより下に行かないように制限
        newY = Mathf.Clamp(newY, groundMinY, groundMaxY);

        // 左端の位置 = 前の足場の右端 + gap
        float leftX = nextRightEdgeX + gap;

        // 中心位置 = 左端 + (幅 / 2)
        float centerX = leftX + (width / 2f);

        GameObject platform = Instantiate(groundPrefab, new Vector2(centerX, newY), Quaternion.identity);

        Vector3 scale = platform.transform.localScale;
        scale.x = width;
        platform.transform.localScale = scale;

        platforms.Add(platform);

        // 次の右端 = 左端 + 幅
        nextRightEdgeX = leftX + width;

        // ★ 今回のYを保存して次に使う
        currentGroundY = newY;

        //敵の出現関係
        if (Random.value < enemySpawnChance)
        {
            int enemyType = Random.Range(0, 3); // 0=ネズミ, 1=蛇, 2=岩
            Vector2 enemyPos = new Vector2(centerX, newY + 0.5f); // 足場の上に出す

            if (enemyType == 2) // 岩（足場の子にする）
            {
                GameObject enemy = Instantiate(rockPrefab, enemyPos, Quaternion.identity);
                enemy.transform.parent = platform.transform; // ★ 足場にくっつける！
            }
            else // ネズミ or 蛇（自分で移動させる）
            {
                GameObject enemyPrefab = (enemyType == 0) ? ratPrefab : snakePrefab;
                Instantiate(enemyPrefab, enemyPos, Quaternion.identity); // 子にしない！
            }
        }

        // 追加の足場関係
        if (platform.CompareTag("Ground") && width >= stackedTriggerWidth)
        {
            // ★ 確率判定：stackedSpawnChanceの値より小さいかどうか
            if (Random.value < stackedSpawnChance)
            {
                // ★ ランダムずらし量を決める
                Vector2 stackedPos = new Vector2(centerX + stackedOffset.x, newY + stackedOffset.y);


                GameObject stacked = Instantiate(smallPlatformPrefab, stackedPos, Quaternion.identity);

                stacked.transform.localScale = new Vector3(stackedPlatformScaleX, stackedPlatformScaleY, 1f);
                stackedPlatforms.Add(stacked);
            }
        }
        if (Random.value < enemySpawnChance)
        {
            int enemyType = Random.Range(0, 2); // 0: ネズミ, 1: 蛇
            Vector2 enemyPos = new Vector2(centerX + stackedOffset.x, newY + stackedOffset.y + 0.5f);

            GameObject enemyPrefab = (enemyType == 0) ? ratPrefab : snakePrefab;
            Instantiate(enemyPrefab, enemyPos, Quaternion.identity);
        }
    }

    void ScrollPlatforms()
    {
        float currentScrollSpeed = scrollSpeed * gameManager.speedMultiplier * Time.deltaTime; 

        foreach (GameObject platform in platforms)
        {
            if (platform != null)
            {
                Vector2 pos = platform.transform.position;
                pos.x -= currentScrollSpeed;
                platform.transform.position = pos;
            }
        }

        // ★ 上にある小さい足場もスクロール
        foreach (GameObject stacked in stackedPlatforms)
        {
            if (stacked != null)
            {
                Vector2 pos = stacked.transform.position;
                pos.x -= currentScrollSpeed;
                stacked.transform.position = pos;
            }
        }

        nextRightEdgeX -= currentScrollSpeed; // ★右端の位置も一緒にスクロール
    }

    void CleanupAndRespawn()
    {
        int removedCount = 0;

        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            if (platforms[i].transform.position.x < destroyX)
            {
                Destroy(platforms[i]);
                platforms.RemoveAt(i);
                removedCount++;
            }
        }

        for (int i = stackedPlatforms.Count - 1; i >= 0; i--)
        {
            if (stackedPlatforms[i] != null && stackedPlatforms[i].transform.position.x < destroyX)
            {
                Destroy(stackedPlatforms[i]);
                stackedPlatforms.RemoveAt(i);
            }
        }

        for (int i = 0; i < removedCount; i++)
        {
            float width = Random.Range(minWidth, maxWidth);
            SpawnPlatform(width);
        }
    }
    
}