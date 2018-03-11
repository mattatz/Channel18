using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class Monolith : AudioReactor {

        [SerializeField] protected VoxelParticleSystem system;
        [SerializeField, Range(0f, 1f)] protected float minX = 0f, minY = 0f, minZ = 0f;
        [SerializeField, Range(0f, 1f)] protected float maxX = 1f, maxY = 1f, maxZ = 1f;
        [SerializeField] protected bool useRandom = false;
        [SerializeField] protected int randomFreq = 10;

        void Start () {
        }
        
        void Update () {
            Constrain();

            if(useRandom && Time.frameCount % Mathf.Max(1, randomFreq) == 0)
            {
                Randomize();
            }
        }

        public void Clip()
        {
            system.Clip(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        public void Randomize()
        {
            minX += (Random.value - 0.5f) * 0.5f;
            maxX = minX + Random.value * (1f - minX);
            minY += (Random.value - 0.5f) * 0.5f;
            maxY = minY + Random.value * (1f - minY);
            minZ += (Random.value - 0.5f) * 0.5f;
            maxZ = minZ + Random.value * (1f - minZ);
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

        public override void OnOSC(string address, List<object> data)
        {
            base.OnOSC(address, data);
        }

        protected override void React(int index, bool on)
        {
        }

    }

}


