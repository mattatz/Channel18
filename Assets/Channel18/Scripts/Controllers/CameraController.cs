using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.PostProcessing;

namespace VJ.Channel18
{

    public class CameraController : MonoBehaviour {

        [SerializeField] protected PostProcessingProfile profile;
        [SerializeField] protected CameraTarget target;
        [SerializeField] protected PolarCoordinate polar;
        [SerializeField] protected Vector3 offset;
        [SerializeField, Range(0f, 5f)] protected float speed = 1f;

        void Start () {
        }
        
        void Update () {
            var dt = Time.deltaTime;
            Apply(dt);
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime * speed;
            polar.Horizontal(dt);
        }

        void Apply(float dt)
        {
            var ct = polar.Cartesian(target.Distance);
            var to = ct + target.transform.position + offset;
            transform.position = Vector3.Lerp(transform.position, to, dt);
            transform.LookAt(target.transform.position);
            var settings = profile.depthOfField.settings;
            settings.focusDistance = (transform.position - target.transform.position).magnitude;
            profile.depthOfField.settings = settings;
        }

    }

}


