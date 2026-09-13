using UnityEngine;
namespace Sandouq.Ducks
{
 public sealed class DuckStage:MonoBehaviour
 {
  public Transform Box,DepositTarget,Shop;
  public Transform[] ToolModels;
  public Renderer BoxRim,ShopHeader;
  public Vector3 BoxPosition=>Box.position;
  public Vector3 ShopPosition=>Shop.position;
 }
}
