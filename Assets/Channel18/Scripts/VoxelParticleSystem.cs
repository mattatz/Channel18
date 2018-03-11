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

    #region Enums

    public enum ParticleMode
    {
        Immediate,
        Delay,
        Transform,
        Clip,
        Flow
    };

    public enum VoxelMode
    {
        Default,
        Randomize,
        Glitch,
        Clip
    };

    #endregion

    public class VoxelParticleSystem : AudioReactor, INanoKontrollable {

        #region Accessors

        public ParticleMode PMode {
            get { return particleMode; }
            set {
                particleMode = value;
            }
        }

        public VoxelMode VMode {
            get { return voxelMode; }
            set {
                voxelMode = value;
                switch(voxelMode)
                {
                    case VoxelMode.Randomize:
                        Randomize();
                        break;
                    case VoxelMode.Glitch:
                        Glitch();
                        break;
                }
            }
        }

        public Bounds BaseClipBounds {
            get { return baseClipBounds; }
        }

        public Bounds ClipBounds {
            get { return clipBounds; }
            set { clipBounds = value; }
        }

        #endregion

        [SerializeField] protected SkinnedMeshRenderer skin;
        [SerializeField] protected int resolution = 128;
        [SerializeField, Range(0, 5)] protected int level = 0;
        [SerializeField] protected int count = 262144;
        [SerializeField] protected ComputeShader voxelizer, voxelControl, particleUpdate;
        [SerializeField] protected ParticleMode particleMode = ParticleMode.Immediate;
        [SerializeField] protected VoxelMode voxelMode = VoxelMode.Default;
        [SerializeField] protected bool voxelVisible = true;

        protected Dictionary<ParticleMode, Kernel> particleKernels;
        protected Dictionary<VoxelMode, Kernel> voxelKernels;

        #region Particle properties

        [SerializeField] protected float speedScaleMin = 2.0f, speedScaleMax = 5.0f;
        [SerializeField] protected float speedLimit = 1.0f;
        [SerializeField, Range(0f, 3f)] protected float drag = 0.1f;
        [SerializeField] protected Vector3 gravity = Vector3.zero;
        [SerializeField] protected float speedToSpin = 60.0f;
        [SerializeField] protected float maxSpin = 20.0f;
        [SerializeField] protected float noiseAmplitude = 1.0f;
        [SerializeField] protected float noiseFrequency = 0.01f;
        [SerializeField] protected float noiseMotion = 1.0f;
        protected Vector3 noiseOffset;

        #endregion

        [SerializeField, Range(1, 10)] protected int frame = 1;
        [SerializeField, Range(0, 50f)] protected float delaySpeed = 1.5f, transformSpeed = 1.5f, clipSpeed = 1.5f;
        [SerializeField, Range(0, 1f)] protected float flowSpeed = 0.1f;
        [SerializeField] protected Bounds baseClipBounds, clipBounds;

        #region Flow Random properties

        [SerializeField] protected bool flowRandom = false;
        [SerializeField] protected int flowRandomFreq = 100;
        [SerializeField, Range(0f, 1f)] protected float flowRandomThrottle = 0.1f;

        #endregion

        #region Voxel control properties

        [SerializeField, Range(0f, 1f)] protected float throttle = 0.1f;

        #endregion

        protected Mesh cached;
        protected GPUVoxelData data;
        protected Bounds bounds;

        protected Kernel setupKer, flowRandomKer;
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
        protected const string kFlowSpeedKey = "_FlowSpeed";
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
            flowRandomKer = new Kernel(particleUpdate, "FlowRandom");
            SetupParticleKernels();
            SetupVoxelKernels();

            Setup();
        }
     
        protected void Update () {
            if(Time.frameCount % frame == 0)
            {
                cached = Sample();
                // bounds = cached.bounds;
                bounds.Encapsulate(cached.bounds.min);
                bounds.Encapsulate(cached.bounds.max);
                Voxelize(cached, bounds);
            }

            // if(flowRandom && Time.frameCount % flowRandomFreq == 0) FlowRandom();

            var dt = Time.deltaTime;

            if(voxelMode != VoxelMode.Default) {
                ComputeVoxel(voxelKernels[voxelMode], dt);
            }

            ComputeParticle(particleKernels[particleMode], dt);
            block.SetBuffer(kParticleBufferKey, particleBuffer);
            renderer.SetPropertyBlock(block);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
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

        #region Initialization

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

        void SetupParticleKernels()
        {
            particleKernels = new Dictionary<ParticleMode, Kernel>();
            foreach(ParticleMode mode in Enum.GetValues(typeof(ParticleMode)))
            {
                particleKernels.Add(mode, new Kernel(particleUpdate, Enum.GetName(typeof(ParticleMode), mode)));
            }
        }

        void SetupVoxelKernels()
        {
            voxelKernels = new Dictionary<VoxelMode, Kernel>();
            foreach(VoxelMode mode in Enum.GetValues(typeof(VoxelMode)))
            {
                voxelKernels.Add(mode, new Kernel(voxelControl, Enum.GetName(typeof(VoxelMode), mode)));
            }
        }
 

        #endregion

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
			data = GPUVoxelizer.Voxelize(voxelizer, mesh, bounds, Mathf.Max(10, resolution) >> level, true, false, voxelVisible);
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

        void ComputeParticle (Kernel kernel, float dt = 0f)
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

            particleUpdate.SetVector(kTimeKey, GetTime(Time.timeSinceLevelLoad));
            particleUpdate.SetVector(kDTKey, new Vector2(dt, 1f / dt));
            particleUpdate.SetVector(kDamperKey, new Vector2(Mathf.Exp(-drag * dt), speedLimit));
            particleUpdate.SetVector(kGravityKey, gravity * dt);

            var pi360dt = Mathf.PI * dt / 360;
            particleUpdate.SetVector(kSpinKey, new Vector2(maxSpin * pi360dt, speedToSpin * pi360dt));

            particleUpdate.SetVector(kNoiseParamsKey, new Vector2(noiseFrequency, noiseAmplitude * dt));

            var noiseDir = (gravity == Vector3.zero) ? Vector3.up : gravity.normalized;
            noiseOffset += noiseDir * noiseMotion * dt;
            particleUpdate.SetVector(kNoiseOffsetKey, noiseOffset);

            particleUpdate.SetFloat(kDelaySpeedKey, delaySpeed);
            particleUpdate.SetFloat(kTransformSpeedKey, transformSpeed);
            particleUpdate.SetFloat(kClipSpeedKey, clipSpeed);
            particleUpdate.SetVector(kClipMinKey, clipBounds.min);
            particleUpdate.SetVector(kClipMaxKey, clipBounds.max);

            particleUpdate.SetFloat(kFlowSpeedKey, flowSpeed);

            particleUpdate.Dispatch(kernel.Index, particleBuffer.count / (int)kernel.ThreadX + 1, (int)kernel.ThreadY, (int)kernel.ThreadZ);
        }

        public void Randomize()
        {
            voxelControl.SetFloat("_Seed", Random.value);
            // Voxelize(cached, bounds);
            // ComputeVoxel(voxelKernels[VoxelMode.Randomize], 0f);
        }

        public void Glitch()
        {
            voxelControl.SetFloat("_Seed", Random.value);
            // Voxelize(cached, bounds);
            // ComputeVoxel(voxelKernels[VoxelMode.Glitch], 0f);
        }

        public void Clip(Vector3 min, Vector3 max)
        {
            voxelControl.SetVector(kClipMinKey, new Vector3(min.x * data.Width, min.y * data.Height, min.z * data.Depth));
            voxelControl.SetVector(kClipMaxKey, new Vector3(max.x * data.Width, max.y * data.Height, max.z * data.Depth));
            // ComputeVoxel(voxelKernels[VoxelMode.Clip], 0f);
            VMode = VoxelMode.Clip;
        }

        public void FlowRandom()
        {
            particleUpdate.SetFloat("_FlowRandomThrottle", flowRandomThrottle);
            ComputeParticle(flowRandomKer);
        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 33:
                    FlowRandom();
                    break;
                case 49:
                    break;
                case 65:
                    break;

                case 34:
                    break;
                case 50:
                    break;
                case 66:
                    break;
            }
        }

        public void NoteOff(int note)
        {
        }

        public void Knob(int knobNumber, float value)
        {
            switch(knobNumber)
            {
                case 1:
                    level = Mathf.FloorToInt(value * 4);
                    break;
            }
        }

        public override void OnOSC(string address, List<object> data)
        {
            base.OnOSC(address, data);

            switch(address)
            {
                case "/voxel/particle/mode":
                    PMode = (ParticleMode)OSCUtils.GetIValue(data);
                    break;

                case "/voxel/control/mode":
                    VMode = (VoxelMode)OSCUtils.GetIValue(data);
                    break;

                case "/voxel/flow":
                    FlowRandom();
                    break;

                case "/voxel/flow/throttle":
                    flowRandomThrottle = OSCUtils.GetFValue(data);
                    break;
            }
        }

        protected override void React(int index, bool on)
        {
        }

    }

}


