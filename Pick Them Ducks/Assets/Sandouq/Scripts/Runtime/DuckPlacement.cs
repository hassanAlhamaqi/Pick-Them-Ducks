using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Sandouq.Ducks
{
    [DisallowMultipleComponent]
    public sealed class DuckPlacement : MonoBehaviour
    {
        [Tooltip("Stable identity in the finite duck population. Each placement must have a unique ID.")]
        [Min(0)] public int duckId;
        [Tooltip("Overrides this duck's value and deposit effect. Empty uses the configured default/ID range.")]
        public DuckVariant variant;

        public static DuckPlacement[] InScene(Scene scene)
        {
            var result = new List<DuckPlacement>();
            if (!scene.IsValid() || !scene.isLoaded) return result.ToArray();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var placement in root.GetComponentsInChildren<DuckPlacement>())
                    if (placement.isActiveAndEnabled) result.Add(placement);
            return result.ToArray();
        }
    }
}
