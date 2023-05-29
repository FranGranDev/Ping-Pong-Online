using System.Collections;
using System.Linq;
using Networking.Data;
using System.Collections.Generic;
using UnityEngine;
using Services;
using Game;


namespace Management
{
    public class GameSceneContext : SceneContext, IBindable<ClientNetworking>, IBindable<ILobby>, IGameEvents
    {
        [SerializeField] private List<PlayerHandler> playerHandlers;
        [SerializeField] private Ball ball;
        [Space]
        [SerializeField] private ObjectSynchronizer objectSynchronizer;


        private ClientNetworking client;
        private ILobby lobby;


        public event System.Action OnStart;
        public event System.Action OnEnd;
        public event System.Action OnRestart;


        protected override void Initialize()
        {
            InitializeService.Bind<IGameEvents>(this, this);

            SubscribeToNetwork();
            SetupSynchronize();

            CallStartRound(1f);
        }
        private void SubscribeToNetwork()
        {
            client.OnRoundStarted += StartRound;
            client.OnRoundEnded += EndRound;
        }
        private void SetupSynchronize()
        {

            foreach (Player player in lobby.Players)
            {
                bool local = player.Equals(lobby.Self);

                PlayerHandler handler = playerHandlers[player.Index];

                handler.SetPlayer(player, local);

                handler.GetComponentsInChildren<NetworkObject>()
                    .ToList()
                    .ForEach(x =>
                    {
                        x.SetId(player);
                        x.Mine = local;
                    });

                handler.OnLose += OnPlayerLose;
            }

            Player master = lobby.Players.FirstOrDefault(x => x.Master);
            if (master != null)
            {
                ball.GetComponentsInChildren<NetworkObject>()
                    .ToList()
                    .ForEach(x =>
                    {
                        x.SetId(master);
                        x.Mine = master.Equals(lobby.Self);
                    });
            }

            objectSynchronizer.OnObjectUpdated += OnObjectUpdated;
            objectSynchronizer.Initialize();
        }


        public void Bind(ClientNetworking obj)
        {
            client = obj;

            client.OnUpdateObject += UpdateObject;
        }
        public void Bind(ILobby obj)
        {
            lobby = obj;
        }


        private void StartRound()
        {
            OnStart?.Invoke();
        }
        private void EndRound(Player looser)
        {
            OnEnd?.Invoke();

            //check if game end
            CallStartRound(1f);
        }

        private void CallStartRound(float delay)
        {
            if (lobby.IsMaster)
            {
                this.Delayed(() =>
                {
                    client.StartRound();
                }, delay);
            }
        }

        private void OnPlayerLose(Player player)
        {
            if (lobby.IsMaster)
            {
                client.EndRound(player);
            }
        }



        private void OnObjectUpdated(string id, object data)
        {
            client.UpdateObject(id, data);
        }
        private void UpdateObject(string id, object data)
        {
            objectSynchronizer.UpdateRemoteObject(id, data);
        }



        public override void Visit(ISceneVisitor visitor)
        {
            visitor.Visited(this);
        }
    }
}