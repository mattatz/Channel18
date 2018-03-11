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

    public void Update(float dt)
    {
    }

    public void Move(float t0, float t1)
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

    public Vector3 Cartesian(float radius, float offset = 0f)
    {
        return new Vector3(
            -radius * Mathf.Cos(theta0 + offset) * Mathf.Cos(theta1),
             radius * Mathf.Sin(theta0 + offset),
             radius * Mathf.Cos(theta0 + offset) * Mathf.Sin(theta1)
        );
    }


}
