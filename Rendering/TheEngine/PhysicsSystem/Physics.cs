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
        private readonly Engine engine;
        private Archetype colliders;
        private MeshManager meshManager;

        public RaycastSystem(Engine engine)
        {
            this.engine = engine;
            meshManager = engine.meshManager;
            colliders = engine.EntityManager.NewArchetype()
                .WithComponentData<Collider>()
                //.WithComponentData<RenderEnabledBit>()
                .WithComponentData<MeshRenderer>()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<WorldMeshBounds>();
        }

        public List<(Entity, Vector3)>? RaycastAll(Ray ray, Vector3? customOrigin, uint collisionMask = 0)
        {
            List<(Entity, Vector3)> destinationList = new();
            RaycastAll(ray, customOrigin, destinationList, collisionMask);
            if (destinationList.Count == 0)
                return null;
            return destinationList;
        }
        
        public void RaycastAll(Ray ray, Vector3? customOrigin, List<(Entity, Vector3)> destinationList, uint collisionMask = 0)
        {
            ThreadLocal<List<(Entity, Vector3)>?> localEntities = new ThreadLocal<List<(Entity, Vector3)>?>(true);
            colliders.ParallelForEachRRRRO<Collider, WorldMeshBounds, MeshRenderer, LocalToWorld, DisabledObjectBit>((itr, start, end, colliders, meshBounds, renderer, localToWorld, disableAccess) =>
            {
                List<(Entity, Vector3)>? result = null;
                for (int i = start; i < end; ++i)
                {
                    if (collisionMask != 0 && (colliders[i].CollisionMask & collisionMask) == 0)
                        continue;
                    if (disableAccess != null && disableAccess.Value[i])
                        continue;
                    //if (!doRender[i])
                    //    continue;
                    var intersects = IntersectsBoundingBox(in ray, ref meshBounds[i]);
                    if (intersects)
                    {
                        var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                        if (Physics.RayIntersectsObject(mesh, renderer[i].SubMeshId, localToWorld[i], ray, customOrigin, out var inter))
                        {
                            result ??= new();
                            result.Add((itr[i], inter));
                        }
                    }
                }

                if (result != null)
                {
                    if (localEntities.IsValueCreated && localEntities.Value != null)
                        localEntities.Value.AddRange(result);
                    else
                        localEntities.Value = result;
                }
            });

            if (localEntities.Values.Count == 0)
                return;

            for (int i = 0; i < localEntities.Values.Count; ++i)
            {
                if (localEntities.Values[i] != null)
                    destinationList.AddRange(localEntities.Values[i]);
            }
        }
        
        public (Entity, Vector3)? RaycastMouse(uint collisionMask = 0)
        {
            var camera = engine.CameraManager;
            var ray = camera.MainCamera.NormalizedScreenPointToRay(engine.InputManager.Mouse.NormalizedPosition);
            return Raycast(ray, null, true, collisionMask);
        }
        
        public (Entity, Vector3)? Raycast(Ray ray, Vector3? customOrigin, bool onlyRendered = false, uint collisionMask = 0)
        {
            ThreadLocal<(Entity, float, Vector3)> localEntities = new ThreadLocal<(Entity, float, Vector3)>(true);
            colliders.ParallelForEachRRRROO<Collider, WorldMeshBounds, MeshRenderer, LocalToWorld, RenderEnabledBit, DisabledObjectBit>((itr, start, end, colliders, meshBounds, renderer, localToWorld, renderEnabledAccess, disabledAccess) =>
            {
                Entity? touchEntity = null;
                float minDist = float.MaxValue;
                Vector3 intersectionPoint = default;
                for (int i = start; i < end; ++i)
                {
                    if (collisionMask != 0 && (colliders[i].CollisionMask & collisionMask) == 0)
                        continue;
                    if (onlyRendered && renderEnabledAccess != null && !renderEnabledAccess.Value[i])
                        continue;
                    if (disabledAccess.HasValue && disabledAccess.Value[i])
                        continue;
                    var intersects = IntersectsBoundingBox(in ray, ref meshBounds[i]);
                    if (intersects)
                    {
                        var mesh = meshManager.GetMeshByHandle(renderer[i].MeshHandle);
                        if (Physics.RayIntersectsObject(mesh, renderer[i].SubMeshId, localToWorld[i], ray, customOrigin, out var inter))
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

                if (touchEntity.HasValue && (!localEntities.IsValueCreated || localEntities.Value.Item2 > minDist))
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
                        var v1_w = Vector4.Transform(v1, l2w);
                        var v2_w = Vector4.Transform(v2, l2w);
                        var v3_w = Vector4.Transform(v3, l2w);

                        vertices.AppendLine($"v {v1_w.X} {v1_w.Y} {v1_w.Z}");
                        vertices.AppendLine($"v {v2_w.X} {v2_w.Y} {v2_w.Z}");
                        vertices.AppendLine($"v {v3_w.X} {v3_w.Y} {v3_w.Z}");
                        indices.AppendLine($"f {index}// {index + 1}// {index + 2}//");
                        index += 3;
                    }
                }
            });
            File.WriteAllText("~/mesh.obj", vertices.ToString() + "\n" + indices.ToString());
        }
    }
    
    public static class Physics
    {
        public static bool RayIntersectsObject(IMesh mesh, int submeshId, Matrix localToWorld, Ray ray, Vector3? compareOrigin, out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = Vector3.Zero;
            bool intersects = false;
            float minSqDist = Single.MaxValue;
            var origin = compareOrigin ?? ray.Position;
            foreach (var triangle in mesh.GetFaces(submeshId))
            {
                var v1 = triangle.Item1;
                var v2 = triangle.Item2;
                var v3 = triangle.Item3;

                var v1_w = Vector4.Transform(v1, localToWorld);
                var v2_w = Vector4.Transform(v2, localToWorld);
                var v3_w = Vector4.Transform(v3, localToWorld);

                if (RayIntersectsTriangleOnlyFront(in ray, v1_w.XYZ(), v2_w.XYZ(), v3_w.XYZ(), out var inte))
                {
                    if (!intersects)
                    {
                        outIntersectionPoint = inte;
                        minSqDist =  (origin - inte).LengthSquared();
                        intersects = true;
                    }
                    else
                    {
                        var distA = (origin - inte).LengthSquared();
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
            Vector3 edge1, edge2, pvec, tVec, qVec;
            float det,invDet,u,v;
            edge1 = vertex1 - vertex0;
            edge2 = vertex2 - vertex0;
            pvec = Vector3.Cross(ray.Direction, edge2);
            det = Vector3.Dot(edge1, pvec);
            if (det > -EPSILON && det < EPSILON)
                return false;    // This ray is parallel to this triangle.
            invDet = 1.0f/det;
            tVec = ray.Position - vertex0;
            u = invDet * Vector3.Dot(tVec, pvec);
            if (u < 0.0 || u > 1.0)
                return false;
            qVec = Vector3.Cross(tVec, edge1);
            v = invDet * Vector3.Dot(ray.Direction, qVec);
            if (v < 0.0 || u + v > 1.0)
                return false;
            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = invDet * Vector3.Dot(edge2, qVec);
            if (t > EPSILON) // ray intersection
            {
                outIntersectionPoint = ray.Position + ray.Direction * t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
                return false;
        }
        
        public static bool RayIntersectsTriangleOnlyFront(in Ray ray,  
            in Vector3 vertex0, 
            in Vector3 vertex1, // change vertex order to get back face culling
            in Vector3 vertex2,
            out Vector3 outIntersectionPoint)
        {
            outIntersectionPoint = default;
            const float EPSILON = 0.0000001f;
            Vector3 edge1, edge2, pvec, tvec, qVec;
            float det,u,v;
            edge1 = vertex1 - vertex0;
            edge2 = vertex2 - vertex0;
            pvec = Vector3.Cross(ray.Direction, edge2);
            det = Vector3.Dot(edge1, pvec);
            if (det < EPSILON)
                return false;    // This ray is parallel to this triangle.
            tvec = ray.Position - vertex0;
            u = Vector3.Dot(tvec, pvec);
            if (u < 0.0 || u > det)
                return false;
            qVec = Vector3.Cross(tvec, edge1);
            v = Vector3.Dot(ray.Direction, qVec);
            if (v < 0.0 || u + v > det)
                return false;
            // At this stage we can compute t to find out where the intersection point is on the line.
            float t = Vector3.Dot(edge2, qVec);
            float invDet = 1.0f / det;
            t *= invDet;
            u *= invDet;
            v *= invDet;
            //outIntersectionPoint = ray.Position + ray.Direction * t;
            //return true;
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