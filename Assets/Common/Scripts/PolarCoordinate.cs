using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PolarCoordinate {

    [SerializeField, Range(0f, 6.28f)] protected float theta0, theta1;

    public PolarCoordinate(float t0, float t1)
    {
        theta0 = t0;
        theta1 = t1;
    }

    public void Vertical(float dt)
    {
        theta0 += dt;
    }

    public void Horizontal(float dt)
    {
        theta1 += dt;
    }

    public Vector3 Cartesian(float radius)
    {
        return new Vector3(
            -radius * Mathf.Cos(theta0) * Mathf.Cos(theta1),
             radius * Mathf.Sin(theta0),
             radius * Mathf.Cos(theta0) * Mathf.Sin(theta1)
        );
    }



}
