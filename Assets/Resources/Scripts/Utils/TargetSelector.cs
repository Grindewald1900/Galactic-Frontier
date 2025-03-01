using System.Collections.Generic;
using System.Linq;
using System;
using Assets.Resources.Scripts.Cards;

namespace Assets.Resources.Scripts.Utils
{
    public static class TargetSelector
    {
        public static List<Card> GetFrontRowCards(List<Card> target)
        {
            List<Card> selectedTargets = target.Where(x => x != null && (x.position == 1 || x.position == 2)).ToList();
            if (selectedTargets.Count == 0)
            {
                selectedTargets = target.Where(x => x != null).ToList();
            }
            if (selectedTargets.Count == 0)
            {
                return null;
            }
            return selectedTargets;
        }

        public static List<Card> GetBackRowCards(List<Card> target)
        {
            List<Card> selectedTargets = target.Where(x => x != null && x.position != 1 && x.position != 2).ToList();
            if (selectedTargets.Count == 0)
            {
                selectedTargets = target.Where(x => x != null).ToList();
            }
            if (selectedTargets.Count == 0)
            {
                return null;
            }
            return selectedTargets;
        }

        public static List<Card> GetAllCards(List<Card> target)
        {
            List<Card> selectedTargets = target.Where(x => x != null).ToList();
            if (selectedTargets.Count == 0)
            {
                return null;
            }
            return selectedTargets;
        }

        public static List<Card> GetRandomCards(List<Card> target, int count)
        {
            List<Card> selectedTargets = target.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
            if (selectedTargets.Count == 0)
            {
                return null;
            }
            return selectedTargets;
        }
    }
}