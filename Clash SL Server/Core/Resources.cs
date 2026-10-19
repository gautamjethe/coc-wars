
using CSS.Core.Events;

namespace CSS.Core
{
    using System;
    using CSS.Core.Network.TCP;

    internal class Resources
    {
        internal static Random Random;
        internal static Gateway Gateway;
        internal static DatabaseManager DatabaseManager;
        internal static Loader Loader;
        internal static Region Region;


        internal static void Initialize()
        {
            Resources.DatabaseManager = new DatabaseManager();
            Resources.Loader = new Loader();
            Resources.Random = new Random();
            Resources.Region = new Region();
            Resources.Gateway = new Gateway();
            CSS.Logic.ClanWarManager.LoadState();
        }
    }
}
