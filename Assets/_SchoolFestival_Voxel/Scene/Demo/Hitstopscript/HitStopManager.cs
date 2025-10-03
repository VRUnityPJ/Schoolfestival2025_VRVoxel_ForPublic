using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    // シングルトンパターンでどこからでもアクセスできるようにする
    public static HitStopManager Instance { get; private set; }

    // 元のTimeScaleを保存しておくための変数 (念のため)
    private float originalTimeScale;

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            // シーンをまたいでも破棄されないようにする場合は、以下をコメント解除
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }

        // 初期TimeScaleを保存
        originalTimeScale = Time.timeScale;
    }

    /// <summary>
    /// 指定した時間だけヒットストップ（時間停止）を発生させます。
    /// Time.timeScaleを0に設定し、指定時間後に元に戻します。
    /// </summary>
    /// <param name="duration">ヒットストップの持続時間（実時間、秒）</param>
    public static void HitStop(float duration)
    {
        if (Instance != null)
        {
            // コルーチンで時間制御を行う
            Instance.StartCoroutine(Instance.DoHitStop(duration));
        }
        else
        {
            Debug.LogError("HitStopManagerのインスタンスがシーンに見つかりません。");
        }
    }

    // ヒットストップを処理するコルーチン
    private IEnumerator DoHitStop(float duration)
    {
        // Time.timeScaleが既に0の場合は、多重実行を避けるため処理をスキップ
        if (Time.timeScale == 0f)
        {
            yield break;
        }

        // 1. TimeScaleを操作してヒットストップを再現する
        Time.timeScale = 0f;

        // 2. メソッドを呼び出してヒットストップをかけるようにする
        // TimeScaleが0の間でもカウントされるWaitForSecondsRealtimeを使用
        yield return new WaitForSecondsRealtime(duration);

        // TimeScaleを元に戻す
        Time.timeScale = originalTimeScale;
    }

    private void OnDestroy()
    {
        // スクリプトが破棄されるときにTimeScaleをリセット
        if (Instance == this)
        {
            Time.timeScale = originalTimeScale;
            Instance = null;
        }
    }
}