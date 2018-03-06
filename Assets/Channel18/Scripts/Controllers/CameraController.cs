using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class CameraController : MonoBehaviour {

        [SerializeField] protected Transform center;
        [SerializeField] protected PolarCoordinate polar;
        [SerializeField] protected Vector3 offset;
        [SerializeField, Range(0f, 5f)] protected float speed = 1f;

        void Start () {
        }
        
        void Update () {
            Apply();
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime * speed;
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


