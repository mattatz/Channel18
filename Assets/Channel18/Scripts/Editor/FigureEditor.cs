using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VJ.Channel18
{

    [CustomEditor(typeof(Figure))]
    public class FigureEditor : Editor {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var figure = target as Figure;
            var names = Enum.GetNames(typeof(FigureMotion));
            var selected = GUILayout.SelectionGrid((int)figure.Motion, names, 2);
            if(selected != (int)figure.Motion)
            {
                figure.Trigger((FigureMotion)selected);
            }
        }

    }

}


