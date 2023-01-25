using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WingsSim.World
{
    public class ProceduralManager : MonoBehaviour
    {


        private void Update()
        {
            for (int i = 0; i < Terrain.activeTerrains.Length; ++i)
            {
                Terrain.activeTerrains[i].gameObject.layer = LayerMask.NameToLayer("World");
                Terrain.activeTerrains[i].gameObject.tag = "World";
            }
        }
    }
}
