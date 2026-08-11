using System;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Market
{
    /// <summary>Credits / bound-credits accessors. Canonical field: PlayerEntity.creditPoints.</summary>
    public static class CurrencyService
    {
        public static event Action Changed;

        public static int Credits =>
            DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;

        public static int CreditsBound =>
            DataUtil.Instance?.currentPlayer?.creditsBound ?? 0;

        public static bool TrySpendCredits(int amount, out string error)
        {
            error = null;
            if (amount <= 0) return true;
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null)
            {
                error = "No player.";
                return false;
            }

            if (player.creditPoints < amount)
            {
                error = $"Need {amount} credits (have {player.creditPoints}).";
                return false;
            }

            player.creditPoints -= amount;
            PersistAndNotify(player);
            return true;
        }

        public static void AddCredits(int amount)
        {
            if (amount <= 0) return;
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null) return;
            player.creditPoints += amount;
            PersistAndNotify(player);
        }

        public static void SetCredits(int amount)
        {
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null) return;
            player.creditPoints = Math.Max(0, amount);
            PersistAndNotify(player);
        }

        /// <summary>Hook for bound currency (NPC-only spends later). MVP: additive grant only.</summary>
        public static void AddCreditsBound(int amount)
        {
            if (amount <= 0) return;
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null) return;
            player.creditsBound += amount;
            PersistAndNotify(player);
        }

        public static bool TrySpendBound(int amount, out string error)
        {
            error = null;
            if (amount <= 0) return true;
            var player = DataUtil.Instance?.currentPlayer;
            if (player == null)
            {
                error = "No player.";
                return false;
            }

            if (player.creditsBound < amount)
            {
                error = $"Need {amount} bound credits.";
                return false;
            }

            player.creditsBound -= amount;
            PersistAndNotify(player);
            return true;
        }

        private static void PersistAndNotify(Entity.PlayerEntity player)
        {
            DataUtil.Instance?.SavePlayerData(player);
            if (CharacterInfoManager.Instance != null)
            {
                CharacterInfoManager.Instance.playerData = player;
                CharacterInfoManager.Instance.RefreshCredits();
            }

            Changed?.Invoke();
        }
    }
}
