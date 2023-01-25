using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public abstract class AbstractBrain <T> : MonoBehaviour where T : MonoBehaviour
    {
        public IBrainOwner<T> BrainOwner { get; set; }
    }
}
