using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

using VoxelSystem;

namespace VJ.Channel18
{

    public class VoxelParticleSystem : MonoBehaviour {
        
        [SerializeField] protected Mesh mesh;
        [SerializeField] protected ComputeShader voxelizer, voxelControl, particleUpdate;
        [SerializeField] protected int count = 64;

        #region Particle properties

        [SerializeField] protected float speedScaleMin = 2.0f, speedScaleMax = 5.0f;
        [SerializeField] protected float speedLimit = 1.0f;
        [SerializeField, Range(0, 15)] protected float drag = 0.1f;
        [SerializeField] protected Vector3 gravity = Vector3.zero;
        [SerializeField] protected float speedToSpin = 60.0f;
        [SerializeField] protected float maxSpin = 20.0f;
        [SerializeField] protected float noiseAmplitude = 1.0f;
        [SerializeField] protected float noiseFrequency = 0.01f;
        [SerializeField] protected float noiseMotion = 1.0f;
        [SerializeField, Range(0f, 1f)] protected float threshold = 0f;
        protected Vector3 noiseOffset;

        #endregion

        #region Voxel control properties

        [SerializeField, Range(0f, 1f)] protected float throttle = 0.1f;

        #endregion

        protected GPUVoxelData data;
        protected Bounds bounds;

        protected Kernel setupKer, updateKer;
        protected Kernel randomizeKer, glitchKer;

        protected ComputeBuffer particleBuffer;

        protected new Renderer renderer;
        protected MaterialPropertyBlock block;

        #region Shader property keys

        protected const string kVoxelBufferKey = "_VoxelBuffer", kIndexBufferKey = "_IndexBuffer";
        protected const string kParticleBufferKey = "_ParticleBuffer", kParticleCountKey = "_ParticleCount";
		protected const string kStartKey = "_Start", kEndKey = "_End", kSizeKey = "_Size";
        protected const string kWidthKey = "_Width", kHeightKey = "_Height", kDepthKey = "_Depth";
        protected const string kUnitLengthKey = "_UnitLength", kInvUnitLengthKey = "_InvUnitLength", kHalfUnitLengthKey = "_HalfUnitLength";

        protected const string kTimeKey = "_Time", kDTKey = "_DT";
        protected const string kSpeedKey = "_Speed";
        protected const string kDamperKey = "_Damper";
        protected const string kGravityKey = "_Gravity";
        protected const string kSpinKey = "_Spin";
        protected const string kNoiseParamsKey = "_NoiseParams", kNoiseOffsetKey = "_NoiseOffset";
        protected const string kThresholdKey = "_Threshold";
        protected const string kThrottleKey = "_Throttle";

        #endregion

        #region MonoBehaviour functions

        void Start () {
            bounds = mesh.bounds;
            Voxelize();

            var pointMesh = BuildPoints(data);
            particleBuffer = new ComputeBuffer(pointMesh.vertexCount, Marshal.SizeOf(typeof(VoxelParticle_t)));
            particleBuffer.SetData(new VoxelParticle_t[pointMesh.vertexCount]);

            GetComponent<MeshFilter>().sharedMesh = pointMesh;

            block = new MaterialPropertyBlock();
            renderer = GetComponent<Renderer>();
            renderer.GetPropertyBlock(block);

            setupKer = new Kernel(particleUpdate, "Setup");
            updateKer = new Kernel(particleUpdate, "Update");
            randomizeKer = new Kernel(voxelControl, "Randomize");
            glitchKer = new Kernel(voxelControl, "Glitch");

            Setup();
        }
      
        void Update () {
            ComputeParticle(updateKer, Time.deltaTime);

            block.SetBuffer(kParticleBufferKey, particleBuffer);
            renderer.SetPropertyBlock(block);

            var t = Time.timeSinceLevelLoad;
            threshold = (Mathf.Cos(t * 0.5f) + 1.0f) * 0.5f;
        }

        void OnDestroy ()
        {
            if(data != null)
            {
                data.Dispose();
                data = null;
            }

            if(particleBuffer != null)
            {
                particleBuffer.Release();
                particleBuffer = null;
            }
        }

        #endregion

