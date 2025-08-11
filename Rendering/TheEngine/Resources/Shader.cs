using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenGLBindings;
using TheEngine.Entities;

namespace TheAvaloniaOpenGL.Resources
{
    internal enum ShaderVariableType
    {
        Int,
        Float,
        Float2,
        Float3,
        Float4,
        Sampler2D,
        SamplerBuffer,
        SamplerBufferArray,
        IntSamplerBuffer,
        UIntSamplerBuffer,
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
            if (variableType.Equals("samplerBuffer", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.SamplerBuffer;
            if (variableType.Equals("sampler2DArray", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.SamplerBufferArray;
            if (variableType.Equals("isamplerBuffer", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.IntSamplerBuffer;
            if (variableType.Equals("usamplerBuffer", StringComparison.InvariantCultureIgnoreCase))
                return ShaderVariableType.UIntSamplerBuffer;
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

    public class ShaderPass : IDisposable
    {
        private readonly IDevice device;
        private readonly string shaderFile;
        internal int VertexShader { get; }
        internal int PixelShader { get; }
        internal int GeometryShader { get; }
        internal int ProgramHandle { get; }

        public bool Instancing { get; }

        public bool ZWrite { get; }

        public DepthFunction DepthTest { get; }

        public bool WriteMask { get; }

        private Dictionary<GlobalUniformHandle, float> uniformFloatValues = new();
        private Dictionary<GlobalUniformHandle, Vector4> uniformVectorValues = new();
        private Dictionary<GlobalUniformHandle, int> uniformIntValues = new();
        private Dictionary<string, int> uniformToLocation = new();

        private HashSet<string> unusedUniforms;
        private Dictionary<string, ShaderVariableType> uniformTypes;
        private Dictionary<int, ShaderVariableType> uniformTypesByLocation;

        internal string ShaderPassDefine;
        internal string ShaderName;

        internal ShaderPass(IDevice device, bool instanced, ShaderData shaderData, string shaderFile, string[] includePaths, string shaderPassDefine)
        {
            this.device = device;
            this.shaderFile = shaderFile;
            ZWrite = shaderData.ZWrite;
            DepthTest = shaderData.DepthTest ?? DepthFunction.Lequal;
            Instancing = shaderData.Instancing && instanced;
            var defines = new List<string>() { "VERTEX_SHADER", shaderPassDefine };
            if (Instancing)
            {
                defines.Add("Instancing");
            }

            ShaderName = Path.GetFileNameWithoutExtension(shaderFile);
            ShaderPassDefine = shaderPassDefine;

            VertexShader = device.CreateShader(ShaderType.VertexShader);
            var vertexSource = ShaderSource.ParseShader(shaderData.Vertex.Path, true, defines.ToArray());
            var vertexUniforms = ShaderSource.ParseUniforms(vertexSource);
            var result = device.CompileShaderAndGetError(VertexShader, vertexSource);
            if (!string.IsNullOrEmpty(result))
            {
                int o = 0;
                foreach (var line in vertexSource.Split('\n'))
                {
                    o++;
                    Console.WriteLine($"{o.ToString():-5}: {line}");
                }
                Console.WriteLine("Error while compiling " + shaderData.Vertex.Path);
                Console.WriteLine(result);
            }

            PixelShader = device.CreateShader(ShaderType.FragmentShader);
            defines = new List<string>() { "PIXEL_SHADER", shaderPassDefine };
            if (Instancing)
            {
                defines.Add("Instancing");
            }
            var fragmentSource = ShaderSource.ParseShader(shaderData.Pixel.Path, true, defines.ToArray());
            var fragmentUniforms = ShaderSource.ParseUniforms(fragmentSource);
            result = device.CompileShaderAndGetError(PixelShader, fragmentSource);
            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine("Error while compiling " + shaderData.Pixel.Path);
                Console.WriteLine(result);
            }

            if (shaderData.Geometry != null)
            {
                GeometryShader = device.CreateShader(ShaderType.GeometryShader);
                defines = new List<string>() { "GEOMETRY_SHADER", shaderPassDefine };
                var geometrySource = ShaderSource.ParseShader(shaderData.Geometry.Path, true, defines.ToArray());
                result = device.CompileShaderAndGetError(GeometryShader, geometrySource);
                if (!string.IsNullOrEmpty(result))
                {
                    Console.WriteLine("Error while compiling " + shaderData.Geometry.Path);
                    Console.WriteLine(result);
                }
            }

            ProgramHandle = device.CreateProgram();

            device.AttachShader(ProgramHandle, VertexShader);
            device.AttachShader(ProgramHandle, PixelShader);
            if (GeometryShader > 0)
                device.AttachShader(ProgramHandle, GeometryShader);

            var error = device.LinkProgramAndGetError(ProgramHandle);
            if (error != null && error.Trim().Length > 0)
            {
                throw new Exception(error);
            }
            device.UseProgram(ProgramHandle);

            var idx = device.GetUniformBlockIndex(ProgramHandle, "SceneData");
            if (idx != -1)
                device.UniformBlockBinding(ProgramHandle, idx, Constants.SCENE_BUFFER_INDEX);
            idx = device.GetUniformBlockIndex(ProgramHandle, "ObjectData");
            if (idx != -1)
                device.UniformBlockBinding(ProgramHandle, idx, Constants.OBJECT_BUFFER_INDEX);
            idx = device.GetUniformBlockIndex(ProgramHandle, "PixelData");
            if (idx != -1)
                device.UniformBlockBinding(ProgramHandle, idx, Constants.PIXEL_SCENE_BUFFER_INDEX);

            int count = device.GetProgramParameter(ProgramHandle, GetProgramParameterName.ActiveUniforms);
            for (int i = 0; i < count; i++)
            {
                var uniformName = device.GetActiveUniform(ProgramHandle, i, 256, out _, out _, out var type);
                int location = device.GetUniformLocation(ProgramHandle, uniformName);
                uniformToLocation[uniformName] = location;
            }

            var uniforms = vertexUniforms.Concat(fragmentUniforms).DistinctBy(x => x.name).ToList();
            uniformTypes = uniforms
                .Where(x => uniformToLocation.ContainsKey(x.name))
                .ToDictionary(x => x.name, x => x.type);
            uniformTypesByLocation = uniformTypes.ToDictionary(x => uniformToLocation[x.Key], x => x.Value);
            unusedUniforms = uniforms.Select(x => x.name).Where(x => !uniformToLocation.ContainsKey(x)).ToHashSet();

            foreach (var (uniformName, uniformLocation) in uniformToLocation)
            {
                var globalLocation = Material.GetUniformLocation(uniformName);
                if (globalToLocal == null || globalToLocal.Length <= globalLocation.Handle)
                    Array.Resize(ref globalToLocal, globalLocation.Handle + 1);
                globalToLocal[globalLocation.Handle] = uniformLocation + 1;
            }
        }

        // public int? GetUniformLocation(string name)
        // {
        //     if (uniformToLocation.TryGetValue(name, out var loc))
        //         return loc;
        //     if (unusedUniforms.Contains(name))
        //         return -1;
        //     return null;
        // }
        //
        // public string? GetUniformName(int location)
        // {
        //     return uniformToLocation.FirstOrDefault(p => p.Value == location).Key;
        // }

        public void Validate()
        {
            device.ValidateProgram(ProgramHandle);
            int ret = device.GetProgramParameter(ProgramHandle, GetProgramParameterName.ValidateStatus);
            if (ret == 0)
            {
                var problem = "In shader: " + shaderFile + "\n\n" + device.GetProgramInfoLog(ProgramHandle);
                Console.WriteLine(problem);
                throw new Exception(problem);
            }
        }

        public void Activate()
        {
            device.UseProgram(ProgramHandle);
        }

        public void Dispose()
        {
            device.DeleteProgram(ProgramHandle);
            //PixelShader.Dispose();
            //VertexShader.Dispose();
            //ShaderInputLayout.Dispose();
        }

        public void SetUniform(GlobalUniformHandle loc, float f)
        {
            if (!uniformFloatValues.TryGetValue(loc, out var curVal) || Math.Abs(curVal - f) > float.Epsilon)
            {
                device.Uniform1f(GetUniformLocationFromGlobal(loc), f);
                uniformFloatValues[loc] = f;
            }
        }

        public void SetUniform(GlobalUniformHandle loc, Matrix m)
        {
            device.UniformMatrix4f(GetUniformLocationFromGlobal(loc), ref m, false);
        }

        public void SetUniformInt(GlobalUniformHandle loc, int val)
        {
            if (!uniformIntValues.TryGetValue(loc, out var curVal) || curVal != val)
            {
                device.Uniform1I(GetUniformLocationFromGlobal(loc), val);
                uniformIntValues[loc] = val;
            }
        }

        public void SetUniform(GlobalUniformHandle loc, float x, float y, float z)
        {
            if (!uniformVectorValues.TryGetValue(loc, out var curVal) ||
                Math.Abs(curVal.X - x) > float.Epsilon ||
                Math.Abs(curVal.Y - y) > float.Epsilon ||
                Math.Abs(curVal.Z - z) > float.Epsilon)
            {
                device.Uniform3f(GetUniformLocationFromGlobal(loc), x, y, z);
                uniformVectorValues[loc] = new Vector4(x, y, z, 0);
            }
        }

        public void SetUniform(GlobalUniformHandle loc, float x, float y, float z, float w)
        {
            if (!uniformVectorValues.TryGetValue(loc, out var curVal) ||
                Math.Abs(curVal.X - x) > float.Epsilon ||
                Math.Abs(curVal.Y - y) > float.Epsilon ||
                Math.Abs(curVal.Z - z) > float.Epsilon ||
                Math.Abs(curVal.W - w) > float.Epsilon)
            {
                device.Uniform4f(GetUniformLocationFromGlobal(loc), x, y, z, w);
                uniformVectorValues[loc] = new Vector4(x, y, z, w);
            }
        }

        internal IReadOnlyDictionary<string, ShaderVariableType> Uniforms => uniformTypes;
        internal IReadOnlyDictionary<int, ShaderVariableType> UniformsByLocation => uniformTypesByLocation;

        public int GetUniformLocationFromGlobal(GlobalUniformHandle uniformGlobalLocation)
        {
            if (globalToLocal == null || globalToLocal.Length <= uniformGlobalLocation.Handle)
                throw new Exception("Uniform location " + uniformGlobalLocation + " is not valid in shader " + shaderFile);
            return globalToLocal[uniformGlobalLocation.Handle] - 1;
        }

        private int[] globalToLocal;

        public bool HasGlobalUniform(GlobalUniformHandle globalId)
        {
            if (globalToLocal == null || globalToLocal.Length <= globalId.Handle)
            {
                return false;
            }

            if (globalToLocal[globalId.Handle] != 0)
            {
                return true;
            }

            return false;
        }
    }

    public class Shader : IDisposable
    {
        private ShaderData shaderData;
        private readonly IDevice device;
        private readonly string shaderFile;

        private ShaderPass forwardPass;
        //private ShaderPass depthPass;
        private ShaderPass? shadowPass;
        private ShaderPass? forwardInstancedPass;
        //private ShaderPass? depthInstancedPass;
        private ShaderPass? shadowInstancedPass;

        public ShaderPass ForwardPass => forwardPass;
        //public ShaderPass DepthPass => depthPass;
        public ShaderPass? ShadowPass => shadowPass;

        public ShaderPass? ForwardInstancedPass => forwardInstancedPass;
        //public ShaderPass? DepthInstancedPass => depthInstancedPass;
        public ShaderPass? ShadowInstancedPass => shadowInstancedPass;

        public string ShaderFile => shaderFile;

        private Dictionary<string, ShaderVariableType> uniformTypes;

        /*public class ShaderInclude : Include
        {
            private readonly string[] incPaths;

            public IDisposable Shadow { get; set; }

            public ShaderInclude(string[] incPaths)
            {
                this.incPaths = incPaths;
            }

            public void Close(Stream stream)
            {
                stream.Close();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Stream Open(IncludeType type, string fileName, Stream parentStream)
            {
                foreach (var dir in incPaths)
                {
                    if (File.Exists(dir + "/" + fileName))
                        return new FileStream(dir + "/" + fileName, FileMode.Open);
                }
                throw new Exception();
            }
        }
*/
        
        internal Shader(IDevice device, string shaderFile, string[] includePaths)
        {
            var shaderContent = File.ReadAllText(shaderFile);
            shaderData = JsonConvert.DeserializeObject<ShaderData>(shaderContent) ?? throw new Exception("Failed to deserialize shader data from " + shaderFile);
            var pixelShader = File.ReadAllText(shaderData.Pixel.Path);

            forwardPass = new ShaderPass(device, false, shaderData, shaderFile, includePaths, "FORWARD_PASS");
            forwardInstancedPass = new ShaderPass(device, true, shaderData, shaderFile, includePaths, "FORWARD_PASS");
            //depthPass = new ShaderPass(device, false, shaderData, shaderFile, includePaths, "DEPTH_PASS", "main");
            //depthInstancedPass = new ShaderPass(device, true, shaderData, shaderFile, includePaths, "DEPTH_PASS", "main");
            if (pixelShader.Contains("#ifdef SHADOW_PASS"))
            {
                shadowPass = new ShaderPass(device, false, shaderData, shaderFile, includePaths, "SHADOW_PASS");
                shadowInstancedPass = new ShaderPass(device, true, shaderData, shaderFile, includePaths, "SHADOW_PASS");

                if (!shadowInstancedPass.Instancing)
                    shadowInstancedPass = null;
            }

            if (!forwardInstancedPass.Instancing)
                forwardInstancedPass = null;
            
            /*var shaderInclude = new ShaderInclude(includePaths);

            var vertexMacros = new List<ShaderMacro> { new ShaderMacro("VERTEX_SHADER", 1) };
            var pixelMacros = new List<ShaderMacro> { new ShaderMacro("PIXEL_SHADER", 1) };

            WriteMask = shaderData.WriteMask;
            if (Instancing)
            {
                vertexMacros.Add(new ShaderMacro("INSTANCING", 1));
                pixelMacros.Add(new ShaderMacro("INSTANCING", 1));
            }

            ShaderBytecode vertexShaderByteCode = ShaderBytecode.CompileFromFile(shaderDir + "/" + shaderData.Vertex.Path, shaderData.Vertex.Entry, "vs_5_0", ShaderFlags.None, EffectFlags.None, vertexMacros.ToArray(), shaderInclude);
            ShaderBytecode pixelShaderByteCode = ShaderBytecode.CompileFromFile(shaderDir + "/" + shaderData.Pixel.Path, shaderData.Pixel.Entry, "ps_5_0", ShaderFlags.None, EffectFlags.None, pixelMacros.ToArray(), shaderInclude);

            InputElement[] inputElements = LoadInputs(shaderData.Vertex.Input, shaderData.Instancing);
            ShaderInputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);

            VertexShader = new VertexShader(device, vertexShaderByteCode);
            PixelShader = new PixelShader(device, pixelShaderByteCode);


            vertexShaderByteCode.Dispose();
            pixelShaderByteCode.Dispose();*/
            this.device = device;
            this.shaderFile = shaderFile;

            var allPasses = new[] { ForwardPass, forwardInstancedPass, ShadowPass, shadowInstancedPass };
            uniformTypes = allPasses.Where(x => x != null).SelectMany(x => x.Uniforms)
                .DistinctBy(x => x.Key)
                .ToDictionary();
        }

        internal IReadOnlyDictionary<string, ShaderVariableType> Uniforms => uniformTypes;

        private static int TypeToSize(ShaderData.ShaderInputType type)
        {
            int size = 1;
            switch (type)
            {
                case ShaderData.ShaderInputType.Float:
                    size = 1;
                    break;
                case ShaderData.ShaderInputType.Float2:
                    size = 2;
                    break;
                case ShaderData.ShaderInputType.Float3:
                    size = 3;
                    break;
                case ShaderData.ShaderInputType.Float4:
                    size = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return size;
        }

        public void Dispose()
        {
            forwardPass.Dispose();
            shadowPass?.Dispose();
            forwardInstancedPass?.Dispose();
            shadowInstancedPass?.Dispose();
        }
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
        public bool Instancing { get; set; }

        public bool ZWrite { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DepthFunction? DepthTest { get; set; } = DepthFunction.Lequal;

        public bool WriteMask { get; set; }
    }
}
