﻿using System.Collections;
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

    public class CameraTarget : MonoBehaviour, IOSCReactable {

        public Vector3 Position
        {
            get { return locations[current].Position; }
        }

        public float Distance
        {
            get { return distance; }
        }

        [SerializeField] List<CameraTargetLocation> locations;
        [SerializeField] protected int current;
        [SerializeField] protected float distance;

        #region Monobehaviour functions

        protected void Start () {
            current = Mathf.Clamp(current, 0, locations.Count - 1);
            var location = locations[current];
            transform.position = location.Position;
            distance = location.Distance;
        }
        
        protected void Update () {
            var dt = Time.deltaTime;
            Apply(dt);
        }

        #endregion

        protected void Apply(float dt)
        {
            current = Mathf.Clamp(current, 0, locations.Count - 1);
            var location = locations[current];
            var p = Vector3.Lerp(transform.position, location.Position, dt);
            transform.position = p;
            distance = Mathf.Lerp(distance, location.Distance, dt);
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

        public void Increment ()
        {
            current = (current + 1) % locations.Count;
        }

        public void Decrement()
        {
            if(current - 1 < 0)
            {
                current = locations.Count - 1;
            } else
            {
                current = (current - 1);
            }
        }

        public void OnOSC(string address, List<object> data)
        {
            switch(address)
            {
                case "/camera/target/index":
                    current = OSCUtils.GetIValue(data, 0);
                    Apply(OSCUtils.GetFValue(data, 1));
                    break;
            }
        }

    }

}


