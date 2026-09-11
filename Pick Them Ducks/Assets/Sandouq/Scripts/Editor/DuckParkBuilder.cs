using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Sandouq.Ducks.Editor
{
    public static class DuckParkBuilder
    {
        const string Folder="Assets/Sandouq/Park";
        static Material grass,wood,leaf,rock,cream,water;
        public static void Build()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Folder+"/Prefabs"); Directory.CreateDirectory(Folder+"/Materials"); AssetDatabase.Refresh();
            var settings=AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath);
            settings.spacing=1.2f;
            settings.tools=new[]{new ToolDefinition("HANDS",0,10,2.8f,.24f,1),new ToolDefinition("BASKET",25,45,3.2f,.28f,4),new ToolDefinition("DUCK VACUUM",220,120,5.5f,.085f,6),new ToolDefinition("BIG FORK",80,120,3.3f,.3f,12),new ToolDefinition("INDUSTRIAL VAC",1500,250,9,.055f,12)};
            settings.upgrades=new[]{new UpgradeDefinition("Capacity +25",35,25),new UpgradeDefinition("Pickup speed +20%",40,.2f),new UpgradeDefinition("Vacuum range +1m",80,1),new UpgradeDefinition("Vacuum speed +25%",100,.25f)};
            foreach(var u in settings.upgrades)u.costMultiplier=1.5f;
            EditorUtility.SetDirty(settings);
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Duck Park — gameplay").AddComponent<DuckGame>(); game.Settings=settings;
            var player=new GameObject("Player",typeof(CharacterController),typeof(DuckPlayer)).GetComponent<DuckPlayer>(); player.Initialize(null); player.transform.position=new Vector3(0,.1f,-2);
            game.authoredPlayer=player;
            var stage=new GameObject("Authored park landmarks").AddComponent<DuckStage>(); stage.Build(280,280,player.CarryTarget); game.authoredStage=stage;
            // Keep meshes/materials as assets so all scene and prefab references survive reload.
            var mats=new System.Collections.Generic.HashSet<Material>();
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) foreach(var m in r.sharedMaterials)if(m!=null && !AssetDatabase.Contains(m) && m.shader.name=="Universal Render Pipeline/Lit")mats.Add(m);
            foreach(var m in mats)Save(m,Folder+"/Materials/"+m.name+".mat");
            foreach(Transform child in new System.Collections.Generic.List<Transform>(stage.transform.GetComponentsInChildren<Transform>(true)))if(child.name.Contains("ground")||child.name.Contains("boundary")||child.name=="Central return lane"||child.name=="Lane marker")Object.DestroyImmediate(child.gameObject);
            var apron=stage.transform.Find("Welcome apron");apron.localScale=new Vector3(28,.03f,12);
            grass=Mat("Meadow",new Color(.34f,.52f,.21f)); wood=Mat("Cedar",new Color(.30f,.17f,.085f)); leaf=Mat("Willow",new Color(.20f,.39f,.15f));rock=Mat("Granite",new Color(.43f,.48f,.43f));cream=Mat("Sand path",new Color(.74f,.65f,.43f));water=Mat("Lake",new Color(.10f,.49f,.59f)); water.SetFloat("_Smoothness",.86f);
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))light.shadows=LightShadows.Soft;
            var park=new GameObject("Park terrain and scenery").AddComponent<DuckPark>(); game.Park=park;
            var td=new TerrainData{heightmapResolution=513,size=new Vector3(300,24,310)};
            var heights=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)
            {
                float wx=x/512f*300-150,wz=z/512f*310-20;
                float hub=Mathf.SmoothStep(0,1,Mathf.Clamp01((wz-8)/38));
                float h=(Mathf.PerlinNoise(wx*.014f+17,wz*.014f+11)*5+Mathf.Sin(wx*.025f)*1.5f+2)*hub;
                h+=hub*(9*Mathf.Exp(-Mathf.Pow((wx-62)/26,2)-Mathf.Pow((wz-110)/31,2)) + 11*Mathf.Exp(-Mathf.Pow((wx+90)/26,2)-Mathf.Pow((wz-216)/30,2)) + 7*Mathf.Exp(-Mathf.Pow((wx-77)/33,2)-Mathf.Pow((wz-230)/29,2)));
                float lake=Mathf.Pow((wx+62)/40,2)+Mathf.Pow((wz-100)/54,2);
                h=Mathf.Lerp(-1.5f,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((lake-.82f)/.5f)));
                heights[z,x]=(h+2)/24;
            }
            td.SetHeights(0,0,heights); Save(td,Folder+"/ParkTerrain.asset");
            var tex=new Texture2D(4,4);var pixels=new Color[16]; for(int i=0;i<16;i++)pixels[i]=grass.color*(i%3==0?.92f:1);tex.SetPixels(pixels);tex.Apply();Save(tex,Folder+"/GrassTexture.asset");
            var layer=new TerrainLayer{diffuseTexture=tex,tileSize=new Vector2(7,7)};Save(layer,Folder+"/Meadow.terrainlayer");td.terrainLayers=new[]{layer};
            var terrain=Terrain.CreateTerrainGameObject(td);terrain.name="Sculpted meadow and lake basin";terrain.transform.SetParent(park.transform);terrain.transform.position=new Vector3(-150,-2,-20);park.terrain=terrain.GetComponent<Terrain>();
            park.terrain.materialTemplate=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));Save(park.terrain.materialTemplate,Folder+"/Materials/Terrain.mat");park.terrain.drawInstanced=true;park.terrain.heightmapPixelError=8;
            water.shader=Shader.Find("Sandouq/Park Water");water.SetColor("_BaseColor",new Color(.08f,.43f,.51f));EditorUtility.SetDirty(water);
            var lakeGo=Primitive("Lake water",PrimitiveType.Cylinder,new Vector3(-62,-.32f,100),new Vector3(79,.06f,107),water,park.transform,false);
            // Trees, rocks and benches are reusable assets, then placed as real prefab instances.
            var tree=new GameObject("Willow tree");Primitive("Trunk",PrimitiveType.Cylinder,new Vector3(0,2,0),new Vector3(.7f,2,.7f),wood,tree.transform,true);
            Primitive("Crown",PrimitiveType.Sphere,new Vector3(0,5,0),new Vector3(5,5,4.5f),leaf,tree.transform,false);
            Primitive("Crown cluster",PrimitiveType.Sphere,new Vector3(1.7f,4.5f,.5f),new Vector3(3.5f,3.7f,3.5f),leaf,tree.transform,false);
            var treePrefab=Prefab(tree,"Willow Tree");Object.DestroyImmediate(tree);
            var stone=Primitive("Lakeside boulder",PrimitiveType.Sphere,Vector3.zero,new Vector3(2,1.4f,1.6f),rock,null,true);var stonePrefab=Prefab(stone,"Boulder");Object.DestroyImmediate(stone);
            var bench=new GameObject("Park bench");Primitive("Seat",PrimitiveType.Cube,new Vector3(0,.55f,0),new Vector3(2,.16f,.6f),wood,bench.transform,true);Primitive("Back",PrimitiveType.Cube,new Vector3(0,1,.27f),new Vector3(2,.7f,.1f),wood,bench.transform,true);for(int i=-1;i<=1;i+=2)Primitive("Leg",PrimitiveType.Cube,new Vector3(i*.7f,.25f,0),new Vector3(.12f,.5f,.5f),rock,bench.transform,true);var benchPrefab=Prefab(bench,"Park Bench");Object.DestroyImmediate(bench);
            var random=new System.Random(761);
            for(int i=0;i<110;i++) { float a=i*Mathf.PI*2/110;var pos=i<55?new Vector3(-62+Mathf.Cos(a*2)*49,0,100+Mathf.Sin(a*2)*67):new Vector3((i%2==0?-1:1)*(136+(float)random.NextDouble()*7),0,15+(float)random.NextDouble()*255);pos=park.Land(pos);var t=Place(treePrefab,pos,park.transform);t.localScale=Vector3.one*(.75f+(float)random.NextDouble()*.65f);t.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0); }
            foreach(var center in new[]{new Vector3(48,0,69),new Vector3(94,0,157),new Vector3(-113,0,198),new Vector3(43,0,246)})
            {
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var t=Place(treePrefab,park.Land(center+new Vector3(Mathf.Cos(a)*7,0,Mathf.Sin(a)*7)),park.transform);t.localScale=Vector3.one*(.8f+i*.055f);}
                Place(benchPrefab,park.Land(center),park.transform);
            }
            for(int i=0;i<25;i++){float a=i*Mathf.PI*2/25;Place(stonePrefab,park.Land(new Vector3(-62+Mathf.Cos(a)*42,0,100+Mathf.Sin(a)*57)),park.transform);}
            for(int i=0;i<5;i++)Place(benchPrefab,park.Land(new Vector3(7,0,25+i*48)),park.transform);
            // A mesh ribbon follows the ground: editable asset, no runtime road generation.
            var vertices=new Vector3[122];var triangles=new int[360];for(int i=0;i<=60;i++){float z=i*4.5f; for(int side=0;side<2;side++){var v=new Vector3(side==0?-1.7f:1.7f,0,z);v.y=park.Ground(v)+.04f;vertices[i*2+side]=v;}if(i<60){int t=i*6,j=i*2;triangles[t]=j;triangles[t+1]=j+2;triangles[t+2]=j+1;triangles[t+3]=j+1;triangles[t+4]=j+2;triangles[t+5]=j+3;}}
            var mesh=new Mesh{name="Meadow trail"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();Save(mesh,Folder+"/Trail.asset");var path=new GameObject("Meadow walking trail",typeof(MeshFilter),typeof(MeshRenderer));path.transform.SetParent(park.transform);path.GetComponent<MeshFilter>().sharedMesh=mesh;path.GetComponent<MeshRenderer>().sharedMaterial=cream;
            var dock=new GameObject("Lakeside dock");for(int i=0;i<18;i++)Primitive("Cedar plank",PrimitiveType.Cube,new Vector3(0,.3f,i*.5f),new Vector3(3,.2f,.46f),wood,dock.transform,true);for(int i=0;i<4;i++)Primitive("Dock post",PrimitiveType.Cylinder,new Vector3(i%2==0?-1.4f:1.4f,-.3f,i<2?0:8),new Vector3(.2f,1,.2f),wood,dock.transform,true);var dockPrefab=Prefab(dock,"Fishing Dock");Object.DestroyImmediate(dock);Place(dockPrefab,new Vector3(-62,0,46),park.transform);
            var casket=new GameObject("Portable casket — 1000 ducks");Primitive("Case",PrimitiveType.Cube,new Vector3(0,.36f,0),new Vector3(1.1f,.6f,.65f),grass,casket.transform,true);Primitive("Lid",PrimitiveType.Cube,new Vector3(0,.7f,0),new Vector3(1.16f,.1f,.7f),wood,casket.transform,false);for(int i=-1;i<=1;i+=2)Primitive("Handle",PrimitiveType.Cube,new Vector3(i*.61f,.45f,0),new Vector3(.16f,.1f,.3f),cream,casket.transform,false);
            var casketPrefab=Prefab(casket,"Portable Casket");Object.DestroyImmediate(casket);park.casket=Place(casketPrefab,new Vector3(7,0,-4),park.transform);
            PrefabUtility.SaveAsPrefabAssetAndConnect(stage.Shop.gameObject,Folder+"/Prefabs/Tool Shop.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(stage.Box.gameObject,Folder+"/Prefabs/Collection Box.prefab",InteractionMode.AutomatedAction);
            for(int i=0;i<stage.ToolModels.Length;i++)PrefabUtility.SaveAsPrefabAssetAndConnect(stage.ToolModels[i].gameObject,Folder+"/Prefabs/"+((DuckTool)i)+" Tool.prefab",InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(player.gameObject,Folder+"/Prefabs/Player.prefab",InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene,DuckPrototypeBuilder.ScenePath);var entries=new System.Collections.Generic.List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(DuckPrototypeBuilder.ScenePath,true)};
            foreach(var entry in EditorBuildSettings.scenes)if(entry.path!=DuckPrototypeBuilder.ScenePath)entries.Add(entry);
            EditorBuildSettings.scenes=entries.ToArray();AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/park-build.txt","PASS: authored terrain, scene references, landmarks and reusable prefabs saved.");
        }
        static void Save(Object o,string path){var existing=AssetDatabase.LoadAssetAtPath<Object>(path);if(existing!=null)AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(o,path);}
        static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",.12f);Save(m,Folder+"/Materials/"+name+".mat");return m;}
        static GameObject Primitive(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material mat,Transform parent,bool collision){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject Prefab(GameObject go,string name)=>PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Prefabs/"+name+".prefab");
        static Transform Place(GameObject prefab,Vector3 p,Transform parent){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);g.transform.position=p;return g.transform;}
    }
}
