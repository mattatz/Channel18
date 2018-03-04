using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class ProceduralFloorGrid : ProceduralGrid, IOSCReactable {

        [SerializeField, Range(0f, 100f)] protected float plasticity = 10f;
        [SerializeField] protected float noiseSpeed = 1f, noiseScale = 0.5f, noiseIntensity = 3f;
        [SerializeField] protected float radius = 7.5f, thickness = 5f; 

        #region Shader property keys

        protected const string kNoiseParamsKey = "_NoiseParams";
        protected const string kPlasticityKey = "_Plasticity";
        protected const string kRadiusKey = "_Radius";

        #endregion

        protected Kernel updateKer, noiseKer;

        protected override void Start () {
            base.Start();

            updateKer = new Kernel(compute, "Update");
            noiseKer = new Kernel(compute, "Noise");
        }

        protected virtual void Update ()
        {
            compute.SetFloat(kPlasticityKey, plasticity);
            Compute(updateKer, Time.deltaTime);
            Render();

            // if(Time.frameCount % 180 == 0) Noise();
        }

        protected override void Compute(Kernel kernel, float dt = 0f)
        {
            base.Compute(kernel, dt);
        }

        public void Noise()
        {
            compute.SetVector(kNoiseParamsKey, new Vector4(noiseIntensity, noiseSpeed, noiseScale, 1f));
            compute.SetVector(kRadiusKey, new Vector2(radius, radius + thickness));
            Compute(noiseKer, Time.deltaTime);
        }

        protected override Mesh Build()
        {
            return BuildCube();
        }

        protected Mesh BuildCube(
                float width = 1f, float height = 1f, float depth = 1f
            )
        {
            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();

            var hw = width * 0.5f;
            var hh = height * 0.5f;
            var hd = depth * 0.5f;

            var zero = Vector3.zero;
            var forward = Vector3.forward;
            var up = Vector3.up;
            var right = Vector3.right;

            var hf = forward * hd;
            var hu = up * hh * 0f; // crush vertically
            var hr = right * hw;

            var v0 =  hf + hu + -hr;
            var v1 = -hf + hu + -hr;
            var v2 = -hf + hu +  hr;
            var v3 =  hf + hu +  hr;
            var v4 =  hf - hu + -hr;
            var v5 = -hf - hu + -hr;
            var v6 = -hf - hu +  hr;
            var v7 =  hf - hu +  hr;

            // top
            AddFace(v0, v3, v1, vertices, triangles);
            tangents.Add(up); tangents.Add(up); tangents.Add(up);
            normals.Add(up); normals.Add(up); normals.Add(up);

            AddFace(v2, v1, v3, vertices, triangles);
            tangents.Add(up); tangents.Add(up); tangents.Add(up);
            normals.Add(up); normals.Add(up); normals.Add(up);

            // bottom
            AddFace(v4, v5, v7, vertices, triangles);
            tangents.Add(zero); tangents.Add(zero); tangents.Add(zero);
            normals.Add(-up); normals.Add(-up); normals.Add(-up);

            AddFace(v6, v7, v5, vertices, triangles);
            tangents.Add(zero); tangents.Add(zero); tangents.Add(zero);
            normals.Add(-up); normals.Add(-up); normals.Add(-up);

            // front
            AddFace(v0, v4, v3, vertices, triangles);
            tangents.Add(up); tangents.Add(zero); tangents.Add(up);
            normals.Add(forward); normals.Add(forward); normals.Add(forward);

            AddFace(v7, v3, v4, vertices, triangles);
            tangents.Add(zero); tangents.Add(up); tangents.Add(zero);
            normals.Add(forward); normals.Add(forward); normals.Add(forward);

            // back
            AddFace(v1, v2, v5, vertices, triangles);
            tangents.Add(up); tangents.Add(up); tangents.Add(zero);
            normals.Add(-forward); normals.Add(-forward); normals.Add(-forward);

            AddFace(v6, v5, v2, vertices, triangles);
            tangents.Add(zero); tangents.Add(zero); tangents.Add(up);
            normals.Add(-forward); normals.Add(-forward); normals.Add(-forward);

            // right
            AddFace(v3, v7, v2, vertices, triangles);
            tangents.Add(up); tangents.Add(zero); tangents.Add(up);
            normals.Add(right); normals.Add(right); normals.Add(right);

            AddFace(v6, v2, v7, vertices, triangles);
            tangents.Add(zero); tangents.Add(up); tangents.Add(zero);
            normals.Add(right); normals.Add(right); normals.Add(right);

            // left
            AddFace(v1, v5, v0, vertices, triangles);
            tangents.Add(up); tangents.Add(zero); tangents.Add(up);
            normals.Add(-right); normals.Add(-right); normals.Add(-right);

            AddFace(v4, v0, v5, vertices, triangles);
            tangents.Add(zero); tangents.Add(up); tangents.Add(zero);
            normals.Add(-right); normals.Add(-right); normals.Add(-right);

            mesh.vertices = vertices.ToArray();
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.RecalculateBounds();

            return mesh;
        }

        void AddFace(Vector3 p0, Vector3 p1, Vector3 p2, List<Vector3> vertices, List<int> triangles)
        {
            int triangleOffset = vertices.Count;
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p2);
            triangles.Add(triangleOffset + 0);
            triangles.Add(triangleOffset + 1);
            triangles.Add(triangleOffset + 2);
        }

        void CalculatePlane(
            List<Vector3> vertices, List<Vector2> uvs, List<Vector4> tangents, List<int> triangles,
            Vector3 origin, Vector3 right, Vector3 up, int rSegments = 2, int uSegments = 2
        )
        {
            float rInv = 1f / (rSegments - 1);
            float uInv = 1f / (uSegments - 1);

            int triangleOffset = vertices.Count;

            var u = Vector3.up;
            for (int y = 0; y < uSegments; y++)
            {
                float ru = y * uInv;
                for (int x = 0; x < rSegments; x++)
                {
                    float rr = x * rInv;
                    var v = right * (rr - 0.5f) + up * (ru - 0.5f);
                    vertices.Add(origin + v);
                    var t = u * (Vector3.Dot(v, u) >= 0.0f ? 1f : 0f);
                    tangents.Add(t);
                    uvs.Add(new Vector2(rr, ru));
                }

                if (y < uSegments - 1)
                {
                    var offset = y * rSegments + triangleOffset;
                    for (int x = 0, n = rSegments - 1; x < n; x++)
                    {
                        triangles.Add(offset + x);
                        triangles.Add(offset + x + rSegments);
                        triangles.Add(offset + x + 1);

                        triangles.Add(offset + x + 1);
                        triangles.Add(offset + x + rSegments);
                        triangles.Add(offset + x + 1 + rSegments);
                    }
                }
            }
        }

        public void OnOSC(string address, List<object> data)
        {
        }

    }

}


