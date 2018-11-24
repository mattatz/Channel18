using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VJ.Channel18
{

    [StructLayout(LayoutKind.Sequential)]
    public struct VoxelParticle_t {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 velocity;
        public Color color;
        public float speed;
        public float lifetime;
        public uint flow;
    }

}


