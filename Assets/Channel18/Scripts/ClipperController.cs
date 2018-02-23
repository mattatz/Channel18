using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    [System.Serializable]
    public class ClipperPair
    {
        [SerializeField] protected bool enabled;
        [SerializeField] protected Clipper min, max;

        public void Update ()
        {
            min.gameObject.SetActive(enabled);
            max.gameObject.SetActive(enabled);
            Constrain();
        }

        public void Constrain()
        {
            min.t = Mathf.Min(min.t, max.t);
            max.t = Mathf.Max(min.t, max.t);
        }
    };

    public class ClipperController : MonoBehaviour {

        [SerializeField] protected ClipperPair x, y, z;

        void Start () {
        }
        
        void Update () {
            x.Update();
            y.Update();
            z.Update();
        }

    }

}


