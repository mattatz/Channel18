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
            if(GUILayout.Button("Clip")) mono.Clip();
            if (GUILayout.Button("BigOne")) mono.BigOne();
            if (GUILayout.Button("Grid")) mono.Grid(Random.Range(0, 64));
            if (GUILayout.Button("Randomize")) mono.Randomize();

        }

    }

}


