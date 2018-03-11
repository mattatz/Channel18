using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ {

    public class OSCUtils {

        public static int GetIValue(List<object> data, int index = 0, int def = 0)
        {
            if(data.Count <= index) {
                return def;
            }

            int result;
            if(int.TryParse(data[index].ToString(), out result)) {
                return result;
            }
            return def;
        }

        public static bool GetBoolFlag(List<object> data, int index = 0, bool def = false)
        {
            if(data.Count <= index) {
                return def;
            }

            int flag;
            if(int.TryParse(data[index].ToString(), out flag)) {
                return flag == 1;
            }
            return def;
        }

    }

}


