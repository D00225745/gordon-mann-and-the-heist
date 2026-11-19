using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class SecurityFailureSequence : MonoBehaviour
    {
        public enum Gag { PIN, Knock }
        static readonly string[] Digits = { "KeyOne", "KeyTwo", "KeyThree", "KeyFour" };
        public Gag gag;
        public Transform gordon, closeShot, outcomeShot;
        public SecurityKeypad keypad;
        public Transform door, guardHead, mug, guardHand, guardElbow, guardForearm, recoilMark;
        public AudioSource speaker;
        public AudioClip tap, answer, reaction;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses = new GordonPoses(gordon);

        public IEnumerator Play()
        {
            Playing = true;
            Shot(closeShot, gag == Gag.PIN ? 1.67f : 2.25f);
            while (ScreenFader.IsBusy) yield return null;
            yield return new WaitForSeconds(.20f);
            if (gag == Gag.PIN)
                yield return Guess();
            else
                yield return Knock();
            Playing = false;
        }

        IEnumerator Guess()
        {
            string prior = null;
            for (int i = 0; i < Digits.Length; i++)
            {
                int key = i;
                string channel = Digits[i];
                yield return Over(i == 0 ? .37f : .18f, t =>
                {
                    float amount = Ease(t);
                    if (prior != null) poses.Set(prior, .72f * (1 - amount));
                    poses.Set(channel, amount);
                });
                speaker.pitch = 1 + i * .065f;
                speaker.PlayOneShot(tap, .73f);
                yield return Over(.15f, t => keypad.Depress(key, Mathf.Sin(Mathf.PI * t)));
                keypad.Depress(i, 0);
                keypad.ShowEntry(i + 1);
                yield return Over(.105f, t => poses.Set(channel, Mathf.Lerp(1, .72f, Ease(t))));
                prior = channel;
            }
            yield return new WaitForSeconds(.27f);
            speaker.pitch = 1;
            speaker.PlayOneShot(answer, .78f);
            keypad.Reject();
            Vector3 start = gordon.position;
            var atDoor = gordon.rotation;
            yield return Over(.32f, t =>
            {
                float amount = Ease(t);
                poses.Set(prior, .72f * (1 - amount));
                poses.Set("Awkward", .92f * amount);
                gordon.SetPositionAndRotation(Vector3.Lerp(start, start + Vector3.back * .16f, amount),
                    Quaternion.Slerp(atDoor, Quaternion.Euler(0, 28, 0), amount));
            });
            yield return new WaitForSeconds(.39f);
        }

        IEnumerator Knock()
        {
            var stand = gordon.position;
            yield return Over(.32f, t => poses.Set("Knock", .78f * Ease(t)));
            for (int i = 0; i < 2; i++)
            {
                yield return Over(.092f, t =>
                {
                    poses.Set("Knock", Mathf.Lerp(.78f, 1, Ease(t)));
                    gordon.position = stand + Vector3.forward * (.019f * Ease(t));
                });
                speaker.pitch = i == 0 ? 1 : .94f;
                speaker.PlayOneShot(tap, i == 0 ? .74f : .63f);
                yield return Over(.122f, t =>
                {
                    poses.Set("Knock", Mathf.Lerp(1, .78f, Ease(t)));
                    gordon.position = stand + Vector3.forward * (.019f * (1 - Ease(t)));
                });
            }
            yield return Over(.24f, t => poses.Set("Knock", .78f * (1 - Ease(t))));
            yield return new WaitForSeconds(.37f);
            Shot(outcomeShot, 2.41f);
            speaker.pitch = 1;
            speaker.PlayOneShot(answer, .69f);
            var closed = door.localRotation;
            var face = gordon.rotation;
            yield return Over(.60f, t =>
            {
                float amount = Ease(t);
                door.localRotation = closed * Quaternion.Euler(0, -67 * amount, 0);
                gordon.SetPositionAndRotation(Vector3.Lerp(stand, recoilMark.position, amount),
                    Quaternion.Slerp(face, recoilMark.rotation, amount));
                poses.Set("Awkward", amount);
            });
            yield return new WaitForSeconds(.20f);
            var headRest = guardHead.localRotation;
            var headSip = headRest * Quaternion.Euler(-10, 0, -9);
            var cupRest = mug.localPosition;
            var cupAngle = mug.localRotation;
            yield return Over(.34f, t =>
            {
                float amount = Ease(t);
                guardHead.localRotation = Quaternion.Slerp(headRest, headSip, amount);
                LiftMug(cupRest, cupAngle, amount);
            });
            speaker.PlayOneShot(reaction, .64f);
            yield return new WaitForSeconds(.19f);
            yield return Over(.33f, t =>
            {
                guardHead.localRotation = Quaternion.Slerp(headSip, headRest, Ease(t));
                LiftMug(cupRest, cupAngle, 1 - Ease(t));
            });
            yield return new WaitForSeconds(.27f);
        }

        void LiftMug(Vector3 rest, Quaternion angle, float amount)
        {
            mug.SetLocalPositionAndRotation(rest + new Vector3(0, .15f, .07f) * amount,
                angle * Quaternion.Euler(-35 * amount, 0, 0));
            Vector3 hand = guardForearm.parent.InverseTransformPoint(guardHand.position);
            Vector3 elbow = guardElbow.localPosition;
            Vector3 forearm = hand - elbow;
            guardForearm.SetLocalPositionAndRotation(elbow + .5f * forearm, Quaternion.FromToRotation(Vector3.up, forearm));
            guardForearm.localScale = new Vector3(.155f, forearm.magnitude + .10f, .18f);
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
