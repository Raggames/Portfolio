using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.WingsSim.Scripts.Control
{
    public class MovablePart : MonoBehaviour
    {
        private List<Vector3> angleMoves = new List<Vector3>();
        private Vector3 moveVector = new Vector3();
        private Vector3 lastMove;
        private Vector3 curVel;

        public float smoothTime = .5f;

        public void Move(float x, float y, float z)
        {
            angleMoves.Add(new Vector3(x, y, z));
        }

        private void LateUpdate()
        {
            if (angleMoves.Count > 0)
            {
                lastMove = moveVector;

                moveVector = Vector3.zero;
                for (int i = 0; i < angleMoves.Count; ++i)
                {
                    moveVector += angleMoves[i];
                }
                moveVector /= angleMoves.Count;

                Vector3 move = Vector3.SmoothDamp(lastMove, moveVector, ref curVel, smoothTime);

                this.transform.localRotation = Quaternion.Euler(move);
                angleMoves.Clear();
            }
        }
    }
}
