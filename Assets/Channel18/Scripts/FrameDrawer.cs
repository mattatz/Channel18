using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class FrameDrawer : MonoBehaviour {

        [SerializeField] protected Material lineMat;
        public float width = 1f, height = 1f;

        void OnRenderObject ()
        {
            RenderFrame();
        }

        protected void RenderFrame()
        {
            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            lineMat.SetPass(0);

            GL.Begin(GL.LINES);

            var hw = width * 0.5f;
            var hh = height * 0.5f;

            Vector3
                v0 = new Vector3(-hw, 0f, -hh),
                v1 = new Vector3(-hw, 0f,  hh),
                v2 = new Vector3( hw, 0f,  hh),
                v3 = new Vector3( hw, 0f, -hh);
            
            GL.Vertex(v0); GL.Vertex(v1);
            GL.Vertex(v1); GL.Vertex(v2);
            GL.Vertex(v2); GL.Vertex(v3);
            GL.Vertex(v3); GL.Vertex(v0);

            GL.End();
            GL.PopMatrix();
        }

        protected void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.matrix = transform.localToWorldMatrix;

            var hw = width * 0.5f;
            var hh = height * 0.5f;

            Vector3
                v0 = new Vector3(-hw, 0f, -hh),
                v1 = new Vector3(-hw, 0f,  hh),
                v2 = new Vector3( hw, 0f,  hh),
                v3 = new Vector3( hw, 0f, -hh);

            Gizmos.DrawLine(v0, v1);
            Gizmos.DrawLine(v1, v2);
            Gizmos.DrawLine(v2, v3);
            Gizmos.DrawLine(v3, v0);
        }
    
    }

}


