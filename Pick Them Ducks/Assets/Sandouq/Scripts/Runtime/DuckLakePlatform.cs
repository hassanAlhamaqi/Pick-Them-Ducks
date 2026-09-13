using UnityEngine;
namespace Sandouq.Ducks
{
 public sealed class DuckLakePlatform:MonoBehaviour
 {
  public Vector2 footprint=new Vector2(2.6f,2.6f);public float topHeight=.3f;
  public bool Contains(Vector3 p){var local=transform.InverseTransformPoint(p);return Mathf.Abs(local.x)<footprint.x*.5f&&Mathf.Abs(local.z)<footprint.y*.5f;}
  public float Height=>transform.TransformPoint(Vector3.up*topHeight).y;
  void OnDrawGizmosSelected(){Gizmos.color=new Color(1,.78f,0,.6f);Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(Vector3.up*topHeight,new Vector3(footprint.x,.05f,footprint.y));}
 }
}
