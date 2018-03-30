using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.PostProcessing;

namespace VJ.Channel18
{

    public class CameraController : MonoBehaviour, IOSCReactable, INanoKontrollable {

        [SerializeField] new protected Camera camera;
        [SerializeField] protected PostProcessingProfile profile;
        [SerializeField] protected CameraTarget target;
        [SerializeField] protected float floorHeight = 1f;
        [SerializeField] protected PolarCoordinate polar;
        [SerializeField] protected Vector3 offset;
        [SerializeField, Range(0f, 5f)] protected float speed = 1f;
        [SerializeField, Range(0f, 5f)] protected float speedMin = 0.1f, speedMax = 2f;

        [SerializeField, Range(0f, 1f)] protected float _distance = 0.5f;
        [SerializeField] protected float distanceMin = -20f, distanceMax = 50f;

        [SerializeField, Range(0f, 1f)] protected float _angle = 0.5f;
        [SerializeField] protected float angleMin = -Mathf.PI * 0.25f, angleMax = Mathf.PI * 0.25f;

        [SerializeField] protected NoiseGen distanceNoiseGen, angleNoiseGen;

        protected float distance, angle;

        [SerializeField] protected bool polarDirection;

        #region Monobehaviour functions

        void Start () {
            distance = _distance;
            angle = _angle;
        }
        
        void Update () {
            var dt = Time.deltaTime;
            var dtt = dt * speed * (polarDirection ? 1f : -1f);
            polar.Horizontal(dtt);
            Apply(1f);

            angle = Mathf.Lerp(angle, _angle, dt);
            distance = Mathf.Lerp(distance, _distance, dt);
        }

        void OnDrawGizmos ()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(new Vector3(0f, floorHeight, 0f), new Vector3(1000f, 1f, 1000f));
        }

        #endregion

        protected void Apply(float dt)
        {
            var ang = Mathf.Lerp(angleMin, angleMax, angle) + angleNoiseGen.Value(0f, Time.timeSinceLevelLoad);
            var ct = polar.Cartesian(target.Distance + distanceNoiseGen.Value(Time.timeSinceLevelLoad, 0f) + Mathf.Lerp(distanceMin, distanceMax, distance), ang);
            var to = ct + target.transform.position + offset;
            to.y = Mathf.Max(to.y, floorHeight);
            camera.transform.position = Vector3.Lerp(camera.transform.position, to, dt);
            Look();
        }

        protected void Look()
        {
            camera.transform.LookAt(target.transform.position);
            var settings = profile.depthOfField.settings;
            settings.focusDistance = (camera.transform.position - target.transform.position).magnitude;
            profile.depthOfField.settings = settings;
        }

        public void Randomize()
        {
            speed = Mathf.Lerp(speedMin, speedMax, Random.value);
            polarDirection = !polarDirection;
            polar.Move(Random.Range(0f, Mathf.PI * 0.5f), Random.Range(0f, Mathf.PI * 2f));
        }

        public void OnOSC(string address, List<object> data)
        {

            switch(address)
            {
                case "/camera/polar/randomize":
                    Randomize();
                    break;
            }

        }

        public void NoteOn(int note)
        {
            switch(note)
            {
                case 39:
                    Randomize();
                    break;
                case 55:
                    target.Increment();
                    break;
                case 71:
                    target.Decrement();
                    break;
            }
        }

        public void NoteOff(int note)
        {
        }

        public void Knob(int knobNumber, float knobValue)
        {
            switch(knobNumber)
            {
                case 7:
                    // update distance
                    _distance = Mathf.Clamp01(1f - knobValue);
                    break;

                case 23:
                    // update angle
                    _angle = knobValue;
                    break;
            }
        }

    }

}


