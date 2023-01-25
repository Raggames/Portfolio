using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public interface IBrainOwner<T> where T : MonoBehaviour
    {
        public T Context { get; }
    }
}
