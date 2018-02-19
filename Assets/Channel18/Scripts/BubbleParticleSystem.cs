using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class BubbleParticleSystem : MonoBehaviour {

        [SerializeField] protected Mesh quad;
        [SerializeField] protected ComputeShader particleUpdate;
        [SerializeField] protected Material render;
        [SerializeField] protected Bounds bounds;
        [SerializeField] AnimationCurve sizeCurve;
        [SerializeField, Range(0f, 1f)] protected float decay = 0.5f;
        [SerializeField, Range(0f, 1f)] protected float massMin = 0.25f, massMax = 1f;
        [SerializeField] protected Vector3 buoyancy = Vector3.up;
        [SerializeField] protected float noiseAmplitude = 1.0f;
        [SerializeField] protected float noiseFrequency = 0.01f;

        #region Shader property keys

        protected const string kWorldToLocalKey = "_WorldToLocal", kLocalToWorldKey = "_LocalToWorld";
        protected const string kBubblesKey = "_Bubbles";
        protected const string kTimeKey = "_Time", kDTKey = "_DT";

        protected const string kInstancesCountKey = "_InstancesCount";
        protected const string kBoundsMinKey = "_BoundsMin", kBoundsMaxKey = "_BoundsMax";
        protected const string kBuoyancyKey = "_Buoyancy";
        protected const string kDecayKey = "_Decay";
        protected const string kNoiseParamsKey = "_NoiseParams";

        #endregion

        protected ComputeBuffer bubbleBuffer, argsBuffer;
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        [SerializeField] protected int instancesCount;

        Kernel updateKer;

        protected void Start() {
            args[0] = quad.GetIndexCount(0);
            args[1] = (uint)instancesCount;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            bubbleBuffer = new ComputeBuffer(instancesCount, Marshal.SizeOf(typeof(Bubble_t)));
            var bubbles = new Bubble_t[instancesCount];
            for(int i = 0; i < instancesCount; i++)
            {
                var bub = bubbles[i];
                bub.position = Random.insideUnitSphere * 100f;
                bub.size = 1f;
                bub.mass = Mathf.Lerp(massMin, massMax, Random.value);
                bub.lifetime = Random.value;
                bubbles[i] = bub;
            }
            bubbleBuffer.SetData(bubbles);

            render.SetTexture("_SizeCurve", CreateCurve(sizeCurve));
            updateKer = new Kernel(particleUpdate, "Update");
        }

        protected void Update() {
            Compute(updateKer, Time.timeSinceLevelLoad, Time.deltaTime);

            render.SetBuffer(kBubblesKey, bubbleBuffer);
            render.SetMatrix(kWorldToLocalKey, transform.worldToLocalMatrix);
            render.SetMatrix(kLocalToWorldKey, transform.localToWorldMatrix);
            Graphics.DrawMeshInstancedIndirect(quad, 0, render, new Bounds(Vector3.zero, Vector3.one * 1000f), argsBuffer, 0, null);
        }

        protected void Compute(Kernel kernel, float t, float dt)
        {
            particleUpdate.SetBuffer(kernel.Index, kBubblesKey, bubbleBuffer);
            particleUpdate.SetInt(kInstancesCountKey, instancesCount);
            particleUpdate.SetVector(kTimeKey, new Vector4(t / 4f, t, t * 2f, t * 3f));
            particleUpdate.SetFloat(kDTKey, dt);
            particleUpdate.SetFloat(kDecayKey, decay);
            particleUpdate.SetVector(kBoundsMinKey, bounds.min);
            particleUpdate.SetVector(kBoundsMaxKey, bounds.max);
            particleUpdate.SetVector(kBuoyancyKey, buoyancy);
            particleUpdate.SetVector(kNoiseParamsKey, new Vector2(noiseFrequency, noiseAmplitude * dt));
            particleUpdate.Dispatch(kernel.Index, instancesCount / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        protected Texture2D CreateCurve(AnimationCurve curve, int resolution = 64)
        {
            var tex = new Texture2D(resolution, 1, TextureFormat.RFloat, false);
            for(int x = 0; x < resolution; x++)
            {
                var t = 1f * x / (resolution - 1);
                var v = curve.Evaluate(t);
                tex.SetPixel(x, 0, new Color(v, 0f, 0f));
            }
            tex.Apply();
            return tex;
        }

        protected virtual void OnDestroy()
        {
            if (argsBuffer != null)
            {
                argsBuffer.Release();
                argsBuffer = null;
            }

            if (bubbleBuffer != null)
            {
                bubbleBuffer.Release();
                bubbleBuffer = null;
            }
        }

        protected struct Bubble_t {
            public Vector3 position;
            public Vector3 velocity;
            public float size;
            public float mass;
            public float lifetime;
        };

    }

}


