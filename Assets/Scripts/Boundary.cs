using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Boundary : MonoBehaviour{
	[SerializeField]
	SplineContainer SplineContainerReference;

	[SerializeField]
	float BoundaryHeight = 5.0f;

	[SerializeField]
	float SegmentCount = 1.0f;

	[SerializeField]
	bool DoubleSided = false;

	Mesh MeshReference;
	MeshCollider MeshColliderReference;

	public Boundary(){
	}

	void OnDisable(){
		Spline.Changed -= OnSplineUpdate;
	}

	void OnEnable(){
		if (SplineContainerReference){
			Spline.Changed += OnSplineUpdate;

			Construct();
		}
	}

	void OnValidate(){
		Construct();
	}

	void OnSplineUpdate(Spline TargetSpline, int KnotIndex, SplineModification Modification){
		if (SplineContainerReference){
			if (SplineContainerReference.Spline == TargetSpline){
				Construct();
			}
		}
	}

	public void Construct(){
		if (SplineContainerReference){
			Spline SplineReference = SplineContainerReference.Spline;
			float SplineLength = SplineReference.GetLength();
			List<Vector3> Vertices = new List<Vector3>();
			List<Vector2> UVs = new List<Vector2>();
			List<int> Triangles = new List<int>();
			int Ring = 2;

			int Segments = Mathf.Max(1, Mathf.CeilToInt(SplineLength * SegmentCount));

			for (int Index = 0; Index <= Segments; Index++){
				float3 Position;
				float3 Tangent;
				float3 UpVector;
				Vector3 WorldPosition;
				Vector3 WorldTangent;
				Vector3 WallBottom;
				Vector3 WallTop;
				float t = ((float)Index / (float)Segments);
				float u = (t * SplineLength);

				SplineReference.Evaluate(t, out Position, out Tangent, out UpVector);

				WorldPosition = SplineContainerReference.transform.TransformPoint(Position);
				WorldTangent = SplineContainerReference.transform.TransformDirection(Tangent).normalized;

				if (WorldTangent.sqrMagnitude < 1e-6f){
					WorldTangent = Vector3.forward;
				}

				WallBottom = WorldPosition;
				WallTop = (Vector3.up * BoundaryHeight);
				WallTop += WallBottom;

				Vertices.Add(transform.InverseTransformPoint(WallBottom));
				Vertices.Add(transform.InverseTransformPoint(WallTop));

				UVs.Add(new Vector2(u, 0.0f));
				UVs.Add(new Vector2(u, 1.0f));
			}

			for (int Index = 0; Index < Segments; Index++){
				int a = Index * Ring;
				int b = ((Index + 1) * Ring);

				Triangles.Add(a);
				Triangles.Add(a + 1);
				Triangles.Add(b);
				Triangles.Add(b);
				Triangles.Add(a + 1);
				Triangles.Add(b + 1);
			}

			if (DoubleSided){
				int FaceCount = Vertices.Count;

				for (int Index = 0; Index < FaceCount; Index++){
					Vertices.Add(Vertices[Index]);
					UVs.Add(UVs[Index]);
				}

				for (int Index = 0; Index < Segments; Index++){
					int a = (Index * Ring);
					int b = (Index + 1);

					a += FaceCount;
					b *= Ring;
					b += FaceCount;

					Triangles.Add(b);
					Triangles.Add(a + 1);
					Triangles.Add(a);
					Triangles.Add(b + 1);
					Triangles.Add(a + 1);
					Triangles.Add(b);
				}
			}

			if (MeshReference == null){
				MeshReference = new Mesh{
					name = "SplineBoundary"
				};

				GetComponent<MeshFilter>().sharedMesh = MeshReference;
			}

			MeshReference.Clear();
			MeshReference.SetVertices(Vertices);
			MeshReference.SetUVs(0, UVs);
			MeshReference.SetTriangles(Triangles, 0);
			MeshReference.RecalculateNormals();
			MeshReference.RecalculateBounds();
			MeshReference.RecalculateTangents();

			if (TryGetComponent(out MeshColliderReference)){
				MeshColliderReference.sharedMesh = null;
				MeshColliderReference.sharedMesh = MeshReference;
			}
		}
	}
}
