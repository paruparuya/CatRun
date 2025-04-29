using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float baseSpeed = 0.02f; // 左に流れる速度
    [SerializeField] private float destroyX = -15f; // 左に出たら削除
    [SerializeField] private string targetTag = "Snake"; // スピードアップ対象のタグ
    [SerializeField] private float boostedSpeedMultiplier = 2f;  // スピードアップ倍率

    private CatController cat;
    private GameManager gameManager;
    private bool hasSpeedBoosted = false; // 一度だけスピードアップ
    // Start is called before the first frame update
    void Start()
    {
        cat = FindObjectOfType<CatController>();
        gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        

        if (cat != null && cat.isEnd)
        {
            return;
        }

        float currentSpeed = baseSpeed;

        // タグが一致していて、X座標が9より小さくなったらスピードアップ（1回だけ）
        if (!hasSpeedBoosted && gameObject.tag == targetTag && transform.position.x < 9f)
        {
            baseSpeed *= boostedSpeedMultiplier; // 元のスピードを強化
            hasSpeedBoosted = true; // 一度だけにする
        }



        // ゲーム全体の倍率をかけたスピード
        currentSpeed = baseSpeed * gameManager.speedMultiplier;

        // 1回だけ、しっかり動かす（deltaTimeあり）
        transform.position += Vector3.left * currentSpeed * Time.deltaTime;


        //オブジェクトを消す処理
        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}
