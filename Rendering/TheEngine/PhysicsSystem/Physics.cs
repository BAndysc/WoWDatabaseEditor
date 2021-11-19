using System;
using System.Text;
using System.Threading;
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
                //.WithComponentData<RenderEnabledBit>()
                .WithComponentData<MeshRenderer>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>();
        }
        
        public List<(Entity, Vector3)>? RaycastAll(Ray ray)
        {
            ThreadLocal<List<(Entity, Vector3)>?> localEntities = new ThreadLocal<List<(Entity, Vector3)>?>(true);
            colliders.ParallelForEach<Collider, WorldMeshBounds, MeshRenderer, LocalToWorld>((itr, start, end, colliders, meshBounds, renderer, localToWorld) =>
            {
                List<(Entity, Vector3)>? result = null;

                for (int i = start; i < end; ++i)
                {
                    //if (!doRender[i])
                    //    continue;
                    var intersects = IntersectsBoundingBox(in ray, ref meshBounds[i]);
                    if (intersects)
                    {
                        var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                        if (Physics.RayIntersectsObject(mesh, renderer[i].SubMeshId, localToWorld[i], ray, out var inter))
                        {
                            result ??= new();
                            result.Add((itr[i], inter));
                        }
                    }
                }

                if (result != null)
                    localEntities.Value = result;
            });

            if (localEntities.Values.Count == 0)
                return null;

            var resultList = localEntities.Values.Where(p => p != null).SelectMany(p => p!).ToList();
            return resultList;
        }
        
        public (Entity, Vector3)? Raycast(Ray ray)
        {
            ThreadLocal<(Entity, float, Vector3)> localEntities = new ThreadLocal<(Entity, float, Vector3)>(true);
            colliders.ParallelForEach<Collider, WorldMeshBounds, MeshRenderer, LocalToWorld>((itr, start, end, colliders, meshBounds, renderer, localToWorld) =>
            {
                Entity? touchEntity = null;
                float minDist = float.MaxValue;
                Vector3 intersectionPoint = default;
                for (int i = start; i < end; ++i)
                {
                    //if (!doRender[i])
                    //    continue;
                    var intersects = IntersectsBoundingBox(in ray, ref meshBounds[i]);
                    if (intersects)
                    {
                        var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                        if (Physics.RayIntersectsObject(mesh, renderer[i].SubMeshId, localToWorld[i], ray, out var inter))
                        {
                            var dist = (ray.Position - inter).LengthSquared();
                            if (dist < minDist)
                            {
                                minDist = dist;
                                touchEntity = itr[i];
                                intersectionPoint = inter;
                            }
                        }
                    }
                }

                if (touchEntity.HasValue)
                {
                    localEntities.Value = (touchEntity.Value, minDist, intersectionPoint);
                }
            });

            if (localEntities.Values.Count == 0)
                return null;

            float minDist = float.MaxValue;
            Entity minEntity = default;
            Vector3 intersectionPoint = default;
            foreach (var pair in localEntities.Values)
            {
                if (pair.Item2 < minDist)
                {
                    minDist = pair.Item2;
                    intersectionPoint = pair.Item3;
                    minEntity = pair.Item1;
                }
            }

            return (minEntity, intersectionPoint);
        }

        private static bool IntersectsBoundingBox(in Ray ray, ref WorldMeshBounds worldMeshBounds)
        {
            ref var bounds = ref worldMeshBounds.box;
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
            var min = bounds.Minimum;
            var max = bounds.Maximum;
            var a = new Vector3(min.X, max.Y, max.Z);
            var b = new Vector3(max.X, max.Y, max.Z);
            var c = new Vector3(min.X, max.Y, min.Z);
            var d = new Vector3(max.X, max.Y, min.Z);

            var e = new Vector3(min.X, min.Y, max.Z);
            var f = new Vector3(max.X, min.Y, max.Z);
            var g = new Vector3(min.X, min.Y, min.Z);
            var h = new Vector3(max.X, min.Y, min.Z);
            bool intersects = Physics.RayIntersectsQuad(in ray, in a, in b, in d, in c, out var inter) ||
                              Physics.RayIntersectsQuad(in ray, in e, in f, in h, in g, out inter) ||
                              Physics.RayIntersectsQuad(in ray, in d, in b, in f, in h, out inter) ||
                              Physics.RayIntersectsQuad(in ray, in c, in a, in e, in g, out inter) ||
                              Physics.RayIntersectsQuad(in ray, in a, in b, in f, in e, out inter) ||
                              Physics.RayIntersectsQuad(in ray, in c, in d, in h, in g, out inter);
            return intersects;
        }

        public void DumpGeometry()
        {
            int index = 1;
            StringBuilder vertices = new();
            StringBuilder indices = new();
            colliders.ForEach<Collider, MeshRenderer, LocalToWorld>((itr, start, end, colliders, renderer, localToWorld) =>
            {
                for (int i = start; i < end; ++i)
                {
                    var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                    int submesh = renderer[i].SubMeshId;
                    var l2w = localToWorld[i].Matrix;

                    foreach (var face in mesh.GetFaces(submesh))
                    {
                        var v1 = face.Item1;
                        var v2 = face.Item2;
                        var v3 = face.Item3;
                        Vector4.Transform(ref v1, ref l2w, out var v1_w);
                        Vector4.Transform(ref v2, ref l2w, out var v2_w);
                        Vector4.Transform(ref v3, ref l2w, out var v3_w);

                        vertices.AppendLine($"v {v1_w.X} {v1_w.Y} {v1_w.Z}");
                        vertices.AppendLine($"v {v2_w.X} {v2_w.Y} {v2_w.Z}");
                        vertices.AppendLine($"v {v3_w.X} {v3_w.Y} {v3_w.Z}");
                        indices.AppendLine($"f {index}// {index + 1}// {index + 2}//");
                        index += 3;
                    }
                }
            });
            File.WriteAllText("/Users/bartek/mesh.obj", vertices.ToString() + "\n" + indices.ToString());
        }
    }
    
    public static class Physics
    {
        public static bool RayIntersectsObject(IMesh mesh, int submeshId, Matrix localToWorld, Ray ray, out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = Vector3.Zero;
            bool intersects = false;
            float minSqDist = Single.MaxValue;
            foreach (var triangle in mesh.GetFaces(submeshId))
            {
                var v1 = triangle.Item1;
                var v2 = triangle.Item2;
                var v3 = triangle.Item3;

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