using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sandouq.Ducks
{
    public sealed class DuckStage : MonoBehaviour
    {
        readonly List<Material> materials = new List<Material>();
        public Transform Box;
        public Transform DepositTarget;
        public Transform Shop;
        public Transform[] ToolModels;
        public Renderer BoxRim;
        public Renderer ShopHeader;
        public Vector3 BoxPosition => Box == null ? new Vector3(-4,0,-4) : Box.position;
        public Vector3 ShopPosition => Shop == null ? new Vector3(4,0,-4) : Shop.position;
        #if UNITY_EDITOR
        Material teal, navy, yellow, cream, dark, orange;
        public void Build(float width, float length, Transform carry)
        {
            teal = Material("Lagoon", new Color(.04f, .48f, .47f)); navy = Material("Deep teal", new Color(.025f, .16f, .19f));
            yellow = Material("Marigold", new Color(1, .72f, .12f)); cream = Material("Warm ivory", new Color(.91f, .88f, .76f));
            dark = Material("Box interior", new Color(.04f, .09f, .11f)); orange = Material("Coral", new Color(.98f, .31f, .16f));
            Cube("Stage ground", new Vector3(0, -.22f, length * .5f - 5), new Vector3(width + 8, .4f, length + 22), teal, transform, true);
            Cube("Central return lane", new Vector3(0, -.008f, length * .5f), new Vector3(3.4f, .025f, length + 2), cream);
            Cube("Welcome apron", new Vector3(0, -.01f, -6), new Vector3(width + 6, .03f, 12), cream);
            Cube("Left boundary", new Vector3(-width * .5f - 2, .45f, length * .5f - 4), new Vector3(.5f, .9f, length + 17), navy, transform, true);
            Cube("Right boundary", new Vector3(width * .5f + 2, .45f, length * .5f - 4), new Vector3(.5f, .9f, length + 17), navy, transform, true);
            Cube("Front boundary", new Vector3(0, .45f, -12), new Vector3(width + 4, .9f, .5f), navy, transform, true);
            Cube("Far boundary", new Vector3(0, .45f, length + 4), new Vector3(width + 4, .9f, .5f), navy, transform, true);
            for (int i = 0; i < 12; i++) Cube("Lane marker", new Vector3(0, .016f, 4 + i * length / 12), new Vector3(.12f, .015f, 1.3f), yellow);
            Box = new GameObject("COLLECTION BOX").transform; Box.SetParent(transform); Box.position = new Vector3(-4,0,-4);
            Cube("Base", new Vector3(0, .15f, 0), new Vector3(2.5f, .3f, 1.8f), navy, Box, true);
            Cube("Interior", new Vector3(0, .32f, 0), new Vector3(2.25f, .12f, 1.5f), dark, Box);
            Cube("Back", new Vector3(0, .65f, .85f), new Vector3(2.5f, 1, .15f), navy, Box, true);
            Cube("Front", new Vector3(0, .55f, -.85f), new Vector3(2.5f, .8f, .15f), navy, Box, true);
            Cube("Side L", new Vector3(-1.2f, .65f, 0), new Vector3(.15f, 1, 1.8f), navy, Box, true);
            Cube("Side R", new Vector3(1.2f, .65f, 0), new Vector3(.15f, 1, 1.8f), navy, Box, true);
            BoxRim = Cube("Golden lip", new Vector3(0, 1.04f, -.86f), new Vector3(2.65f, .15f, .22f), yellow, Box).GetComponent<Renderer>();
            Cube("Sign post", new Vector3(0, 1.9f, .9f), new Vector3(.12f, 2.2f, .12f), navy, Box);
            Cube("Collection sign", new Vector3(0, 2.55f, .85f), new Vector3(3.3f, .85f, .15f), navy, Box);
            Label("COLLECTION BOX", new Vector3(0, 2.63f, .75f), .16f, Color.white, Box);
            Label("E  /  DEPOSIT & EARN", new Vector3(0, 2.32f, .74f), .09f, new Color(1, .8f, .2f), Box);
            DepositTarget = new GameObject("Deposit landing").transform; DepositTarget.SetParent(Box, false); DepositTarget.localPosition = Vector3.up * .5f;
            Shop = new GameObject("TOOL SHOP").transform; Shop.SetParent(transform); Shop.position = new Vector3(4,0,-4);
            Cube("Counter", new Vector3(0, .6f, 0), new Vector3(2.8f, 1.2f, 1.6f), orange, Shop, true);
            Cube("Countertop", new Vector3(0, 1.25f, 0), new Vector3(3, .15f, 1.8f), cream, Shop);
            Cube("Post L", new Vector3(-1.3f, 1.8f, .6f), new Vector3(.12f, 3.4f, .12f), navy, Shop);
            Cube("Post R", new Vector3(1.3f, 1.8f, .6f), new Vector3(.12f, 3.4f, .12f), navy, Shop);
            ShopHeader = Cube("Awning", new Vector3(0, 3, 0), new Vector3(3.3f, .55f, 2), yellow, Shop).GetComponent<Renderer>();
            Label("THE TOOL SHED", new Vector3(0, 2.98f, -1.02f), .16f, new Color(.02f, .12f, .14f), Shop);
            Label("E  /  BETTER TOOLS. MORE DUCKS.", new Vector3(0, .8f, -.815f), .085f, Color.white, Shop);
            Label("PICK THEM DUCKS", new Vector3(0, 4, -10), .35f, Color.white, transform, 180);
            BuildTools(carry);
            var sun = new GameObject("Afternoon sun", typeof(Light)); sun.transform.SetParent(transform); sun.transform.rotation = Quaternion.Euler(48, -28, 0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f; light.color = new Color(1, .94f, .80f); light.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .5f, .56f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.65f, .84f, .91f); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 110; RenderSettings.fogEndDistance = 350;
        }
        void BuildTools(Transform carry)
        {
            ToolModels = new Transform[5];
            for (int i = 0; i < 5; i++) { ToolModels[i] = new GameObject(((DuckTool)i).ToString()).transform; ToolModels[i].SetParent(carry, false); ToolModels[i].localScale = Vector3.one * .65f; }
            Cube("Glove", new Vector3(.06f, -.06f, -.06f), new Vector3(.18f, .11f, .28f), orange, ToolModels[0]);
            var b = ToolModels[1]; Cube("Basket floor", Vector3.zero, new Vector3(.5f, .07f, .4f), yellow, b);
            Cube("Basket front", new Vector3(0, .08f, .2f), new Vector3(.5f, .18f, .04f), yellow, b);
            Cube("Basket back", new Vector3(0, .08f, -.2f), new Vector3(.5f, .18f, .04f), yellow, b);
            for (int i = -1; i <= 1; i += 2) Cube("Basket side", new Vector3(.24f * i, .08f, 0), new Vector3(.04f, .18f, .4f), yellow, b);
            var v = ToolModels[2]; Cube("Vacuum body", new Vector3(0, -.08f, -.1f), new Vector3(.27f, .3f, .45f), orange, v);
            Cube("Vacuum nozzle", new Vector3(0, .02f, .28f), new Vector3(.18f, .16f, .45f), navy, v);
            Cube("Vacuum mouth", new Vector3(0, .02f, .52f), new Vector3(.3f, .22f, .08f), yellow, v);
            var fork=ToolModels[3];
            Cube("Long oak handle",new Vector3(0,0,.15f),new Vector3(.07f,.07f,1.5f),orange,fork);
            Cube("Fork crossbar",new Vector3(0,0,.88f),new Vector3(.95f,.07f,.1f),navy,fork);
            for(int i=0;i<5;i++)Cube("Steel tine",new Vector3((i-2)*.21f,0,1.18f),new Vector3(.045f,.045f,.65f),cream,fork);
            var industrial=ToolModels[4]; Cube("Tank",new Vector3(0,-.1f,-.1f),new Vector3(.4f,.4f,.55f),teal,industrial);
            Cube("Wide intake",new Vector3(0,0,.5f),new Vector3(.65f,.22f,.7f),yellow,industrial);
        }
        Material Material(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = name; mat.color = color; mat.SetFloat("_Smoothness", .15f); materials.Add(mat); return mat;
        }
        GameObject Cube(string name, Vector3 position, Vector3 scale, Material mat, Transform parent = null, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collision) DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static void Label(string text, Vector3 position, float size, Color color, Transform parent, float yaw = 0)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var label = go.GetComponent<TextMesh>(); label.text = text; label.fontSize = 64; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter; label.color = color;
            // TextMesh characterSize multiplies the font metrics; fit to world-space sign
            // dimensions rather than assuming it is the rendered glyph height.
            var bounds = go.GetComponent<Renderer>().bounds.size;
            if (bounds.x > 0 && bounds.y > 0)
                go.transform.localScale = Vector3.one * Mathf.Min((size >= .3f ? 8f : 2.9f) / bounds.x, size * 2 / bounds.y);
        }
        #endif
    }
}
