using System;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.PhysicsSystem
{
    public struct MeshTransform
    {
        public Transform transform;
        public IMesh mesh;

        public MeshTransform(Transform transform, IMesh mesh)
        {
            this.transform = transform;
            this.mesh = mesh;
        }
    }
    
    public static class Physics
    {
        public static bool RayIntersectsObject(IMesh mesh, Transform transform, Ray ray, out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = Vector3.Zero;
            bool intersects = false;
            var localToWorld = transform.LocalToWorldMatrix;
            float minSqDist = Single.MaxValue;
            for (int i = 0; i < mesh.SubmeshCount; ++i)
            {
                foreach (var triangle in mesh.GetFaces(i))
                {
                    var v1 = triangle.Item1;
                    var v2 =  triangle.Item2;
                    var v3 =  triangle.Item3;

                    Vector4.Transform(ref v1, ref localToWorld, out var v1_w);
                    Vector4.Transform(ref v2, ref localToWorld, out var v2_w);
                    Vector4.Transform(ref v3, ref localToWorld, out var v3_w);

                    if (RayIntersectsTriangle(ray.Position, ray.Direction, v1_w.XYZ, v2_w.XYZ, v3_w.XYZ, out var inte))
                    {
                        if (!intersects)
                        {
                            outIntersectionPoint = inte;
                            minSqDist =  (ray.Position - inte).LengthSquared();
                            intersects = true;
                        }
                        else
                        {
                            var distA = (ray.Position - inte).LengthSquared();
                            if (distA < minSqDist)
                            {
                                minSqDist = distA;
                                outIntersectionPoint = inte;
                            }
                        }
                    }   
                }
            }
            return intersects;
        }
        
        public static bool RayIntersectsTriangle(Vector3 rayOrigin, 
            Vector3 rayVector,  
            Vector3 vertex0, 
            Vector3 vertex1,
            Vector3 vertex2,
            out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = default;
            const float EPSILON = 0.0000001f;
            Vector3 edge1, edge2, h, s, q;
            float a,f,u,v;
            edge1 = vertex1 - vertex0;
            edge2 = vertex2 - vertex0;
            h = Vector3.Cross(rayVector, edge2);
            a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON)
                return false;    // This ray is parallel to this triangle.
            f = 1.0f/a;
            s = rayOrigin - vertex0;
            u = f * Vector3.Dot(s, h);
            if (u < 0.0 || u > 1.0)
                return false;
            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(rayVector, q);
            if (v < 0.0 || u + v > 1.0)
                return false;
            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
            {
                outIntersectionPoint = rayOrigin + rayVector * t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
                return false;
        }
    }
}