﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;

namespace VJ.Channel18
{

    public class ProceduralMidairGrid : ProceduralGrid, IOSCReactable, INanoKontrollable {

        protected enum MidairMode
        {
        };

        [SerializeField, Range(0f, 1f)] protected float extrusion = 0.05f, thickness = 0.001f;
        [SerializeField, Range(0f, 1f)] protected float throttle = 0.5f;

        protected float _extrusion, _thickness;

        protected ComputeBuffer supportBuffer;

        protected Kernel updateKer, 
            setupInitKer, initKer,
            setupRotKer, rotKer, autoRotKer;

        protected const string kSupportDataKey = "_SupportData";
        protected const string kExtrusionKey = "_Extrusion", kThicknessKey = "_Thickness";
        protected const string kThrottleKey = "_Throttle";

        Coroutine animationCo;

        protected override void Start ()
        {
            base.Start();

            supportBuffer = new ComputeBuffer(instancesCount, Marshal.SizeOf(typeof(MidairSupportData_t)));
            var supportData = new MidairSupportData_t[instancesCount];
            supportBuffer.SetData(supportData);

            updateKer = new Kernel(compute, "Update");

            setupInitKer = new Kernel(compute, "SetupInit");
            initKer = new Kernel(compute, "Init");
            setupRotKer = new Kernel(compute, "SetupRotate");
            rotKer = new Kernel(compute, "Rotate");
            autoRotKer = new Kernel(compute, "RotateAuto");

            var grids = new MidairGrid_t[instancesCount];
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
                        grids[x + yoff + zoff] = new MidairGrid_t(
                            new Vector3(x, y, z) + poffset,
                            Quaternion.identity,
                            Vector3.one,
                            Color.white,
                            Mathf.Lerp(massMin, massMax, Random.value)
                        );
                    }
                }
            }
            gridBuffer = new ComputeBuffer(instancesCount, Marshal.SizeOf(typeof(MidairGrid_t)));
            gridBuffer.SetData(grids);
        }

        protected virtual void Update ()
        {
            var dt = Time.deltaTime;
            // Compute(autoRotKer, dt * 5f);
            Compute(updateKer, dt);

            _extrusion = Mathf.Lerp(_extrusion, extrusion, dt);
            _thickness = Mathf.Lerp(_thickness, thickness, dt);

            Render();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();
            if(supportBuffer != null)
            {
                supportBuffer.Release();
                supportBuffer = null;
            }
        }

        protected override void Render()
        {
            render.SetBuffer(kGridsKey, gridBuffer);
            render.SetFloat(kExtrusionKey, _extrusion);
            render.SetFloat(kThicknessKey, _thickness);

            Graphics.DrawMesh(mesh, transform.localToWorldMatrix, render, 0, null, 0, null, shadowCasting, receiveShadow);
        }

        protected override void Compute(Kernel kernel, float dt)
        {
            compute.SetBuffer(kernel.Index, kSupportDataKey, supportBuffer);
            compute.SetFloat(kThrottleKey, throttle);
            base.Compute(kernel, dt);
        }

        public void Init()
        {
            Animate(setupInitKer, initKer);
        }

        public void Rotate()
        {
            Animate(setupRotKer, rotKer);
        }

        void Animate(Kernel setupKer, Kernel animateKer)
        {
            Compute(setupKer, Time.deltaTime);
            if(animationCo != null)
            {
                StopCoroutine(animationCo);
            }
            animationCo = StartCoroutine(IAnimator(animateKer, 0.5f));
        }

        IEnumerator IAnimator(Kernel kernel, float duration)
        {
            yield return 0;

            var time = 0f;
            while(time < duration)
            {
                yield return 0;
                var dt = Time.deltaTime;
                time += dt;
                compute.SetFloat("_T", time / duration);
                Compute(kernel, dt);
            }

            compute.SetFloat("_T", 1f);
            Compute(kernel, Time.deltaTime);
        }

        #region Build mesh

        protected override Mesh Build()
        {
            // return BuildCross(extrusion, thickness);

            var mesh = new Mesh();
            var count = width * height * depth;
            var indices = new int[count];
            for(int i = 0; i < count; i++) indices[i] = i;
            mesh.vertices = new Vector3[count];
			mesh.indexFormat = (count > 65000) ? IndexFormat.UInt16 : IndexFormat.UInt32;
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            return mesh;
        }

        protected Mesh BuildCross(
            float extrusion, float thickness
        )
        {
            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var tangents = new List<Vector4>();
            var triangles = new List<int>();

            // front
            CalculateFace(
                vertices, tangents, triangles,
                -Vector3.forward, Vector3.right, Vector3.up, extrusion, thickness
            );

            // right
            CalculateFace(
                vertices, tangents, triangles,
                Vector3.right, Vector3.forward, Vector3.up, extrusion, thickness
            );

            // back
            CalculateFace(
                vertices, tangents, triangles,
                Vector3.forward, Vector3.left, Vector3.up, extrusion, thickness
            );

            // left
            CalculateFace(
                vertices, tangents, triangles,
                -Vector3.right, Vector3.back, Vector3.up, extrusion, thickness
            );

            // top
            CalculateFace(
                vertices, tangents, triangles,
                Vector3.up, Vector3.right, Vector3.forward, extrusion, thickness
            );

            // bottom
            CalculateFace(
                vertices, tangents, triangles,
                -Vector3.up, Vector3.right, Vector3.back, extrusion, thickness
            );

            mesh.vertices = vertices.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        void CalculateFace(
            List<Vector3> vertices, List<Vector4> tangents, List<int> triangles,
            Vector3 forward, Vector3 right, Vector3 up, float extrusion = 0.1f, float thickness = 0.025f
        )
        {
            var er = extrusion * right;
            var eu = extrusion * up;
            var her = er * 0.5f;
            var heu = eu * 0.5f;

            var htf = forward * thickness * 0.5f;
            var tr = right * thickness;
            var tu = up * thickness;

            // center
            CalculatePlane(
                vertices, tangents, triangles,
                forward * extrusion, tr, tu, forward
            );

            // left
            CalculatePlane(
                vertices, tangents, triangles,
                htf - her, er, tu, -er
            );

            // top
            CalculatePlane(
                vertices, tangents, triangles,
                htf + heu, tr, eu, eu
            );

            // right
            CalculatePlane(
                vertices, tangents, triangles,
                htf + her, er, tu, er
            );

            // bottom
            CalculatePlane(
                vertices, tangents, triangles,
                htf - heu, tr, eu, -eu
            );

        }

        void CalculatePlane(
            List<Vector3> vertices, List<Vector4> tangents, List<int> triangles,
            Vector3 origin, Vector3 right, Vector3 up, Vector3 tangent, int rSegments = 2, int uSegments = 2
        )
        {
            float rInv = 1f / (rSegments - 1);
            float uInv = 1f / (uSegments - 1);

            int triangleOffset = vertices.Count;
            // var normal = origin.normalized;

            var nt = tangent.normalized;

            for (int y = 0; y < uSegments; y++)
            {
                float ru = y * uInv;
                for (int x = 0; x < rSegments; x++)
                {
                    float rr = x * rInv;
                    var v0 = right * (rr - 0.5f);
                    var v1 = up * (ru - 0.5f);
                    vertices.Add(origin + v0 + v1);
                    var dot = Vector3.Dot((v0 + v1).normalized, nt);
                    // tangents.Add((tangent * Mathf.Clamp01(dot)).normalized);
                    tangents.Add(nt * (dot > 0f ? 1f : 0f));
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

        #endregion

        public void OnOSC(string address, List<object> data)
        {
        }

        #region IKorgKontrollable interfaces

        public void NoteOn(int note)
        {
        }

        public void NoteOff(int note)
        {
        }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 4:
                    thickness = knobValue;
                    break;

                case 20:
                    extrusion = knobValue;
                    break;
            }
        }

        #endregion

    }

    #region Define structures

    [StructLayout(LayoutKind.Sequential)]
    public struct MidairGrid_t
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
        public float mass;
        public MidairGrid_t(Vector3 p, Quaternion q, Vector3 s, Color c, float m = 1f)
        {
            position = p;
            rotation = q;
            scale = s;
            color = c;
            mass = m;
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct MidairSupportData_t
    {
        float extrusion, thickness;
        Quaternion prevRot, toRot;
        float time;
        float offset;
        int flag;
    };

    #endregion

}


