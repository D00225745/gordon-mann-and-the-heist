using System;
using System.Collections;
using UnityEngine;

namespace GordonMann
{
    public sealed class BankFailureSequence : MonoBehaviour
    {
        public enum Gag { Pogo, Pizza, Cannon }
        public Gag gag;
        public Transform gordon,prop,actionMark,closeShot,wideShot;
        public GameObject effect;
        public AudioSource speaker;
        public AudioClip firstCue,secondCue,punchlineCue;
        public bool Playing { get; private set; }
        GordonPoses poses;

        void Awake() => poses=new GordonPoses(gordon);

        public IEnumerator Play()
        {
            Playing=true;
            while(ScreenFader.IsBusy) yield return null;
            yield return new WaitForSeconds(.14f);
            yield return gag switch
            {
                Gag.Pogo=>Pogo(),
                Gag.Pizza=>Pizza(),
                Gag.Cannon=>Cannon(),
                _=>throw new InvalidOperationException("Unrecognized bank gag.")
            };
            Playing=false;
        }

        IEnumerator Pogo()
        {
            Shot(closeShot,2.47f);
            Vector3 start=gordon.position;
            Quaternion rotation=gordon.rotation;
            Vector3 rest=prop.position;
            Quaternion propRotation=prop.rotation;
            yield return Over(.33f,t=>
            {
                gordon.position=Vector3.Lerp(start,actionMark.position,Ease(t));
                poses.Set("Reach",.78f*Ease(t));
            });
            Vector3 planted=gordon.position;
            for(int bounce=0;bounce<2;bounce++)
            {
                speaker.pitch=bounce==0?.92f:1.08f;
                speaker.PlayOneShot(firstCue,.71f);
                float height=bounce==0?.24f:.51f;
                yield return Over(bounce==0?.39f:.45f,t=>
                {
                    float lift=Mathf.Sin(t*Mathf.PI)*height;
                    gordon.position=planted+Vector3.up*lift;
                    prop.position=rest+Vector3.up*lift;
                });
            }
            yield return new WaitForSeconds(.16f);
            Shot(wideShot,5.05f);
            speaker.pitch=1;
            speaker.PlayOneShot(secondCue,.77f);
            yield return Over(.82f,t=>
            {
                Vector3 motion=new Vector3(t*3.35f,t*(12.8f-2.6f*t),t*1.14f);
                gordon.SetPositionAndRotation(planted+motion,rotation*Quaternion.Euler(0,0,-39*Ease(t)));
                prop.position=rest+motion;
                prop.rotation=Quaternion.Euler(0,0,-37*Ease(t))*propRotation;
            });
            gordon.gameObject.SetActive(false);
            prop.gameObject.SetActive(false);
            yield return new WaitForSeconds(.26f);
            speaker.PlayOneShot(punchlineCue,.77f);
            yield return new WaitForSeconds(.45f);
        }

        IEnumerator Pizza()
        {
            Shot(closeShot,2.33f);
            Vector3 start=gordon.position;
            Vector3 box=prop.position;
            Quaternion facing=gordon.rotation;
            yield return Over(.38f,t=>
            {
                poses.Set("Reach",Ease(t)*.66f);
                gordon.rotation=Quaternion.Slerp(facing,Quaternion.Euler(0,43,0),Ease(t));
                prop.SetPositionAndRotation(box+Vector3.up*(.04f*Ease(t)),
                    Quaternion.Euler(-9*Ease(t),-8,0));
            });
            speaker.PlayOneShot(firstCue,.66f);
            yield return new WaitForSeconds(.48f);
            poses.Set("WatchDesk",.27f);
            yield return new WaitForSeconds(.26f);
            effect.SetActive(true);
            speaker.PlayOneShot(secondCue,.68f);
            yield return new WaitForSeconds(.41f);
            yield return Over(.44f,t=>
            {
                poses.Set("Reach",.66f*(1-Ease(t)));
                poses.Set("WatchDesk",.27f*(1-Ease(t)));
                poses.Set("WatchPatrol",.73f*Ease(t));
                gordon.position=Vector3.Lerp(start,start+Vector3.back*.17f,Ease(t));
                gordon.rotation=Quaternion.Euler(0,Mathf.Lerp(43,106,Ease(t)),0);
                prop.SetPositionAndRotation(Vector3.Lerp(box+Vector3.up*.04f,box,Ease(t)),
                    Quaternion.Euler(-9*(1-Ease(t)),-8,-3*Ease(t)));
            });
            speaker.PlayOneShot(punchlineCue,.42f);
            yield return new WaitForSeconds(.47f);
        }

        IEnumerator Cannon()
        {
            Shot(closeShot,2.91f);
            var start=gordon.position;
            var rotation=gordon.rotation;
            var baseAngle=prop.rotation;
            yield return Over(.39f,t=>
            {
                poses.Set("Reach",.76f*Ease(t));
                gordon.position=Vector3.Lerp(start,actionMark.position,Ease(t));
                prop.rotation=baseAngle*Quaternion.Euler(0,0,6*Mathf.Sin(t*Mathf.PI));
            });
            speaker.PlayOneShot(firstCue,.62f);
            yield return new WaitForSeconds(.60f);
            speaker.PlayOneShot(secondCue,.78f);
            effect.SetActive(true);
            var cloud=effect.transform;
            Vector3 cloudSize=cloud.localScale;
            var camera=Camera.main;
            Shot(wideShot,5.10f);
            yield return Over(.86f,t=>
            {
                float height=4.4f*Mathf.Sin(t*Mathf.PI*.82f)+2.3f*t;
                gordon.SetPositionAndRotation(actionMark.position+new Vector3(14.3f*t,height,2.6f*t),
                    rotation*Quaternion.Euler(0,55*t,-80*t));
                prop.rotation=baseAngle*Quaternion.Euler(0,0,-10*Mathf.Sin(Mathf.PI*t));
                cloud.localScale=cloudSize*Mathf.Lerp(.47f,1.33f,Mathf.Sqrt(t));
                if(camera!=null) camera.transform.position=wideShot.position+Vector3.right*(2.8f*t);
            });
            gordon.gameObject.SetActive(false);
            effect.SetActive(false);
            yield return new WaitForSeconds(.28f);
            speaker.PlayOneShot(punchlineCue,.73f);
            yield return new WaitForSeconds(.43f);
        }

        static void Shot(Transform mark,float size)
        {
            if(Camera.main is not Camera camera)return;
            camera.transform.SetPositionAndRotation(mark.position,mark.rotation);
            camera.orthographicSize=size;
        }

        static float Ease(float t)=>Mathf.SmoothStep(0,1,t);
        static IEnumerator Over(float seconds,Action<float> frame)
        {
            for(float t=0;t<seconds;t+=Time.deltaTime)
            {
                frame(t/seconds);
                yield return null;
            }
            frame(1);
        }
    }
}
