using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{

    [CustomEditor(typeof(ProceduralMidairGrid))]
    public class ProceduralMidairGridEditor : Editor {

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            var grid = target as ProceduralMidairGrid;
            if(GUILayout.Button("Init"))
            {
                grid.Init();
            } else if(GUILayout.Button("Rotate"))
            {
                grid.Rotate();
            } else if(GUILayout.Button("Scale"))
            {
                grid.Scale();
            }
        }

    }

}


