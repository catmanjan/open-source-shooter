/*
 * Created on Mon Mar 28 2022
 *
 * The MIT License (MIT)
 * Copyright (c) 2022 Stefan Boronczyk, Striked GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
 * TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Godot;
using Framework.Game;
using Framework;
using System.Collections.Generic;
using Framework.Network.Services;
using System;
using RCONServerLib.Utils;

namespace Framework.Game.Server
{
    /// <summary>
    /// The core server logic
    /// </summary>
    public class ServerLogic : GameLogic
    {
        /// <inheritdoc />
        internal ServerNetworkService netService = null;

        /// <inheritdoc />
        internal RconServerService rconService = null;

        /// <summary>
        /// maximal possible connections
        /// </summary>
        [Export]
        public int MaxConnections { get; set; } = 15;

        /// <summary>
        /// If clients can connect
        /// </summary>
        [Export]
        public bool AcceptClients { get; set; } = false;

        /// <summary>
        /// The dictonary with all server settings (vars);
        /// </summary>
        [Export]
        public Dictionary<string, string> DefaultVars = new Dictionary<string, string>  {

            // enable or disable interpolation
            { "sv_interpolate", "true" },

            // enable or disable showing raycasts
            { "sv_raycast", "true" },
            
            // force for soft or hard lag reduction
            { "sv_agressive_lag_reduction", "true" },
            // maximal input stages per ms
            { "sv_max_stages_ms", "500" },
            // force freeze client in case of lags
            { "sv_freze_client", "false" },

            // crouching
            { "sv_crouching", "true" },
            { "sv_crouching_down_speed", "9.0" },
            { "sv_crouching_up_speed", "4.0" },
            { "sv_crouching_accel", "8.0" },
            { "sv_crouching_deaccel", "4.0" },
            { "sv_crouching_friction", "3.0" },
            { "sv_crouching_speed", "4.0" },

            // walking
            { "sv_walk_accel", "14.0" },
            { "sv_walk_deaccel", "10.0" },

            { "sv_walk_speed", "7.0" },
            { "sv_walk_friction", "6.0" },

            // air control
            { "sv_air_control", "0.4" },
            { "sv_air_accel", "12.0" },
            { "sv_air_deaccel", "2.0" },

            // gravity and jump
            { "sv_gravity", "20.0" },
            { "sv_jumpspeed", "6.5" },

            // side movement
            { "sv_strafe_speed", "1.0" },
            { "sv_strafe_accel", "50.0" },
        };

        /// <summary>
        /// Contains the server vars
        /// </summary>
        /// <param name="Vars()"></param>
        /// <returns></returns>
        public VarsCollection Variables { get; private set; } = new VarsCollection(new Vars());

        /// <summary>
        /// Network server port
        /// </summary>
        [Export]
        public int NetworkPort { get; set; } = 27015;

        /// <summary>
        /// Called when after server started succeffull
        /// </summary>
        public virtual void OnServerStarted()
        {

        }

        /// <inheritdoc />
        internal override void InternalTreeEntered()
        {
            this.RenderTargetUpdateMode = UpdateMode.Always;
            this.PhysicsObjectPicking = true;
            this.ProcessMode = ProcessModeEnum.Always;
            //    this.Disable3d = true;
            this.HandleInputLocally = false;

            this.netService = this.Services.Create<ServerNetworkService>();
            this.rconService = this.Services.Create<RconServerService>();
            this.netService.ConnectionEstablished += () =>
            {
                Logger.LogDebug(this, "Server was started.");
                this.OnServerStarted();
            };

            this.netService.ConnectionRequest += (request) =>
            {
                if (this.currentWorld == null)
                {
                    request.Reject();
                }
                else
                {
                    if (this.netService.GetConnectionCount() < MaxConnections && AcceptClients && this.OnPreConnect(request))
                    {
                        Logger.LogDebug(this, request.RemoteEndPoint.Address + ":" + request.RemoteEndPoint.Port + " established");
                        request.AcceptIfKey(this.secureConnectionKey);
                    }
                    else
                    {
                        Logger.LogDebug(this, request.RemoteEndPoint.Address + ":" + request.RemoteEndPoint.Port + " rejected");
                        request.Reject();
                    }
                }
            };

            this.rconService.ServerStarted += InitRconServer;

            base.InternalTreeEntered();
            this.netService.Bind(NetworkPort);
        }

        /// <summary>
        /// Initialized the rcon server, eg attaching commands
        /// </summary>
        /// <param name="manager"></param>
        public virtual void InitRconServer(CommandManager manager)
        {
            manager.Add("set", "Set an server variable", (command, arguments) =>
            {
                if (arguments.Count != 2)
                {
                    return "Required arguments: key, variable";
                }

                if (this.currentWorld != null)
                {
                    var key = this.currentWorld.ServerVars.Vars.AllVariables.ContainsKey(arguments[0]);
                    if (!key)
                    {
                        return "Varaible " + arguments[0] + " not exist.";
                    }
                    else
                    {
                        this.currentWorld.ServerVars.Set(arguments[0], arguments[1]);
                        return "Successfull changed.";
                    }
                }
                else
                {
                    return "No active game world";
                }
            });

            manager.Add("get", "Get an server variable", (command, arguments) =>
            {
                if (arguments.Count != 1)
                {
                    return "Required arguments: key";
                }

                if (this.currentWorld != null)
                {
                    var key = this.currentWorld.ServerVars.Vars.AllVariables.ContainsKey(arguments[0]);
                    if (!key)
                    {
                        return "Varaible " + arguments[0] + " not exist.";
                    }
                    else
                    {
                        return this.currentWorld.ServerVars.Vars.AllVariables[arguments[0]];
                    }
                }
                else
                {
                    return "No active game world";
                }
            });
        }

        /// <summary>
        /// Called when client connected 
        /// </summary>
        /// <param name="request">boolean for rejected or accepted</param>
        /// <returns></returns>
        public virtual bool OnPreConnect(LiteNetLib.ConnectionRequest request)
        {
            return true;
        }

        /// <inheritdoc />  
        internal override void LoadWorldInternal(string mapName, uint worldTick)
        {
            this.AcceptClients = false;

            //disconnect all clients
            foreach (var connectedClient in this.netService.GetConnectedPeers())
            {
                connectedClient.Disconnect();
            }

            base.LoadWorldInternal(mapName, worldTick);
        }

        /// <summary>
        /// Create an server world
        /// </summary>
        /// <returns></returns>
        public virtual ServerWorld CreateWorld()
        {
            return new ServerWorld();
        }

        /// <inheritdoc />
        internal override void OnMapInstanceInternal(PackedScene res, uint worldTick)
        {
            ServerWorld newWorld = this.CreateWorld();
            newWorld.Name = "world";
            this.AddChild(newWorld);
            newWorld._resourceWorldPath = res.ResourcePath;
            newWorld.InstanceLevel(res);

            this.currentWorld = newWorld;

            this.Variables = new VarsCollection(new Vars(this.DefaultVars));
            this.Variables.LoadConfig("server.cfg");

            newWorld.Init(this.Variables, 0);

            //accept clients
            this.AcceptClients = true;
            this.AfterMapLoaded();
        }

        /// <inheritdoc />
        internal override void DestroyMapInternal()
        {
            this.currentWorld?.Destroy();
            this.currentWorld = null;

            //reject clients
            this.AcceptClients = false;

            this.AfterMapDestroy();
        }
    }
}
