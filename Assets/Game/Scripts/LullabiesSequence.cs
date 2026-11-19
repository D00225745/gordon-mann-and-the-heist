using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GordonMann
{
    public sealed class LullabiesSequence : MonoBehaviour
    {
        [Serializable]
        public struct Guard
        {
            public Transform torso;
            public Transform head;
            public GameObject openEyes;
            public GameObject shutEyes;
        }

        public Transform gordon;
        public PortalJarSequence portalArrival;
        public Transform radioLight;
        public Guard desk;
        public Guard patrol;
        public Transform radioShot;
        public Transform guardsShot;
        public Transform routeShot;
        public Transform doorMark;
        public GameObject heading;
        public GameObject narration;
        public AudioSource radioAudio;
        public AudioSource effects;
        public AudioClip buttonClick;
        public AudioClip lullaby;
        public AudioClip cloth;
        public AudioClip footfall;

        public bool Playing { get; private set; }
        readonly Dictionary<string, List<(SkinnedMeshRenderer mesh, int index)>> channels = new();

        void Awake()
        {
            foreach (var skin in gordon.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                {
                    string path = skin.sharedMesh.GetBlendShapeName(i);
                    string pose = path.Substring(path.LastIndexOf('.') + 1);
                    if (!channels.TryGetValue(pose, out var bindings))
                        channels.Add(pose, bindings = new List<(SkinnedMeshRenderer, int)>());
                    bindings.Add((skin, i));
                }
            }
        }

        public bool TryPlay(Choice choice)
        {
            if (choice.label != "Lullabies" || choice.targetScene != "Choice_3") return false;
            if (!Playing)
            {
                Playing = true;
                StartCoroutine(Perform(choice.targetScene));
            }
            return true;
        }

        IEnumerator Perform(string destination)
        {
            gordon.SetPositionAndRotation(portalArrival.gordon.position, portalArrival.gordon.rotation);
            portalArrival.gordon.gameObject.SetActive(false);
            gordon.gameObject.SetActive(true);
            heading.SetActive(false);
            narration.SetActive(false);
            Shot(radioShot, 2.18f);
            var startRotation = gordon.rotation;
            var atPlayer = Quaternion.Euler(0, 65, 0);
            yield return Over(.28f, t => gordon.rotation = Quaternion.Slerp(startRotation, atPlayer, Ease(t)));
            yield return Blend(.34f, null, "Reach");
            effects.PlayOneShot(buttonClick, .93f);
            radioLight.gameObject.SetActive(true);
            radioAudio.clip = lullaby;
            radioAudio.Play();
            yield return new WaitForSeconds(.27f);
            yield return Blend(.32f, "Reach", "WatchDesk");

            Shot(guardsShot, 2.68f);
            yield return Over(.45f, t =>
            {
                desk.head.localRotation = Quaternion.Euler(-14 * Ease(t), 5 * Ease(t), 7 * Ease(t));
                patrol.head.localRotation = Quaternion.Euler(0, -11 * Ease(t), 0);
            });
            yield return Over(.22f, t => desk.head.localRotation = Quaternion.Euler(Mathf.Lerp(-14, 3, Ease(t)), 5, 7));
            yield return new WaitForSeconds(.19f);
            CloseEyes(desk);
            Vector3 deskUpright = desk.torso.localPosition;
            yield return Over(.45f, t =>
            {
                float settle = Ease(t);
                desk.torso.localRotation = Quaternion.Euler(-49 * settle, 0, -8 * settle);
                desk.torso.localPosition = deskUpright + new Vector3(0, .25f * settle, -.08f * settle);
                desk.head.localRotation = Quaternion.Euler(Mathf.Lerp(3, 23, settle), 5, 7);
                patrol.head.localRotation = Quaternion.Euler(-19 * Ease(t), -11, -9 * Ease(t));
            });
            effects.PlayOneShot(cloth, .66f);
            yield return new WaitForSeconds(.15f);
            yield return Over(.24f, t => patrol.head.localRotation = Quaternion.Euler(Mathf.Lerp(-19, -4, Ease(t)), -11, -9));
            yield return new WaitForSeconds(.26f);
            CloseEyes(patrol);
            Vector3 upright = patrol.torso.localPosition;
            yield return Over(.53f, t =>
            {
                patrol.torso.localRotation = Quaternion.Euler(17 * Ease(t), 0, -29 * Ease(t));
                patrol.head.localRotation = Quaternion.Euler(Mathf.Lerp(-4, -18, Ease(t)), -11, -9);
                patrol.torso.localPosition = upright + new Vector3(0, -.23f * Ease(t), .15f * Ease(t));
            });
            effects.PlayOneShot(cloth, .38f);

            Shot(routeShot, 3.72f);
            yield return new WaitForSeconds(.20f);
            yield return Blend(.25f, "WatchDesk", "WatchPatrol");
            yield return new WaitForSeconds(.32f);
            Vector3 start = gordon.position;
            Vector3 end = doorMark.position;
            var direction = Quaternion.LookRotation(end - start);
            var beforeSneak = gordon.rotation;
            yield return Over(.32f, t =>
            {
                Crossfade("WatchPatrol", "ReadySneak", Ease(t));
                gordon.rotation = Quaternion.Slerp(beforeSneak, direction, Ease(t));
            });

            // The stride/pose distance matches Maya's 40 cm stance change at Unity character scale.
            float length = Vector3.Distance(start, end);
            int steps = Mathf.Max(2, Mathf.RoundToInt(length / .48f));
            float stepDistance = length / steps;
            float travel = stepDistance * .5f;
            yield return Walk(start, end, length, 0, travel, .56f, "ReadySneak", "PassRight", "StrideRight");
            for (int i = 1; i < steps; i++)
            {
                bool right = i % 2 == 0;
                string side = right ? "Right" : "Left";
                string other = right ? "Left" : "Right";
                float duration = i == 5 ? .67f : (right ? .46f : .51f);
                yield return Walk(start, end, length, travel, travel + stepDistance, duration,
                    "Stride" + other, "Pass" + side, "Stride" + side);
                travel += stepDistance;
                if (i == 5) yield return new WaitForSeconds(.22f);
            }
            string last = steps % 2 != 0 ? "Right" : "Left";
            string lastPass = steps % 2 != 0 ? "Left" : "Right";
            yield return Walk(start, end, length, travel, length, .46f, "Stride" + last, "Pass" + lastPass, "ReadySneak");
            float level = radioAudio.volume;
            yield return Over(.26f, t => radioAudio.volume = level * (1 - t));
            GameFlow.PickOutcome(destination);
        }

        IEnumerator Walk(Vector3 start, Vector3 end, float length, float from, float to, float seconds,
            string previous, string passing, string landing)
        {
            yield return Over(seconds * .49f, t =>
            {
                Crossfade(previous, passing, t);
                gordon.position = Vector3.Lerp(start, end, Mathf.Lerp(from, (from + to) * .5f, t) / length);
            });
            yield return Over(seconds * .51f, t =>
            {
                Crossfade(passing, landing, t);
                gordon.position = Vector3.Lerp(start, end, Mathf.Lerp((from + to) * .5f, to, t) / length);
            });
            effects.pitch = landing == "StrideLeft" ? .96f : 1.04f;
            effects.PlayOneShot(footfall, landing == "ReadySneak" ? .34f : .42f);
        }

        static void CloseEyes(Guard guard)
        {
            guard.openEyes.SetActive(false);
            guard.shutEyes.SetActive(true);
        }

        void Shot(Transform mark, float size)
        {
            var camera = Camera.main;
            if (camera == null) return;
            camera.transform.SetPositionAndRotation(mark.position, mark.rotation);
            camera.orthographicSize = size;
        }

        IEnumerator Blend(float seconds, string from, string to) => Over(seconds, t => Crossfade(from, to, Ease(t)));

        void Crossfade(string from, string to, float t)
        {
            if (from != null) ApplyPose(from, 1 - t);
            ApplyPose(to, t);
        }

        void ApplyPose(string pose, float value)
        {
            if (!channels.TryGetValue(pose, out var bindings)) return;
            foreach (var (mesh, index) in bindings) mesh.SetBlendShapeWeight(index, 100 * value);
        }

        static IEnumerator Over(float seconds, Action<float> set)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                set(t / seconds);
                yield return null;
            }
            set(1);
        }

        static float Ease(float t) => t * t * (3 - 2 * t);
    }
}
