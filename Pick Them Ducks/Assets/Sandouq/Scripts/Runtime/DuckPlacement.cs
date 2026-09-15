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

        public static DuckPlacement[] InScene(Scene scene, bool allSlots=false)
        {
            var result = new List<DuckPlacement>();
            if (!scene.IsValid() || !scene.isLoaded) return result.ToArray();
            int seed=1731;
            foreach(var root in scene.GetRootGameObjects()){var game=root.GetComponentInChildren<DuckGame>();if(game!=null&&game.Settings!=null){seed=game.Settings.seed;break;}}
            var chosen=new Dictionary<DuckPile,HashSet<DuckPlacement>>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var placement in root.GetComponentsInChildren<DuckPlacement>())
                {
                    if(!placement.isActiveAndEnabled)continue;
                    var pile=placement.GetComponentInParent<DuckPile>();
                    if(!allSlots&&pile!=null&&pile.enabled){if(!chosen.TryGetValue(pile,out var members)){members=new HashSet<DuckPlacement>(pile.SelectedPlacements(seed));chosen.Add(pile,members);}if(!members.Contains(placement))continue;}
                    result.Add(placement);
                }
            return result.ToArray();
        }
    }
}
