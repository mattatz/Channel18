using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{

    [CustomEditor(typeof(VoxelParticleSystem))]
    public class VoxelParticleSystemEditor : Editor {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var system = target as VoxelParticleSystem;
            if(GUILayout.Button("Randomize"))
            {
                system.Randomize();
            } else if(GUILayout.Button("Glitch"))
            {
                system.Glitch();
            }
        }

    }

}

