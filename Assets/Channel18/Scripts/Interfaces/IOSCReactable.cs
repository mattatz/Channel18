﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace VJ
{

    public interface IOSCReactable 
    {

        void OnOSC(string addresss, List<object> data);

    }

}


