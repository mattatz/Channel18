using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VJ.Channel18 {

    public class BlockNoise : PostEffectBase, INanoKontrollable
    {
        [SerializeField, Range(0f, 1f)] protected float t = 0f;

        protected void Update()
        {
            material.SetFloat("_T", t);
        }

        public void NoteOff(int note)
        {
        }

        public void NoteOn(int note)
        {
        }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 6:
                    t = knobValue;
                    break;
            }
        }

        protected override void React(int index, bool on)
        {
        }

        public override void OnOSC(string addr, List<object> data)
        {
            base.OnOSC(addr, data);
            switch(addr)
            {
                case "/posteffects/block_noise":
                    t = OSCUtils.GetBoolFlag(data, 0) ? 1f : 0f;
                    break;

                case "/posteffects/block_noise/shift":
                    break;

                case "/posteffects/block_noise/speed":
                    break;
            }
        }

    }

}