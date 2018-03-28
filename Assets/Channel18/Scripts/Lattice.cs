using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class Lattice : AudioReactor, INanoKontrollable {

        [SerializeField, Range(1, 512)] protected int width = 128, height = 64, depth = 128;
        [SerializeField, Range(0f, 1f)] protected float thickness = 0.25f, useLine = 1f;

        [SerializeField] protected Vector3 noiseScale = new Vector3(1f, 1f, 1f);
        [SerializeField] protected float baseNoiseSpeed = 1f, baseNoiseIntensity = 0.1f;
        [SerializeField, Range(0.0f, 2.0f)] protected float noiseSpeed = 1f;
        [SerializeField, Range(0.0f, 0.25f)] protected float noiseIntensity = 0.1f;
        protected float noiseOffset = 0f;

        [SerializeField] protected LatticeRenderer line, cuboid;

        [SerializeField] List<Material> latticeMaterials;

        protected float _useLine, _thickness, _noiseIntensity;
        protected Coroutine co;

        protected void Start () {
            var mesh = Build();
            line.Setup(mesh);
            cuboid.Setup(mesh);

            _thickness = thickness;
            _useLine = useLine;
            _noiseIntensity = noiseIntensity;
        }

        protected void Update()
        {
            line.SetThickness(_thickness * _useLine);
            cuboid.SetThickness(_thickness * (1f - _useLine));

            latticeMaterials.ForEach(m =>
            {
                m.SetVector("_NoiseScale", noiseScale);
                m.SetFloat("_NoiseOffset", noiseOffset);
                m.SetFloat("_NoiseIntensity", _noiseIntensity);
            });
        }

        protected void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime * 5f;
            _thickness = Mathf.Lerp(_thickness, thickness, dt);
            _useLine  = Mathf.Lerp(_useLine, useLine, dt);
            _noiseIntensity = Mathf.Lerp(_noiseIntensity, noiseIntensity, dt);
            noiseOffset += dt * noiseSpeed;
        }

        #region Initialize

        protected Mesh Build()
        {
            var mesh = new Mesh();
            var count = width * height * depth;
            var vertices = new Vector3[count];

            var poffset = new Vector3(
                -(width - 1) * 0.5f, -(height - 1) * 0.5f, -(depth - 1) * 0.5f
            );

            var lineIndices = new List<int>();

            Action<int> addRightLine = (int c) => {
                lineIndices.Add(c);
                lineIndices.Add(c + 1);
            };

            Action<int> addTopLine = (int c) => {
                lineIndices.Add(c);
                lineIndices.Add(c + width);
            };

            Action<int> addForwardLine = (int c) => {
                lineIndices.Add(c);
                lineIndices.Add(c + (width * height));
            };

            for(int z = 0; z < depth; z++)
            {
                var zlast = z == (depth - 1);
                var zoffset = z * (width * height);
                for(int y = 0; y < height; y++)
                {
                    var ylast = y == (height - 1);
                    var yoffset = y * width;
                    for(int x = 0; x < width; x++)
                    {
                        var xlast = x == (width - 1);
                        var index = x + yoffset + zoffset;
                        vertices[index] = new Vector3(x, y, z) + poffset;
                        if (!xlast) {
                            addRightLine(index);
                        }
                        if (!ylast) {
                            addTopLine(index);
                        }
                        if (!zlast) {
                            addForwardLine(index);
                        }
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
            return mesh;
        }

        #endregion

        protected void Wave(float duration = 0.45f)
        {
            if(co != null) {
                StopCoroutine(co);
            }

            float duration0 = duration * 0.5f;
            float duration1 = duration * 0.5f;
            float speedMin = baseNoiseSpeed, speedMax = baseNoiseSpeed * 5f;
            float intensityMin = baseNoiseIntensity, intensityMax = baseNoiseIntensity * 1.1f;
            co = StartCoroutine(Easing.Ease(duration0, Easing.Exponential.Out, duration1, Easing.Linear, (float t) => {
                noiseSpeed = Mathf.Lerp(speedMin, speedMax, t);
                _noiseIntensity = noiseIntensity = Mathf.Lerp(intensityMin, intensityMax, t);
            }, 0f, 1f));
        }

        protected override void React(int index, bool on)
        {
        }

        public override void OnOSC(string addr, List<object> data)
        {
            base.OnOSC(addr, data);

            switch(addr)
            {
                case "/lattice/wave":
                    Wave();
                    break;

                case "/lattice/line":
                    break;
            }
        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 37:
                    Wave();
                    break;
                case 53:
                    break;
                case 69:
                    break;
            }
        }

        public void NoteOff(int note)
        {
        }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 5:
                    noiseIntensity = Mathf.Lerp(0f, 0.5f, knobValue);
                    break;
                case 21:
                    thickness = Mathf.Clamp01(knobValue);
                    break;
            }
        }
    }

}


