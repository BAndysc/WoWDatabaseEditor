using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TheEngine.Entities;

namespace TheEngine.Resources
{
    internal enum ShaderVariableType
    {
        Int,
        Float,
        Float2,
        Float3,
        Float4,
        Sampler2D,
        Matrix
    }
    
    internal class ShaderSource
    {
        private const string IncludeMacro = "#include ";

        public static IEnumerable<(ShaderVariableType type, string name)> ParseUniforms(string source)
        {
            int newLine;
            int lastNewLine = 0;
            do
            {
                newLine = source.IndexOf('\n', lastNewLine);
                var end = newLine == -1 ? source.Length : newLine;
                var line = source.AsSpan(lastNewLine, end - lastNewLine);
                line = line.Trim();
                if (line.StartsWith("uniform"))
                {
                    line = line.Slice(7).TrimStart();
                    var spaceIndex = line.IndexOf(' ');
                    if (spaceIndex != -1)
                    {
                        var variableType = line.Slice(0, spaceIndex);
                        var variableName = line.Slice(spaceIndex + 1).TrimStart().TrimEnd(';');
                        yield return (ParseType(variableType), variableName.ToString());
                    }
                }
                lastNewLine = newLine + 1;
            } while (newLine != -1);
        }

        private static ShaderVariableType ParseType(ReadOnlySpan<char> variableType)
        {
            if (variableType.Equals("int", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Int;
            if (variableType.Equals("bool", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Int;
            if (variableType.Equals("float", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Float;
            if (variableType.Equals("vec2", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Float2;
            if (variableType.Equals("vec3", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Float3;
            if (variableType.Equals("vec4", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Float4;
            if (variableType.Equals("mat4", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Matrix;
            if (variableType.Equals("sampler2D", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.Sampler2D;
            throw new Exception("Unknown variable type " + variableType.ToString());
        }

        public static string ParseShader(string file, bool mainFile, params string[] defines)
        {
            var source = File.ReadAllText(file);
            var shaderDir = Path.GetDirectoryName(file);

            var lineEnumerator = new StringLineEnumerator(source);

            if (!lineEnumerator.MoveNext())
                throw new Exception("Shader file is empty");

            var version = lineEnumerator.Current;
            if (!version.StartsWith("#version") && mainFile)
                throw new Exception("#version macro missing in the very top of the file");
            
            if (!version.StartsWith("#version"))
                lineEnumerator = new StringLineEnumerator(source);
            
            List<string> includes = new();
            while (lineEnumerator.MoveNext() && lineEnumerator.Current.StartsWith(IncludeMacro))
            {
                var path = lineEnumerator.Current.Slice(IncludeMacro.Length + 1,
                    lineEnumerator.Current.Length - IncludeMacro.Length - 2);
                includes.Add(ParseShader(Path.Join(shaderDir,  path.ToString()), false));
            }

            StringBuilder final = new();
            if (version.StartsWith("#version"))
            {
                final.Append(version);
                final.AppendLine();
            }

            foreach (var define in defines)
                final.AppendLine($"#define {define}");

            foreach (var incl in includes)
                final.AppendLine(incl);

            final.Append(lineEnumerator.Current);
            final.AppendLine();
            final.Append(lineEnumerator.Rest);
            return final.ToString();
        }
    }

    public enum ShaderPassType
    {
        None,
        Forward,
        Depth,
        Shadow
    }

    /// <summary>
    /// Backend-neutral view of a compiled shader pass: the identity bound together with a
    /// <see cref="TheEngine.Resources.Pipeline"/> via ICommandList.SetPipeline, plus the
    /// reflection queries shared recording code needs. Everything else (GL uniform setters,
    /// Vulkan modules/layouts) lives on the backend's concrete type.
    /// </summary>
    public interface IShaderPass
    {
        /// <summary>Whether this pass references the named resource (texture/buffer/uniform).</summary>
        bool HasGlobalUniform(GlobalUniformHandle globalId);
    }

    /// <summary>Backend-neutral compiled shader: the set of passes loaded from one shader json.</summary>
    internal interface IShader : IDisposable
    {
        IShaderPass ForwardPass { get; }
        /// <summary>Dedicated depth/shadow-pass variant, compiled when the fragment shader contains
        /// <c>#ifdef DEPTH_PASS</c>; null when the shader has no depth variant (callers fall back to
        /// <see cref="ForwardPass"/>).</summary>
        IShaderPass? DepthPass { get; }
        IShaderPass? ShadowPass { get; }
        string ShaderFile { get; }
        void Recompile();
        IReadOnlyDictionary<string, ShaderVariableType> Uniforms { get; }
    }

    internal class ShaderData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ShaderInputType
        {
            Float,
            Float2,
            Float3,
            Float4
        }

        public class ShaderInput
        {
            public ShaderInputType Type { get; set; }
            public string Semantic { get; set; }
        }

        public class PixelVertexData
        {
            public string Path { get; set; }
            public string Entry { get; set; }
            public List<ShaderInput> Input { get; set; }
        }

        public class GeometryShaderData
        {
            public string Path { get; set; }
        }

        public PixelVertexData Pixel { get; set; }
        public PixelVertexData Vertex { get; set; }
        public GeometryShaderData? Geometry { get; set; }
        public int Textures { get; set; }

        public bool ZWrite { get; set; }

        public bool WriteMask { get; set; }
    }
}
