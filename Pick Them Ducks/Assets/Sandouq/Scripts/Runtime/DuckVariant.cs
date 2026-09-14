using UnityEngine;
namespace Sandouq.Ducks {
 [CreateAssetMenu(menuName="Sandouq/Duck Variant")]
 public sealed class DuckVariant:ScriptableObject {
  public string displayName="Rubber Duck";
  [Min(1)] public int coinValue=1;
  [Tooltip("Optional override for the station's deposit effect.")] public GameObject depositEffectPrefab;
 }
 [System.Serializable] public struct DuckVariantRange { [Min(0)] public int firstDuckId; [Min(1)] public int count; public DuckVariant variant; }
}
