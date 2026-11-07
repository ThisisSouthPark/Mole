using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace WhackAMole
{
    public enum MoleState { Hidden, Rising, Up, Hit, Falling }

    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class Mole : MonoBehaviour
    {
        public Sprite idleSprite, upSprite, hitSprite, downSprite;
        public UnityEvent onHit = new UnityEvent();
        public MoleState State { get; private set; }
        private SpriteRenderer visual;
        private Collider2D hitbox;

        private int hp = 2;

        // 연출용 변수 추가
        [Header("Death Animation Settings")]
        [Min(0.1f)] public float deathDuration = 0.4f; // 연출 총 시간
        public float maxScaleMultiplier = 1.4f;       // 커졌을 때 최대 크기 배율
        public float totalRotationAngle = 360f;        // 회전할 총 각도 (360도 = 1바퀴)

        private Vector3 originalScale;                 // 두더지의 원래 스케일 저장

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            originalScale = transform.localScale; // 게임 시작 시 원래 크기 기억
            Hide();
        }

        private void Show(MoleState state, Sprite sprite, bool hittable)
        {
            State = state;
            visual.sprite = sprite;
            visual.enabled = state != MoleState.Hidden;
            hitbox.enabled = hittable;
        }

        public void Hide()
        {
            // 숨을 때는 크기와 회전값을 원래대로 초기화
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
            Show(MoleState.Hidden, idleSprite, false);
        }

        public IEnumerator Appear(float transition, float duration)
        {
            hp = 2;
            // 다시 나타날 때 스케일/회전 리셋 확인
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;

            Show(MoleState.Rising, downSprite, false);
            yield return new WaitForSeconds(transition);
            Show(MoleState.Up, upSprite, true);
            float end = Time.time + duration;

            while (State == MoleState.Up && hp > 0 && Time.time < end) yield return null;

            // 만약 체력이 다 닳아서 죽은 거라면, 일반 Falling 과정을 거치지 않고 종료 (Appear 루틴 내부 탈출)
            if (State == MoleState.Hit)
            {
                yield break;
            }

            Show(MoleState.Falling, downSprite, false);
            yield return new WaitForSeconds(transition);
            Hide();
        }

        public bool TryHit()
        {
            if (State != MoleState.Up || hp <= 0) return false;

            hp--;
            onHit.Invoke();

            if (hp <= 0)
            {
                Show(MoleState.Hit, hitSprite, false);
                // 사망 시 크기가 변하며 뱅글뱅글 도는 연출 루틴 실행
                StartCoroutine(PlayDeathAnimation());
                return true;
            }

            return false;
        }

        // 사망 연출 코루틴
        private IEnumerator PlayDeathAnimation()
        {
            float elapsed = 0f;

            while (elapsed < deathDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / deathDuration; // 0에서 1까지 진행률

                // 1. 크기 변화 연출 (Sin 그래프 활용: 원래 크기 -> 커졌다가 -> 0으로 수렴)
                float scaleProgress;
                if (t < 0.3f)
                {
                    // 초반 30% 동안 원래 크기(1)에서 최대 크기로 증가
                    scaleProgress = Mathf.Lerp(1f, maxScaleMultiplier, t / 0.3f);
                }
                else
                {
                    // 나머지 70% 동안 최대 크기에서 0(사라짐)으로 감소
                    scaleProgress = Mathf.Lerp(maxScaleMultiplier, 0f, (t - 0.3f) / 0.7f);
                }
                transform.localScale = originalScale * scaleProgress;

                // 2. 비틀림 회전 연출 (Z축 기준 뱅글뱅글)
                float currentAngle = t * totalRotationAngle;
                transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

                yield return null;
            }

            // 연출이 완전히 끝나면 최종 숨기기 및 리셋
            Hide();
        }
    }
}
