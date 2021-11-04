using TheEngine.Interfaces;
using TheMaths;

namespace TheEngine.Managers
{
	using System;
	public class BoundingFrustum
	{
		#region Public fields 

		/// <summary>
		/// The number of planes in the frustum.
		/// </summary>
		public const int PlaneCount = 6;

		/// <summary>
		/// The number of corner points in the frustum.
		/// </summary>
		public const int CornerCount = 8;

		/// <summary>
		/// Returns the current position of the frustum
		/// </summary>
		public Vector3 Position { get; private set; }

		#endregion 

		#region Private variables

		/// <summary>
		/// Ordering: [0] = Far Bottom Left, [1] = Far Top Left, [2] = Far Top Right, [3] = Far Bottom Right, 
		/// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right
		/// </summary>
		private Vector3[] _corners = new Vector3[ CornerCount ];

		/// <summary>
		/// Defines the set of planes that bound the camera's frustum. All plane normals point to the inside of the 
		/// frustum.
		/// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
		/// </summary>
		private Plane[] _planes = new Plane[ PlaneCount ];

		/// <summary>
		/// Caches the absolute values of plane normals for re-use during frustum culling of multiple AABB instances
		/// </summary>
		private Vector3[] _absNormals = new Vector3[ PlaneCount ];

		/// <summary>
		/// Caching the plane normals allows the culling code to avoid calling property getters on the Plane instances
		/// </summary>
		private Vector3[] _planeNormal = new Vector3[ PlaneCount ];

		/// <summary>
		/// Caching the plane distances allows the culling code to avoid calling property getters on the Plane instances
		/// </summary>
		private float[] _planeDistance = new float[ PlaneCount ];

		#endregion 

		#region Public functions

		/// <summary>
		/// Extracts the frustum corners. The destination array must contain space for no less than CornerCount elements.
		/// Ordering: [0] = Far Bottom Left, [1] = Far Top Left, [2] = Far Top Right, [3] = Far Bottom Right, [4] = Camera Position
		/// </summary>
		public void GetCorners( Vector3[] outCorners )
		{
			if( outCorners == null || outCorners.Length < CornerCount )
			{
				throw new InvalidOperationException( "Destination array is null or too small" );
			}

			Array.Copy( _corners, outCorners, CornerCount );
		}

		/// <summary>
		/// Extracts the frustum planes. The destination array must contain space for no less than PlaneCount elements.
		/// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
		/// </summary>
		public void GetPlanes( Plane[] outPlanes )
		{
			if( outPlanes == null || outPlanes.Length < PlaneCount )
			{
				throw new InvalidOperationException( "Destination array is null or too small" );
			}

			Array.Copy( _planes, outPlanes, PlaneCount );
		}

		/// <summary>
		/// Update the bounding frustum from the current camera settings
		/// </summary>
		public void Update(ICamera camera )
		{
			Update( camera, camera.FarClip );
		}

		/// <summary>
		/// Update the bounding frustum from the current camera settings
		/// </summary>
		public void Update(ICamera camera, float farClipPlane )
		{
			var camTransform = camera.Transform;
			var position = camTransform.Position;
			var orientation = camTransform.Rotation;

			this.Position = position;
			var forward = Vector3.ForwardLH * orientation;

			//if( camera.orthographic )
			//{
			//	calculateFrustumCornersOrthographic( camera );
			//}
			//else
			{
				calculateFrustumCornersPerspective(
					ref position,
					ref orientation,
					camera.FOV,
					camera.NearClip,
					camera.FarClip,
					camera.Aspect
				);
			}

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

			_planes[ 0 ] = new Plane( _corners[ 4 ], _corners[ 1 ], _corners[ 0 ] );
			_planes[ 1 ] = new Plane( _corners[ 6 ], _corners[ 3 ], _corners[ 2 ] );
			_planes[ 2 ] = new Plane( _corners[ 7 ], _corners[ 0 ], _corners[ 3 ] );
			_planes[ 3 ] = new Plane( _corners[ 5 ], _corners[ 2 ], _corners[ 1 ] );
			_planes[ 4 ] = new Plane( forward, position + forward * camera.NearClip );
			_planes[ 5 ] = new Plane( -forward, position + forward * farClipPlane );

			for( int i = 0; i < PlaneCount; i++ )
			{
				var plane = _planes[ i ];
				var normal = plane.Normal;

				_absNormals[ i ] = new Vector3( Math.Abs( normal.X ), Math.Abs( normal.Y ), Math.Abs( normal.Z ) );
				_planeNormal[ i ] = normal;
				_planeDistance[ i ] = plane.D;
			}
		}

