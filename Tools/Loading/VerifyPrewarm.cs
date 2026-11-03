var p = PoolManager.Instance;
if(p == null || !p.IsPrewarmed) return "Still warming";
var method = typeof(PoolManager).GetMethod("GetPooledObject",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
int before = p.GetComponentsInChildren<PooledObject>(true).Length;
var held = new System.Collections.Generic.List<PooledObject>();
foreach(var entry in p.prefabsDic)
{
    int count = p.GetInactiveCount(entry.Key);
    for(int i=0;i<count;i++) held.Add((PooledObject)method.Invoke(p,new object[]{entry.Key}));
}
int during = p.GetComponentsInChildren<PooledObject>(true).Length;
foreach(var obj in held) obj.DestroyObj();
var result = new {
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
    ready = p.IsPrewarmed,
    total = p.PrewarmedObjectCount,
    before, during,
    inactive = p.prefabsDic.Sum(x=>p.GetInactiveCount(x.Key)),
    active = p.GetComponentsInChildren<PooledObject>(true).Count(x=>x.gameObject.activeSelf),
    badParent = p.GetComponentsInChildren<PooledObject>(true).Count(x=>x.transform.parent!=x.PoolParent),
    groups = p.prefabsDic.Select(x=>new {id=x.Key,name=x.Value.name,count=p.GetInactiveCount(x.Key)}).ToArray(),
    dataReady = DataManager.Instance != null,
    enemyDataMatches = p.GetComponentsInChildren<EnemyInfo>(true).Where(x=>DataManager.Instance.enemyDatasDic.ContainsKey(x.id)).All(x=>x.hp == DataManager.Instance.enemyDatasDic[x.id].hp)
};
return result;
