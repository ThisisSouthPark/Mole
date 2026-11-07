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

        [Header("Death Animation Settings")]
        [Min(0.1f)] public float deathDuration = 0.4f;
        public float maxScaleMultiplier = 1.4f;
        public float totalRotationAngle = 360f;
        private Vector3 originalScale;

        [Header("Evasion Settings (New)")]
        public bool isEvasive = false;   // 이번에 나타난 두더지가 '회피형'인지 여부
        public int gridIndex = 0;        // 현재 보드판에서의 인덱스 위치 기억

        private void Awake()
        {
            visual = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<Collider2D>();
            originalScale = transform.localScale;
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
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
            Show(MoleState.Hidden, idleSprite, false);
        }

        // Setup 메서드를 통해 등장할 때 특성을 부여합니다.
        public void Setup(int index, bool evasive)
        {
            gridIndex = index;
            isEvasive = evasive;
            hp = isEvasive ? 1 : 2; // 회피형 두더지는 체력 1, 일반은 2
        }

        public IEnumerator Appear(float transition, float duration)
        {
            // 나타날 때 기본 위치의 로컬 포지션으로 리셋 (순간이동했다가 복귀 대비)
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;

            Show(MoleState.Rising, downSprite, false);
            yield return new WaitForSeconds(transition);
            Show(MoleState.Up, upSprite, true);
            float end = Time.time + duration;

            while (State == MoleState.Up && hp > 0 && Time.time < end) yield return null;

            if (State == MoleState.Hit)
            {
                yield break;
            }

            Show(MoleState.Falling, downSprite, false);
            yield return new WaitForSeconds(transition);
            Hide();
        }

        // 2D 콜라이더에 마우스가 닿았을 때 자동으로 호출되는 유니티 이벤트 함수
        private void OnMouseEnter()
        {
            if (State == MoleState.Up && isEvasive)
            {
                TriggerEvasion();
            }
        }

        // 도망치기(순간이동) 핵심 로직
        public void TriggerEvasion()
        {
            // 게임 매니저에게 상하좌우 비어있는 좌표를 요청하여 순간이동
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.TryEadeMole(this);
            }
        }

        public bool TryHit()
        {
            if (State != MoleState.Up || hp <= 0) return false;

            // 회피형 두더지인데 클릭을 당했다면 도망치기를 시도합니다.
            if (isEvasive)
            {
                TriggerEvasion();
                return false; // 죽지 않고 도망쳤으므로 false 반환
            }

            hp--;
            onHit.Invoke();

            if (hp <= 0)
            {
                Show(MoleState.Hit, hitSprite, false);
                StartCoroutine(PlayDeathAnimation());
                return true;
            }

            return false;
        }

        private IEnumerator PlayDeathAnimation()
        {
            float elapsed = 0f;
            while (elapsed < deathDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / deathDuration;
                float scaleProgress = (t < 0.3f) ? Mathf.Lerp(1f, maxScaleMultiplier, t / 0.3f) : Mathf.Lerp(maxScaleMultiplier, 0f, (t - 0.3f) / 0.7f);
                transform.localScale = originalScale * scaleProgress;
                transform.localRotation = Quaternion.Euler(0f, 0f, t * totalRotationAngle);
                yield return null;
            }
            Hide();
        }
    }
}
