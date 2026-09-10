using DG.Tweening;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckFeedback : MonoBehaviour
    {
        sealed class Flight
        {
            public GameObject visual;
            public Transform target;
            public Vector3 start;
            public float progress;
            public bool depositing;
            public Tween tween;
            public void Animate(float t)
            {
                progress = t;
                Vector3 destination = target.position;
                visual.transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * (depositing ? 1.2f : .3f));
                visual.transform.localScale = Vector3.one * Mathf.Lerp(1, depositing ? .18f : .1f, t * t);
            }
            public void Finish() { visual.SetActive(false); }
        }
        Flight[] pool;
        AudioSource pickupAudio, depositAudio;
        AudioClip pickupClip, depositClip;
        int cursor;
        float nextSound;
        public void Initialize(DuckPopulationManager population, int size)
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(256, 32);
            pool = new Flight[size];
            for (int i = 0; i < size; i++)
            {
                var f = pool[i] = new Flight { visual = population.CreateVisual(transform) };
                f.tween = DOTween.To(() => f.progress, f.Animate, 1f, .34f).SetEase(Ease.InQuad).SetAutoKill(false).Pause().OnComplete(f.Finish);
            }
            pickupAudio = gameObject.AddComponent<AudioSource>(); pickupAudio.playOnAwake = false; pickupAudio.volume = .2f;
            depositAudio = gameObject.AddComponent<AudioSource>(); depositAudio.playOnAwake = false; depositAudio.volume = .28f;
            pickupClip = Tone("Rubber squeak", .09f, 580, 970);
            depositClip = Tone("Deposit chime", .45f, 520, 1040);
            pickupAudio.clip = pickupClip; depositAudio.clip = depositClip;
        }
        static AudioClip Tone(string name, float duration, float low, float high)
        {
            const int rate = 22050; var samples = new float[(int)(rate * duration)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length; phase += Mathf.Lerp(low, high, t) * 2 * Mathf.PI / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * (1 - t) * .55f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public void Fly(Vector3 from, float angle, Transform target, bool depositing = false)
        {
            Flight f = null;
            for (int i = 0; i < pool.Length; i++)
            {
                var candidate = pool[(cursor + i) % pool.Length];
                if (candidate.visual.activeSelf) continue;
                f = candidate; cursor = (cursor + i + 1) % pool.Length; break;
            }
            if (f == null) return; // Visual budget is capped; the transaction still succeeds.
            f.target = target; f.start = from; f.depositing = depositing;
            f.visual.transform.SetPositionAndRotation(from, Quaternion.Euler(0, angle, 0));
            f.visual.transform.localScale = Vector3.one; f.visual.SetActive(true); f.tween.Restart();
        }
        public void PickupSound(float fullness)
        {
            if (Time.unscaledTime < nextSound) return;
            nextSound = Time.unscaledTime + .065f; pickupAudio.pitch = .9f + fullness * .5f; pickupAudio.Play();
        }
        public void DepositSound(int amount) { depositAudio.pitch = Mathf.Clamp(1 + amount * .001f, 1, 1.3f); depositAudio.Play(); }
        void OnDestroy()
        {
            if (pool != null) foreach (var f in pool) f.tween.Kill();
            if (pickupClip != null) Destroy(pickupClip); if (depositClip != null) Destroy(depositClip);
        }
    }
}
