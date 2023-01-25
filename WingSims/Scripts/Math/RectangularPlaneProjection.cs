using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.WingsSim.Scripts.Math
{
    class RectangularPlaneProjection : MonoBehaviour
    {
        public Transform Center;
        public Transform CornerA;
        public Transform CornerB;
        public Transform CornerC;
        public Transform CornerD;

        public Vector3 PlaneNormal;

        public Vector3 CornerAProjection;
        public Vector3 CornerBProjection;
        public Vector3 CornerCProjection;
        public Vector3 CornerDProjection;

        public float Surface;

        private void OnDrawGizmos()
        {
            if (Center == null || CornerA == null || CornerB == null || CornerC == null || CornerD == null)
                return;

            CornerAProjection = Vector3.ProjectOnPlane(CornerA.position, PlaneNormal);
            CornerBProjection = Vector3.ProjectOnPlane(CornerB.position, PlaneNormal);
            CornerCProjection = Vector3.ProjectOnPlane(CornerC.position, PlaneNormal);
            CornerDProjection = Vector3.ProjectOnPlane(CornerD.position, PlaneNormal);

            Gizmos.DrawSphere(CornerAProjection, .25f);
            Gizmos.DrawSphere(CornerBProjection, .25f);
            Gizmos.DrawSphere(CornerCProjection, .25f);
            Gizmos.DrawSphere(CornerDProjection, .25f);

            Gizmos.DrawLine(CornerAProjection, CornerBProjection);
            Gizmos.DrawLine(CornerBProjection, CornerCProjection);
            Gizmos.DrawLine(CornerCProjection, CornerAProjection);

            Gizmos.DrawLine(CornerCProjection, CornerDProjection);
            Gizmos.DrawLine(CornerDProjection, CornerAProjection);
            Gizmos.DrawLine(CornerAProjection, CornerCProjection);

            Surface = ComputeTriangleSurfaceTrigo(CornerAProjection - CornerBProjection, CornerBProjection - CornerCProjection, CornerCProjection - CornerAProjection) + ComputeTriangleSurfaceTrigo(CornerCProjection - CornerDProjection, CornerDProjection - CornerAProjection, CornerAProjection - CornerCProjection);
        }

        private static Vector3 cp_a;
        private static Vector3 cp_b;
        private static Vector3 cp_c;
        private static Vector3 cp_d;

        public static float ComputeRectangularSurfaceOnPlane(Vector3 planeNormal, Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD)
        {
            cp_a = Vector3.ProjectOnPlane(cornerA, planeNormal);
            cp_b = Vector3.ProjectOnPlane(cornerB, planeNormal);
            cp_c = Vector3.ProjectOnPlane(cornerC, planeNormal);
            cp_d = Vector3.ProjectOnPlane(cornerD, planeNormal);

            return ComputeTriangleSurfaceTrigo(cp_a - cp_b, cp_b - cp_c, cp_c - cp_a) 
                + ComputeTriangleSurfaceTrigo(cp_c - cp_d, cp_d - cp_a, cp_a - cp_c);
        }

        private static float ComputeTriangleSurfaceTrigo(Vector3 a, Vector3 b, Vector3 c)
        {
            return 0.5f * a.magnitude * c.magnitude * Mathf.Sin(Vector3.Angle(a, c) * Mathf.Deg2Rad);
        }
    }
}
