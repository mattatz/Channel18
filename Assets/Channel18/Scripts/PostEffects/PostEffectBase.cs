using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    [RequireComponent (typeof(Camera))]
    public class PostEffectBase : MonoBehaviour {

        [SerializeField] protected Material material;

        protected virtual void Start () {
        }
        
        protected virtual void Update () {
        }

        protected virtual void OnRenderImage (RenderTexture src, RenderTexture dst)
        {
            Graphics.Blit(src, dst, material);
        }

    }

}


