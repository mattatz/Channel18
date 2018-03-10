using System;
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

            foreach(FloorMode mode in Enum.GetValues(typeof(FloorMode)))
            {
                if(GUILayout.Button(mode.ToString()))
                {
                    var floor = target as ProceduralFloorGrid;
                    floor.Apply(mode);
                }
            }
        }

    }

}


