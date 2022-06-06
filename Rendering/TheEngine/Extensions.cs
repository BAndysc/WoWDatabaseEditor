using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine
{
    public static class Extensions
    {
        public static Vector3 PointToScreen(this ICamera camera, Vector3 worldPoint)
        {
            var vPoint = Vector4.Transform(new Vector4(worldPoint, 1), camera.ViewMatrix);
            var point = Vector4.Transform(vPoint, camera.ProjectionMatrix);
            return new Vector3((point.X / point.W) / 2 + 0.5f, (point.Y / point.W) / 2 + 0.5f, point.Z / point.W);
        }
        
        public static Ray NormalizedScreenPointToRay(this ICamera camera, Vector2 normalized)
        {
            // with love to https://antongerdelan.net/opengl/raycasting.html
            //step 1
            float x = 2 * normalized.X - 1.0f;
            float y = 2 * normalized.Y - 1.0f;
            float z = 1.0f;
            Vector3 ray_nds = new Vector3(x, y, z);
                
            //step 2
            Vector4 ray_clip = new Vector4(ray_nds.X, ray_nds.Y, -1, 1.0f);
                
            //step 3
            var proj = camera.ProjectionMatrix;
            Matrix.Invert(proj, out proj);
            var ray_eye = Vector4.Transform(ray_clip, proj);
            ray_eye = new Vector4(ray_eye.X, ray_eye.Y, -1.0f, 0.0f);
                
            //step4
            var invVm = camera.InverseViewMatrix;
            var ray_wor4 = Vector4.Transform(ray_eye, invVm);
            Vector3 ray_wor = new Vector3(ray_wor4.X, ray_wor4.Y, ray_wor4.Z);
            // don't forget to normalise the vector at some point
            ray_wor = Vectors.Normalize(ray_wor);

            return new Ray(camera.Transform.Position, ray_wor);
        }
    }
}