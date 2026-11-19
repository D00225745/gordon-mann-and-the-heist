using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class PortalJarSequence : MonoBehaviour
    {
        public Transform gordon;
        public Transform jar;
        public Transform portal;
        public Transform slideEnd;
        public GameObject heading;
        public GameObject narration;
        public AudioSource speaker;
        public AudioClip jarHit;
        public AudioClip portalOpen;
        public AudioClip slide;
        public Transform cutCamera;

        public bool Playing { get; private set; }
        static bool pendingArrival;
        SkinnedMeshRenderer[] posing;
        Vector3 standingScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetArrival() => pendingArrival = false;

        void Awake()
        {
            posing = gordon.GetComponentsInChildren<SkinnedMeshRenderer>();
            standingScale = gordon.localScale;
            portal.gameObject.SetActive(false);
        }

        public bool TryPlay(Choice choice)
        {
            if (jar == null || choice.label != "Portal Jar" || choice.targetScene != "Choice_2") return false;
            if (!Playing)
            {
                Playing = true;
                StartCoroutine(Depart(choice.targetScene));
            }
            return true;
        }

        public bool TryBeginArrival()
        {
            if (jar != null || !pendingArrival) return false;
            pendingArrival = false;
            Playing = true;
            StartCoroutine(Arrive());
            return true;
        }

        IEnumerator Depart(string destination)
        {
            if (heading != null) heading.SetActive(false);
            if (narration != null) narration.SetActive(false);
            var camera = Camera.main;
            if (camera != null && cutCamera != null)
            {
                camera.transform.SetPositionAndRotation(cutCamera.position, cutCamera.rotation);
                camera.orthographicSize = 3.12f;
            }
            Vector3 rest = jar.position;
            Vector3 lift = rest + new Vector3(-.18f, 1.08f, -.18f);
            Quaternion jarRotation = jar.rotation;
            yield return Over(.31f, p =>
            {
                jar.position = Vector3.Lerp(rest, lift, Ease(p));
                Pose("Release", Ease(p));
            });
            yield return Over(.48f, p =>
            {
                jar.position = Vector3.Lerp(lift, rest, p * p);
                jar.rotation = jarRotation * Quaternion.Euler(0, 0, Mathf.Sin(p * Mathf.PI) * 38);
            });
            speaker.PlayOneShot(jarHit);
            yield return Over(.22f, p =>
            {
                jar.position = rest + Vector3.up * (Mathf.Sin(p * Mathf.PI) * .11f);
                Pose("Release", 1 - p);
                Pose("Recoil", p);
            });
            speaker.PlayOneShot(portalOpen);
            portal.gameObject.SetActive(true);
            Vector3 portalSize = portal.localScale;
            yield return Over(.61f, p => portal.localScale = portalSize * Mathf.Lerp(.015f, 1, Ease(p)));
            yield return new WaitForSeconds(.17f);
            speaker.PlayOneShot(slide);
            Vector3 start = gordon.position;
            Quaternion face = gordon.rotation;
            Quaternion travel = Quaternion.LookRotation(slideEnd.position - start) * Quaternion.Euler(-12, 0, -9);
            yield return Over(.88f, p =>
            {
                gordon.position = Vector3.Lerp(start, slideEnd.position, Ease(p));
                gordon.rotation = Quaternion.Slerp(face, travel, Ease(p));
                Pose("Recoil", 1 - p);
                Pose("Slide", p);
                gordon.localScale = standingScale * Mathf.Lerp(1, .018f, Ease(Mathf.InverseLerp(.68f, 1, p)));
            });
            gordon.gameObject.SetActive(false);
            yield return Over(.21f, p => portal.localScale = portalSize * (1 - Ease(p)));
            pendingArrival = true;
            GameFlow.PickOutcome(destination);
        }

        IEnumerator Arrive()
        {
            Vector3 end = gordon.position;
            Quaternion face = gordon.rotation;
            Vector3 start = slideEnd.position;
            Vector3 portalSize = portal.localScale;
            gordon.position = start;
            gordon.localScale = standingScale * .02f;
            portal.gameObject.SetActive(true);
            while (ScreenFader.IsBusy) yield return null;
            speaker.PlayOneShot(slide, .78f);
            yield return Over(.78f, p =>
            {
                gordon.position = Vector3.Lerp(start, end, Ease(p));
                gordon.localScale = standingScale * Mathf.Lerp(.02f, 1, Ease(Mathf.Clamp01(p * 2.3f)));
                gordon.rotation = Quaternion.Slerp(Quaternion.LookRotation(end - start), face, Ease(p));
                Pose("Slide", 1 - Ease(p));
                portal.localScale = portalSize * (1 - Ease(Mathf.InverseLerp(.34f, 1, p)));
            });
            portal.gameObject.SetActive(false);
            portal.localScale = portalSize;
            Playing = false;
        }

        void Pose(string label, float amount)
        {
            foreach (var skin in posing)
            {
                var mesh = skin.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    if (mesh.GetBlendShapeName(i).EndsWith(label, System.StringComparison.Ordinal))
                        skin.SetBlendShapeWeight(i, amount * 100);
            }
        }

        static IEnumerator Over(float seconds, System.Action<float> update)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                update(t / seconds);
                yield return null;
            }
            update(1);
        }

        static float Ease(float t) => t * t * (3 - 2 * t);
    }
}
