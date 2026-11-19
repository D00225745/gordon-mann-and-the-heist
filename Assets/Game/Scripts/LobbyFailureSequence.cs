using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class LobbyFailureSequence : MonoBehaviour
    {
        public enum Gag { Statue, Chair, Alarm }

        [Serializable]
        public struct Responder
        {
            public Transform actor, leftLeg, rightLeg, stop;
        }

        public Gag gag;
        public Transform gordon, closeShot, reactionShot, prop, actionMark, effect;
        public Transform patrolHead, chairTurn;
        public Transform[] casters;
        public Responder[] responders;
        public Light beacon;
        public AudioSource speaker;
        public AudioClip firstCue, impactCue, lastCue;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses = new GordonPoses(gordon);

        public IEnumerator Play()
        {
            Playing = true;
            Shot(closeShot, gag == Gag.Chair ? 3.42f : 2.04f);
            if (gag == Gag.Chair) poses.Set("Seated", 1);
            while (ScreenFader.IsBusy) yield return null;
            yield return new WaitForSeconds(.22f);
            yield return gag switch
            {
                Gag.Statue => Statue(),
                Gag.Chair => Chair(),
                Gag.Alarm => Alarm(),
                _ => throw new InvalidOperationException("Unknown lobby gag.")
            };
            Playing = false;
        }

        IEnumerator Statue()
        {
            speaker.PlayOneShot(firstCue, .48f);
            yield return new WaitForSeconds(.41f);
            yield return Over(.33f, t => poses.Set("Tickle", .42f * Mathf.Sin(Mathf.PI * t)));
            yield return new WaitForSeconds(.27f);
            yield return Over(.28f, t => poses.Set("Tickle", Ease(t)));
            speaker.PlayOneShot(impactCue, .87f);
            effect.gameObject.SetActive(true);
            Vector3 puffSize = effect.localScale;
            Vector3 puffOrigin = effect.localPosition;
            yield return Over(.28f, t =>
            {
                poses.Set("Tickle", 1 - Ease(t));
                poses.Set("Sneeze", Ease(t));
                effect.localScale = puffSize * Mathf.Lerp(.22f, 1.33f, Ease(t));
                effect.localPosition = puffOrigin + gordon.forward * (.31f * Ease(t));
            });
            effect.gameObject.SetActive(false);
            yield return Over(.30f, t => poses.Set("Sneeze", 1 - Ease(t)));
            Shot(reactionShot, 2.83f);
            var start = patrolHead.localRotation;
            Vector3 look = patrolHead.position - (gordon.position + Vector3.up * 1.6f);
            Quaternion glance = Quaternion.Inverse(patrolHead.parent.rotation) * Quaternion.LookRotation(look);
            yield return Over(.36f, t => patrolHead.localRotation = Quaternion.Slerp(start, glance, Ease(t)));
            speaker.PlayOneShot(lastCue, .61f);
            yield return new WaitForSeconds(.50f);
        }

        IEnumerator Chair()
        {
            Vector3 origin = prop.position;
            Vector3 rim = actionMark.position;
            Vector3 turn = chairTurn.position;
            speaker.PlayOneShot(firstCue, .65f);
            yield return Over(1.20f, t =>
            {
                float u = t * t;
                var near = Vector3.Lerp(origin, turn, u);
                var far = Vector3.Lerp(turn, rim, u);
                prop.SetPositionAndRotation(Vector3.Lerp(near, far, u), Quaternion.LookRotation(far - near));
                foreach (var caster in casters)
                    caster.localRotation = Quaternion.Euler(-660 * u, 0, 0);
                float brake = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f, 1, t));
                poses.Set("Seated", 1 - brake);
                poses.Set("SeatedBrake", brake);
            });
            Shot(reactionShot, 2.35f);
            var facing = prop.rotation;
            speaker.PlayOneShot(lastCue, .73f);
            yield return Over(.34f, t =>
            {
                prop.position = rim + facing * Vector3.forward * (.94f * t) + Vector3.up * (.39f * Mathf.Sin(t * Mathf.PI));
                prop.rotation = facing * Quaternion.Euler(62 * Ease(t), 0, -12 * Ease(t));
            });
            speaker.PlayOneShot(impactCue, .88f);
            effect.gameObject.SetActive(true);
            prop.gameObject.SetActive(false);
            var droplets = new Vector3[effect.childCount];
            for (int i = 0; i < droplets.Length; i++) droplets[i] = effect.GetChild(i).localPosition;
            yield return Over(.57f, t =>
            {
                for (int i = 0; i < droplets.Length; i++)
                {
                    var drop = effect.GetChild(i);
                    drop.localPosition = droplets[i] * (.28f + t * .89f)
                        + Vector3.up * ((.61f + (i % 3) * .17f) * Mathf.Sin(Mathf.PI * t));
                    float size = Mathf.Lerp(.205f - (i % 4) * .024f, .058f, Ease(t));
                    drop.localScale = new Vector3(size, size * (1.21f + (i % 3) * .36f), size);
                }
            });
            effect.gameObject.SetActive(false);
            yield return new WaitForSeconds(.25f);
        }

        IEnumerator Alarm()
        {
            yield return Over(.44f, t => poses.Set("Press", Ease(t)));
            speaker.PlayOneShot(firstCue, .70f);
            var pullStart = prop.localRotation;
            yield return Over(.16f, t => prop.localRotation = pullStart * Quaternion.Euler(-32 * Ease(t), 0, 0));
            effect.gameObject.SetActive(true);
            beacon.enabled = true;
            speaker.PlayOneShot(impactCue, .59f);
            Vector3 start = gordon.position;
            var rotation = gordon.rotation;
            yield return Over(.40f, t =>
            {
                gordon.SetPositionAndRotation(Vector3.Lerp(start, actionMark.position, Ease(t)),
                    Quaternion.Slerp(rotation, actionMark.rotation, Ease(t)));
                poses.Set("Press", 1 - Ease(t));
                poses.Set("Caught", Ease(t));
                beacon.intensity = Mathf.Lerp(.25f, 2.4f, Mathf.Sin(t * Mathf.PI));
            });
            Shot(reactionShot, 3.08f);
            var starts = new Vector3[responders.Length];
            for (int i = 0; i < responders.Length; i++)
            {
                var responder = responders[i];
                starts[i] = responder.actor.position;
                responder.actor.rotation = Quaternion.LookRotation(responder.stop.position - starts[i]);
            }
            yield return Over(1.34f, t =>
            {
                beacon.intensity = 1.05f + .58f * Mathf.Cos(2.3f * Mathf.PI * t);
                for (int i = 0; i < responders.Length; i++)
                {
                    var responder = responders[i];
                    float progress = Mathf.InverseLerp(i * .10f, .76f + i * .08f, t);
                    if (progress <= 0) continue;
                    if (!responder.actor.gameObject.activeSelf) responder.actor.gameObject.SetActive(true);
                    float swing = 23 * Mathf.Sin(progress * 8 * Mathf.PI) * Mathf.Sin(progress * Mathf.PI);
                    responder.actor.position = Vector3.Lerp(starts[i], responder.stop.position, Ease(progress));
                    responder.leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
                    responder.rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
                }
            });
            speaker.PlayOneShot(lastCue, .67f);
            yield return new WaitForSeconds(.39f);
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
