using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class Inverse : PostEffectBase {

        protected void Start()
        {
        }

        public override void OnOSC(string address, List<object> data)
        {
            base.OnOSC(address, data);
            switch(address)
            {
                case "/posteffects/inverse":
                    break;
            }
        }

        protected override void React(int index, bool on) {
            switch(index)
            {
                case 7:
                    material.SetFloat("_T", on ? 1f : 0f);
                    break;
            }
        }

    }

}


