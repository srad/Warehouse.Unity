namespace DefaultNamespace
{
    public static class LoadInfo
    {
        public const string NoLoad = "0";
        public const string One = "1";
        public const string Two = "2";
        public const string Three = "3";
        public const string Four = "4";

        public static int LoadToNum(string loadInfo) => int.Parse(loadInfo);
        public static string IntToTag(int layers) => $"pallet.load.{layers}";
    }

    public static class PalletInfo
    {
        public static class Plank
        {
            public const string Top = "Pallet.Plank.Top";
            public const string Middle = "Pallet.Plank.Middle";
            public const string Bottom = "Pallet.Plank.Bottom";
            public const string Prefix = "Pallet.Plank.";
        }

        public static class Brick
        {
            public const string Prefix = "Pallet.Brick";
            public const string Corner = "Pallet.Brick.Corner";
            public const string Side = "Pallet.Brick.Side";
            public const string Front = "Pallet.Brick.Front";

            /// <summary>
            /// There is an almost invisible brick at the center of pallets.
            /// </summary>
            public const string Center = "Pallet.Brick.Center";
        }

        public static class Box
        {
            /// <summary>
            /// These are the object name not the tag names!
            /// </summary>
            public const string Prefix = "Box.Layer.";

            public const string Layer1Prefix = "Box.Layer.1";
            public const string Layer2Prefix = "Box.Layer.2";
            public const string Layer3Prefix = "Box.Layer.3";
            public const string Layer4Prefix = "Box.Layer.4";
        }
    }
}