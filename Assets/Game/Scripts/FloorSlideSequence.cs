using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class FloorSlideSequence : MonoBehaviour
    {
        public Transform actor, approach, slideStart, slideEnd, takeoffCamera, trackingCamera, finishCamera;
        public GameObject heading, narration;
        public AudioSource speaker;
        public AudioClip plant, scrape, settle;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses = new GordonPoses(actor);

        public bool TryPlay(Choice choice)
        {
            if (choice.label!="Floor Slide" || choice.targetScene!="Choice_5") return false;
            if (Playing) return true;
            Playing=true;
            StartCoroutine(Perform(choice.targetScene));
            return true;
        }

        IEnumerator Perform(string destination)
        {
            heading.SetActive(false);
            narration.SetActive(false);
            Shot(takeoffCamera,3.34f);
            Vector3 start=actor.position;
            Quaternion facing=actor.rotation;
            yield return Over(.58f,t =>
            {
                actor.SetPositionAndRotation(Vector3.Lerp(start,approach.position,Ease(t)),
                    Quaternion.Slerp(facing,Quaternion.identity,Ease(t)));
                poses.Set("Coil",.54f*Mathf.Sin(t*Mathf.PI));
            });
            speaker.PlayOneShot(plant,.72f);
            yield return Over(.26f,t=>poses.Set("Coil",Ease(t)));
            yield return new WaitForSeconds(.13f);
            speaker.PlayOneShot(scrape,.72f);
            yield return Over(.34f,t =>
            {
                float progress=Ease(t);
                actor.position=Vector3.Lerp(approach.position,slideStart.position,progress);
                poses.Set("Coil",1-progress);
                poses.Set("SlideLow",progress);
            });

            Shot(trackingCamera,3.15f);
            Camera camera=Camera.main;
            Vector3 eye=trackingCamera.position;
            yield return Over(1.65f,t =>
            {
                float distance=1-Mathf.Pow(1-t,1.25f);
                actor.position=Vector3.Lerp(slideStart.position,slideEnd.position,distance);
                if (camera!=null) camera.transform.position=eye+Vector3.forward*(distance*4.85f);
                float shift=Mathf.Sin(Mathf.PI*t)*.85f;
                poses.Set("SlideLow",1-shift);
                poses.Set("SlideShift",shift);
            });
            Shot(finishCamera,2.56f);
            speaker.PlayOneShot(settle,.64f);
            yield return new WaitForSeconds(.16f);
            yield return Over(.43f,t =>
            {
                poses.Set("SlideLow",1-Ease(t));
                poses.Set("Recover",Ease(t));
            });
            yield return Over(.31f,t=>poses.Set("Recover",1-Ease(t)));
            yield return new WaitForSeconds(.23f);
            GameFlow.PickOutcome(destination);
        }

        static void Shot(Transform mark,float size)
        {
            if (Camera.main is not Camera camera) return;
            camera.transform.SetPositionAndRotation(mark.position,mark.rotation);
            camera.orthographicSize=size;
        }

        static float Ease(float t) => Mathf.SmoothStep(0,1,t);

        static IEnumerator Over(float duration,Action<float> sample)
        {
            for(float t=0;t<duration;t+=Time.deltaTime)
            {
                sample(t/duration);
                yield return null;
            }
            sample(1);
        }
    }
}
