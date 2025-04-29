using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using unityroom.Api;

public class GameManager : MonoBehaviour
{

    public CatController catController; //猫のスクリプト参照
    public float speedMultiplier = 1.0f; // ゲーム全体のスピード倍率
    public float interval = 10.0f;       // スピードアップの間隔（秒）
    public float increaseRate = 0.2f;    // 増加率（1回ごとに+0.2など）
    private float timer = 0f;

    private float time = 0f;
    public TMP_Text timeText;
    

    public CatController controller;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (catController != null && catController.isEnd)  // 猫がゲームオーバー状態になったらここにくる！
        {
            return;
        }

        time += Time.deltaTime;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);



        timer += Time.deltaTime;　

        if (timer >= interval)
        {
            timer = 0f;
            speedMultiplier += increaseRate;
            Debug.Log($"スピード倍率アップ！現在：{speedMultiplier}");
        }


        //UnityroomApiClient.Instance.SendScore(1, 0f, ScoreboardWriteMode.HighScoreDesc);
        //UnityroomApiClient.Instance.SendScore(2, 0f, ScoreboardWriteMode.HighScoreDesc);
    }
}
