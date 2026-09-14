using UnityEngine;
namespace Sandouq.Ducks {
 public sealed class DuckRollerVehicle:MonoBehaviour {
  public Transform visual;
  public Collider parkedCollider;
  public float interactionRange=3.8f,steeringSpeed=85;
  [Header("Driving")]
  public float acceleration=9,deceleration=5,braking=22,reverseSpeed=8;
  public float CurrentSpeed {get;private set;}
  public bool IsMoving=>Mathf.Abs(CurrentSpeed)>.05f;
  [Header("Optional child particle systems")]
  public ParticleSystem exhaustLoop,wheelDustLoop;
  public void Stop(){CurrentSpeed=0;}
  public float TickDrive(float throttle,float deltaTime){float target=throttle>0?game.Progress.DriveSpeed:throttle<0?-reverseSpeed:0;bool changingDirection=CurrentSpeed*throttle<0;float rate=changingDirection?braking:throttle==0?deceleration:acceleration;if(changingDirection)target=0;CurrentSpeed=Mathf.MoveTowards(CurrentSpeed,target,Mathf.Max(.01f,rate)*deltaTime);return CurrentSpeed;}
  public Vector3 exitOffset=new Vector3(2.4f,0,0);
  public bool Riding {get;private set;}
  DuckGame game;int previousTool;
  public void Initialize(DuckGame owner){game=owner;UpdateSize();}
  public bool Nearby => game!=null&&(game.Player.transform.position-transform.position).sqrMagnitude<interactionRange*interactionRange;
  public void UpdateSize(){if(visual!=null)visual.localScale=new Vector3(1+game.Progress.Data.toolLevels[4]*.18f,1,1);}
  public bool Enter(){if(game==null||Riding||!Nearby||game.MenuOpen||game.Deposits.Transferring||game.Placing)return false;previousTool=game.Progress.Data.currentTool==4?0:game.Progress.Data.currentTool;game.Physics.FlushFront();Stop();Riding=true;parkedCollider.enabled=false;game.Player.Teleport(transform.position);game.Player.transform.rotation=transform.rotation;game.Player.ResetLook();game.Progress.Equip(4);game.RefreshTool();game.Save();return true;}
  public bool Exit(){if(!Riding)return false;Vector3 position=Vector3.zero;bool found=false;foreach(var offset in new[]{exitOffset,-exitOffset,Vector3.back*3.5f}){var p=game.Player.transform.TransformPoint(offset);if(game.Park.InLake(p)&&!game.Park.WalkableWater(p))continue;p.y=(game.Park.WalkableWater(p)?game.Park.WaterSupportHeight(p):game.Park.Ground(p))+.05f;if(UnityEngine.Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.5f,.3f,~0,QueryTriggerInteraction.Ignore))continue;position=p;found=true;break;}if(!found){game.ShowNotice("Exit blocked. Move to clear ground.");return false;}Follow();game.Physics.FlushFront();Riding=false;Stop();parkedCollider.enabled=true;game.Player.Teleport(position);game.Player.ResetLook();game.Progress.Equip(previousTool);game.RefreshTool();game.Save();return true;}
  public void Follow(){if(Riding)transform.SetPositionAndRotation(game.Player.transform.position,game.Player.transform.rotation);}
  void LateUpdate(){Follow();SetLoop(exhaustLoop,Riding&&game!=null&&!game.MenuOpen);SetLoop(wheelDustLoop,Riding&&IsMoving&&game!=null&&!game.MenuOpen);}
  static void SetLoop(ParticleSystem system,bool active){if(system==null)return;if(active&&!system.isPlaying)system.Play(true);else if(!active&&system.isPlaying)system.Stop(true,ParticleSystemStopBehavior.StopEmitting);}
 }
}
