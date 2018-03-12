using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class Background : PostEffectBase {

        [SerializeField] protected GradientTextureGen gradient;

        protected void Start ()
        {
            var grad = gradient.Create(128, 1, TextureWrapMode.Mirror);
            material.SetTexture("_Gradient", grad);
        }

        protected override void React(int index, bool on)
        {
        }

    }

}


