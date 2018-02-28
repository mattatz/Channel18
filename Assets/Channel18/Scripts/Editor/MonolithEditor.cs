using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{

    [CustomEditor(typeof(Monolith))]
    public class MonolithEditor : Editor {

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            var mono = target as Monolith;
            if(GUILayout.Button("Clip"))
            {
                mono.Clip();
            } else if(GUILayout.Button("Randomize"))
            {
                mono.Randomize();
            }

        }

    }

}


