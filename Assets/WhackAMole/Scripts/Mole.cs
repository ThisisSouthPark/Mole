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

        // 체력 변수 추가 (2번 클릭해야 하므로 기본값 2)
        private int hp = 2;

        private void Awake() { visual = GetComponent<SpriteRenderer>(); hitbox = GetComponent<Collider2D>(); Hide(); }

        private void Show(MoleState state, Sprite sprite, bool hittable)
        {
            State = state;
            visual.sprite = sprite;
            visual.enabled = state != MoleState.Hidden;
            hitbox.enabled = hittable;
        }
        public void Hide() => Show(MoleState.Hidden, idleSprite, false);

        public IEnumerator Appear(float transition, float duration)
        {
            // 나타날 때마다 체력을 다시 2로 리셋
            hp = 2;

            Show(MoleState.Rising, downSprite, false);
            yield return new WaitForSeconds(transition);
            Show(MoleState.Up, upSprite, true);
            float end = Time.time + duration;

            // Up 상태이거나 체력이 아직 남아있는 동안 대기
            while (State == MoleState.Up && hp > 0 && Time.time < end) yield return null;

            if (State == MoleState.Hit) yield return new WaitForSeconds(transition);
            Show(MoleState.Falling, downSprite, false);
            yield return new WaitForSeconds(transition);
            Hide();
        }

        // bool 반환값을 '완전 사망 처리가 되었는지' 여부로 사용합니다.
        public bool TryHit()
        {
            if (State != MoleState.Up || hp <= 0) return false;

            hp--; // 타격 시 체력 감소
            onHit.Invoke();

            if (hp <= 0)
            {
                // 두 번 맞아서 체력이 0이 되었을 때만 Hit 이미지로 변경 및 사망 처리
                Show(MoleState.Hit, hitSprite, false);
                return true;
            }

            // 첫 번째 탭일 때는 점수/히트 카운트는 올려주되, 상태는 여전히 Up을 유지 (아직 안 죽음)
            return false;
        }
    }
}
