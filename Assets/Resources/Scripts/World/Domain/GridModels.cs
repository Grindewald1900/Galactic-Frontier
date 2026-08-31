using System;

namespace Assets.Resources.Scripts.World.Domain
{
    [Serializable]
    public class InnerEdgeDef
    {
        public string edgeId = "";
        public string a = "";
        public string b = "";
        public GridRouteTag route = GridRouteTag.None;
        public float entropy;
        public bool rift;
    }

    public readonly struct CruiseCost
    {
        public readonly float Multiplier;
        public readonly bool AutoReturn;
        public readonly int RecommendedLv;
        public readonly int ShipLv;
        public readonly InnerEdgeDef Edge;

        public CruiseCost(float multiplier, bool autoReturn, int recommendedLv, int shipLv, InnerEdgeDef edge)
        {
            Multiplier = multiplier;
            AutoReturn = autoReturn;
            RecommendedLv = recommendedLv;
            ShipLv = shipLv;
            Edge = edge;
        }

        public bool Overleveled => RecommendedLv > ShipLv;
    }
}
