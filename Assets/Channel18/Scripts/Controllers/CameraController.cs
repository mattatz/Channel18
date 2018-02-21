using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class CameraController : MonoBehaviour {

        [SerializeField] protected Transform center;
        [SerializeField] protected PolarCoordinate polar;
        [SerializeField] protected Vector3 offset;

        void Start () {
        }
        
        void Update () {
            Apply();
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;
            polar.Horizontal(dt);
        }

        void Apply()
        {
            var ct = polar.Cartesian;
            transform.position = ct + offset;
            transform.LookAt(center.position);
        }

    }

}


