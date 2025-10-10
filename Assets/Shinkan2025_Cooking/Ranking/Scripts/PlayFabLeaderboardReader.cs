using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using System.Text;
using Cysharp.Threading.Tasks;

/// <summary>
/// PlayFabのリーダーボードから情報を取得し、ログに出力するクラス
/// Start()時に自動的に処理を開始する
/// </summary>
public class PlayFabLeaderboardReader : MonoBehaviour
{
    [Tooltip("取得するリーダーボードの統計名")]
    [SerializeField]
    private string statisticName = "HighScores";

    [Tooltip("取得するランキングの上限")]
    [SerializeField]
    private int maxResultsCount = 10;

    // ▼▼▼▼▼ 変更点 ▼▼▼▼▼
    /// <summary>
    /// このコンポーネントが有効になった最初のフレームで呼び出される
    /// </summary>
    private async UniTask Start()
    {
        // ログインが完了していることを確認してから処理を開始する
        // 注意：このスクリプトが実行される時点でPlayFabにログイン済みである必要があります。
        await UniTask.Delay(TimeSpan.FromSeconds(5f));
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            
            FetchLeaderboardData();
        }
        else
        {
            Debug.LogError("PlayFabに未ログインのため、リーダーボードを取得できません。ログイン処理の後に実行してください。");
        }
    }
    // ▲▲▲▲▲ 変更点はここまで ▲▲▲▲▲

    /// <summary>
    /// リーダーボードのデータ取得処理を開始する
    /// </summary>
    private void FetchLeaderboardData()
    {
        Debug.Log($"リーダーボード「{statisticName}」の取得を開始します...");

        var leaderboardRequest = new GetLeaderboardRequest
        {
            StatisticName = this.statisticName,
            StartPosition = 0,
            MaxResultsCount = this.maxResultsCount,
            ProfileConstraints = new PlayerProfileViewConstraints
            {
                ShowDisplayName = true
            }
        };

        PlayFabClientAPI.GetLeaderboard(leaderboardRequest, OnGetLeaderboardSuccess, OnFailure);
    }

    private void OnGetLeaderboardSuccess(GetLeaderboardResult result)
    {
        Debug.Log("リーダーボードの基本情報を取得しました。各プレイヤーのUserDataを取得します...");

        if (result.Leaderboard.Count == 0)
        {
            Debug.Log("リーダーボードにエントリーがありません。");
            return;
        }

        foreach (var entry in result.Leaderboard)
        {
            var userDataRequest = new GetUserDataRequest { PlayFabId = entry.PlayFabId };

            string displayName = entry.DisplayName;
            int score = entry.StatValue;

            PlayFabClientAPI.GetUserData(userDataRequest, (userDataResult) =>
            {
                OnGetUserDataSuccess(userDataResult, displayName, score);
            }, OnFailure);
        }
    }
    
    private void OnGetUserDataSuccess(GetUserDataResult result, string displayName, int score)
    {
        var logMessage = new StringBuilder();
        logMessage.AppendLine($"--- Name: {displayName}, Score: {score} ---");

        if (result.Data != null && result.Data.Count > 0)
        {
            logMessage.AppendLine("  ▼ Custom UserData:");
            foreach (var data in result.Data)
            {
                logMessage.AppendLine($"    - {data.Key}: {data.Value.Value}");
            }
        }
        else
        {
            logMessage.AppendLine("  - Custom UserDataはありません。");
        }
        
        Debug.Log(logMessage.ToString());
    }

    private void OnFailure(PlayFabError error)
    {
        Debug.LogError("PlayFab API呼び出しに失敗しました。");
        Debug.LogError(error.GenerateErrorReport());
    }
}