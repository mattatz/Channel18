using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class LatticeRenderer : MonoBehaviour {

        [SerializeField, Range(0f, 1f)] protected float thickness = 1f;

        protected new Renderer renderer;
        protected MaterialPropertyBlock block;

        protected void Start () {
            renderer = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
        }
        
        protected void Update () {
            block.SetFloat("_Thickness", thickness);
            renderer.SetPropertyBlock(block);
        }

        public void Setup(Mesh mesh)
        {
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        public void SetThickness(float thickness)
        {
            this.thickness = thickness;
        }

        public void SetAxis(Vector3 axis)
        {
            block.SetVector("_Axis", axis);
        }

    }

}