		/// <summary>
		/// Update the bounding frustum
		/// </summary>
		public void Update( Vector3 position, Quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect )
		{
			this.Position = position;

			calculateFrustumCornersPerspective( ref position, ref orientation, fov, nearClipPlane, farClipPlane, aspect );

			var forward = Vector3.ForwardLH * orientation;

			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

			// PLANES:
			// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

			_planes[ 0 ] = new Plane( _corners[ 4 ], _corners[ 1 ], _corners[ 0 ] );
			_planes[ 1 ] = new Plane( _corners[ 6 ], _corners[ 3 ], _corners[ 2 ] );
			_planes[ 2 ] = new Plane( _corners[ 7 ], _corners[ 0 ], _corners[ 3 ] );
			_planes[ 3 ] = new Plane( _corners[ 5 ], _corners[ 2 ], _corners[ 1 ] );
			_planes[ 4 ] = new Plane( forward, position + forward * nearClipPlane );
			_planes[ 5 ] = new Plane( -forward, position + forward * farClipPlane );

			for( int i = 0; i < PlaneCount; i++ )
			{
				var plane = _planes[ i ];
				var normal = plane.Normal;

				_absNormals[ i ] = new Vector3( Math.Abs( normal.X ), Math.Abs( normal.Y ), Math.Abs( normal.Z ) );
				_planeNormal[ i ] = normal;
				_planeDistance[ i ] = plane.D;
			}
		}

