using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

namespace VJ.Channel18
{

    public class Inverse : PostEffectBase, INanoKontrollable {

        [SerializeField, Range(0f, 1f)] protected float t = 0f;
        [SerializeField] protected float speed = 10f;
        protected float _t;

        #region Monobehaviour functions

        protected void Start()
        {
            _t = t;
        }

        protected void Update()
        {
            _t = Mathf.Lerp(_t, t, Time.deltaTime * speed);
            material.SetFloat("_T", _t);
        }

        #endregion

        public override void OnOSC(string address, List<object> data)
        {
            base.OnOSC(address, data);
            switch(address)
            {
                case "/posteffects/inverse":
                    break;

                case "/posteffects/inverse/toggle":
                    t = Mathf.Clamp01(1f - t);
                    break;
            }
        }

        protected override void React(int index, bool on) {
            switch(index)
            {
                case 7:
                    // material.SetFloat("_T", on ? 1f : 0f);
                    t = on ? 1f : 0f;
                    break;
            }
        }

        public void Toggle()
        {
            t = 1f - Mathf.RoundToInt(t);
        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 38:
                    Toggle();
                    break;
            }
        }

        public void NoteOff(int note)
        {
        }

        public void Knob(int knobNumber, float knobValue)
        {
        }
    }

}


