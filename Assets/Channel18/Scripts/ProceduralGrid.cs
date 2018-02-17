using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;

namespace VJ.Channel18
{

    public abstract class ProceduralGrid : MonoBehaviour {

        [SerializeField, Range(1, 512)] protected int width = 128, height = 128, depth = 1;

        [SerializeField] protected Mesh mesh;
        [SerializeField] protected Material render;
        [SerializeField] protected ComputeShader compute;
        [SerializeField] protected ShadowCastingMode shadowCasting = ShadowCastingMode.On;
        [SerializeField] protected bool receiveShadow = true;

        #region Grid properties

        [SerializeField, Range(0f, 1f)] protected float massMin = 0.25f, massMax = 1f;

        #endregion

        #region Shader property keys

        protected const string kWorldToLocalKey = "_WorldToLocal", kLocalToWorldKey = "_LocalToWorld";
        protected const string kGridsKey = "_Grids";
        protected const string kTimeKey = "_Time", kDTKey = "_DT";

        protected const string kInstancesCountKey = "_InstancesCount";
        protected const string kWidthKey = "_Width", kHeightKey = "_Height", kDepthKey = "_Depth";

        #endregion

        protected ComputeBuffer gridBuffer, argsBuffer;
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        protected int instancesCount;

        protected virtual void Start () {
            mesh = Build();

            instancesCount = (width * height * depth);

            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)instancesCount;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            var grids = new Grid_t[instancesCount];
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
                        grids[x + yoff + zoff] = new Grid_t(
                            new Vector3(x, y, z) + poffset,
                            Quaternion.identity,
                            Vector3.one,
                            Mathf.Lerp(massMin, massMax, Random.value)
                        );
                    }
                }
            }
            gridBuffer = new ComputeBuffer(instancesCount, Marshal.SizeOf(typeof(Grid_t)));
            gridBuffer.SetData(grids);
        }

        protected virtual void Compute(Kernel kernel, float dt = 0f)
        {
            compute.SetBuffer(kernel.Index, kGridsKey, gridBuffer);
            compute.SetInt(kInstancesCountKey, instancesCount);
            compute.SetInt(kWidthKey, width);
            compute.SetInt(kHeightKey, height);
            compute.SetInt(kDepthKey, depth);

            var t = Time.timeSinceLevelLoad;
            compute.SetVector(kTimeKey, new Vector4(t / 20f, t, t * 2f, t * 3f));
            compute.SetFloat(kDTKey, dt);
            compute.Dispatch(kernel.Index, (int)(instancesCount / kernel.ThreadX + 1), (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        protected virtual void Render ()
        {
            render.SetBuffer(kGridsKey, gridBuffer);
            render.SetMatrix(kWorldToLocalKey, transform.worldToLocalMatrix);
            render.SetMatrix(kLocalToWorldKey, transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, render, new Bounds(Vector3.zero, Vector3.one * 1000f), argsBuffer, 0, null, shadowCasting, receiveShadow);
        }

        protected abstract Mesh Build();

        protected virtual void OnDestroy ()
        {
            if(gridBuffer != null)
            {
                gridBuffer.Release();
                gridBuffer = null;
            }

            if(argsBuffer != null)
            {
                argsBuffer.Release();
                argsBuffer = null;
            }
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Grid_t
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float mass;
        public Grid_t(Vector3 p, Quaternion q, Vector3 s, float m = 1f)
        {
            position = p;
            rotation = q;
            scale = s;
            mass = m;
        }
    };

}


