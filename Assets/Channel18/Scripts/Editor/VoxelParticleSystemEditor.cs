using System;
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
            var names = Enum.GetNames(typeof(ParticleMode));
            var selected = GUILayout.SelectionGrid((int)system.PMode, names, 2);
            if(selected != (int)system.PMode)
            {
                system.PMode = (ParticleMode)selected;
            }

        }

    }

}

