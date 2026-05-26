using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Match3.Unity
{
    /// <summary>Match3 리더보드 API HTTP 클라이언트 (WebGL UnityWebRequest)</summary>
    public class LeaderboardClient : MonoBehaviour
    {
        [SerializeField] private string _apiUrl = "https://games.olivilo.shop/api/match3/leaderboard";

        [Serializable]
        public class Entry
        {
            public int id;
            public string player_name;
            public int score;
            public int max_combo;
            public int total_moves;
            public string played_at;
            public int rank; // POST 응답에만 포함
        }

        [Serializable]
        public class LeaderboardResponse
        {
            public int total;
            public List<Entry> items;
        }

        /// <summary>점수 제출 → 등록된 Entry + rank 반환</summary>
        public IEnumerator SubmitScore(string playerName, int score, int maxCombo, int totalMoves,
            Action<Entry> onSuccess, Action<string> onError)
        {
            var json = JsonUtility.ToJson(new SubmitData
            {
                playerName = playerName,
                score = score,
                maxCombo = maxCombo,
                totalMoves = totalMoves
            });
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(_apiUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var entry = JsonUtility.FromJson<Entry>(req.downloadHandler.text);
                onSuccess?.Invoke(entry);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }

        /// <summary>상위 랭킹 조회</summary>
        public IEnumerator GetLeaderboard(int limit, Action<LeaderboardResponse> onSuccess, Action<string> onError)
        {
            using var req = UnityWebRequest.Get($"{_apiUrl}?limit={limit}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<LeaderboardResponse>(req.downloadHandler.text);
                onSuccess?.Invoke(response);
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }

        [Serializable]
        private class SubmitData
        {
            public string playerName;
            public int score;
            public int maxCombo;
            public int totalMoves;
        }
    }
}
