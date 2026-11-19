using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class RocketSkatesSequence : MonoBehaviour
    {
        public Transform gordon, rider, stop, barrier, dust;
        public Transform closeShot, runShot, impactShot;
        public Transform[] wheels, trails;
        public Transform[] cones;
        public AudioSource speaker;
        public AudioClip ignition, glide, brake, crash;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses = new GordonPoses(gordon);

        public IEnumerator Play()
        {
            Playing = true;
            Shot(closeShot, 1.83f);
            while (ScreenFader.IsBusy) yield return null;
            yield return new WaitForSeconds(.15f);
            Vector3 start = rider.position;
            speaker.PlayOneShot(ignition, .71f);
            yield return Over(.33f, t =>
            {
                poses.Set("Coil", Ease(t));
                SetTrails(.13f + .31f * Ease(t));
            });
            yield return new WaitForSeconds(.13f);
            speaker.PlayOneShot(glide, .70f);
            yield return Over(.37f, t => Advance(start, .31f * t));
            Shot(runShot, 3.55f);
            var camera = Camera.main;
            yield return Over(.83f, t =>
            {
                Advance(start, Mathf.Lerp(.31f, 1, t));
                if (camera != null)
                    camera.transform.position = runShot.position + Vector3.left * (1.65f * t);
            });

            Shot(impactShot, 2.38f);
            speaker.PlayOneShot(brake, .67f);
            yield return Over(.20f, t =>
            {
                poses.Set("Brace", Ease(t));
                rider.position = stop.position + Vector3.left * (.31f * Ease(t));
                SetTrails(1 - Ease(t));
            });
            speaker.PlayOneShot(crash, .84f);
            dust.gameObject.SetActive(true);
            var rest = barrier.localRotation;
            var coneA = cones[0].localRotation;
            var coneB = cones[1].localRotation;
            yield return Over(.40f, t =>
            {
                float amount = Ease(t);
                dust.localScale = Vector3.one * Mathf.Lerp(.29f, 1.51f, amount);
                barrier.localRotation = rest * Quaternion.Euler(73 * amount, 0, -9 * amount);
                cones[0].localRotation = coneA * Quaternion.Euler(57 * amount, 0, 28 * amount);
                cones[1].localRotation = coneB * Quaternion.Euler(-39 * amount, 0, 56 * amount);
                if (t >= .69f && rider.gameObject.activeSelf) rider.gameObject.SetActive(false);
            });
            yield return Over(.30f, t => dust.localScale = Vector3.one * Mathf.Lerp(1.51f, .12f, Ease(t)));
            dust.gameObject.SetActive(false);
            yield return new WaitForSeconds(.30f);
            Playing = false;
        }

        void Advance(Vector3 start, float t)
        {
            float progress = t * t;
            float drift = .31f * Mathf.Sin(t * Mathf.PI * 2) * Mathf.Sin(t * Mathf.PI);
            rider.SetPositionAndRotation(Vector3.Lerp(start, stop.position, progress) + Vector3.forward * drift,
                Quaternion.Euler(0, -90 - 13 * Mathf.Sin(t * Mathf.PI * 3) * Mathf.Sin(t * Mathf.PI), 0));
            float wobble = Mathf.Sin(t * Mathf.PI) * (.49f + .40f * Mathf.Sin(t * Mathf.PI * 4));
            float lean = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .32f, t));
            poses.Set("Coil", 1 - lean);
            poses.Set("Glide", lean * (1 - wobble) * (1 - Mathf.InverseLerp(.76f, 1, t)));
            poses.Set("Wobble", lean * wobble * (1 - Mathf.InverseLerp(.76f, 1, t)));
            foreach (var wheel in wheels)
                wheel.localRotation = Quaternion.Euler(-progress * 1900, 0, 0);
            SetTrails(.38f + .62f * progress);
        }

        void SetTrails(float amount)
        {
            foreach (var trail in trails)
            {
                trail.gameObject.SetActive(amount > .04f);
                trail.localScale = new Vector3(.72f + .28f * amount, .77f + .23f * amount, .30f + 1.75f * amount);
            }
        }

        static void Shot(Transform view, float size)
        {
            if (Camera.main is not Camera camera) return;
            camera.transform.SetPositionAndRotation(view.position, view.rotation);
            camera.orthographicSize = size;
        }

        static float Ease(float t) => Mathf.SmoothStep(0, 1, t);

        static IEnumerator Over(float seconds, Action<float> frame)
        {
            for (float time = 0; time < seconds; time += Time.deltaTime)
            {
                frame(time / seconds);
                yield return null;
            }
            frame(1);
        }
    }
}
