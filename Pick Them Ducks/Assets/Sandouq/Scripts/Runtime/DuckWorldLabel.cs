using UnityEngine;

namespace Sandouq.Ducks
{
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public sealed class DuckWorldLabel : MonoBehaviour
    {
        Material instance;
        void Awake()
        {
            var renderer = GetComponent<MeshRenderer>();
            instance = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = instance;
            Font.textureRebuilt += Refresh;
            Refresh(GetComponent<TextMesh>().font);
        }
        void Refresh(Font font)
        {
            var text = GetComponent<TextMesh>();
            if (font == text.font && instance != null) instance.mainTexture = font.material.mainTexture;
        }
        void OnDestroy() { Font.textureRebuilt -= Refresh; if (instance != null) Destroy(instance); }
    }
}
