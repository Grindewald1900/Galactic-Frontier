using NUnit.Framework;
using UnityEngine;
using XCharts.Runtime;
using Assets.Resources.Scripts.Cards;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Resources.Scripts.Battle
{
    public class BattleReportManager : MonoBehaviour
    {
        public static BattleReportManager Instance
        {
            get
            {
                if (instance == null)
                    instance = Object.FindAnyObjectByType<BattleReportManager>();
                return instance;
            }
        }
        private static BattleReportManager instance;
        [SerializeField] private BarChart pDmgChart, pInjuryChart, pHealChart, eDmgChart, eInjuryChart, eHealChart;
        public List<BarChart> barCharts = new();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            Init();
        }

        private void Init()
        {
            SetSeries(pDmgChart, Color.red);
            SetSeries(pInjuryChart, Color.cyan);
            SetSeries(pHealChart, Color.green);
            // SetSeries(eDmgChart);
            // SetSeries(eInjuryChart);
            // SetSeries(eHealChart);
        }

        public void RefreshChart(ChartType type, List<Card> playerCards)
        {
            List<float> values = new();
            List<string> cardNames = new();
            switch (type)
            {
                case ChartType.pDamageChart:
                case ChartType.eDmgChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.damage)
                                         .ToList()
                                         .ConvertAll(card => card.cardBattleInfoEntity.damage);
                    break;
                case ChartType.pInjuryChart:
                case ChartType.eInjuryChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.injury)
                                        .ToList()
                                        .ConvertAll(card => card.cardBattleInfoEntity.injury);
                    break;
                case ChartType.pHealChart:
                case ChartType.eHealChart:
                    values = playerCards.OrderByDescending(card => card.cardBattleInfoEntity.healing)
                                        .ToList()
                                        .ConvertAll(card => card.cardBattleInfoEntity.healing);
                    break;
            }

            playerCards.OrderBy(card => card.cardBattleInfoEntity.damage).ToList().ConvertAll(card => card.cardEntity.cardName);
            var chart = barCharts[(int)type];
            chart.ClearData();
            foreach (var cardName in cardNames)
            {
                Debug.Log("Adding card name: " + cardName);
                chart.AddXAxisData(cardName);
            }
            foreach (var value in values)
            {
                Debug.Log("Adding value:  " + value);
                chart.AddData(0, value);
            }
            Debug.Log("Refreshing chart..");
            chart.RefreshChart();
        }

        private void SetSeries(BarChart chart, Color color)
        {
            chart.Init();
            var serie = chart?.series[0];
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