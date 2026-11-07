using System;
using System.Collections;
using UnityEditor.Rendering.LookDev;
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

        private void Start() { if (autoStart) StartGame(); }
        public void StartGame()
        {
            StopAllCoroutines();
            IsPlaying = false;
            if (!board || !board.Build()) { Debug.LogError("Board, Config, Mole Prefab을 연결하세요.", this); return; }
            if (rules) Destroy(rules);
            // Freeze rules for this round; config edits apply at the next start.
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
                yield return board.Moles[index].Appear(rules.transitionSeconds, rules.upSeconds);
                index = UnityEngine.Random.Range(0, board.Moles.Length); //(index + 1) % board.Moles.Length;
                yield return new WaitForSeconds(rules.intervalSeconds);
            }
        }
        private void Update()
        {
            if (!IsPlaying) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (Remaining <= 0) EndGame();
            else Changed?.Invoke();
        }

        public void Attempt(Mole target)
        {
            if (!IsPlaying || Remaining <= 0) return;

            // 두더지가 지상에 있고 타격 가능한 상태일 때
            if (target && target.State == MoleState.Up)
            {
                // 타격 성공 시 매번 카메라 흔들기 실행 (기본값: 0.15초 동안 0.2 세기로)
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.Shake(0.15f, 0.2f);
                }

                // 체력 깎기 실행 (이전 답변 코드 참고)
                bool isDead = target.TryHit();

                // 점수 및 타격 횟수 증가
                Hits++;
                Score += rules.pointsPerHit;
            }
            else
            {
                // 허공을 쳤거나 이미 들어가는 두더지를 친 경우
                Misses++;
            }
            Changed?.Invoke();
        }

        /*public void Attempt(Mole target)
        {
            if (!IsPlaying || Remaining <= 0) return;
            if (target && target.TryHit()) { Hits++; Score += rules.pointsPerHit; }
            else Misses++;
            Changed?.Invoke();
        }*/


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
