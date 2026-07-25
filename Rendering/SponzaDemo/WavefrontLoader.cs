using System.Globalization;
using TheEngine.Data;
using TheMaths;

namespace SponzaDemo;

/// <summary>
/// A small wavefront .obj + .mtl loader: enough for multi-material scenes like Sponza
/// (groups split by usemtl, quads triangulated, per-group vertex deduplication, groups
/// split when they would exceed the engine's 16-bit index range). Deliberately not part
/// of the engine - asset formats are the application's business.
/// </summary>
public static class WavefrontLoader
{
    public class ObjMaterial
    {
        public string Name = "";
        public Vector3 DiffuseColor = Vector3.One;
        public string? DiffuseTexture;
        public string? MaskTexture;
    }

    public class ObjGroup
    {
        public ObjMaterial Material = new();
        public MeshData Mesh;
    }

    public static List<ObjGroup> Load(string objPath)
    {
        var materials = new Dictionary<string, ObjMaterial>();
        var directory = Path.GetDirectoryName(objPath)!;

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        var groups = new List<ObjGroup>();
        var currentMaterial = new ObjMaterial();

        // per-group builders
        var groupVertices = new List<Vector3>();
        var groupNormals = new List<Vector3>();
        var groupUvs = new List<Vector2>();
        var groupIndices = new List<ushort>();
        var vertexCache = new Dictionary<(int v, int vt, int vn), ushort>();
        Span<ushort> faceCorners = stackalloc ushort[8];

        void FlushGroup()
        {
            if (groupIndices.Count == 0)
                return;
            groups.Add(new ObjGroup
            {
                Material = currentMaterial,
                Mesh = new MeshData(groupVertices.ToArray(), groupNormals.ToArray(), groupUvs.ToArray(), groupIndices.ToArray())
            });
            groupVertices.Clear();
            groupNormals.Clear();
            groupUvs.Clear();
            groupIndices.Clear();
            vertexCache.Clear();
        }

        ushort AddCorner(ReadOnlySpan<char> corner)
        {
            // v, v/vt, v/vt/vn or v//vn - indices are 1-based
            int v = 0, vt = 0, vn = 0, part = 0, value = 0;
            bool any = false;
            foreach (var c in corner)
            {
                if (c == '/')
                {
                    if (part == 0) v = any ? value : 0;
                    else if (part == 1) vt = any ? value : 0;
                    part++;
                    value = 0;
                    any = false;
                }
                else
                {
                    value = value * 10 + (c - '0');
                    any = true;
                }
            }
            if (part == 0) v = any ? value : 0;
            else if (part == 1) vt = any ? value : 0;
            else vn = any ? value : 0;

            if (vertexCache.TryGetValue((v, vt, vn), out var cached))
                return cached;

            var index = (ushort)groupVertices.Count;
            groupVertices.Add(positions[v - 1]);
            groupNormals.Add(vn > 0 ? normals[vn - 1] : Vector3.UnitZ);
            groupUvs.Add(vt > 0 ? uvs[vt - 1] : Vector2.Zero);
            vertexCache[(v, vt, vn)] = index;
            return index;
        }

        foreach (var rawLine in File.ReadLines(objPath))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.IsEmpty || line[0] == '#')
                continue;

            if (line.StartsWith("v "))
            {
                positions.Add(ParseVector3(line[2..]));
            }
            else if (line.StartsWith("vn "))
            {
                normals.Add(ParseVector3(line[3..]));
            }
            else if (line.StartsWith("vt "))
            {
                var uv = ParseVector3(line[3..]);
                // obj uv origin is bottom-left, the engine samples top-left
                uvs.Add(new Vector2(uv.X, 1 - uv.Y));
            }
            else if (line.StartsWith("f "))
            {
                // a full face must fit in 16-bit indices; flush before parsing if close to the limit
                if (groupVertices.Count >= ushort.MaxValue - 8)
                    FlushGroup();

                int corners = 0;
                foreach (var range in Tokenize(line[2..]))
                {
                    if (corners < faceCorners.Length)
                        faceCorners[corners++] = AddCorner(range);
                }
                // triangulate as a fan (handles quads and convex n-gons)
                for (int i = 2; i < corners; ++i)
                {
                    groupIndices.Add(faceCorners[0]);
                    groupIndices.Add(faceCorners[i - 1]);
                    groupIndices.Add(faceCorners[i]);
                }
            }
            else if (line.StartsWith("usemtl "))
            {
                FlushGroup();
                var name = line[7..].Trim().ToString();
                if (!materials.TryGetValue(name, out var material))
                    material = materials[name] = new ObjMaterial { Name = name };
                currentMaterial = material;
            }
            else if (line.StartsWith("mtllib "))
            {
                LoadMtl(Path.Combine(directory, line[7..].Trim().ToString()), materials);
            }
        }
        FlushGroup();
        return groups;
    }

    private static void LoadMtl(string path, Dictionary<string, ObjMaterial> materials)
    {
        if (!File.Exists(path))
            return;
        var directory = Path.GetDirectoryName(path)!;
        ObjMaterial? current = null;
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.AsSpan().Trim();
            if (line.IsEmpty || line[0] == '#')
                continue;
            if (line.StartsWith("newmtl "))
            {
                var name = line[7..].Trim().ToString();
                current = materials[name] = new ObjMaterial { Name = name };
            }
            else if (current == null)
            {
            }
            else if (line.StartsWith("Kd "))
            {
                current.DiffuseColor = ParseVector3(line[3..]);
            }
            else if (line.StartsWith("map_Kd "))
            {
                current.DiffuseTexture = Path.Combine(directory, line[7..].Trim().ToString());
            }
            else if (line.StartsWith("map_d "))
            {
                current.MaskTexture = Path.Combine(directory, line[6..].Trim().ToString());
            }
        }
    }

    private static Vector3 ParseVector3(ReadOnlySpan<char> text)
    {
        Span<float> components = stackalloc float[3];
        int i = 0;
        foreach (var range in Tokenize(text))
        {
            if (i < 3)
                components[i++] = float.Parse(range, CultureInfo.InvariantCulture);
        }
        return new Vector3(components[0], components[1], components[2]);
    }

    private static TokenEnumerator Tokenize(ReadOnlySpan<char> text) => new(text);

    private ref struct TokenEnumerator
    {
        private ReadOnlySpan<char> remaining;
        public ReadOnlySpan<char> Current { get; private set; }

        public TokenEnumerator(ReadOnlySpan<char> text) => remaining = text;
        public TokenEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = remaining;
            int start = 0;
            while (start < span.Length && span[start] == ' ')
                start++;
            if (start == span.Length)
                return false;
            int end = start;
            while (end < span.Length && span[end] != ' ')
                end++;
            Current = span[start..end];
            remaining = span[end..];
            return true;
        }
    }
}
