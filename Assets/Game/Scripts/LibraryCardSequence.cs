using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class LibraryCardSequence : MonoBehaviour
    {
        public Transform actor, card, door, scanMark, readerMark, doorway, exit, closeShot, wideShot;
        public GameObject lockedLamp, openLamp, heading, narration;
        public AudioSource speaker;
        public AudioClip footstep, cardTap, accepted, doorMotor;
        public bool Playing { get; private set; }
        GordonPoses poses;
        int steps;

        void Awake() => poses = new GordonPoses(actor);

        public bool TryPlay(Choice choice)
        {
            if (choice.label != "Library Card" || choice.targetScene != "Choice_4") return false;
            if (Playing) return true;
            Playing = true;
            StartCoroutine(Perform(choice.targetScene));
            return true;
        }

        IEnumerator Perform(string destination)
        {
            heading.SetActive(false);
            narration.SetActive(false);
            card.gameObject.SetActive(false);
            yield return Walk(readerMark.position);
            card.position = readerMark.position + new Vector3(.34f,.80f,.28f);
            card.rotation = Quaternion.Euler(7,-12,-14);
            card.gameObject.SetActive(true);
            Vector3 originalCard = card.position;
            Quaternion originalRotation = card.rotation;
            Shot(closeShot,1.72f);
            yield return Over(.37f,t =>
            {
                float ease = Ease(t);
                poses.Set("Reach",.83f*ease);
                card.SetPositionAndRotation(Vector3.Lerp(originalCard,scanMark.position,ease) +
                    Vector3.up*(.04f*Mathf.Sin(Mathf.PI*t)),Quaternion.Slerp(originalRotation,scanMark.rotation,ease));
            });
            speaker.PlayOneShot(cardTap,.67f);
            yield return new WaitForSeconds(.36f);
            lockedLamp.SetActive(false);
            openLamp.SetActive(true);
            speaker.PlayOneShot(accepted,.77f);
            yield return new WaitForSeconds(.22f);
            yield return Over(.22f,t=>card.position=Vector3.Lerp(scanMark.position,originalCard,Ease(t)));
            card.gameObject.SetActive(false);
            Shot(wideShot,3.38f);
            Vector3 shut = door.localPosition;
            speaker.PlayOneShot(doorMotor,.74f);
            yield return Over(1.16f,t =>
            {
                door.localPosition = shut + Vector3.right*(3.30f*Ease(t));
                poses.Set("Reach",.83f*(1-Ease(t)));
            });
            poses.Set("WatchPatrol",.55f);
            yield return new WaitForSeconds(.25f);
            poses.Set("WatchPatrol",0);
            yield return Walk(doorway.position);
            yield return Walk(exit.position);
            GameFlow.PickOutcome(destination);
        }

        IEnumerator Walk(Vector3 target)
        {
            Vector3 start = actor.position;
            Quaternion initial = actor.rotation;
            Quaternion facing = Quaternion.LookRotation(target-start);
            yield return Over(.19f,t => actor.rotation = Quaternion.Slerp(initial,facing,Ease(t)));
            int segments = Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(start,target)/.53f));
            string planted = null;
            for(int i=0;i<segments;i++)
            {
                string side = steps++%2==0 ? "Right" : "Left";
                string pass = "Pass"+side;
                string stride = "Stride"+side;
                float from = i/(float)segments;
                float to = (i+1f)/segments;
                yield return Over(side=="Right"?.30f:.33f,t =>
                {
                    actor.position = Vector3.Lerp(start,target,Mathf.Lerp(from,to,t));
                    if (planted != null) poses.Set(planted,.66f*Mathf.Clamp01(1-2*t));
                    poses.Set(pass,.67f*(1-Mathf.Abs(2*t-1)));
                    poses.Set(stride,.66f*Mathf.Clamp01(2*t-1));
                });
                planted = stride;
                speaker.PlayOneShot(footstep,.40f);
            }
            yield return Over(.13f,t => poses.Set(planted,.66f*(1-Ease(t))));
        }

        static float Ease(float t) => Mathf.SmoothStep(0,1,t);

        static IEnumerator Over(float seconds,Action<float> sample)
        {
            for(float t=0;t<seconds;t+=Time.deltaTime)
            {
                sample(t/seconds);
                yield return null;
            }
            sample(1);
        }

        static void Shot(Transform mark,float size)
        {
            if (Camera.main is not Camera camera) return;
            camera.transform.SetPositionAndRotation(mark.position,mark.rotation);
            camera.orthographicSize = size;
        }
    }
}
