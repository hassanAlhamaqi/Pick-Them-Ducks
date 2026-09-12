using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
namespace Sandouq.Ducks
{
    public sealed class DuckHabitat:MonoBehaviour
    {
        public bool bush;public int index;public float crownHeight=3;
        DuckGame game;readonly List<int> ducks=new List<int>();bool broken,shaking;float cooldown;
        public string Key=>"habitat-"+index;
        public bool Available=>!broken;
        public string Hint=>bush?"E BREAK BUSH / REVEAL HIDDEN DUCKS":"E SHAKE TREE / DROP PERCHED DUCKS";
        public void Initialize(DuckGame owner,HashSet<int> moved)
        {
            game=owner;broken=bush&&Array.IndexOf(game.Progress.Data.brokenBushes??Array.Empty<string>(),Key)>=0;
            for(int i=0;i<10;i++)
            {
                int id=game.Population.Total-501-index*10-i;if(id<0)continue;ducks.Add(id);
                if(!game.Population.IsAvailable(id)||moved.Contains(id))continue;
                float angle=i*2.399f;var p=transform.position+new Vector3(Mathf.Cos(angle)*(bush?.45f:1.1f),0,Mathf.Sin(angle)*(bush?.45f:1.1f));
                p.y=bush?game.Park.Ground(p)+(broken?.15f:-6):transform.position.y+Mathf.Lerp(2.1f,crownHeight,i/9f);
                game.Population.Detach(id,false);game.Population.Settle(id,p,Quaternion.Euler(0,i*47,0));
            }
            if(broken)Hide();
        }
        public bool Interact()
        {
            if(game==null||broken||Time.time<cooldown)return false;cooldown=Time.time+1;
            if(bush)
            {
                broken=true;var keys=new List<string>(game.Progress.Data.brokenBushes??Array.Empty<string>());keys.Add(Key);game.Progress.Data.brokenBushes=keys.ToArray();
                int n=0;foreach(int id in ducks)if(game.Population.IsAvailable(id))
                {var p=transform.position+new Vector3(Mathf.Sin(n*2.4f),0,Mathf.Cos(n*2.4f))*.7f;p.y=game.Park.Ground(p)+.12f+(n++/5)*.22f;game.Population.Detach(id,false);game.Population.Settle(id,p,Quaternion.identity);}
                transform.DOPunchScale(Vector3.one*.2f,.15f).OnComplete(()=>transform.DOScale(Vector3.zero,.2f).OnComplete(Hide));
            }
            else{shaking=true;transform.DOPunchRotation(new Vector3(0,0,4),.75f,8,.5f);}
            game.Progress.Touch();return true;
        }
        void Update()
        {
            if(!shaking||game.MenuOpen)return;bool pending=false;
            foreach(int id in ducks)if(game.Population.IsAvailable(id)&&game.Population.Position(id).y>game.Park.Ground(game.Population.Position(id))+.7f)
            {if(!game.Physics.Launch(id,game.Population.Position(id),new Vector3(Mathf.Sin(id),0,Mathf.Cos(id))*.5f))pending=true;}
            shaking=pending;
        }
        void Hide(){foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=false;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;}
        void OnDestroy(){transform.DOKill();}
    }
}