		/// <summary>
		/// Returns true if the frustum contains the specified point
		/// </summary>
		public bool Contains( ref Vector3 point )
		{
			for( int i = 0; i < PlaneCount; i++ )
			{
				var normal = _planeNormal[ i ];
				var distance = _planeDistance[ i ];

				float dist = normal.X * point.X + normal.Y * point.Y + normal.Z * point.Z + distance;

				if( dist < 0f )
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="center">The world position of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		public IntersectionType GetSphereIntersection( ref Vector3 center, float radius, float frustumPadding = 0 )
		{
			var intersecting = false;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var normal = _planeNormal[ i ];
				var distance = _planeDistance[ i ];

				float dist = normal.X * center.X + normal.Y * center.Y + normal.Z * center.Z + distance;

				if( dist < -radius - frustumPadding )
				{
					return IntersectionType.Disjoint;
				}

				intersecting |= ( dist <= radius );
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		public IntersectionType GetSphereIntersection( ref BoundingSphere sphere, float frustumPadding = 0 )
		{
			var intersecting = false;

			var center = sphere.Center;
			var radius = sphere.Radius;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var normal = _planeNormal[ i ];
				var distance = _planeDistance[ i ];

				float dist = normal.X * center.X + normal.Y * center.Y + normal.Z * center.Z + distance;

				if( dist < -radius - frustumPadding )
				{
					return IntersectionType.Disjoint;
				}

				intersecting |= ( dist <= radius );
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Iterates through each sphere in the array and sets the Result field to the result of the sphere/frustum intersection test
		/// This function is intended primarily for use with static geometry (or quadtrees, etc) where the bounding volumes will not 
		/// be updated frequently, but the frustum will. 
		/// </summary>
		public void CullSpheres( CullingSphere[] spheres, int sphereCount )
		{
			Vector3 planeNormal = Vector3.Zero;

			var planeNormal0 = _planeNormal[ 0 ];
			var planeNormal1 = _planeNormal[ 1 ];
			var planeNormal2 = _planeNormal[ 2 ];
			var planeNormal3 = _planeNormal[ 3 ];
			var planeNormal4 = _planeNormal[ 4 ];
			var planeNormal5 = _planeNormal[ 5 ];

			var planeDistance0 = _planeDistance[ 0 ];
			var planeDistance1 = _planeDistance[ 1 ];
			var planeDistance2 = _planeDistance[ 2 ];
			var planeDistance3 = _planeDistance[ 3 ];
			var planeDistance4 = _planeDistance[ 4 ];
			var planeDistance5 = _planeDistance[ 5 ];

			for( int si = 0; si < sphereCount; si++ )
			{
				var sphere = spheres[ si ];
				var center = sphere.SphereCenter;
				var radius = sphere.SphereRadius;

				bool outOfFrustum = false;

				outOfFrustum = outOfFrustum || ( planeNormal0.X * center.X + planeNormal0.Y * center.Y + planeNormal0.Z * center.Z + planeDistance0 ) < -radius;
				outOfFrustum = outOfFrustum || ( planeNormal1.X * center.X + planeNormal1.Y * center.Y + planeNormal1.Z * center.Z + planeDistance1 ) < -radius;
				outOfFrustum = outOfFrustum || ( planeNormal2.X * center.X + planeNormal2.Y * center.Y + planeNormal2.Z * center.Z + planeDistance2 ) < -radius;
				outOfFrustum = outOfFrustum || ( planeNormal3.X * center.X + planeNormal3.Y * center.Y + planeNormal3.Z * center.Z + planeDistance3 ) < -radius;
				outOfFrustum = outOfFrustum || ( planeNormal4.X * center.X + planeNormal4.Y * center.Y + planeNormal4.Z * center.Z + planeDistance4 ) < -radius;
				outOfFrustum = outOfFrustum || ( planeNormal5.X * center.X + planeNormal5.Y * center.Y + planeNormal5.Z * center.Z + planeDistance5 ) < -radius;

				spheres[ si ].IsInFrustum = !outOfFrustum;
			}
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="center">The world position of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		public bool IntersectsSphere( ref Vector3 center, float radius, float frustumPadding = 0 )
		{
			for( int i = 0; i < PlaneCount; i++ )
			{
				var normal = _planeNormal[ i ];
				var distance = _planeDistance[ i ];

				float dist = normal.X * center.X + normal.Y * center.Y + normal.Z * center.Z + distance;

				if( dist < -radius - frustumPadding )
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the frustum and the sphere
		/// </summary>
		/// <param name="sphere">The sphere to check</param>
		public bool IntersectsSphere( ref BoundingSphere sphere, float frustumPadding = 0 )
		{
			var center = sphere.Center;
		
			for( int i = 0; i < PlaneCount; i++ )
			{
				var normal = _planeNormal[ i ];
				var distance = _planeDistance[ i ];

				float dist = normal.X * center.X + normal.Y * center.Y + normal.Z * center.Z + distance;

				if( dist < -sphere.Radius - frustumPadding )
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns TRUE if the box and frustum intersect
		/// </summary>
		public bool IntersectsBox( ref Bounds box, float frustumPadding = 0 )
		{
			// Exit early if the box contains the frustum origin
			if( box.Contains( _corners[ CornerCount - 1 ] ) )
			{
				return true;
			}

			var center = box.center;
			var extents = box.extents;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var abs = _absNormals[ i ];

				var planeNormal = _planeNormal[ i ];
				var planeDistance = _planeDistance[ i ];

				float r = extents.X * abs.X + extents.Y * abs.Y + extents.Z * abs.Z;
				float s = planeNormal.X * center.X + planeNormal.Y * center.Y + planeNormal.Z * center.Z;

				if( s + r < -planeDistance - frustumPadding ) 
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) between the bounding box and the frustum
		/// </summary>
		public IntersectionType GetBoxIntersection( ref Bounds box, float frustumPadding = 0 )
		{
			var center = box.center;
			var extents = box.extents;

			var intersecting = false;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var abs = _absNormals[ i ];

				var planeNormal = _planeNormal[ i ];
				var planeDistance = _planeDistance[ i ];

				float r = extents.X * abs.X + extents.Y * abs.Y + extents.Z * abs.Z;
				float s = planeNormal.X * center.X + planeNormal.Y * center.Y + planeNormal.Z * center.Z;

				if( s + r < -planeDistance - frustumPadding )
				{
					return IntersectionType.Disjoint;
				}

				intersecting |= ( s - r <= -planeDistance );
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		/// <summary>
		/// Iterates through each box in the boxes array and sets the Result field to the result of the box/frustum intersection test.
		/// This function is intended primarily for use with static geometry (or quadtrees, etc) where the bounding volumes will not 
		/// be updated frequently, but the frustum will. 
		/// </summary>
		public void CullBoxes( CullingBox[] boxes, int boxCount )
		{
			var abs0 = _absNormals[ 0 ];
			var abs1 = _absNormals[ 1 ];
			var abs2 = _absNormals[ 2 ];
			var abs3 = _absNormals[ 3 ];
			var abs4 = _absNormals[ 4 ];
			var abs5 = _absNormals[ 5 ];

			var planeNormal0 = _planeNormal[ 0 ];
			var planeNormal1 = _planeNormal[ 1 ];
			var planeNormal2 = _planeNormal[ 2 ];
			var planeNormal3 = _planeNormal[ 3 ];
			var planeNormal4 = _planeNormal[ 4 ];
			var planeNormal5 = _planeNormal[ 5 ];

			var planeDistance0 = _planeDistance[ 0 ];
			var planeDistance1 = _planeDistance[ 1 ];
			var planeDistance2 = _planeDistance[ 2 ];
			var planeDistance3 = _planeDistance[ 3 ];
			var planeDistance4 = _planeDistance[ 4 ];
			var planeDistance5 = _planeDistance[ 5 ];

			for( int bi = 0; bi < boxCount; bi++ )
			{
				var box = boxes[ bi ];
				var center = box.BoxCenter;
				var extents = box.BoxExtents;

				bool outOfFrustum = false;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs0.X + extents.Y * abs0.Y + extents.Z * abs0.Z ) +
					( planeNormal0.X * center.X + planeNormal0.Y * center.Y + planeNormal0.Z * center.Z ) ) < -planeDistance0;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs1.X + extents.Y * abs1.Y + extents.Z * abs1.Z ) +
					( planeNormal1.X * center.X + planeNormal1.Y * center.Y + planeNormal1.Z * center.Z ) ) < -planeDistance1;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs2.X + extents.Y * abs2.Y + extents.Z * abs2.Z ) +
					( planeNormal2.X * center.X + planeNormal2.Y * center.Y + planeNormal2.Z * center.Z ) ) < -planeDistance2;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs3.X + extents.Y * abs3.Y + extents.Z * abs3.Z ) +
					( planeNormal3.X * center.X + planeNormal3.Y * center.Y + planeNormal3.Z * center.Z ) ) < -planeDistance3;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs4.X + extents.Y * abs4.Y + extents.Z * abs4.Z ) +
					( planeNormal4.X * center.X + planeNormal4.Y * center.Y + planeNormal4.Z * center.Z ) ) < -planeDistance4;

				outOfFrustum = outOfFrustum || (
					( extents.X * abs5.X + extents.Y * abs5.Y + extents.Z * abs5.Z ) +
					( planeNormal5.X * center.X + planeNormal5.Y * center.Y + planeNormal5.Z * center.Z ) ) < -planeDistance5;

				boxes[ bi ].IsInFrustum = !outOfFrustum;
			}
		}

