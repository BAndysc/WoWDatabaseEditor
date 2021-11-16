using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenGLBindings;
using TheMaths;

namespace TheAvaloniaOpenGL.Resources
{
    internal class ShaderSource
    {
        private const string IncludeMacro = "#include ";
        
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
    
    public class Shader : IDisposable
    {
        private readonly IDevice device;

        internal int VertexShader { get; }
        internal int PixelShader { get; }
        internal int ProgramHandle { get; }
        
        public bool Instancing { get; }

        public bool ZWrite { get; }
        
        public bool DepthTest { get; }

        public bool WriteMask { get; }

        private Dictionary<int, float> uniformFloatValues = new();
        private Dictionary<int, Vector4> uniformVectorValues = new();
        private Dictionary<int, int> uniformIntValues = new();
        private Dictionary<string, int> uniformToLocation = new();

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
            var shaderData = JsonConvert.DeserializeObject<ShaderData>(shaderContent);

            var shaderDir = Path.GetDirectoryName(shaderFile);

            ZWrite = shaderData.ZWrite;
            DepthTest = shaderData.DepthTest ?? true;
            Instancing = shaderData.Instancing;
            var defines = new List<string>() { "VERTEX_SHADER" };
            if (Instancing)
                defines.Add("Instancing");

            

            VertexShader = device.CreateShader(ShaderType.VertexShader);
            var vertexSource = ShaderSource.ParseShader(shaderData.Vertex.Path, true, defines.ToArray());
            var result = device.CompileShaderAndGetError(VertexShader, vertexSource);
            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine("Error while compiling " + shaderData.Vertex.Path);
                Console.WriteLine(result);
            }
            
            PixelShader = device.CreateShader(ShaderType.FragmentShader);
            defines = new List<string>() { "PIXEL_SHADER" };
            var fragmentSource = ShaderSource.ParseShader(shaderData.Pixel.Path, true, defines.ToArray());
            result = device.CompileShaderAndGetError(PixelShader, fragmentSource);
            if (!string.IsNullOrEmpty(result))
            {
                Console.WriteLine("Error while compiling " + shaderData.Pixel.Path);
                Console.WriteLine(result);
            }

            ProgramHandle = device.CreateProgram();

            device.AttachShader(ProgramHandle, VertexShader);
            device.AttachShader(ProgramHandle, PixelShader);

            var error = device.LinkProgramAndGetError(ProgramHandle);
            if (error != null && error.Trim().Length > 0)
            {
                Console.WriteLine("Error, EXITING: '" + error + "'");
                Environment.Exit(2);
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
        }

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

        public int GetUniformLocation(string name)
        {
            if (uniformToLocation.TryGetValue(name, out var loc))
                return loc;
            return -1;
        }

        public void Validate()
        {
            device.ValidateProgram(ProgramHandle);
            int ret = device.GetProgramParameter(ProgramHandle, GetProgramParameterName.ValidateStatus);
            if (ret == 0)
            {
                var problem = device.GetProgramInfoLog(ProgramHandle);
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
            //PixelShader.Dispose();
            //VertexShader.Dispose();
            //ShaderInputLayout.Dispose();
        }

        public void SetUniform(int loc, float f)
        {
            if (!uniformFloatValues.TryGetValue(loc, out var curVal) || Math.Abs(curVal - f) > float.Epsilon)
            {
                device.Uniform1f(loc, f);
                uniformFloatValues[loc] = f;
            }
        }
        
        public void SetUniformInt(int loc, int val)
        {
            if (!uniformIntValues.TryGetValue(loc, out var curVal) || curVal != val)
            {
                device.Uniform1I(loc, val);
                uniformIntValues[loc] = val;
            }
        }

        public void SetUniform(int loc, float x, float y, float z, float w)
        {
            if (!uniformVectorValues.TryGetValue(loc, out var curVal) || 
                Math.Abs(curVal.X - x) > float.Epsilon ||
                Math.Abs(curVal.Y - y) > float.Epsilon ||
                Math.Abs(curVal.Z - z) > float.Epsilon ||
                Math.Abs(curVal.W - w) > float.Epsilon)
            {
                device.Uniform4f(loc, x, y, z, w);
                uniformVectorValues[loc] = new Vector4(x, y, z, w);
            }
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

        public PixelVertexData Pixel { get; set; }
        public PixelVertexData Vertex { get; set; }
        public int Textures { get; set; }
        public bool Instancing { get; set; }

        public bool ZWrite { get; set; }
        public bool? DepthTest { get; set; } = true;

        public bool WriteMask { get; set; }
    }
}
