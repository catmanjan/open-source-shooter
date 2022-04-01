using Framework.Game.Server;
using Framework.Game;
using Godot;

namespace Shooter.Server
{
    public partial class MyServerLogic : ServerLogic
    {
        [Export]
        /// <summary>
        /// The default map name which is loaded after startup
        /// </summary>
        public string DefaultMapName = "res://Assets/Maps/Test.tscn";

        public override void OnServerStarted()
        {
            this.LoadWorld(DefaultMapName, 0);
        }

        public override ServerWorld CreateWorld()
        {
            return new MyServerWorld();
        }

        public override void AfterMapLoaded()
        {
            var deathmatch = new DeathmatchGameRule(this.currentWorld as MyServerWorld);
            (this.currentWorld as MyServerWorld).ActiveGameRule = deathmatch;
        }
    }
}