		/// <summary>
		/// Returns TRUE if the oriented bounding box and frustum intersect
		/// </summary>
		/// <param name="box">The bounding box to test. Note: box.center is expected to be in world coordinates</param>
		/// <param name="right">The horizontal local coordinate axis (equivalent to Transform.right)</param>
		/// <param name="up">The vertical local coordinate axis (equivalent to Transform.up)</param>
		/// <param name="forward">The forward local coordinate axis (equivalent to Transform.ForwardWoW)</param>
		/// <returns></returns>
		public bool IntersectsOrientedBox( ref Bounds box, ref Vector3 right, ref Vector3 up, ref Vector3 forward, float frustumPadding = 0 )
		{
			var center = box.center;
			var extents = box.extents;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var planeNormal = _planeNormal[ i ];
				var planeDistance = _planeDistance[ i ];

				float r =
					extents.X * Math.Abs( Vector3.Dot( planeNormal, right ) ) +
					extents.Y * Math.Abs( Vector3.Dot( planeNormal, up ) ) +
					extents.Z * Math.Abs( Vector3.Dot( planeNormal, forward ) );

				float s = planeNormal.X * center.X + planeNormal.Y * center.Y + planeNormal.Z * center.Z;

				if( s + r < -planeDistance - frustumPadding )
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the type of intersection (if any) of an oriented bounding box and the frustum.
		/// </summary>
		/// <param name="box">The bounding box to test. Note: box.center is expected to be in world coordinates</param>
		/// <param name="right">The horizontal local coordinate axis (equivalent to Transform.right)</param>
		/// <param name="up">The vertical local coordinate axis (equivalent to Transform.up)</param>
		/// <param name="forward">The forward local coordinate axis (equivalent to Transform.ForwardWoW)</param>
		/// <returns></returns>
		public IntersectionType GetOrientedBoxIntersection( ref Bounds box, ref Vector3 right, ref Vector3 up, ref Vector3 forward, float frustumPadding = 0 )
		{
			var center = box.center;
			var extents = box.extents;

			var intersecting = false;

			for( int i = 0; i < PlaneCount; i++ )
			{
				var planeNormal = _planeNormal[ i ];
				var planeDistance = _planeDistance[ i ];

				float r =
					extents.X * Math.Abs( Vector3.Dot( planeNormal, right ) ) +
					extents.Y * Math.Abs( Vector3.Dot( planeNormal, up ) ) +
					extents.Z * Math.Abs( Vector3.Dot( planeNormal, forward ) );

				float s = planeNormal.X * center.X + planeNormal.Y * center.Y + planeNormal.Z * center.Z;

				if( s + r < -planeDistance - frustumPadding )
				{
					return IntersectionType.Disjoint;
				}

				intersecting |= ( s - r <= -planeDistance );
			}

			return intersecting ? IntersectionType.Intersects : IntersectionType.Contains;
		}

		#endregion

		#region Private functions 

		/*private void calculateFrustumCornersOrthographic( ICamera camera )
		{
			var camTransform = camera.transform;
			var position = camTransform.position;
			var orientation = camTransform.rotation;
			var farClipPlane = camera.farClipPlane;
			var nearClipPlane = camera.nearClipPlane;
	
			var forward = orientation * Vector3.ForwardWoW;
			var right = orientation * Vector3.RightWoW * camera.orthographicSize * camera.aspect;
			var up = orientation * Vector3.UpWoW * camera.orthographicSize;
	
			// CORNERS:
			// [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
			// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right
	
			_corners[ 0 ] = position + forward * farClipPlane - up - right;
			_corners[ 1 ] = position + forward * farClipPlane + up - right;
			_corners[ 2 ] = position + forward * farClipPlane + up + right;
			_corners[ 3 ] = position + forward * farClipPlane - up + right;
			_corners[ 4 ] = position + forward * nearClipPlane - up - right;
			_corners[ 5 ] = position + forward * nearClipPlane + up - right;
			_corners[ 6 ] = position + forward * nearClipPlane + up + right;
			_corners[ 7 ] = position + forward * nearClipPlane - up + right;
		}*/

		private void calculateFrustumCornersPerspective( ref Vector3 position, ref Quaternion orientation, float fov, float nearClipPlane, float farClipPlane, float aspect )
		{
			float fovWHalf = fov;

			Vector3 toRight = Vector3.Right * (float)Math.Tan( fovWHalf * (float)Math.PI / 180 ) * aspect;
			Vector3 toTop = Vector3.Up * (float)Math.Tan( fovWHalf * (float)Math.PI / 180 );
			var forward = Vector3.ForwardLH;

			Vector3 topLeft = ( forward - toRight + toTop );
			float camScale = topLeft.Length() * farClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			Vector3 topRight = ( forward + toRight + toTop );
			topRight.Normalize();
			topRight *= camScale;

			Vector3 bottomRight = ( forward + toRight - toTop );
			bottomRight.Normalize();
			bottomRight *= camScale;

			Vector3 bottomLeft = ( forward - toRight - toTop );
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			_corners[ 0 ] = position + bottomLeft * orientation;
			_corners[ 1 ] = position + topLeft * orientation;
			_corners[ 2 ] = position + topRight  * orientation;
			_corners[ 3 ] = position + bottomRight * orientation;

			topLeft = ( forward - toRight + toTop );
			camScale = topLeft.Length() * nearClipPlane;

			topLeft.Normalize();
			topLeft *= camScale;

			topRight = ( forward + toRight + toTop );
			topRight.Normalize();
			topRight *= camScale;

			bottomRight = ( forward + toRight - toTop );
			bottomRight.Normalize();
			bottomRight *= camScale;

			bottomLeft = ( forward - toRight - toTop );
			bottomLeft.Normalize();
			bottomLeft *= camScale;

			_corners[ 4 ] = position + bottomLeft * orientation;
			_corners[ 5 ] = position +  topLeft * orientation;
			_corners[ 6 ] = position + topRight * orientation;
			_corners[ 7 ] = position + bottomRight * orientation;
		}

		#endregion 

		#region Nested types

		// When culling large numbers of static volumes per frame, it can be faster and more efficient to store just their 
		// bounding volume representations in a single indexed array, together with the culling results. This allows for 
		// extremely fast brute-force culling of large numbers of objects without the need to recursively traverse hierarchical 
		// spatial partition structures. This can in some particular cases actually be significantly faster.
		// This was implemented for a specific use case in my own code and YMMV, so profile rigorously and make no assumptions.
	
		public struct CullingBox
		{
			public Vector3 BoxCenter;
			public Vector3 BoxExtents;
			public bool IsInFrustum;
		}

		public struct CullingSphere
		{
			public Vector3 SphereCenter;
			public float SphereRadius;
			public bool IsInFrustum;
		}

		#endregion 
	}

	public class Bounds
	{
		public Vector3 center;
		public Vector3 extents;

		public bool Contains(Vector3 vector3)
		{
			return vector3.X >= center.X - extents.X &&
			       vector3.X <= center.X + extents.X &&
			       vector3.Y >= center.Y - extents.Y &&
			       vector3.Y <= center.Y + extents.Y &&
			       vector3.Z >= center.Z - extents.Z &&
			       vector3.Z <= center.Z + extents.Z;

		}
	}

	public enum IntersectionType
	{
		Intersects,
		Disjoint,
		Contains
	}
}