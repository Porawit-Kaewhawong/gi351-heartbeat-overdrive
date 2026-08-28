using System.Collections;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// รวมฟีดแบ็กความสะใจ: Screen Shake + Hit Freeze + สั่งเล่น SFX
    /// </summary>
    public class FeedbackDirector : MonoBehaviour
    {
        public GameConfig config;
        public Camera targetCamera;
        public AudioDirector audioDirector;

        Vector3 camHome;
        float shakeAmp;
        Coroutine hitStopCo;

        void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null) camHome = targetCamera.transform.localPosition;
        }

        public void OnPerfect()
        {
            Shake(config.shakePerfect);
            HitStop(config.hitStopDuration);
            audioDirector.PlayPerfect();
        }

        public void OnGreat()
        {
            Shake(config.shakePerfect * 0.4f);
            audioDirector.PlayGreat();
        }

        public void OnMiss()
        {
            Shake(config.shakeMiss);
            audioDirector.PlayMiss();
        }

        void Shake(float amount) { shakeAmp = Mathf.Max(shakeAmp, amount); }

        void HitStop(float duration)
        {
            if (hitStopCo != null) StopCoroutine(hitStopCo);
            hitStopCo = StartCoroutine(HitStopCo(duration));
        }

        IEnumerator HitStopCo(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            hitStopCo = null;
        }

        void OnDisable() { Time.timeScale = 1f; }

        void LateUpdate()
        {
            if (targetCamera == null) return;
            if (shakeAmp > 0.001f)
            {
                targetCamera.transform.localPosition =
                    camHome + (Vector3)(Random.insideUnitCircle * shakeAmp);
                shakeAmp = Mathf.Lerp(shakeAmp, 0f, Time.unscaledDeltaTime * 8f);
            }
            else
            {
                targetCamera.transform.localPosition = camHome;
            }
        }
    }
}
