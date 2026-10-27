using System.Text;
using System.Text.Json;
using PriFormat;
using XbfTools.XbfFormat;

if (args.Length != 2) throw new ArgumentException("Usage: UiResourceExtractor <resources.pri> <output-directory>");
var output = Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
using var stream = File.OpenRead(args[0]);
var pri = PriFile.Parse(stream);
var inventory = new List<object>();
int converted = 0, failed = 0;
foreach (var mapRef in pri.PriDescriptorSection.ResourceMapSections)
{
    var map = pri.GetSectionByRef(mapRef);
    if (map.HierarchicalSchemaReference != null) continue;
    foreach (var set in map.CandidateSets.Values)
    {
        var item = pri.GetResourceMapItemByRef(set.ResourceMapItem);
        int index = 0;
        foreach (var candidate in set.Candidates)
        {
            index++;
            if (candidate.SourceFile != null) { inventory.Add(new { name = item.FullName, type = candidate.Type.ToString(), external = true }); continue; }
            var span = candidate.DataItem != null ? pri.GetDataItemByRef(candidate.DataItem.Value) : candidate.Data!.Value;
            byte[] data = new byte[span.Length];
            stream.Position = span.Offset;
            stream.ReadExactly(data);
            if (candidate.Type != ResourceValueType.EmbeddedData)
            {
                var encoding = candidate.Type is ResourceValueType.AsciiPath or ResourceValueType.AsciiString ? Encoding.ASCII : candidate.Type is ResourceValueType.Utf8Path or ResourceValueType.Utf8String ? Encoding.UTF8 : Encoding.Unicode;
                inventory.Add(new { name = item.FullName, type = candidate.Type.ToString(), value = encoding.GetString(data).TrimEnd('\0') });
                continue;
            }
            string relative = item.FullName.Replace('\\', '/').TrimStart('/');
            if (set.Candidates.Count() > 1) relative += $".candidate-{index}";
            var path = Path.GetFullPath(Path.Combine(output, "raw", relative));
            if (!path.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid resource path");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, data);
            string? xaml = null, error = null;
            if (data.Length > 4 && data[0] == 'X' && data[1] == 'B' && data[2] == 'F')
            {
                try
                {
                    using var reader = new XbfReader(path);
                    xaml = Path.ChangeExtension(relative, ".xaml");
                    var destination = Path.Combine(output, "xaml", xaml);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.WriteAllText(destination, reader.ReadRootNodeSection().ToString(), Encoding.UTF8);
                    converted++;
                }
                catch (Exception e) { error = e.ToString(); failed++; }
            }
            inventory.Add(new { name = item.FullName, type = "EmbeddedData", bytes = data.Length, xaml, error });
        }
    }
}
File.WriteAllText(Path.Combine(output, "inventory.json"), JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
Console.WriteLine($"Resources: {inventory.Count}; XAML converted: {converted}; failed: {failed}");
