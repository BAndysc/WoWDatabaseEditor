using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TheMaths;

namespace TheEngine.Data
{
    public class ObjParser
    {
        public static ObjModel LoadObj(string path)
        {
            var allLines = File.ReadAllLines(path);

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<ObjModel.Face> faces = new List<ObjModel.Face>();

            foreach (var line in allLines)
            {

                var splitted = line.Split(' ');

                if (line.StartsWith("v "))
                {
                    var x = float.Parse(splitted[1], CultureInfo.InvariantCulture);
                    var y = float.Parse(splitted[2], CultureInfo.InvariantCulture);
                    var z = float.Parse(splitted[3], CultureInfo.InvariantCulture);
                    vertices.Add(new Vector3(x, y, z));
                }
                else if (line.StartsWith("vt "))
                {
                    var u = float.Parse(splitted[1], CultureInfo.InvariantCulture);
                    var v = float.Parse(splitted[2], CultureInfo.InvariantCulture);
                    uv.Add(new Vector2(u, v));
                }
                else if (line.StartsWith("vn "))
                {
                    var x = float.Parse(splitted[1], CultureInfo.InvariantCulture);
                    var y = float.Parse(splitted[2], CultureInfo.InvariantCulture);
                    var z = float.Parse(splitted[3], CultureInfo.InvariantCulture);
                    normals.Add(new Vector3(x, y, z));
                }
                else if (line.StartsWith("f "))
                {
                    faces.Add(new ObjModel.Face(ParseFace(splitted[1]), ParseFace(splitted[2]), ParseFace(splitted[3])));
                }

            }

            return new ObjModel(vertices, normals, uv, faces);
        }

        private static ObjModel.Node ParseFace(string face)
        {
            var splitted = face.Split('/');

            int vert = int.Parse(splitted[0]);
            int uv = int.Parse(splitted[1]);
            int norm = int.Parse(splitted[2]);

            return new ObjModel.Node(vert, norm, uv);
        }
    }
}
