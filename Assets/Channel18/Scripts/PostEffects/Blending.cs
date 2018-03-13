using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ.Channel18
{

    public class Blending : PostEffectBase {

        [SerializeField] protected GradientTextureGen gradientGen;

        void Start () {
            material.SetTexture("_Gradient", gradientGen.Create(128, 1, TextureWrapMode.Mirror));
        }
        
        void Update () {
        }

        protected override void React(int index, bool on)
        {
       }

    }

}


