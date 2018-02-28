using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class Monolith : MonoBehaviour {

        [SerializeField] protected VoxelParticleSystem system;
        [SerializeField, Range(0f, 1f)] protected float minX = 0f, minY = 0f, minZ = 0f;
        [SerializeField, Range(0f, 1f)] protected float maxX = 1f, maxY = 1f, maxZ = 1f;

        void Start () {
        }
        
        void Update () {
            Constrain();
            Randomize();
        }

        public void Clip()
        {
            system.Clip(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        public void Randomize()
        {
            minX += (Random.value - 0.5f) * 0.5f;
            maxX += (Random.value - 0.5f) * 0.5f;
            minY += (Random.value - 0.5f) * 0.5f;
            maxY += (Random.value - 0.5f) * 0.5f;
            minZ += (Random.value - 0.5f) * 0.5f;
            maxZ += (Random.value - 0.5f) * 0.5f;
            Constrain();
            Clip();
        }

        void Constrain()
        {
            minX = Mathf.Clamp01(minX);
            minY = Mathf.Clamp01(minY);
            minZ = Mathf.Clamp01(minZ);
            maxX = Mathf.Clamp01(maxX);
            maxY = Mathf.Clamp01(maxY);
            maxZ = Mathf.Clamp01(maxZ);

            minX = Mathf.Min(minX, maxX);
            minY = Mathf.Min(minY, maxY);
            minZ = Mathf.Min(minZ, maxZ);
        }

    }

}


