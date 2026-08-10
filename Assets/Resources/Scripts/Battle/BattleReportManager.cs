using UnityEngine;
using XCharts.Runtime;
using Assets.Resources.Scripts.Cards;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Owns battle-report bar charts for the active BattleScene report panel.
    /// Scene-scoped (not DontDestroyOnLoad) so Explore → Battle → return can repeat cleanly.
    /// </summary>
    public class BattleReportManager : MonoBehaviour
    {
        public static BattleReportManager Instance { get; private set; }

        [SerializeField] private BarChart pDmgChart, pInjuryChart, pHealChart, eDmgChart, eInjuryChart, eHealChart;
        public List<BarChart> barCharts = new();

        private void Awake()
        {
            Instance = this;
            Init();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Init()
        {
            barCharts.Clear();
            SetSeries(pDmgChart, Color.red);
            SetSeries(pInjuryChart, Color.cyan);
            SetSeries(pHealChart, Color.green);
        }

        public void RefreshChart(ChartType type, List<Card> playerCards)
        {
            if (playerCards == null || barCharts == null || (int)type >= barCharts.Count)
                return;

            List<float> values = new();
            List<string> cardNames = new();
            switch (type)
            {
                case ChartType.pDamageChart:
                case ChartType.eDmgChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.damage)
                                         .ToList()
                                         .ConvertAll(card => card.cardBattleInfoEntity.damage);
                    cardNames = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.damage)
                                         .ToList()
                                         .ConvertAll(card => card.cardEntity != null ? card.cardEntity.cardName : "?");
                    break;
                case ChartType.pInjuryChart:
                case ChartType.eInjuryChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.injury)
                                        .ToList()
                                        .ConvertAll(card => card.cardBattleInfoEntity.injury);
                    cardNames = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.injury)
                                        .ToList()
                                        .ConvertAll(card => card.cardEntity != null ? card.cardEntity.cardName : "?");
                    break;
                case ChartType.pHealChart:
                case ChartType.eHealChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.healing)
                                        .ToList()
                                        .ConvertAll(card => card.cardBattleInfoEntity.healing);
                    cardNames = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.healing)
                                        .ToList()
                                        .ConvertAll(card => card.cardEntity != null ? card.cardEntity.cardName : "?");
                    break;
            }

            var chart = barCharts[(int)type];
            if (chart == null)
                return;

            chart.ClearData();
            foreach (var cardName in cardNames)
                chart.AddXAxisData(cardName);
            foreach (var value in values)
                chart.AddData(0, value);
            chart.RefreshChart();
        }

        private void SetSeries(BarChart chart, Color color)
        {
            if (chart == null)
            {
                barCharts.Add(null);
                return;
            }

            chart.Init();
            var serie = chart.series[0];
            serie.itemStyle.color = color;
            barCharts.Add(chart);
        }
    }

    public enum ChartType
    {
        pDamageChart,
        pInjuryChart,
        pHealChart,
        eDmgChart,
        eInjuryChart,
        eHealChart
    }
}
