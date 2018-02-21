using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PolarCoordinate {

    [SerializeField, Range(0f, 6.28f)] protected float theta0, theta1;
    [SerializeField] protected float radius = 30f;

    public Vector3 Cartesian
    {
        get {
            return new Vector3(
                -radius * Mathf.Cos(theta0) * Mathf.Cos(theta1),
                 radius * Mathf.Sin(theta0),
                 radius * Mathf.Cos(theta0) * Mathf.Sin(theta1)
            );
        }
    }

    public PolarCoordinate(float t0, float t1, float r)
    {
        theta0 = t0;
        theta1 = t1;
        radius = r;
    }

    public void Vertical(float dt)
    {
        theta0 += dt;
    }

    public void Horizontal(float dt)
    {
        theta1 += dt;
    }

}
