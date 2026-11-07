using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhackAMole
{
    public sealed class GameManager : MonoBehaviour
    {
        public BoardLayout board;
        public bool autoStart = true;
        public bool saveBest = true;
        public const string BestKey = "WhackAMole.Best.v1";
        public bool IsPlaying { get; private set; }
        public float Remaining { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public string Rank { get; private set; }
        public event Action Changed;
        public event Action Finished;
        private Coroutine sequence;
        private BoardConfig rules;

        [Header("Evasive Mole Settings")]
        [Range(0f, 1f)] public float evasiveProbability = 0.3f; // 30% 확률로 회피형 두더지 등장

        private void Start() { if (autoStart) StartGame(); }

        public void StartGame()
        {
            StopAllCoroutines();
            IsPlaying = false;
            if (!board || !board.Build()) { Debug.LogError("Board, Config, Mole Prefab을 연결하세요.", this); return; }
            if (rules) Destroy(rules);
            rules = Instantiate(board.config);
            Hits = Misses = Score = 0;
            Remaining = rules.gameSeconds;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Rank = "B";
            IsPlaying = true;
            Changed?.Invoke();
            sequence = StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            int index = 0;
            while (IsPlaying)
            {
                // 등장할 때 랜덤 확률로 회피형 두더지 설정 (예: 30% 확률)
                bool isEvasive = UnityEngine.Random.value < evasiveProbability;
                board.Moles[index].Setup(index, isEvasive);

                yield return board.Moles[index].Appear(rules.transitionSeconds, rules.upSeconds);
                index = UnityEngine.Random.Range(0, board.Moles.Length);
                yield return new WaitForSeconds(rules.intervalSeconds);
            }
        }

        public void Attempt(Mole target)
        {
            if (!IsPlaying || Remaining <= 0) return;

            if (target && target.State == MoleState.Up)
            {
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.15f, 0.2f);
                }

                bool isDead = target.TryHit();

                // 도망치지 않고 실제로 맞아서 체력이 깎였거나(일반 두더지) 죽었을 때만 점수 획득
                if (!target.isEvasive)
                {
                    Hits++;
                    Score += rules.pointsPerHit;
                }
            }
            else
            {
                Misses++;
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// 회피형 두더지가 상하좌우 인접한 칸 중 비어있는 곳으로 순간이동하도록 처리합니다.
        /// </summary>
        public void TryEadeMole(Mole mole)
        {
            int r = rules.rows;
            int c = rules.columns;
            int curIndex = mole.gridIndex;

            int curRow = curIndex / c;
            int curCol = curIndex % c;

            // 상하좌우 탐색 레이아웃 생성
            List<int> neighbors = new List<int>();
            if (curRow > 0) neighbors.Add(curIndex - c); // 상
            if (curRow < r - 1) neighbors.Add(curIndex + c); // 하
            if (curCol > 0) neighbors.Add(curIndex - 1); // 좌
            if (curCol < c - 1) neighbors.Add(curIndex + 1); // 우

            // 현재 다른 두더지가 튀어나와 있거나(Up/Rising), 예약되지 않은 비어있는 인덱스 필터링
            List<int> validIndices = new List<int>();
            foreach (int nIndex in neighbors)
            {
                if (board.Moles[nIndex].State == MoleState.Hidden)
                {
                    validIndices.Add(nIndex);
                }
            }

            // 도망칠 빈 구멍이 있다면 위치 이동
            if (validIndices.Count > 0)
            {
                int targetIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];

                // 1. 기존 구멍의 상태를 숨김 처리
                mole.Hide();

                // 2. 새로운 구멍의 두더지 객체를 가져와 스왑하거나, 
                // 간단히 현재 두더지의 물리적 위치를 해당 빈 칸의 좌표로 변경하고 상태를 유지합니다.
                mole.transform.localPosition = board.LocalPositions[targetIndex];
                mole.Setup(targetIndex, true); // 새 위치 인덱스로 리셋
                mole.StartCoroutine(mole.Appear(0f, rules.upSeconds * 0.7f)); // 즉시 튀어나옴 (남은시간 살짝 보정)
            }
        }

        public void EndGame()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (sequence != null) StopCoroutine(sequence);
            foreach (var mole in board.Moles) if (mole) mole.Hide();
            Best = Mathf.Max(Best, Score);
            if (saveBest) { PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); }
            Rank = Score >= rules.rankS ? "S" : Score >= rules.rankA ? "A" : "B";
            Changed?.Invoke();
            Finished?.Invoke();
        }
        private void OnDestroy() { if (rules) Destroy(rules); }
    }
}
