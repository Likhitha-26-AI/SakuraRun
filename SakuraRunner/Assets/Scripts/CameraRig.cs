using UnityEngine;

namespace SakuraRunner
{
    /// <summary>Third-person follow camera with menu orbit, speed FOV and shake.</summary>
    public class CameraRig : MonoBehaviour
    {
        public PlayerController Player;
        public Camera Cam;

        float shakeAmp, shakeTime;
        Vector3 smoothPos;
        Quaternion smoothRot = Quaternion.identity;
        bool init;

        public void Shake(float amp, float duration)
        {
            shakeAmp = amp;
            shakeTime = duration;
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || Player == null || Cam == null) return;

            float dt = Time.deltaTime;
            Vector3 pp = Player.transform.position;
            Vector3 desiredPos, lookAt;
            float k;

            bool showcase = gm.State == GameState.Menu || gm.State == GameState.GameOver;
            if (showcase)
            {
                float sway = Mathf.Sin(Time.unscaledTime * 0.4f);
                desiredPos = pp + new Vector3(2.3f + sway * 0.8f, 1.7f, 3.9f);
                lookAt = pp + new Vector3(0f, 1.05f, 0f);
                k = 3f;
            }
            else
            {
                desiredPos = new Vector3(pp.x * 0.62f, 4.3f + pp.y * 0.3f, -7.6f);
                lookAt = new Vector3(pp.x * 0.85f, 1.5f + pp.y * 0.25f, 6f);
                k = 7f;
            }

            float a = 1f - Mathf.Exp(-k * dt);
            if (!init)
            {
                smoothPos = desiredPos;
                smoothRot = Quaternion.LookRotation(lookAt - desiredPos);
                init = true;
            }
            smoothPos = Vector3.Lerp(smoothPos, desiredPos, a);
            smoothRot = Quaternion.Slerp(smoothRot, Quaternion.LookRotation(lookAt - smoothPos), a * 1.2f);

            Vector3 offset = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float f = Mathf.Clamp01(shakeTime * 4f);
                offset = Random.insideUnitSphere * shakeAmp * f;
            }

            transform.position = smoothPos + offset;
            transform.rotation = smoothRot;

            float speedT = Mathf.InverseLerp(GameManager.BaseSpeed, GameManager.MaxSpeed * 1.6f, gm.Speed);
            float fov = showcase ? 45f : 58f + speedT * 14f;
            Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, fov, 1f - Mathf.Exp(-3f * dt));
        }
    }
}
