using System;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
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

    public class RaycastSystem
    {
        private Archetype colliders;
        private MeshManager meshManager;

        public RaycastSystem(Engine engine)
        {
            meshManager = engine.meshManager;
            colliders = engine.EntityManager.NewArchetype()
                .WithComponentData<Collider>()
                .WithComponentData<RenderEnabledBit>()
                .WithComponentData<MeshRenderer>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>();
        }

        public Entity? Raycast(Ray ray)
        {
            ThreadLocal<Entity> localEntities = new ThreadLocal<Entity>(true);
            colliders.ParallelForEach<Collider, WorldMeshBounds, RenderEnabledBit, MeshRenderer, LocalToWorld>((itr, start, end, colliders, meshBounds, doRender, renderer, localToWorld) =>
            {
                for (int i = start; i < end; ++i)
                {
                    if (!doRender[i])
                        continue;
                    var bounds = meshBounds[i].box;
                    /*
                     *   a ---- b
                     *   | \    | \
                     *   c------d  \       ^
                     *   \   e--\---f     /
                     *    \ /    \ /     z
                     *     g-----h/     
                     *                
                     *        x ->
                     */
                    var Minimum = bounds.Minimum;
                    var Maximum = bounds.Maximum;
                    var a = new Vector3(Minimum.X, Maximum.Y, Maximum.Z);
                    var b = new Vector3(Maximum.X, Maximum.Y, Maximum.Z);
                    var c = new Vector3(Minimum.X, Maximum.Y, Minimum.Z);
                    var d = new Vector3(Maximum.X, Maximum.Y, Minimum.Z);
                    
                    var e = new Vector3(Minimum.X, Minimum.Y, Maximum.Z);
                    var f = new Vector3(Maximum.X, Minimum.Y, Maximum.Z);
                    var g = new Vector3(Minimum.X, Minimum.Y, Minimum.Z);
                    var h = new Vector3(Maximum.X, Minimum.Y, Minimum.Z);
                    bool intersects = Physics.RayIntersectsQuad(in ray, in a, in b, in d, in c, out var inter) ||
                                      Physics.RayIntersectsQuad(in ray, in e, in f, in h, in g, out inter) ||
                                      Physics.RayIntersectsQuad(in ray, in d, in b, in f, in h, out inter) ||
                                      Physics.RayIntersectsQuad(in ray, in c, in a, in e, in g, out inter) ||
                                      Physics.RayIntersectsQuad(in ray, in a, in b, in f, in e, out inter) ||
                                      Physics.RayIntersectsQuad(in ray, in c, in d, in h, in g, out inter);
                    if (intersects)
                    {
                        var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                        if (Physics.RayIntersectsObject(mesh, localToWorld[i], ray, out inter))
                            localEntities.Value = itr[i];
                    }
                }
            });
            return localEntities.Values.FirstOrDefault();
        }
    }
    
    public static class Physics
    {
        public static bool RayIntersectsObject(IMesh mesh, Matrix localToWorld, Ray ray, out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = Vector3.Zero;
            bool intersects = false;
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

                    if (RayIntersectsTriangle(in ray, v1_w.XYZ, v2_w.XYZ, v3_w.XYZ, out var inte))
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

        public static bool RayIntersectsQuad(in Ray ray, in Vector3 v0, in Vector3 v1, in Vector3 v2, in Vector3 v3,
            out Vector3 outIntersectionPoint)
        {
            if (RayIntersectsTriangle(in ray, in v0, in v1, in v2, out outIntersectionPoint))
                return true;
            return RayIntersectsTriangle(in ray, in v0, in v2, in v3, out outIntersectionPoint);
        }
        
        public static bool RayIntersectsTriangle(in Ray ray,  
            in Vector3 vertex0, 
            in Vector3 vertex1,
            in Vector3 vertex2,
            out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = default;
            const float EPSILON = 0.0000001f;
            Vector3 edge1, edge2, h, s, q;
            float a,f,u,v;
            edge1 = vertex1 - vertex0;
            edge2 = vertex2 - vertex0;
            h = Vector3.Cross(ray.Direction, edge2);
            a = Vector3.Dot(edge1, h);
            if (a > -EPSILON && a < EPSILON)
                return false;    // This ray is parallel to this triangle.
            f = 1.0f/a;
            s = ray.Position - vertex0;
            u = f * Vector3.Dot(s, h);
            if (u < 0.0 || u > 1.0)
                return false;
            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(ray.Direction, q);
            if (v < 0.0 || u + v > 1.0)
                return false;
            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = f * Vector3.Dot(edge2, q);
            if (t > EPSILON) // ray intersection
            {
                outIntersectionPoint = ray.Position + ray.Direction * t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
                return false;
        }
    }
}