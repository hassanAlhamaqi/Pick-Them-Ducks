using UnityEngine;using System.Collections.Generic;
namespace Sandouq.Ducks {
 public sealed class DuckParticleFeedback:MonoBehaviour {
  [Header("Assign ParticleSystem prefabs; empty slots are optional")]
  public ParticleSystem pickup,rollerComplete,pileCollapse,duckThrow,duckSettle,waterSplash,jump,land,bagFull,casketPlaced,placementBlocked;
  [Min(0)] public float minimumInterval=.04f;
  sealed class Pool {public readonly List<ParticleSystem> instances=new List<ParticleSystem>();public float last=-100;public int next;}
  readonly Dictionary<ParticleSystem,Pool> pools=new Dictionary<ParticleSystem,Pool>();
  public void Play(ParticleSystem prefab,Vector3 position){if(prefab==null)return;if(!pools.TryGetValue(prefab,out var pool)){pool=new Pool();pools.Add(prefab,pool);}if(Time.time-pool.last<minimumInterval)return;pool.last=Time.time;ParticleSystem effect=null;foreach(var item in pool.instances)if(item!=null&&!item.IsAlive(true)){effect=item;break;}if(effect==null&&pool.instances.Count<4){effect=Instantiate(prefab,transform);pool.instances.Add(effect);}if(effect==null){int index=pool.next++%pool.instances.Count;effect=pool.instances[index];if(effect==null)pool.instances[index]=effect=Instantiate(prefab,transform);}effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);effect.transform.position=position;foreach(var child in effect.GetComponentsInChildren<ParticleSystem>(true)){var main=child.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;main.simulationSpace=ParticleSystemSimulationSpace.World;}effect.gameObject.SetActive(true);effect.Play(true);}
 }
}
