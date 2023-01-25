using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class Vector3Utils
{
    public static Vector3 Rand(float max = 50)
    {
        float x = Randomizer.Rand(-max, max);
        float y = Randomizer.Rand(-max, max);
        float z = Randomizer.Rand(-max, max);
        return new Vector3(x, y, z);
    }

    public static Vector3 RandomPositionBetween(Vector3 min, Vector3 max)
    {
        Vector3 randomPosition = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
        return randomPosition;
    }

    public static Vector3 Average(Vector3[] vector3s)
    {
        Vector3 averageVector = Vector2.zero;
        foreach (Vector3 vector3 in vector3s)
        {
            averageVector += vector3;
        }
        averageVector /= vector3s.Length;
        return averageVector;
    }

    public static float GetTrajectoryDistance(Vector3[] traj)
    {
        float result = 0;
        for (int i = 0; i < traj.Length - 1; ++i)
        {
            result += Vector3.Distance(traj[i], traj[i + 1]);
        }
        return result;
    }

    public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness) // Bezier Algorithm
    {
        List<Vector3> points;
        List<Vector3> curvedPoints;
        int pointsLength = 0;
        int curvedLength = 0;

        smoothness = Mathf.Clamp(smoothness, 1, 2000);

        pointsLength = arrayToCurve.Length;

        curvedLength = (pointsLength * Mathf.RoundToInt(smoothness)) - 1;
        //Debug.LogError(curvedLength);
        curvedPoints = new List<Vector3>();

        float t = 0.0f;
        for (int p = 0; p < curvedLength + 1; p++)
        {
            t = Mathf.InverseLerp(0, curvedLength, p);

            points = new List<Vector3>(arrayToCurve);

            for (int j = pointsLength - 1; j > 0; j--)
            {
                for (int i = 0; i < j; i++)
                {
                    points[i] = (1 - t) * points[i] + t * points[i + 1];
                }
            }

            curvedPoints.Add(points[0]);
        }

        return (curvedPoints.ToArray());
    }

    public static Vector3[] TranslateArray(Vector3[] arrayIn, Vector3 translationVector)
    {
        for (int i = 0; i < arrayIn.Length; ++i)
        {
            arrayIn[i] += translationVector;
        }
        return arrayIn;
    }


    public static Vector3[] RotateArray(Vector3[] input, Vector3 axis, float angle)
    {
        Vector3[] result = input;
        for (int i = 0; i < result.Length; ++i)
        {
            result[i] = RotateVector(result[i], axis, angle);
        }
        return result;
    }

    public static Vector3 RotateVector(Vector3 toRotate, Vector3 axis, float angle)
    {
        Vector3 origin = Vector3.zero;
        Vector3 dir = toRotate - origin;
        dir = Quaternion.AngleAxis(angle, axis) * dir;
        Vector3 result = dir + origin;
        return result;
    }

    public static NativeArray<Vector3> ToNativeArray(this Vector3[] array)
    {
        NativeArray<Vector3> output = new NativeArray<Vector3>(array.Length, Allocator.TempJob);
        for (int i = 0; i < array.Length; ++i)
        {
            output[i] = array[i];
        }
        return output;
    }
    public static Vector3 EvaluatePositionRound(Vector3[] pos, float time)
    {
        if (time > 1)
            time = 1;

        int position = 0;
        float result = time * pos.Length;
        position = Mathf.RoundToInt(result);
        if (position >= pos.Length)
            position = pos.Length - 1;

        return pos[position];
    }

    public static Vector3 EvaluatePositionLerp(Vector3[] pos, float time)
    {
        if (time > 1)
            time = 1;

        int position = 0;
        float result = time * pos.Length;
        position = Mathf.RoundToInt(result);
        float rest = result - position;

        if (position >= pos.Length)
            position = pos.Length - 1;

        Vector3 _pos = pos[position];
        if (position < pos.Length - 1)
        {
            _pos = Vector3.Lerp(_pos, pos[position + 1], rest);
        }

        return _pos;
    }

    public static Vector3[] getBezierParabola(Vector3 start, Vector3 direction, float distance, float ratio, float zAngle, float smoothness)
    {
        return GetBezierParabola(start, start + (direction * distance), ratio, zAngle, smoothness);
    }

    public static Vector3[] GetBezierParabola(Vector3 start, Vector3 end, float ratio, float zAngle, float smoothness)
    {
        Vector3 dir = end - start;
        Vector3 middlePoint = dir / 2f + start;
        Vector3 axle = middlePoint + Vector3.up * (dir.magnitude * ratio);

        axle = RotateVectorAroundAxis(axle, dir, zAngle);

        Vector3[] positions = new Vector3[3]
        {
            start, axle, end
        };

        return Vector3Utils.MakeSmoothCurve(positions, smoothness * dir.magnitude * (1 + ratio));
    }

    public static Vector3 RotateVectorAroundAxis(Vector3 toRotate, Vector3 axis, float angle)
    {
        Vector3 origin = new Vector3(toRotate.x, 0, toRotate.z);
        Vector3 dir = toRotate - origin;
        dir = Quaternion.AngleAxis(angle, axis) * dir;
        Vector3 result = dir + origin;
        return result;
    }


    public static Vector3[] SplitLine(Vector3 from, Vector3 to, float stepSize = 0.05f)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        dir *= stepSize;

        int splitLenght = Mathf.RoundToInt(Vector3.Distance(from, to) / stepSize);
        Vector3[] points = new Vector3[splitLenght];
        for (int i = 0; i < splitLenght; ++i)
        {
            points[i] = i * dir + from;
        }
        return points;
    }

    public static Vector3 GetBarycenter(List<Vector3> positions)
    {
        Vector3 barycenter = Vector3.zero;

        for (int i = 0; i < positions.Count; ++i)
        {
            barycenter += positions[i];
        }

        barycenter /= positions.Count;

        return barycenter;
    }

}

