using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace VJ.Channel18
{

    public class DemoController : MonoBehaviour {

        [SerializeField] protected CameraController cam;
        [SerializeField] protected ProceduralMidairGrid midair;
        [SerializeField] protected ProceduralFloorGrid floor;
        [SerializeField] protected VoxelParticleSystem voxel;

        [SerializeField] protected float cameraInterval = 5f, gridInterval = 0.75f, flowInterval = 2f;

        void Start () {
            StartCoroutine(IRepeater(cameraInterval, () => {
                cam.Randomize();
            }));

            StartCoroutine(IRepeater(gridInterval, () => {
                if(Random.value < 0.5f)
                {
                    midair.Rotate();
                } else {
                    midair.Scale();
                }
                if(Random.value < 0.5f)
                {
                    floor.Apply(FloorMode.Noise);
                } else
                {
                    floor.Apply(FloorMode.Circle);
                }
            }));

            StartCoroutine(IRepeater(flowInterval, () => {
                voxel.FlowRandom(Random.value);
            }));
        }

        protected IEnumerator IRepeater(float interval, Action callback)
        {
            yield return 0;
            while(true)
            {
                yield return new WaitForSeconds(interval);
                callback();
            }
        }

    }

}


