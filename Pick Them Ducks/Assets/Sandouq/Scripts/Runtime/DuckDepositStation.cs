using DG.Tweening;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckDepositStation : MonoBehaviour
    {
        public Transform landing;
        public Transform bounceRoot;
        public Bounds intake = new Bounds(new Vector3(0,.55f,0),new Vector3(3.2f,1.8f,3.2f));
        [Header("Deposit feedback")]
        public GameObject depositEffectPrefab;
        public Transform effectOrigin;
        [Min(.1f)] public float effectDuration=1;
        public UnityEngine.Events.UnityEvent<int,int> onDuckDeposited=new UnityEngine.Events.UnityEvent<int,int>();
        sealed class Effect { public GameObject prefab,instance;public float until; }
        readonly System.Collections.Generic.List<Effect> effects=new System.Collections.Generic.List<Effect>();
        public int LastDuckValue {get;private set;}
        Vector3 scale;
        Tween bounce;
        public int Landed { get; private set; }
        void Awake(){if(bounceRoot==null)bounceRoot=transform;scale=bounceRoot.localScale;}
        public bool Intersects(Vector3 from,Vector3 to)
        {
            var a=transform.InverseTransformPoint(from);var b=transform.InverseTransformPoint(to);
            if(intake.Contains(b))return true;var d=b-a;
            return d.sqrMagnitude>.0001f&&intake.IntersectRay(new Ray(a,d.normalized),out float distance)&&distance<=d.magnitude;
        }
        public void Arrived(int duckId=-1,int coinValue=1,DuckVariant variant=null)
        {
            Landed++;LastDuckValue=coinValue;PlayEffect(variant!=null&&variant.depositEffectPrefab!=null?variant.depositEffectPrefab:depositEffectPrefab);onDuckDeposited.Invoke(duckId,coinValue);bounce?.Kill();bounceRoot.localScale=scale;
            bounce=bounceRoot.DOPunchScale(new Vector3(.045f,-.075f,.045f),.22f,2,.4f);
        }
        void PlayEffect(GameObject prefab)
        {
            if(prefab==null)return;Effect effect=null;
            foreach(var candidate in effects)if(candidate.prefab==prefab&&candidate.until<=Time.time){effect=candidate;break;}
            if(effect==null&&effects.Count<16){effect=new Effect{prefab=prefab,instance=Instantiate(prefab,transform)};effects.Add(effect);}
            if(effect==null){effect=effects[0];foreach(var candidate in effects)if(candidate.until<effect.until)effect=candidate;if(effect.prefab!=prefab){Destroy(effect.instance);effect.instance=Instantiate(prefab,transform);effect.prefab=prefab;}}
            if(effect.instance==null)effect.instance=Instantiate(effect.prefab,transform);
            effect.instance.SetActive(false);effect.instance.transform.position=effectOrigin!=null?effectOrigin.position:landing!=null?landing.position:transform.position+Vector3.up;effect.instance.SetActive(true);
            foreach(var particle in effect.instance.GetComponentsInChildren<ParticleSystem>()){particle.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);particle.Play(true);}effect.until=Time.time+effectDuration;
        }
        void Update(){foreach(var effect in effects)if(effect.instance!=null&&effect.instance.activeSelf&&Time.time>=effect.until)effect.instance.SetActive(false);}
        void OnDestroy(){bounce?.Kill();}
    }
}
