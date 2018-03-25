
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ.Channel18
{

    public class Mirror : PostEffectBase, INanoKontrollable {

        [SerializeField] protected bool horizontal, vertical;
        [SerializeField] protected bool left, up;

        protected void Start()
        {
            Apply();
        }

        protected void Apply()
        {
            material.SetFloat("_Horizontal", horizontal ? 1f : 0f);
            material.SetFloat("_Left", left ? 1f : 0f);
            material.SetFloat("_Vertical", vertical ? 1f : 0f);
            material.SetFloat("_Up", up ? 1f : 0f);
        }

        public override void OnOSC(string address, List<object> data)
        {
            base.OnOSC(address, data);
            switch(address)
            {
                case "/posteffects/mirror/horizontal":
                    horizontal = OSCUtils.GetBoolFlag(data, 0);
                    left = OSCUtils.GetBoolFlag(data, 1);
                    Apply();
                    break;
                case "/posteffects/mirror/vertical":
                    vertical = OSCUtils.GetBoolFlag(data, 0);
                    up = OSCUtils.GetBoolFlag(data, 1);
                    Apply();
                    break;
            }
        }

        protected override void React(int index, bool on)
        {
        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 54:
                    horizontal = !horizontal;
                    Apply();
                    break;
                case 70:
                    vertical = !vertical;
                    Apply();
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


