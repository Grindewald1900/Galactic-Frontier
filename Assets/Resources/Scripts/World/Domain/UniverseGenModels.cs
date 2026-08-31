using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public enum UniverseRingId
    {
        OuterRim = 0,
        Pioneer = 1,
        Severance = 2,
        Plane = 3,
        Inner = 4,
        Core = 5
    }

    public enum UniverseSectorKind
    {
        Birth = 0,
        Normal = 1,
        FactionCore = 2,
        Resource = 3,
        Relic = 4,
        EntropyHazard = 5,
        Special = 6,
        Nexus = 7
    }

    public enum PlaneBonusKind
    {
        Metal = 0,
        Energy = 1,
        Relic = 2,
        Bio = 3,
        Balanced = 4
    }

    [Serializable]
    public sealed class PlaneModifiersState
    {
        public string primary = PlaneBonusKind.Metal.ToString();
        public string secondary = PlaneBonusKind.Relic.ToString();
        public string gap = PlaneBonusKind.Bio.ToString();
    }

    [Serializable]
    public sealed class GeneratedSector
    {
        public string sectorId = "";
        public float x;
        public float y;
        public UniverseRingId ring;
        public UniverseSectorKind kind;
        public int difficulty;
        public int recommendedExpeditionLv;
        public int danger;
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string factionTag = "";
        public string resourceProfile = "";
        public bool playable;
        public int spriteIndex;
        public bool mainPathRequired;
    }

    [Serializable]
    public sealed class GeneratedRoute
    {
        public string edgeId = "";
        public string fromId = "";
        public string toId = "";
        public GridRouteTag route;
        public float entropy;
        public bool rift;
        public bool wormhole;
    }

    [Serializable]
    public sealed class UniverseValidationIssue
    {
        public string code = "";
        public string message = "";
    }

    [Serializable]
    public sealed class UniverseValidationResult
    {
        public bool isValid;
        public List<UniverseValidationIssue> issues = new List<UniverseValidationIssue>();
    }

    public sealed class GeneratedUniverse
    {
        public int seed;
        public float centerX = 90f;
        public float centerY = 10f;
        public PlaneModifiersState planeModifiers = new PlaneModifiersState();
        public List<GeneratedSector> sectors = new List<GeneratedSector>();
        public List<GeneratedRoute> routes = new List<GeneratedRoute>();
        public UniverseValidationResult validation = new UniverseValidationResult();
    }

    public sealed class UniverseGenOptions
    {
        public int targetSectorCount = 18;
        public bool useFullRings;
        public int maxRepairPasses = 3;
    }
}
