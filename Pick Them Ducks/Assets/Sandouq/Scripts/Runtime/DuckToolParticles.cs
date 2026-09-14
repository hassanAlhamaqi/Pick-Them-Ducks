using UnityEngine;
namespace Sandouq.Ducks {
 public sealed class DuckToolParticles:MonoBehaviour {
  public DuckTool tool;public ParticleSystem activeLoop;
  DuckGame game;
  void Start(){game=FindAnyObjectByType<DuckGame>();}
  void Update(){if(activeLoop==null||game==null||game.Progress==null)return;bool active=!game.MenuOpen&&game.Progress.Tool==tool&&(game.ToolActive||(game.RidingCar&&game.Car.IsMoving));if(active&&!activeLoop.isPlaying)activeLoop.Play(true);else if(!active&&activeLoop.isPlaying)activeLoop.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
  void OnDisable(){if(activeLoop!=null)activeLoop.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
 }
}
