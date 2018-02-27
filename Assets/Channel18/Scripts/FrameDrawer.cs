using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VJ.Channel18
{

    public class FrameDrawer : MonoBehaviour {

        #region Accessors

        public float Width
        {
            get {
                return width;
            }
            set {
                if(width != value)
                {
                    width = value;
                    Rebuild();
                }
            }
        }

        public float Height
        {
            get {
                return height;
            }
            set {
                if(height != value)
                {
                    height = value;
                    Rebuild();
                }
            }
        }

        #endregion

        [SerializeField] protected Material lineMat;

        [SerializeField] protected float width = 1f, height = 1f;

        void Start() {
            Rebuild();
        }

        void Rebuild()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            if(mesh != null)
            {
                Destroy(mesh);
            }
            GetComponent<MeshFilter>().sharedMesh = Build();
        }

        Mesh Build()
        {
            var hw = Width * 0.5f;
            var hh = Height * 0.5f;

            Vector3
                v0 = new Vector3(-hw, 0f, -hh),
                v1 = new Vector3(-hw, 0f,  hh),
                v2 = new Vector3( hw, 0f,  hh),
                v3 = new Vector3( hw, 0f, -hh);

            var mesh = new Mesh();
            mesh.vertices = (new Vector3[4] { v0, v1, v2, v3 });
            mesh.SetIndices(new int[8] { 0, 1, 1, 2, 2, 3, 3, 0 }, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnRenderObject ()
        {
            // RenderFrame();
        }

        protected void RenderFrame()
        {
            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            lineMat.SetPass(0);

            GL.Begin(GL.LINES);

            var hw = Width * 0.5f;
            var hh = Height * 0.5f;

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

            var hw = Width * 0.5f;
            var hh = Height * 0.5f;

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


