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
        [SerializeField, Range(0f, 1f)] protected float tmin = 0f, tmax = 1f;

        public void Update ()
        {
            min.gameObject.SetActive(enabled);
            max.gameObject.SetActive(enabled);
            Constrain();
        }

        public void Constrain()
        {
            tmin = Mathf.Min(tmin, tmax);
            tmax = Mathf.Max(tmin, tmax);
            min.t = tmin;
            max.t = tmax;
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


