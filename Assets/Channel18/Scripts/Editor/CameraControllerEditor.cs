using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{

    [CustomEditor (typeof(CameraController))]
    public class CameraControllerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Randomize"))
            {
                var controller = target as CameraController;
                controller.Randomize();
            }
        }

    }

}


