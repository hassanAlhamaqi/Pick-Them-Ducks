using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sandouq.Ducks.Editor
{
    public static class DuckNatureBuilder
    {
        const string Pack="Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs/";
        static DuckPark park;static Transform root;static System.Random random;
        static float Next(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
        public static void Build()
        {
            EditorSceneManager.OpenScene(DuckPrototypeBuilder.ScenePath);park=Object.FindAnyObjectByType<DuckPark>();random=new System.Random(97213);
            var previous=park.transform.Find("Ghibli nature");if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            root=new GameObject("Ghibli nature").transform;root.SetParent(park.transform,false);
            var oldDetails=park.transform.Find("Meadow details");if(oldDetails!=null)oldDetails.gameObject.SetActive(false);
            var original=new List<Transform>();foreach(Transform t in park.transform)original.Add(t);
            int trees=0,rocks=0;
            foreach(var t in original)
            {
                string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if(source.Contains("Willow Tree")){t.gameObject.SetActive(false);Place("Tree_0"+(trees++%5+1),t.position,Next(8,13));}
                else if(source.Contains("Boulder")){t.gameObject.SetActive(false);Place("Rock_0"+(rocks++%9+1),t.position,Next(1,2.5f));}
            }
            // Groves on the outer slopes frame the open collection meadows and lake.
            for(int i=0;i<110;i++)
            {var p=new Vector3(Next(-132,132),0,Next(28,264));if(Mathf.Abs(p.x)<45||park.InLake(p))continue;Place("Tree_0"+(i%5+1),p,Next(9,16));}
            int habitatIndex=0;
            foreach(Transform child in root){string source=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject);if(source.Contains("Tree")){var habitat=child.gameObject.AddComponent<DuckHabitat>();habitat.index=habitatIndex++;habitat.crownHeight=3.8f;}}
            for(int i=0;i<155;i++)
            {
                var p=new Vector3(Next(-130,130),0,Next(15,265));if(Mathf.Abs(p.x)<7||park.InLake(p))continue;
                var bush=Place("Shrubs_0"+(i%3+1),p,Next(1,1.5f));var habitat=bush.AddComponent<DuckHabitat>();habitat.bush=true;habitat.index=habitatIndex++;
            }
            // Reusable dense grass prototype for terrain GPU instancing; no per-clump scene objects.
            string grassPath="Assets/Sandouq/Park/Prefabs/Meadow Grass.prefab";
            var sourceGrass=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+"Gras_01.prefab");
            var sample=(GameObject)PrefabUtility.InstantiatePrefab(sourceGrass);var filter=sample.GetComponentInChildren<MeshFilter>();var renderer=filter.GetComponent<Renderer>();
            var grassObject=new GameObject("Meadow Grass",typeof(MeshFilter),typeof(MeshRenderer));
            var mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Normalized package grass";var bounds=mesh.bounds;var verts=mesh.vertices;
            for(int v=0;v<verts.Length;v++)verts[v]=(verts[v]-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))/Mathf.Max(.001f,bounds.size.y);mesh.vertices=verts;mesh.RecalculateBounds();
            string meshPath="Assets/Sandouq/Park/GrassMesh.asset";var existingMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existingMesh!=null){EditorUtility.CopySerialized(mesh,existingMesh);Object.DestroyImmediate(mesh);mesh=existingMesh;}else AssetDatabase.CreateAsset(mesh,meshPath);
            string matPath="Assets/Sandouq/Park/Materials/Terrain Grass.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(renderer.sharedMaterial);AssetDatabase.CreateAsset(material,matPath);}material.enableInstancing=true;if(material.HasProperty("_Wind_Strength"))material.SetFloat("_Wind_Strength",.06f);if(material.HasProperty("_Wind_Strenght"))material.SetFloat("_Wind_Strenght",.06f);EditorUtility.SetDirty(material);
            grassObject.GetComponent<MeshFilter>().sharedMesh=mesh;grassObject.GetComponent<MeshRenderer>().sharedMaterial=material;
            System.IO.File.WriteAllText("Logs/grass-dimensions.txt","Source mesh "+bounds+"; normalized "+mesh.bounds+"; vertices "+mesh.vertexCount);
            var grassPrefab=PrefabUtility.SaveAsPrefabAsset(grassObject,grassPath);Object.DestroyImmediate(grassObject);Object.DestroyImmediate(sample);
            var data=park.terrain.terrainData;data.SetDetailResolution(512,16);data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            data.detailPrototypes=new[]{new DetailPrototype{prototype=grassPrefab,usePrototypeMesh=true,useInstancing=true,renderMode=DetailRenderMode.VertexLit,minWidth=.55f,maxWidth=.9f,minHeight=.35f,maxHeight=.6f,noiseSpread=.3f}};
            var density=new int[512,512];
            for(int z=0;z<512;z++)for(int x=0;x<512;x++)
            {var p=park.terrain.transform.position+new Vector3((x+.5f)/512*data.size.x,0,(z+.5f)/512*data.size.z);if(!park.InLake(p)&&Mathf.Abs(p.x)>5&&p.z>5)density[z,x]=2;}
            data.SetDetailLayer(0,0,0,density);park.terrain.detailObjectDistance=65;park.terrain.detailObjectDensity=1;EditorUtility.SetDirty(data);EditorUtility.SetDirty(park.terrain);
            var oldBridge=park.transform.Find("Lake bridges");if(oldBridge!=null)Object.DestroyImmediate(oldBridge.gameObject);
            var bridges=new GameObject("Lake bridges").transform;bridges.SetParent(park.transform,false);
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Cedar.mat");
            for(int axis=0;axis<2;axis++)
            {
                float length=(axis==0?park.lakeRadius.y:park.lakeRadius.x)*2+10;
                var bridge=new GameObject(axis==0?"North south bridge":"East west bridge");
                for(float t=-length*.5f;t<length*.5f;t+=.5f){var plank=GameObject.CreatePrimitive(PrimitiveType.Cube);plank.name="Bridge plank";plank.transform.SetParent(bridge.transform,false);plank.transform.localPosition=new Vector3(0,.35f,t);plank.transform.localScale=new Vector3(4,.22f,.48f);plank.GetComponent<Renderer>().sharedMaterial=wood;}
                string path="Assets/Sandouq/Park/Prefabs/"+bridge.name+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(bridge,path);Object.DestroyImmediate(bridge);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,bridges);instance.transform.SetPositionAndRotation(park.lakeCenter,Quaternion.Euler(0,axis*90,0));
            }
            var meadow=AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Sandouq/Park/Meadow.terrainlayer");
            meadow.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/3D set of stylized nature - GHIBLI style/Art/Textures/Grass_ground_Base_Color.png");
            meadow.tileSize=new Vector2(4,4);EditorUtility.SetDirty(meadow);
            var pathMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sandouq/Park/Materials/Sand path.mat");pathMaterial.SetTexture("_BaseMap",null);pathMaterial.SetColor("_BaseColor",new Color(.74f,.65f,.43f));EditorUtility.SetDirty(pathMaterial);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            DuckPrototypeChecks.Run(Object.FindAnyObjectByType<DuckGame>().Settings);DuckPrototypeBuilder.BuildValidationPlayer();
        }
        static GameObject Place(string name,Vector3 position,float height,float yaw=-1,float width=0)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+name+".prefab");if(prefab==null)throw new System.Exception("Missing nature prefab: "+name);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return go;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float scale=height/Mathf.Max(.01f,bounds.size.y);go.transform.localScale*=scale;
            if(width>0){var s=go.transform.localScale;s.x*=width/Mathf.Max(.01f,bounds.size.x*scale);go.transform.localScale=s;}
            bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            position.y=park.Ground(position)-bounds.min.y;go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw<0?Next(0,360):yaw,0);
            // Decorative foliage should never obstruct duck sweeps or installation queries.
            bool small=name.StartsWith("Flower")||name.StartsWith("Gras")||name.StartsWith("Shrubs");
            foreach(var c in go.GetComponentsInChildren<Collider>())if(small)c.enabled=false;
            return go;
        }
    }
}
