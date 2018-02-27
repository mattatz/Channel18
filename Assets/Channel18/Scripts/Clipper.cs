using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class Clipper : MonoBehaviour {

        protected enum ClipDirection {
            Min, Max
        };

        protected enum ClipAxis {
            X, Y, Z
        };

        [SerializeField] protected VoxelParticleSystem system;
        [SerializeField] protected ClipDirection direction = ClipDirection.Max;
        [SerializeField] protected FrameDrawer frame;
        [SerializeField] protected ClipAxis axis = ClipAxis.Y;
        public float t = 0f;

        void Start ()
        {
            var bb = system.BaseClipBounds;
            switch(axis)
            {
                case ClipAxis.X:
                    transform.localRotation = Quaternion.AngleAxis(90f, Vector3.forward);
                    frame.Width = bb.size.y;
                    frame.Height = bb.size.z;
                    break;
                case ClipAxis.Y:
                    frame.Width = bb.size.x;
                    frame.Height = bb.size.z;
                    break;
                case ClipAxis.Z:
                    transform.localRotation = Quaternion.AngleAxis(90f, Vector3.right);
                    frame.Width = bb.size.x;
                    frame.Height = bb.size.y;
                    break;
            }
        }
        
        void Update () {
            switch(direction)
            {
                case ClipDirection.Min:
                    ClipMin();
                    break;
                case ClipDirection.Max:
                    ClipMax();
                    break;
            }
        }

        void ClipMin()
        {
            var bounds = system.ClipBounds;
            bounds.SetMinMax(Clip(bounds.min), bounds.max);
            system.ClipBounds = bounds;
        }

        void ClipMax()
        {
            var bounds = system.ClipBounds;
            bounds.SetMinMax(bounds.min, Clip(bounds.max));
            system.ClipBounds = bounds;
        }

        Vector3 Clip(Vector3 v)
        {
            var bb = system.BaseClipBounds;
            switch(axis)
            {
                case ClipAxis.X:
                    var x = Mathf.Lerp(bb.min.x, bb.max.x, t);
                    v.x = x;
                    transform.position = system.transform.TransformPoint(new Vector3(x, bb.center.y, bb.center.z));
                    break;
                case ClipAxis.Y:
                    var y = Mathf.Lerp(bb.min.y, bb.max.y, t);
                    v.y = y;
                    transform.position = system.transform.TransformPoint(new Vector3(bb.center.x, y, bb.center.z));
                    break;
                case ClipAxis.Z:
                    var z = Mathf.Lerp(bb.min.z, bb.max.z, t);
                    v.z = z;
                    transform.position = system.transform.TransformPoint(new Vector3(bb.center.x, bb.center.y, z));
                    break;
            }
            return v;
        }

    }

}


