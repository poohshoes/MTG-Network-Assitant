using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GameName1
{
    class Network
    {
        string name = "IANSMTG";
        int port;
        string ip;

        public string type;
        NetPeer peer;
                
        public Network(Settings settings)
        {
            type = settings.netType;
            port = settings.port;
            NetPeerConfiguration config = new NetPeerConfiguration(name);

            if (IsServer())
            {
                config.Port = port;
                config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
                NetServer netServer = new NetServer(config);
                netServer.Start();
                peer = netServer;
            }
            else
            {
                ip = settings.ip;
                NetClient client = new NetClient(config);
                client.Start();
                peer = client;
            }
        }

        internal bool IsServer()
        {
            return type == "server";
        }

        internal void Update(Microsoft.Xna.Framework.GameTime gameTime, Game1 game)
        {
            if (IsServer())
            {
                /// SERVER
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.ConnectionApproval:
                            msg.SenderConnection.Approve();
                            break;
                        case NetIncomingMessageType.Data:
                            ProcessDataMessage(msg, game);
                            break;
                        default:
                            Console.WriteLine("Unhandled type: " + msg.MessageType);
                            break;
                    }
                    peer.Recycle(msg);
                }
            }
            else
            {
                if (peer.ConnectionsCount == 0)
                {
                    peer.Connect(ip, port);
                    System.Threading.Thread.Sleep(2000);
                }

                /// CLIENT
                NetIncomingMessage msg;
                while ((msg = peer.ReadMessage()) != null)
                {
                    switch (msg.MessageType)
                    {
                        case NetIncomingMessageType.VerboseDebugMessage:
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.ErrorMessage:
                            Console.WriteLine(msg.ReadString());
                            break;
                        case NetIncomingMessageType.Data:
                            ProcessDataMessage(msg, game);
                            break;
                        default:
                            Console.WriteLine("Unhandled type: " + msg.MessageType);
                            break;
                    }
                    peer.Recycle(msg);
                }
            }
        }

        private void ProcessDataMessage(NetIncomingMessage msg, Game1 game)
        {
            NetworkMessageType type = (NetworkMessageType)msg.ReadByte();

            switch (type)
            {
                case NetworkMessageType.CreateEntity:
                    {
                        int id = msg.ReadInt32();
                        float newX = msg.ReadFloat();
                        float newY = msg.ReadFloat();
                        string path = msg.ReadString();
                        Entity card = game.SpawnCard(path, id);
                        card.Position.X = newX;
                        card.Position.Y = newY;
                        card.Depth = game.GetNextHeighestDepth();
                        game.Entities.Add(card);
                    }
                    break;
                case NetworkMessageType.MoveEntity:
                    {
                        int id = msg.ReadInt32();
                        float newX = msg.ReadFloat();
                        float newY = msg.ReadFloat();
                        var entity = game.Entities.FirstOrDefault(x => x.NetworkID == id);
                        if (entity != null)
                        {
                            entity.Position.X = newX;
                            entity.Position.Y = newY;
                        }
                    }
                    break;
                case NetworkMessageType.TapEntity:
                    {
                        int id = msg.ReadInt32();
                        var entity = game.Entities.FirstOrDefault(x => x.NetworkID == id);
                        if (entity != null)
                        {
                            entity.Tapped = !entity.Tapped;
                        }
                    }
                    break;
            }
        }

        enum NetworkMessageType
        {
            CreateEntity,
            MoveEntity,
            TapEntity
        }

        internal void SendEntityCreateMessage(int entityNetworkID, string textureName, Microsoft.Xna.Framework.Vector2 entityNewPosition)
        {
            NetOutgoingMessage message = peer.CreateMessage();
            message.Write((byte)NetworkMessageType.CreateEntity);
            message.Write(entityNetworkID);
            message.Write(entityNewPosition.X);
            message.Write(entityNewPosition.Y);
            message.Write(textureName);
            if (peer.ConnectionsCount > 0)
            {
                peer.Connections[0].SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        internal void SendEntityMovedMessage(int entityNetworkID, Microsoft.Xna.Framework.Vector2 entityNewPosition)
        {
            NetOutgoingMessage message = peer.CreateMessage();
            message.Write((byte)NetworkMessageType.MoveEntity);
            message.Write(entityNetworkID);
            message.Write(entityNewPosition.X);
            message.Write(entityNewPosition.Y);
            if (peer.ConnectionsCount> 0)
            {
                peer.Connections[0].SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        internal void SendEntityTappedMessage(int entityNetworkID)
        {
            NetOutgoingMessage message = peer.CreateMessage();
            message.Write((byte)NetworkMessageType.TapEntity);
            message.Write(entityNetworkID);
            if (peer.ConnectionsCount > 0)
            {
                peer.Connections[0].SendMessage(message, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }
    }
}
