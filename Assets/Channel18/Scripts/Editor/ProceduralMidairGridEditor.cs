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

            if(GUILayout.Button("Init"))
            {
                var grid = target as ProceduralMidairGrid;
                grid.Init();
            } else if(GUILayout.Button("Rotate"))
            {
                var grid = target as ProceduralMidairGrid;
                grid.Rotate();
            }
        }

    }

}


