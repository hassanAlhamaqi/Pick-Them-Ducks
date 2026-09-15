using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
namespace Sandouq.Ducks
{
    public sealed class DuckHabitat:MonoBehaviour
    {
        public bool bush;public int index;public float crownHeight=3;
        [Range(1,10)] public int duckCount=10;public float interactionRange=5;public float shakeStrength=4;
        DuckGame game;readonly List<int> ducks=new List<int>();bool broken,shaking;float cooldown;
        public Transform[] spawnPoints=Array.Empty<Transform>();
        [Tooltip("Dedicated trigger for hover and LMB/E interaction. Edit its collider bounds independently of the visual mesh.")] public Collider interactionCollider;
        public ParticleSystem breakParticles,shakeParticles;public Transform particleOrigin;
        public bool Hovered {get;set;}
        MeshFilter[] meshes;Material hoverMaterial;
        public string Key=>"habitat-"+index;
        public bool Available=>!broken;
        public string Hint=>bush?"LMB / E BREAK BUSH / REVEAL HIDDEN DUCKS":"LMB / E SHAKE TREE / DROP PERCHED DUCKS";
        public void Initialize(DuckGame owner,HashSet<int> moved)
        {
            game=owner;meshes=GetComponentsInChildren<MeshFilter>();hoverMaterial=new Material(owner.Settings.outlineShader);hoverMaterial.SetFloat("_Width",.035f);broken=bush&&Array.IndexOf(game.Progress.Data.brokenBushes??Array.Empty<string>(),Key)>=0;
            for(int i=0;i<duckCount;i++)
            {
                int id=game.Population.Total-501-index*10-i;if(id<0||game.Settings.HasPlacement(id))continue;ducks.Add(id);
                if(!game.Population.IsAvailable(id)||moved.Contains(id))continue;
                var p=SpawnPosition(i);if(bush)p.y=game.Park.Ground(p)+(broken?.15f:-6);
                game.Population.Detach(id,false);game.Population.Settle(id,p,Quaternion.Euler(0,i*47,0));
            }
            if(!bush)foreach(int id in ducks)if(game.Population.IsAvailable(id)&&game.Population.Position(id).y>game.Park.Ground(game.Population.Position(id))+.7f)game.Population.supportedDucks.Add(id);
            if(broken)Hide();
        }
        public Vector3 SpawnPosition(int i)
        {
            if(i<spawnPoints.Length&&spawnPoints[i]!=null)return spawnPoints[i].position;
            float angle=i*2.399f;return transform.position+new Vector3(Mathf.Cos(angle)*1.1f,Mathf.Lerp(2.1f,crownHeight,i/9f),Mathf.Sin(angle)*1.1f);
        }
        public bool RayHit(Ray ray,out float distance)
        {
            distance=float.PositiveInfinity;
            if(interactionCollider==null||!interactionCollider.enabled||!interactionCollider.gameObject.activeInHierarchy)return false;
            if(!interactionCollider.Raycast(ray,out var hit,interactionRange))return false;
            distance=hit.distance;return true;
        }
        void LateUpdate()
        {
            if(!Hovered||broken||game==null||game.MenuOpen||hoverMaterial==null)return;
            foreach(var filter in meshes){var renderer=filter.GetComponent<Renderer>();if(renderer==null||!renderer.enabled)continue;for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)Graphics.DrawMesh(filter.sharedMesh,filter.transform.localToWorldMatrix,hoverMaterial,0,game.Player.View,sub,null,UnityEngine.Rendering.ShadowCastingMode.Off,false);}
        }
        public bool Interact()
        {
            if(game==null||broken||Time.time<cooldown)return false;cooldown=Time.time+1;
            foreach(int id in ducks)game.Population.supportedDucks.Remove(id);
            game.particles?.Play(bush?breakParticles:shakeParticles,particleOrigin!=null?particleOrigin.position:transform.position+Vector3.up);
            if(bush)
            {
                broken=true;var keys=new List<string>(game.Progress.Data.brokenBushes??Array.Empty<string>());keys.Add(Key);game.Progress.Data.brokenBushes=keys.ToArray();
                int n=0;foreach(int id in ducks)if(game.Population.IsAvailable(id))
                {var p=transform.position+new Vector3(Mathf.Sin(n*2.4f),0,Mathf.Cos(n*2.4f))*.7f;p.y=game.Park.Ground(p)+.12f+(n++/5)*.22f;game.Population.Detach(id,false);game.Population.Settle(id,p,Quaternion.identity);}
                transform.DOPunchScale(Vector3.one*.2f,.15f).OnComplete(()=>transform.DOScale(Vector3.zero,.2f).OnComplete(Hide));
            }
            else{shaking=true;transform.DOPunchRotation(new Vector3(0,0,shakeStrength),.75f,8,.5f);}
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
        void OnDestroy(){transform.DOKill();if(hoverMaterial!=null)Destroy(hoverMaterial);}
    }
}
