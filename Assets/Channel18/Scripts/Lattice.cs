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
        [SerializeField] List<Vector3> scales;
        [SerializeField] protected int iscale = 0;

        protected float _useLine, _thickness, _noiseIntensity;
        protected Coroutine waver, scaler;

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
            var dt = Time.fixedDeltaTime;
            _thickness = Mathf.Lerp(_thickness, thickness, dt * 3f);
            _useLine  = Mathf.Lerp(_useLine, useLine, dt * 5f);
            _noiseIntensity = Mathf.Lerp(_noiseIntensity, noiseIntensity, Mathf.Clamp01(dt * 50f));
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
            if(waver != null) {
                StopCoroutine(waver);
            }

            float duration0 = duration * 0.5f;
            float duration1 = duration * 0.5f;
            float speedMin = baseNoiseSpeed, speedMax = baseNoiseSpeed * 5f;
            float intensityMin = baseNoiseIntensity, intensityMax = baseNoiseIntensity * 1.25f;
            waver = StartCoroutine(Easing.Ease(duration0, Easing.Exponential.Out, duration1, Easing.Linear, (float t) => {
                noiseSpeed = Mathf.Lerp(speedMin, speedMax, t);
                _noiseIntensity = noiseIntensity = Mathf.Lerp(intensityMin, intensityMax, t);
            }, 0f, 1f));
        }

        protected void Scale(int index = -1, float duration = 1.0f)
        {
            if(scaler != null) {
                StopCoroutine(scaler);
                scaler = null;
            }
            scaler = StartCoroutine(IScale(scales[index < 0 ? Random.Range(0, scales.Count) : (index % scales.Count)], duration));
        }

        protected IEnumerator IScale(Vector3 to, float duration)
        {
            yield return 0;

            var from = transform.localScale;
            var time = 0f;
            while(time < duration)
            {
                yield return 0;
                time += Time.deltaTime;
                var t = time / duration;
                var et = Easing.Quadratic.Out(t);
                transform.localScale = Vector3.Lerp(from, to, et);
            }
            transform.localScale = to;
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
                    useLine = OSCUtils.GetIValue(data, 0);
                    break;

                case "/lattice/line/toggle":
                    useLine = Mathf.Clamp01(1f - useLine);
                    break;

                case "/lattice/scale":
                    iscale = OSCUtils.GetIValue(data, 0);
                    Scale(iscale);
                    break;
            }
        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 37:
                    useLine = Mathf.Clamp01(1f - useLine);
                    break;
                case 53:
                    Wave();
                    break;
                case 69:
                    Scale((++iscale));
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
                    noiseIntensity = Mathf.Lerp(0f, baseNoiseIntensity, knobValue);
                    break;
                case 21:
                    thickness = Mathf.Clamp01(knobValue);
                    break;
            }
        }
    }

}


