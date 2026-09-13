using UnityEditor;
using UnityEngine;
namespace Sandouq.Ducks.Editor
{
 [CustomEditor(typeof(DuckHabitat))]
 public sealed class DuckHabitatEditor:UnityEditor.Editor
 {
  public override void OnInspectorGUI(){DrawDefaultInspector();EditorGUILayout.HelpBox("Expand Duck spawn points and move the numbered children. Gold ducks preview the fresh-spawn positions. Saved moved ducks keep their saved positions. Keep the habitat Index unique.",MessageType.Info);}
  void OnSceneGUI()
  {
   var habitat=(DuckHabitat)target;
   for(int i=0;i<Mathf.Min(habitat.duckCount,habitat.spawnPoints.Length);i++)
   {
    var point=habitat.spawnPoints[i];if(point==null)continue;Handles.Label(point.position+Vector3.up*.25f,"Duck "+(i+1));EditorGUI.BeginChangeCheck();var position=Handles.PositionHandle(point.position,point.rotation);
    if(EditorGUI.EndChangeCheck()){Undo.RecordObject(point,"Move duck spawn");point.position=position;PrefabUtility.RecordPrefabInstancePropertyModifications(point);}
   }
  }
  [DrawGizmo(GizmoType.Selected|GizmoType.InSelectionHierarchy)]
  static void DrawSpawns(DuckHabitat habitat,GizmoType type)
  {
   var settings=AssetDatabase.LoadAssetAtPath<PrototypeSettings>(DuckPrototypeBuilder.SettingsPath);if(settings==null||settings.duckPrefab==null)return;
   var renderers=settings.duckPrefab.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)return;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float scale=settings.duckSize/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
   Gizmos.color=new Color(1,.78f,0,.8f);
   for(int i=0;i<Mathf.Min(habitat.duckCount,habitat.spawnPoints.Length);i++){var point=habitat.spawnPoints[i];if(point==null)continue;foreach(var r in renderers){Gizmos.matrix=Matrix4x4.TRS(point.position,point.rotation,Vector3.one*scale)*Matrix4x4.Translate(-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z))*r.transform.localToWorldMatrix;Gizmos.DrawMesh(r.GetComponent<MeshFilter>().sharedMesh);}Gizmos.matrix=Matrix4x4.identity;}
  }
 }
}
