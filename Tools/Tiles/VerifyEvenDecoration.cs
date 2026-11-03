StageManager.Instance.StopRun();
var stage=StageManager.Instance;
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var place=typeof(StageManager).GetMethod("PlaceDecorationTiles",flags);
var field=typeof(StageManager).GetField("generatedDecorations",flags);
var bounds=stage.backgroundTilemap.cellBounds;
var min=bounds.min;var max=bounds.max-UnityEngine.Vector3Int.one;min.z=max.z=0;
var output=new System.Collections.Generic.List<string>();var random=UnityEngine.Random.state;
try{
for(int seed=0;seed<20;seed++){
UnityEngine.Random.InitState(seed);place.Invoke(stage,new object[]{min,max});
var entries=(System.Collections.Generic.Dictionary<UnityEngine.Vector3Int,UnityEngine.Tilemaps.TileBase>)field.GetValue(stage);
if(entries.Count==0)throw new System.Exception("no decorations");
float width=max.x-min.x+1,height=max.y-min.y+1;
int cols=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Sqrt(stage.decorationCount*width/height)),1,(int)width);
int rows=UnityEngine.Mathf.Clamp(UnityEngine.Mathf.CeilToInt((float)stage.decorationCount/cols),1,(int)height);
var regions=new System.Collections.Generic.HashSet<int>();
var positions=entries.Keys.Select(c=>(UnityEngine.Vector2)stage.decorationTilemap.GetCellCenterWorld(c)).ToArray();
foreach(var c in entries.Keys){var g=stage.backgroundTilemap.WorldToCell(stage.decorationTilemap.GetCellCenterWorld(c));
int x=UnityEngine.Mathf.Clamp((int)((g.x-min.x)/width*cols),0,cols-1),y=UnityEngine.Mathf.Clamp((int)((g.y-min.y)/height*rows),0,rows-1);regions.Add(y*cols+x);}
for(int a=0;a<positions.Length;a++)for(int b=a+1;b<positions.Length;b++)if(UnityEngine.Vector2.Distance(positions[a],positions[b])+.001f<stage.decorationMinimumSpacing)throw new System.Exception("spacing");
var quadrants=new int[4];
foreach(var pos in positions){var g=stage.backgroundTilemap.WorldToCell(pos);int x=g.x<(min.x+max.x)*.5f?0:1,y=g.y<(min.y+max.y)*.5f?0:1;quadrants[y*2+x]++;}
if(quadrants.Any(n=>n<entries.Count*.1f))throw new System.Exception("poor coverage "+string.Join(",",quadrants));
output.Add("seed "+seed+": "+entries.Count+" tiles; "+regions.Count+"/"+(cols*rows)+" regions covered; spacing PASS");
}
}finally{UnityEngine.Random.state=random;}
System.IO.File.WriteAllLines("Tools/Tiles/EvenDecorationVerification.txt",output);
UnityEditor.EditorApplication.isPlaying=false;
return output.ToArray();
