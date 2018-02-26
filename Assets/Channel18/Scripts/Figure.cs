using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ.Channel18
{

    public enum FigureMotion
    {
        Idle, Samba, HipHop, Silly, Walking
    };

    public class Figure : MonoBehaviour {

        public FigureMotion Motion { get { return motion; } }

        [SerializeField] protected Animator animator;
        [SerializeField] protected FigureMotion motion = FigureMotion.Idle;

        void Start () {
            Trigger(motion);
        }
        
        void Update () {
        }

        public void Trigger(FigureMotion m)
        {
            var key = Enum.GetName(typeof(FigureMotion), (int)m);
            animator.SetTrigger(key);
            motion = m;
        }
        
    }

}


