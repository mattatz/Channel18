using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class Sculpture : MonoBehaviour {

        protected new Renderer renderer;
        protected MaterialPropertyBlock block;

        void Start () {
            renderer = GetComponent<Renderer>();

            block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
        }
        
        void Update () {
            renderer.SetPropertyBlock(block);
        }

    }

}


