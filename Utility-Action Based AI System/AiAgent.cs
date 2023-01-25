using Assets.SteamAndMagic.Scripts.Managers;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    /// <summary>
    /// Controls the Brain and the AI actions
    /// No direct gameplay stuff in here, should go in AiAgentEntity 
    /// </summary>
    public class AiAgent : MonoBehaviour, IBrainOwner<AiAgent>
    {
        /// <summary>
        /// The entity of the agent
        /// </summary>
        public AiAgentEntity AgentEntity { get; set; }

        /// <summary>
        /// The Ai Brain
        /// </summary>
        public Brain Brain = null;

        /// <summary>
        /// Interface implementation 
        /// </summary>
        public AiAgent Context => this;

        [Header("Brain Parameters")]
        public Vector2 TicksPerSecond = new Vector2(3, 6);
        public float Timer;
        public float TickTime;
        public bool Thinking = false;
        private bool initialized = false;

        void Awake()
        {
            if (!GameServer.IsMaster)
                this.enabled = false;

            AgentEntity = GetComponent<AiAgentEntity>();
            Brain = GetComponent<Brain>();            
        }

        public void InitializeBrain()
        {
            if (initialized)
                return;

            Brain.Initialize(this);
            initialized = true;
        }

        public void StartBrain()
        {
            if (Thinking)
                return;

            InitializeBrain();

            TickTime = 1f / (float)Random.Range(TicksPerSecond.x, TicksPerSecond.y);
            Thinking = true;
        }

        public void StopBrain()
        {
            Thinking = false;
        }

        // AiAgent n'est actif que sur le serveur
        void Update()
        {
            if (!Thinking)
                return;

            Timer += Time.deltaTime * WorldManager.Instance.GetTimeFactor(AgentEntity.Team);

            if (Timer > TickTime)
            {

                AgentEntity.targetHandlingSubsystem.UpdateNpcTargets();
                Brain.UpdateBrainLogic();

                TickTime = 1f / (float)Random.Range(TicksPerSecond.x, TicksPerSecond.y);
                Timer = 0;
            }
        }
    }
}
