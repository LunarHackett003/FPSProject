using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorHelpers
{
    public static Vector3 SmoothDampAngle(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime)
    {
        return new Vector3()
        {
            x = Mathf.SmoothDampAngle(current.x, target.x, ref velocity.x, smoothTime),
            y = Mathf.SmoothDampAngle(current.y, target.y, ref velocity.y, smoothTime),
            z = Mathf.SmoothDampAngle(current.z, target.z, ref velocity.z, smoothTime)
        };
    }
    public static float Spring(float from, float to, float time)
    {
        time = Mathf.Clamp01(time);
        time = (Mathf.Sin(time * Mathf.PI * (.2f + 2.5f * time * time * time)) * Mathf.Pow(1f - time, 2.2f) + time) * (1f + (1.2f * (1f - time)));
        return from + (to - from) * time;
    }
    public static Vector3 Spring(Vector3 from, Vector3 to, float time)
    {
        return new Vector3(Spring(from.x, to.x, time), Spring(from.y, to.y, time), Spring(from.z, to.z, time));
    }
}
