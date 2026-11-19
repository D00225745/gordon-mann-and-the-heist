using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class LaserFailureSequence : MonoBehaviour
    {
        public enum Gag { Cart, Unplug, Selfie, Frozen }
        public Gag gag;
        public Transform gordon, closeShot, wideShot, prop, actionMark;
        public Transform[] beams;
        public Transform[] wheels;
        public Transform scannerRig;
        public Transform cableAnchor;
        public LineRenderer cable;
        public Light[] roomLights;
        public Light flash;
        public GameObject workLights, warning;
        public AudioSource speaker;
        public AudioClip startCue, eventCue, alarmCue;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses = new GordonPoses(gordon);

        public IEnumerator Play()
        {
            Playing = true;
            Shot(closeShot, gag == Gag.Cart ? 3.05f : 2.15f);
            if (gag == Gag.Cart) poses.Set("Seated", 1);
            if (gag == Gag.Selfie) poses.Set("PhoneLow", 1);
            if (gag == Gag.Unplug) DrawCable();
            while (ScreenFader.IsBusy) yield return null;
            yield return new WaitForSeconds(.19f);
            yield return gag switch
            {
                Gag.Cart => Cart(),
                Gag.Unplug => Unplug(),
                Gag.Selfie => Selfie(),
                Gag.Frozen => Frozen(),
                _ => throw new InvalidOperationException("Unknown laser hallway gag.")
            };
            Playing = false;
        }

        IEnumerator Cart()
        {
            Vector3 start = prop.position;
            Vector3 stop = actionMark.position;
            speaker.PlayOneShot(startCue, .68f);
            yield return Over(1.34f, t =>
            {
                prop.position = Vector3.Lerp(start, stop, t * t);
                foreach (var wheel in wheels)
                    wheel.localRotation = Quaternion.Euler(-1100 * t * t, 0, 0);
                float brake = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.78f, 1, t));
                poses.Set("Seated", 1 - brake);
                poses.Set("SeatedBrake", brake);
            });
            speaker.PlayOneShot(eventCue, .74f);
            Alarm();
            yield return Over(.28f, t =>
            {
                prop.position = stop + Vector3.back * (.18f * Ease(t));
                prop.rotation = Quaternion.Euler(-8 * Mathf.Sin(t * Mathf.PI), 0, 2 * Mathf.Sin(t * Mathf.PI));
            });
            yield return BeamPulse(beams[0], .39f);
            yield return new WaitForSeconds(.31f);
        }

        IEnumerator Unplug()
        {
            speaker.PlayOneShot(startCue, .60f);
            yield return Over(.43f, t => poses.Set("PullHandle", Ease(t)));
            Vector3 start = prop.position;
            Quaternion turn = prop.rotation;
            Vector3 actorRest = gordon.position;
            yield return Over(.27f, t =>
            {
                float amount = Ease(t);
                prop.SetPositionAndRotation(Vector3.Lerp(start, actionMark.position, amount),
                    Quaternion.Slerp(turn, actionMark.rotation, amount));
                gordon.position = Vector3.Lerp(actorRest, actorRest - gordon.forward * .17f, amount);
                DrawCable();
            });
            speaker.PlayOneShot(eventCue, .77f);
            foreach (var light in roomLights) light.intensity *= .085f;
            workLights.SetActive(false);
            Shot(wideShot, 2.94f);
            yield return new WaitForSeconds(.34f);
            Alarm();
            yield return Over(.38f, t =>
            {
                poses.Set("PullHandle", 1 - Ease(t));
                poses.Set("Wince", .78f * Ease(t));
                prop.position = actionMark.position + Vector3.down * (.19f * Ease(t));
                DrawCable();
            });
            yield return BeamPulse(beams[0], .37f);
            yield return new WaitForSeconds(.22f);
        }

        IEnumerator Selfie()
        {
            Vector3 low = prop.localPosition;
            Quaternion angle = prop.localRotation;
            yield return Over(.57f, t =>
            {
                float amount = Ease(t);
                poses.Set("PhoneLow", 1 - amount);
                poses.Set("PhoneUp", amount);
                prop.SetLocalPositionAndRotation(Vector3.Lerp(low, actionMark.localPosition, amount),
                    Quaternion.Slerp(angle, actionMark.localRotation, amount));
            });
            speaker.PlayOneShot(startCue, .62f);
            yield return new WaitForSeconds(.24f);
            speaker.PlayOneShot(eventCue, .78f);
            flash.enabled = true;
            yield return new WaitForSeconds(.088f);
            flash.enabled = false;
            yield return new WaitForSeconds(.24f);
            Alarm();
            yield return Over(.38f, t =>
            {
                poses.Set("PhoneUp", 1 - Ease(t));
                poses.Set("PhoneLow", Ease(t));
                prop.SetLocalPositionAndRotation(Vector3.Lerp(actionMark.localPosition, low, Ease(t)),
                    Quaternion.Slerp(actionMark.localRotation, angle, Ease(t)));
            });
            yield return BeamPulse(beams[0], .34f);
            yield return new WaitForSeconds(.31f);
        }

        IEnumerator Frozen()
        {
            speaker.PlayOneShot(startCue, .69f);
            yield return Over(.31f, t => poses.Set("Hesitate", Ease(t)));
            yield return new WaitForSeconds(.36f);
            Alarm();
            Transform scan = beams[0];
            Vector3 start = scannerRig.position;
            speaker.PlayOneShot(eventCue, .72f);
            yield return Over(.69f, t =>
            {
                scannerRig.position = Vector3.Lerp(start, actionMark.position, Ease(t));
                poses.Set("Hesitate", 1 - Ease(t));
                poses.Set("Wince", Ease(t));
            });
            yield return BeamPulse(scan, .29f);
            yield return new WaitForSeconds(.30f);
        }

        void DrawCable()
        {
            Vector3 start = cableAnchor.position;
            Vector3 end = prop.TransformPoint(new Vector3(.10f, -.03f, 0));
            for (int i = 0; i < cable.positionCount; i++)
            {
                float t = i / (float)(cable.positionCount - 1);
                cable.SetPosition(i, Vector3.Lerp(start, end, t) + Vector3.down * (.22f * Mathf.Sin(Mathf.PI * t)));
            }
        }

        void Alarm()
        {
            warning.SetActive(true);
            speaker.PlayOneShot(alarmCue, .74f);
        }

        static IEnumerator BeamPulse(Transform beam, float time)
        {
            Vector3 original = beam.localScale;
            yield return Over(time, t =>
            {
                float width = 1 + 2.2f * Mathf.Sin(Mathf.PI * t);
                beam.localScale = new Vector3(original.x, original.y * width, original.z * width);
            });
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
