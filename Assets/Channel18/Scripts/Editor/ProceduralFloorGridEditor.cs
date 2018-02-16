using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{


    [CustomEditor(typeof(ProceduralFloorGrid))]
    public class ProceduralFloorGridEditor : Editor {

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Noise"))
            {
                var floor = target as ProceduralFloorGrid;
                floor.Noise();
            }
        }

    }

}


