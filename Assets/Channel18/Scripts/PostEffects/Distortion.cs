using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class Distortion : PostEffectBase, INanoKontrollable {

        [SerializeField, Range(0f, 1f)] protected float t = 0f;
        [SerializeField] protected float speed = 3f;
        protected float _t = 0f;

        protected void Start()
        {
            _t = t;
        }

        protected void Update()
        {
            var dt = Time.deltaTime * speed;
            _t = Mathf.Lerp(_t, t, Mathf.Clamp01(dt));
            material.SetFloat("_T", _t);
        }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 22:
                    t = knobValue;
                    break;
            }
        }

        public void NoteOff(int note)
        {
        }

        public void NoteOn(int note)
        {
        }

        protected override void React(int index, bool on)
        {
        }

    }

}


