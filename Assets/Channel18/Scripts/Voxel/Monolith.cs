using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class Monolith : AudioReactor {

        [SerializeField] protected VoxelParticleSystem system;
        [SerializeField, Range(0f, 1f)] protected float limit = 0.15f;
        [SerializeField, Range(0f, 1f)] protected float minX = 0f, minY = 0f, minZ = 0f;
        [SerializeField, Range(0f, 1f)] protected float maxX = 1f, maxY = 1f, maxZ = 1f;

        #region Monobehaviour functions

        protected void Start () {
        }
        
        protected void Update () {
            Constrain();
        }

        #endregion

        public void Clip()
        {
            system.Clip(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        public void BigOne()
        {
            minX = 0.25f;
            maxX = 0.75f;
            minY = 0f;
            maxY = 1f;
            minZ = 0.25f;
            maxZ = 0.75f;
            Clip();
        }

        public void Grid(int index = 0, int width = 4, int height = 4, int depth = 4)
        {
            Debug.Log(index + "," + width + "," + height + "," + depth);
            int count = width * height * depth;
            var invW = 1f / width;
            var invH = 1f / height;
            var invD = 1f / depth;
            index = index % count;
            int z = index / (width * height);
            int rem = index % (width * height);
            int y = rem / width;
            int x = rem % width;
            minX = x * invW;
            maxX = minX + invW;
            minY = y * invH;
            maxY = minY + invH;
            minZ = z * invD;
            maxZ = minZ + invD;
            Clip();
        }

        public void Randomize()
        {
            minX = Random.value;
            maxX = minX + limit + Random.value * (1f - minX - limit);
            minY = Random.value;
            maxY = minY + limit + Random.value * (1f - minY - limit);
            minZ = Random.value;
            maxZ = minZ + limit + Random.value * (1f - minZ - limit);
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

            switch(address)
            {
                case "/monolith/big":
                    BigOne();
                    break;

                case "/monolith/grid":
                    Grid(
                        OSCUtils.GetIValue(data, 0, Random.Range(0, 64)),
                        OSCUtils.GetIValue(data, 1, 4),
                        OSCUtils.GetIValue(data, 2, 4),
                        OSCUtils.GetIValue(data, 3, 4)
                    );
                    break;

                case "/monolith/randomize":
                    Randomize();
                    break;

                case "/monolith/":
                    break;
            }
        }

        protected override void React(int index, bool on)
        {
        }

    }

}


