using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{
    public enum FloorMode
    {
        Noise = 3,
        Circle = 4,
        Line = 5
    };

    public class ProceduralFloorGrid : ProceduralGrid, IOSCReactable, INanoKontrollable, IAudioReactable {

        [SerializeField, Range(0f, 100f)] protected float plasticity = 10f;
        [SerializeField] protected float noiseSpeed = 1f, noiseScale = 0.5f;
        [SerializeField] protected float elevation = 100f, elevationMax = 300f;
        [SerializeField] protected float radius = 7.5f, thickness = 5f;
        [SerializeField] protected GradientTextureGen gradGen;

        [SerializeField] protected Vector4 circle, line;

        #region Shader property keys

        protected const string kNoiseParamsKey = "_NoiseParams";
        protected const string kElevationKey = "_Elevation";
        protected const string kPlasticityKey = "_Plasticity";
        protected const string kRadiusKey = "_Radius";
        protected const string kGradientKey = "_Gradient";

        #endregion

        protected Kernel updateKer;
        protected Dictionary<FloorMode, Kernel> kernels;

        protected Texture2D gradTex;

        protected override void Start () {
            base.Start();

            updateKer = new Kernel(compute, "Update");
            kernels = SetupFloorKernels();

            gradTex = gradGen.Create(128, 1);

            var grids = new FloorGrid_t[instancesCount];
            var poffset = new Vector3(
                -(width - 1) * 0.5f, -(height - 1) * 0.5f, -(depth - 1) * 0.5f
            );
            for(int z = 0; z < depth; z++)
            {
                var zoff = z * (width * height);
                for(int y = 0; y < height; y++)
                {
                    var yoff = y * width;
                    for(int x = 0; x < width; x++)
                    {
                        grids[x + yoff + zoff] = new FloorGrid_t(
                            new Vector3(x, y, z) + poffset,
                            Quaternion.identity,
                            Vector3.one,
                            Color.white,
                            Mathf.Lerp(massMin, massMax, Random.value)
                        );
                    }
                }
            }
            gridBuffer = new ComputeBuffer(instancesCount, Marshal.SizeOf(typeof(FloorGrid_t)));
            gridBuffer.SetData(grids);
        }

        protected Dictionary<FloorMode, Kernel> SetupFloorKernels()
        {
            var kernels = new Dictionary<FloorMode, Kernel>();
            foreach(FloorMode mode in Enum.GetValues(typeof(FloorMode)))
            {
                kernels.Add(mode, new Kernel(compute, Enum.GetName(typeof(FloorMode), mode)));
            }
            return kernels;
        }

        protected virtual void Update ()
        {
            // if(Time.frameCount % 4 == 0) Apply(FloorMode.Line);

            compute.SetFloat(kPlasticityKey, plasticity);
            Compute(updateKer, Time.deltaTime);
            Render();
        }

        protected override void Compute(Kernel kernel, float dt = 0f)
        {
            compute.SetFloat(kElevationKey, elevation);
            compute.SetTexture(kernel.Index, kGradientKey, gradTex);
            base.Compute(kernel, dt);
        }

        public void Apply(FloorMode mode)
        {
            switch(mode)
            {
                case FloorMode.Noise:
                    compute.SetVector(kNoiseParamsKey, new Vector3(noiseSpeed, noiseScale, 1f));
                    compute.SetVector(kRadiusKey, new Vector2(radius, radius + thickness));
                    break;
                case FloorMode.Circle:
                    break;
                case FloorMode.Line:
                    var hw = width * 0.5f;
                    var hd = depth * 0.5f;
                    line.x = Random.Range(-hw, hw);
                    line.y = Random.Range(-hd, hd);
                    line.z = Random.Range(-hw, hw);
                    line.w = Random.Range(-hd, hd);
                    break;
            }

            compute.SetVector("_Circle", circle);
            compute.SetVector("_Line", line);
            Compute(kernels[mode], Time.deltaTime);
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

        public void NoteOn(int note) {
            switch(note)
            {
                case 35:
                    Apply(FloorMode.Noise);
                    break;
                case 51:
                    Apply(FloorMode.Circle);
                    break;
                case 67:
                    break;
            }
        }

        public void NoteOff(int note) {
        }

        public void Knob(int knobNumber, float value)
        {
            switch(knobNumber)
            {
                case 3:
                    elevation = Mathf.Lerp(0f, elevationMax, value);
                    break;
                case 35:
                    break;
                case 51:
                    break;
                case 67:
                    break;
            }
        }

        public void React(int index, bool on)
        {
            switch(index)
            {
                case 0:
                    if (on) Apply(FloorMode.Noise);
                    break;

                case 4:
                    if (on) Apply(FloorMode.Line);
                    break;
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FloorGrid_t
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
        public float duration;
        public float mass;
        public FloorGrid_t(Vector3 p, Quaternion q, Vector3 s, Color c, float dur = 0f, float m = 1f)
        {
            position = p;
            rotation = q;
            scale = s;
            color = c;
            duration = dur;
            mass = m;
        }
    };



}


