using System.Collections;
using UnityEngine;

namespace WhackAMole
{
    public sealed class CameraShake : MonoBehaviour
    {
        // 외부에서 싱글톤처럼 쉽게 접근할 수 있도록 인스턴스 지정
        public static CameraShake Instance { get; private set; }

        private Vector3 originalPosition;
        private Coroutine shakeRoutine;

        private void Awake()
        {
            Instance = this;
            originalPosition = transform.localPosition;
        }

        /// <summary>
        /// 카메라를 흔드는 루틴을 시작합니다.
        /// </summary>
        /// <param name="duration">흔들릴 시간(초)</param>
        /// <param name="magnitude">흔들림 세기</param>
        public void Shake(float duration = 0.15f, float magnitude = 0.2f)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            shakeRoutine = StartCoroutine(Animate(duration, magnitude));
        }

        private IEnumerator Animate(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 무작위로 위치 이동 (2D/3D 범용을 위해 Z축은 고정)
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 흔들림이 끝나면 원위치로 복귀
            transform.localPosition = originalPosition;
            shakeRoutine = null;
        }
    }
}