        void Setup()
        {
            particleUpdate.SetBuffer(setupKer.Index, kVoxelBufferKey, data.Buffer);
            particleUpdate.SetBuffer(setupKer.Index, kParticleBufferKey, particleBuffer);
            particleUpdate.SetInt(kParticleCountKey, particleBuffer.count);
            particleUpdate.SetInt(kWidthKey, data.Width);
            particleUpdate.SetInt(kHeightKey, data.Height);
            particleUpdate.SetInt(kDepthKey, data.Depth);
            particleUpdate.SetVector(kSpeedKey, new Vector2(speedScaleMin, speedScaleMax));
            particleUpdate.SetFloat(kUnitLengthKey, data.UnitLength);

            particleUpdate.Dispatch(setupKer.Index, particleBuffer.count / (int)setupKer.ThreadX + 1, (int)setupKer.ThreadY, (int)setupKer.ThreadZ);
        }

        Vector4 GetTime(float t)
        {
            return new Vector4(t / 4f, t, t * 2f, t * 3f);
        }

        public void Voxelize()
        {
            if(data != null)
            {
                data.Dispose();
                data = null;
            }
			data = GPUVoxelizer.Voxelize(voxelizer, mesh, bounds, count, true, false);
        }

        void ComputeVoxel (Kernel kernel, float dt)
        {
            voxelControl.SetBuffer(kernel.Index, kVoxelBufferKey, data.Buffer);

			voxelControl.SetVector(kStartKey, bounds.min);
			voxelControl.SetVector(kEndKey, bounds.max);
			voxelControl.SetVector(kSizeKey, bounds.size);

            voxelControl.SetFloat(kUnitLengthKey, data.UnitLength);
			voxelControl.SetFloat(kInvUnitLengthKey, 1f / data.UnitLength);
			voxelControl.SetFloat(kHalfUnitLengthKey, data.UnitLength * 0.5f);

            voxelControl.SetInt(kWidthKey, data.Width);
            voxelControl.SetInt(kHeightKey, data.Height);
            voxelControl.SetInt(kDepthKey, data.Depth);

            voxelControl.SetVector(kTimeKey, GetTime(Time.timeSinceLevelLoad));
            voxelControl.SetFloat(kDTKey, dt);

            voxelControl.SetFloat(kThrottleKey, throttle);

			voxelControl.Dispatch(kernel.Index, data.Width / (int)kernel.ThreadX + 1, data.Height / (int)kernel.ThreadY + 1, (int)kernel.ThreadZ);
        }

        void ComputeParticle (Kernel kernel, float dt)
        {
            particleUpdate.SetBuffer(kernel.Index, kVoxelBufferKey, data.Buffer);
            particleUpdate.SetBuffer(kernel.Index, kParticleBufferKey, particleBuffer);
            particleUpdate.SetInt(kParticleCountKey, particleBuffer.count);

            particleUpdate.SetVector(kDTKey, new Vector2(dt, 1f / dt));
            particleUpdate.SetVector(kDamperKey, new Vector2(Mathf.Exp(-drag * dt), speedLimit));
            particleUpdate.SetVector(kGravityKey, gravity * dt);

            var pi360dt = Mathf.PI * dt / 360;
            particleUpdate.SetVector(kSpinKey, new Vector2(maxSpin * pi360dt, speedToSpin * pi360dt));

            particleUpdate.SetVector(kNoiseParamsKey, new Vector2(noiseFrequency, noiseAmplitude * dt));

            var noiseDir = (gravity == Vector3.zero) ? Vector3.up : gravity.normalized;
            noiseOffset += noiseDir * noiseMotion * dt;
            particleUpdate.SetVector(kNoiseOffsetKey, noiseOffset);

            threshold = Mathf.Clamp01(threshold);
            particleUpdate.SetInt(kThresholdKey, Mathf.FloorToInt(threshold * data.Height));

            particleUpdate.Dispatch(kernel.Index, particleBuffer.count / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        public void Randomize()
        {
            Voxelize();
            ComputeVoxel(randomizeKer, 0f);
        }

        public void Glitch()
        {
            Voxelize();
            ComputeVoxel(glitchKer, 0f);
        }

        Mesh BuildPoints(GPUVoxelData data)
        {
			var voxels = data.GetData();
            var count = data.Width * data.Height * data.Depth;
            var indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            var mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = new Vector3[count];
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

    }

}


