using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class RaycastManager
{    
    public static bool SphereCastTo(Vector3 from, Vector3 to, int layerMask, float radius)
    {
        Vector3 dir = to - from;
        RaycastHit hit;

        if (Physics.SphereCast(from, radius, dir, out hit, dir.magnitude * 2f, layerMask))
        {
            return false;
        }
        return true;
    }

    public static RaycastHit SphereCastHitTo(Vector3 from, Vector3 to, int layerMask, float radius)
    {
        Vector3 dir = to - from;
        RaycastHit hit;

        if (Physics.SphereCast(from, radius, dir, out hit, dir.magnitude * 2f, layerMask))
        {
        }
        return hit;
    }

    public static RaycastHit[] SphereCastHitAllTo(Vector3 from, Vector3 to, int layerMask, float radius)
    {
        Vector3 dir = to - from;

        return Physics.SphereCastAll(from, radius, dir, dir.magnitude, layerMask);
    }


}
