using DG.Tweening;
using UnityEngine;

namespace Sandouq.Ducks
{
    public sealed class DuckDepositStation : MonoBehaviour
    {
        public Transform landing;
        public Transform bounceRoot;
        public Bounds intake = new Bounds(new Vector3(0,.35f,-.2f),new Vector3(2.1f,1.1f,2.2f));
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
        public void Arrived()
        {
            Landed++;bounce?.Kill();bounceRoot.localScale=scale;
            bounce=bounceRoot.DOPunchScale(new Vector3(.045f,-.075f,.045f),.22f,2,.4f);
        }
        void OnDestroy(){bounce?.Kill();}
    }
}
