
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ.Channel18
{

    public class Mirror : PostEffectBase {

        public void OnOSC(string address, List<object> data)
        {
            switch(address)
            {
                case "/posteffects/mirror":
                    // data[0].ToString();
                    break;
            }
        }

    }

}


