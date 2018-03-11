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
            var pNames = Enum.GetNames(typeof(ParticleMode));
            var pSelected = GUILayout.SelectionGrid((int)system.PMode, pNames, 2);
            if(pSelected != (int)system.PMode) {
                system.PMode = (ParticleMode)pSelected;
            }

            var vNames = Enum.GetNames(typeof(VoxelMode));
            var vSelected = GUILayout.SelectionGrid((int)system.VMode, vNames, 2);
            if(vSelected != (int)system.VMode) {
                system.VMode = (VoxelMode)vSelected;
            }

            if(GUILayout.Button("FlowRandom"))
            {
                system.FlowRandom();
            }

        }

    }

}

