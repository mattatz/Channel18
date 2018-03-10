using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VJ.Channel18
{

    [System.Serializable]
    public class CameraTargetLocation
    {
        public string Label { get { return label; } }
        public Vector3 Position { get { return position; } }
        public float Distance {get { return distance; } }

        [SerializeField] protected string label;
        [SerializeField] protected Vector3 position;
        [SerializeField] protected float distance = 30f;
    }

    public class CameraTarget : MonoBehaviour {

        public Vector3 Position
        {
            get { return locations[current].Position; }
        }

        public float Distance
        {
            get { return locations[current].Distance + distanceNoiseGen.Value(Time.timeSinceLevelLoad, 0f); }
        }

        [SerializeField] List<CameraTargetLocation> locations;
        [SerializeField] protected int current;
        [SerializeField] protected NoiseGen distanceNoiseGen;

        void Start () {
        }
        
        void Update () {
            current = Mathf.Clamp(current, 0, locations.Count - 1);
            var location = locations[current];
            var p = Vector3.Lerp(transform.position, location.Position, Time.deltaTime);
            transform.position = p;
        }

        protected void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            locations.ForEach(loc => {
#if UNITY_EDITOR
                Handles.Label(loc.Position, loc.Label);
#endif
                Gizmos.DrawWireSphere(loc.Position, 1f);
            });
        }

    }

}


