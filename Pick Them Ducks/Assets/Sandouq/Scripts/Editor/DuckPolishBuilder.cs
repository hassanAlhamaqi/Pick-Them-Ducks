using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Sandouq.Ducks.Editor
{
    public static class DuckPolishBuilder
    {
        const string Root="Assets/Sandouq/Park/";
        public static void Build()
        {
            EditorSceneManager.OpenScene(DuckPrototypeBuilder.ScenePath);
            var game=Object.FindAnyObjectByType<DuckGame>();var park=game.Park;
            var station=game.authoredStage.Box.GetComponent<DuckDepositStation>();
            station.intake=new Bounds(new Vector3(0,.55f,0),new Vector3(3.5f,1.8f,3.5f));EditorUtility.SetDirty(station);
            var casket=PrefabUtility.LoadPrefabContents(Root+"Prefabs/Duck Casket.prefab");
            casket.GetComponent<DuckDepositStation>().intake=new Bounds(new Vector3(0,.55f,0),new Vector3(3.2f,1.8f,3.2f));
            PrefabUtility.SaveAsPrefabAsset(casket,Root+"Prefabs/Duck Casket.prefab");PrefabUtility.UnloadPrefabContents(casket);
            var old=park.transform.Find("Meadow details");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var details=new GameObject("Meadow details");details.transform.SetParent(park.transform,false);
            if(!AssetDatabase.IsValidFolder(Root+"Grass"))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Grass");
            var grass=AssetDatabase.LoadAssetAtPath<Material>(Root+"Grass/Grass.mat");
            if(grass==null){grass=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(grass,Root+"Grass/Grass.mat");}
            grass.SetColor("_BaseColor",new Color(.31f,.48f,.16f));grass.SetFloat("_Cull",0);grass.enableInstancing=true;
            var random=new System.Random(82371);
            for(int x=0;x<12;x++)for(int z=0;z<12;z++)
            {
                var origin=new Vector3(-144+x*24,0,-10+z*24);var vertices=new List<Vector3>();var triangles=new List<int>();
                for(int i=0;i<180;i++)
                {
                    var p=origin+new Vector3((float)random.NextDouble()*24,0,(float)random.NextDouble()*24);
                    if(Mathf.Abs(p.x)<5||park.InLake(p)||p.z<8)continue;p.y=park.Ground(p)+.015f;
                    float height=.18f+(float)random.NextDouble()*.3f;
                    for(int blade=0;blade<3;blade++)
                    {
                        float angle=(float)random.NextDouble()*Mathf.PI*2;var side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.07f;
                        int n=vertices.Count;vertices.Add(p-origin-side);vertices.Add(p-origin+side);vertices.Add(p-origin+Vector3.up*height+side*.7f);
                        triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
                    }
                }
                var mesh=new Mesh{name="Meadow grass patch"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                string path=Root+"Grass/Patch-"+x+"-"+z+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
                var patch=new GameObject("Grass patch "+x+", "+z,typeof(MeshFilter),typeof(MeshRenderer));patch.transform.SetParent(details.transform);patch.transform.position=origin;
                patch.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=patch.GetComponent<MeshRenderer>();renderer.sharedMaterial=grass;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            var wood=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Cedar.mat");
            var fence=new GameObject("Park Fence");
            Piece(fence,"Post",new Vector3(-3,.65f,0),new Vector3(.18f,1.3f,.18f),wood);
            Piece(fence,"Upper rail",new Vector3(0,1.05f,0),new Vector3(6,.13f,.12f),wood);
            Piece(fence,"Lower rail",new Vector3(0,.48f,0),new Vector3(6,.13f,.12f),wood);
            var prefab=PrefabUtility.SaveAsPrefabAsset(fence,Root+"Prefabs/Park Fence.prefab");Object.DestroyImmediate(fence);
            for(int i=0;i<48;i++)
            {
                float coordinate=-141+i*6;
                Place(prefab,details.transform,park,new Vector3(coordinate,0,277),0);
                if(Mathf.Abs(coordinate)>9)Place(prefab,details.transform,park,new Vector3(coordinate,0,-15),0);
                Place(prefab,details.transform,park,new Vector3(-144,0,-12+i*6),90);
                Place(prefab,details.transform,park,new Vector3(144,0,-12+i*6),90);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            DuckPrototypeChecks.Run(game.Settings);
            var data=new SaveData{total=1000,seed=1,money=10000};var progress=new Progression(game.Settings,data);progress.BuyTool(2);int capacity=progress.Capacity;progress.Equip(0);
            if(progress.Capacity!=capacity)throw new System.Exception("Shared bag capacity regression");
            DuckPrototypeBuilder.BuildValidationPlayer();
        }
        static void Piece(GameObject parent,string name,Vector3 position,Vector3 scale,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;}
        static void Place(GameObject prefab,Transform parent,DuckPark park,Vector3 position,float yaw)
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);position.y=park.Ground(position);go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));}
    }
}
