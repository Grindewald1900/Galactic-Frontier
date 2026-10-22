using UnityEngine;
using TMPro;
using XCharts.Runtime;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.UI.Nexus;
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

        [UnityEngine.Tooltip("Box that receives the outcome summary; defaults to Player/SubTotal.")]
        [SerializeField] private RectTransform summaryRoot;
        [SerializeField] private float summaryFontSize = 26f;
        [SerializeField] private float summaryPadding = 20f;
        [SerializeField] private Color summaryColor = new(0.89f, 0.91f, 0.94f, 1f);
        private TextMeshProUGUI summaryText;
        private readonly List<Color> chartColors = new();
        private readonly List<string> chartTitles = new();

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
            chartColors.Clear();
            chartTitles.Clear();
            SetSeries(pDmgChart, Color.red, UiText.T("Damage dealt", "造成伤害"));
            SetSeries(pInjuryChart, Color.cyan, UiText.T("Damage taken", "承受伤害"));
            SetSeries(pHealChart, Color.green, UiText.T("Healing", "治疗量"));
        }

        /// <summary>Writes the outcome / rewards / next-step text into the report's summary box.</summary>
        public void ShowSummary(string text)
        {
            if (summaryText == null)
            {
                var root = summaryRoot != null ? summaryRoot : transform.Find("Player/SubTotal") as RectTransform;
                if (root == null)
                    return;

                var go = new GameObject("SummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
                var rt = (RectTransform)go.transform;
                rt.SetParent(root, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(summaryPadding, summaryPadding);
                rt.offsetMax = new Vector2(-summaryPadding, -summaryPadding);

                summaryText = go.GetComponent<TextMeshProUGUI>();
                var titleFont = transform.Find("ReportTitle")?.GetComponent<TMP_Text>()?.font;
                if (titleFont != null)
                    summaryText.font = titleFont;
                summaryText.fontSize = summaryFontSize;
                summaryText.color = summaryColor;
                summaryText.alignment = TextAlignmentOptions.MidlineLeft;
                summaryText.enableWordWrapping = true;
                summaryText.raycastTarget = false;
            }

            summaryText.text = text ?? "";
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

            // Charts on hidden tabs have not run XCharts' Awake yet, which would reset them on first
            // show; wake them while styling and filling.
            var woken = new List<GameObject>();
            for (var t = chart.transform; t != null && t != transform; t = t.parent)
            {
                if (t.gameObject.activeSelf) continue;
                t.gameObject.SetActive(true);
                woken.Add(t.gameObject);
            }

            ApplyStyle(chart, (int)type);
            chart.ClearData();
            foreach (var cardName in cardNames)
                chart.AddXAxisData(cardName);
            foreach (var value in values)
                chart.AddData(0, value);
            chart.RefreshChart();

            foreach (var go in woken)
                go.SetActive(false);
        }

        private void SetSeries(BarChart chart, Color color, string title)
        {
            barCharts.Add(chart);
            chartColors.Add(color);
            chartTitles.Add(title);
        }

        private void ApplyStyle(BarChart chart, int index)
        {
            chart.Init();
            var serie = chart.series[0];
            serie.itemStyle.color = chartColors[index];
            serie.serieName = chartTitles[index];
            var chartTitle = chart.GetChartComponent<Title>();
            if (chartTitle != null)
                chartTitle.text = chartTitles[index];
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
