var text = UnityEngine.Resources.Load<UnityEngine.TextAsset>("TextData/PlayerData").text;
var lines = text.Split('\n');
var parse = typeof(DataManager).GetMethod("ParseCsvLine", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
var headers = (string[])parse.Invoke(null, new object[]{lines[0]});
var values = (string[])parse.Invoke(null, new object[]{lines[1]});
if (headers.Length != 12) throw new System.Exception("Expected 12 CSV columns");
values[9] = "0,0,1,1,2,2";
values[10] = "1,1.1,1.2,1.3,1.4,1.5";
values[11] = "1,1.05,1.1,1.15,1.2,1.25";
var sample = lines[0] + "\n" + string.Join(",", values.Select(v => "\"" + v.Replace("\"", "\"\"") + "\""));
var parsed = CharacterEnhancementTable.Parse(sample);
var field = typeof(CharacterEnhancementTable).GetField("curves", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
var original = field.GetValue(null);
try
{
    field.SetValue(null, parsed);
    var m = CharacterEnhancementTable.BasicAttack(1, 5, out float speed);
    var zero = CharacterEnhancementTable.BasicAttack(1, 0, out float zeroSpeed);
    CharacterEnhancementTable.TryGet(1, 5, true, out var bonus);
    if (m.pierceCount != 2 || UnityEngine.Mathf.Abs(m.rangeMultiplier - 1.5f) > .0001f || UnityEngine.Mathf.Abs(speed - 1.25f) > .0001f || bonus.hp != 50 || zeroSpeed != 1f || zero.pierceCount != 0)
        throw new System.Exception("Combat array verification failed");
    return new { pierce = m.pierceCount, range = m.rangeMultiplier, attackSpeed = speed, cooldownAtBaseOneSecond = 1f / speed, cumulativeHp = bonus.hp };
}
finally { field.SetValue(null, original); }
