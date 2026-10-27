using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
string assemblyPath = Path.GetFullPath(args[1]), path = Path.GetFullPath(args[2]);
AssemblyLoadContext.Default.Resolving += (_, name) => { string candidate = Path.Combine(Path.GetDirectoryName(assemblyPath)!, name.Name + ".dll"); return File.Exists(candidate) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate) : null; };
var assembly = Assembly.LoadFrom(assemblyPath);
Func<Task> run;
if (args[0] == "baseline")
{
    var type = assembly.GetType("Wallhaven.Desktop.MainWindow")!;
    var validate = type.GetMethod("ValidateCachedImage", BindingFlags.Static | BindingFlags.NonPublic)!;
    var decode = type.GetMethod("LoadBitmapAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
    run = async () => { validate.Invoke(null, [path]); await (Task)decode.Invoke(null, [path, 2560])!; };
}
else
{
    var type = assembly.GetType("Wallhaven.Desktop.BitmapMemoryCache")!;
    var cache = Activator.CreateInstance(type, nonPublic: true)!;
    var validate = type.GetMethod("Validate")!;
    var decode = type.GetMethod("LoadAsync")!;
    run = async () => { validate.Invoke(cache, [path]); await (Task)decode.Invoke(cache, [path, 2560])!; };
}
var timings = new List<double>();
long before = GC.GetTotalAllocatedBytes(true);
for (int i = 0; i < 7; i++) { var watch = Stopwatch.StartNew(); await run(); timings.Add(watch.Elapsed.TotalMilliseconds); }
var result = new { stage = args[0], file = path, length = new FileInfo(path).Length, coldMs = timings[0], warmMedianMs = timings.Skip(1).Order().ElementAt(3), samplesMs = timings, allocatedBytes = GC.GetTotalAllocatedBytes(true) - before };
string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
File.WriteAllText(args[3], json);
