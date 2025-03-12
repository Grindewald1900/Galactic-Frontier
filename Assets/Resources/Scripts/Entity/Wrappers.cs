namespace Assets.Resources.Scripts.Entity
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class SkillListWrapper
    {
        public List<SkillEntity> skillEntities;
    }

    [Serializable]
    public class CardDataContainer
    {
        public List<CardEntity> cards = new List<CardEntity>();
    }
    [Serializable]
    public class CardListWrapper
    {
        public int count = 0;
        public List<CardEntity> cardEntities;
    }

    [Serializable]
    public class PlayerListWrapper
    {
        public int count = 0;
        public List<PlayerEntity> playerEntities;
    }

    [Serializable]
    public class ExpertiseListWrapper
    {
        public int count = 0;
        public List<ExpertiseEntity> expertiseEntities;
    }
}