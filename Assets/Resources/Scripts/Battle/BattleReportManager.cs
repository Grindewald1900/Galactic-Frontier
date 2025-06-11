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
        public List<BarChart> barCharts = new List<BarChart>();

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
            SetSeries(pDmgChart);
            // SetSeries(pInjuryChart);
            // SetSeries(pHealChart);
            // SetSeries(eDmgChart);
            // SetSeries(eInjuryChart);
            // SetSeries(eHealChart);
            var serie = pDmgChart?.series[0];
            Debug.Log(serie == null ? "null" : "not null");
            serie.itemStyle.color = Color.red;
        }

        public void RefreshChart(ChartType type, List<Card> playerCards)
        {
            List<float> damages = playerCards.OrderBy(card => card.cardBattleInfoEntity.damage).ToList().ConvertAll(card => card.cardBattleInfoEntity.damage);
            List<string> cardNames = playerCards.OrderBy(card => card.cardBattleInfoEntity.damage).ToList().ConvertAll(card => card.cardEntity.cardName);
            var chart = barCharts[(int)type];
            chart.ClearData();
            foreach (var cardName in cardNames)
            {
                Debug.Log("Adding card name: " + cardName);
                chart.AddXAxisData(cardName);
            }
            chart.AddSerie<Bar>("Damage");
            foreach (var damage in damages)
            {
                Debug.Log("Adding damage:  " + damage);
                chart.AddData(0, damage);
            }
            chart.RefreshChart();
        }

        private void SetSeries(BarChart chart)
        {
            chart.Init();
            barCharts.Add(chart);
        }
    }

    public enum ChartType
    {
        PlayerDamage,
        PlayerInjury,
        PlayerHeal,
        EnermyDamage,
        EnermyInjury,
        EnermyHeal,
    }
}