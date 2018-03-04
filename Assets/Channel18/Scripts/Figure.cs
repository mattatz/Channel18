using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ.Channel18
{

    public enum FigureMotion
    {
        Idle,
        Samba,
        HipHop,
        Silly,
        Walking
    };

    public class Figure : MonoBehaviour, INanoKontrollable {

        public FigureMotion Motion { get { return motion; } }

        [SerializeField] protected Animator animator;
        [SerializeField] protected FigureMotion motion = FigureMotion.Idle;

        Array motions;

        void Start () {
            Trigger(motion);
            motions = Enum.GetValues(typeof(FigureMotion));
        }

        public void Trigger(FigureMotion m)
        {
            if (m == motion) return;

            var key = Enum.GetName(typeof(FigureMotion), (int)m);
            animator.SetTrigger(key);
            motion = m;
        }

        public void NoteOn(int note) { }

        public void NoteOff(int note) { }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 17:
                    var index = Mathf.Clamp(Mathf.FloorToInt(motions.Length * knobValue), 0, motions.Length - 1);
                    Enum e = Enum.Parse(typeof(FigureMotion), motions.GetValue(index).ToString()) as Enum;
                    int x = Convert.ToInt32(e);
                    Trigger((FigureMotion)x);
                    break;
            }
        }
        
    }

}


