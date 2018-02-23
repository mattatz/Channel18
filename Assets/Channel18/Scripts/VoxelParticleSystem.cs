using System;
using System.Linq;
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

        #region Accessors

        public Bounds BaseClipBounds {
            get { return baseClipBounds; }
        }

        public Bounds ClipBounds {
            get { return clipBounds; }
            set { clipBounds = value; }
        }

        #endregion

        #region Enums

        protected enum ParticleMode
        {
            Immediate,
            Delay,
            Transform,
            Clip
        };

        protected enum VoxelMode
        {
            Default,
            Randomize,
            Glitch
        };

        #endregion

        [SerializeField] protected SkinnedMeshRenderer skin;
        [SerializeField] protected int resolution = 128;
        [SerializeField, Range(0, 5)] protected int level = 0;
        [SerializeField] protected int count = 262144;
        [SerializeField] protected ComputeShader voxelizer, voxelControl, particleUpdate;
        [SerializeField] protected ParticleMode particleMode = ParticleMode.Immediate;
        [SerializeField] protected VoxelMode voxelMode = VoxelMode.Default;

        protected Dictionary<ParticleMode, Kernel> particleKernels;
        protected Dictionary<VoxelMode, Kernel> voxelKernels;

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

        [SerializeField, Range(1, 10)] protected int frame = 1;
        [SerializeField, Range(0, 50f)] protected float delaySpeed = 1.5f, transformSpeed = 1.5f, clipSpeed = 1.5f;
        [SerializeField] protected Bounds baseClipBounds, clipBounds;

        #region Voxel control properties

        [SerializeField, Range(0f, 1f)] protected float throttle = 0.1f;

        #endregion

        protected Mesh cached;
        protected GPUVoxelData data;
        protected Bounds bounds;

        protected Kernel setupKer, immediateKer, delayKer;
        protected Kernel randomizeKer, glitchKer;

        protected ComputeBuffer particleBuffer;

        protected new Renderer renderer;
        protected MaterialPropertyBlock block;

        #region Shader property keys

        protected const string kVoxelBufferKey = "_VoxelBuffer", kVoxelCountKey = "_VoxelCount";
        protected const string kParticleBufferKey = "_ParticleBuffer", kParticleCountKey = "_ParticleCount";
		protected const string kStartKey = "_Start", kEndKey = "_End", kSizeKey = "_Size";
        protected const string kWidthKey = "_Width", kHeightKey = "_Height", kDepthKey = "_Depth";
        protected const string kLevelKey = "_Level";
        protected const string kUnitLengthKey = "_UnitLength", kInvUnitLengthKey = "_InvUnitLength", kHalfUnitLengthKey = "_HalfUnitLength";

        protected const string kTimeKey = "_Time", kDTKey = "_DT";
        protected const string kSpeedKey = "_Speed";
        protected const string kDamperKey = "_Damper";
        protected const string kGravityKey = "_Gravity";
        protected const string kSpinKey = "_Spin";
        protected const string kNoiseParamsKey = "_NoiseParams", kNoiseOffsetKey = "_NoiseOffset";
        protected const string kThresholdKey = "_Threshold";
        protected const string kThrottleKey = "_Throttle";
        protected const string kDelaySpeedKey = "_DelaySpeed", kTransformSpeedKey = "_TransformSpeed", kClipSpeedKey = "_ClipSpeed";
        protected const string kClipMinKey = "_ClipMin", kClipMaxKey = "_ClipMax";

        #endregion

        #region MonoBehaviour functions

        void Start () {
            cached = Sample();
            bounds = cached.bounds;
            Voxelize(cached, bounds);

            var pointMesh = BuildPoints(count);
            particleBuffer = new ComputeBuffer(pointMesh.vertexCount, Marshal.SizeOf(typeof(VoxelParticle_t)));
            particleBuffer.SetData(new VoxelParticle_t[pointMesh.vertexCount]);

            GetComponent<MeshFilter>().sharedMesh = pointMesh;

            block = new MaterialPropertyBlock();
            renderer = GetComponent<Renderer>();
            renderer.GetPropertyBlock(block);

            setupKer = new Kernel(particleUpdate, "Setup");
            SetupParticleKernels();
            voxelKernels = new Dictionary<VoxelMode, Kernel>();
            voxelKernels.Add(VoxelMode.Randomize, new Kernel(voxelControl, "Randomize"));
            voxelKernels.Add(VoxelMode.Glitch, new Kernel(voxelControl, "Glitch"));

            Setup();
        }

        void SetupParticleKernels()
        {
            particleKernels = new Dictionary<ParticleMode, Kernel>();
            foreach(ParticleMode mode in Enum.GetValues(typeof(ParticleMode)))
            {
                particleKernels.Add(mode, new Kernel(particleUpdate, Enum.GetName(typeof(ParticleMode), mode)));
            }
        }
      
        void Update () {
            if(Time.frameCount % frame == 0)
            {
                cached = Sample();
                // bounds = cached.bounds;
                bounds.Encapsulate(cached.bounds.min);
                bounds.Encapsulate(cached.bounds.max);
                Voxelize(cached, bounds);
            }

            if(voxelMode != VoxelMode.Default) {
                ComputeVoxel(voxelKernels[voxelMode], 0f);
            }

            ComputeParticle(particleKernels[particleMode], Time.deltaTime);
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
            particleUpdate.SetInt(kVoxelCountKey, data.Width * data.Height * data.Depth);
            particleUpdate.SetInt(kWidthKey, data.Width);
            particleUpdate.SetInt(kHeightKey, data.Height);
            particleUpdate.SetInt(kLevelKey, level);
            particleUpdate.SetInt(kDepthKey, data.Depth);

            particleUpdate.SetBuffer(setupKer.Index, kParticleBufferKey, particleBuffer);
            particleUpdate.SetInt(kParticleCountKey, particleBuffer.count);

            particleUpdate.SetVector(kSpeedKey, new Vector2(speedScaleMin, speedScaleMax));
            particleUpdate.SetFloat(kUnitLengthKey, data.UnitLength);

            particleUpdate.Dispatch(setupKer.Index, particleBuffer.count / (int)setupKer.ThreadX + 1, (int)setupKer.ThreadY, (int)setupKer.ThreadZ);
        }

        Vector4 GetTime(float t)
        {
            return new Vector4(t / 4f, t, t * 2f, t * 3f);
        }

        public void Voxelize(Mesh mesh, Bounds bounds)
        {
            if(data != null)
            {
                data.Dispose();
                data = null;
            }
			// data = GPUVoxelizer.Voxelize(voxelizer, mesh, mesh.bounds, resolution, true, false);
			data = GPUVoxelizer.Voxelize(voxelizer, mesh, bounds, resolution >> level, true, false);
        }

        Mesh Sample()
        {
            var mesh = new Mesh();
            skin.BakeMesh(mesh);
            return mesh;
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
            particleUpdate.SetInt(kVoxelCountKey, data.Width * data.Height * data.Depth);
            particleUpdate.SetInt(kWidthKey, data.Width);
            particleUpdate.SetInt(kHeightKey, data.Height);
            particleUpdate.SetInt(kDepthKey, data.Depth);
            particleUpdate.SetInt(kLevelKey, level);
            particleUpdate.SetFloat(kUnitLengthKey, data.UnitLength);

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

            particleUpdate.SetFloat(kDelaySpeedKey, delaySpeed);
            particleUpdate.SetFloat(kTransformSpeedKey, transformSpeed);
            particleUpdate.SetFloat(kClipSpeedKey, clipSpeed);
            particleUpdate.SetVector(kClipMinKey, clipBounds.min);
            particleUpdate.SetVector(kClipMaxKey, clipBounds.max);

            particleUpdate.Dispatch(kernel.Index, particleBuffer.count / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        public void Randomize()
        {
            Voxelize(cached, bounds);
            ComputeVoxel(voxelKernels[VoxelMode.Randomize], 0f);
        }

        public void Glitch()
        {
            Voxelize(cached, bounds);
            ComputeVoxel(voxelKernels[VoxelMode.Glitch], 0f);
        }

        Mesh BuildPoints(int count)
        {
            var indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;

            var mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = new Vector3[count];
            mesh.SetIndices(indices, MeshTopology.Points, 0);
            mesh.RecalculateBounds();
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            return mesh;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

    }

}


