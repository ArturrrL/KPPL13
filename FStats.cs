using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace l13._1
{
    public partial class FStats : Form
    {
        private readonly TSklad _sklad;

        public FStats(TSklad sklad)
        {
            InitializeComponent();
            _sklad = sklad;

            CBField.Items.AddRange(new object[] { "Група", "Постачальник", "Виробник", "Од.виміру", "Склад" });
            CBField.SelectedIndex = 0;

            CHOnlyFiltered.Checked = true;

            BuildChart();
        }

        private void BRefresh_Click(object sender, EventArgs e) => BuildChart();

        private DataView GetView()
        {
            return CHOnlyFiltered.Checked ? _sklad.ViewSklad : new DataView(_sklad.TabSklad);
        }

        private void BuildChart()
        {
            chart1.Series.Clear();

            string field = CBField.SelectedItem?.ToString() ?? "Група";
            var dv = GetView();

            var groups = dv.ToTable()
                .AsEnumerable()
                .GroupBy(r => (r[field]?.ToString() ?? "(порожньо)"))
                .Select(g => new
                {
                    Key = g.Key,
                    Sum = g.Sum(r => r.Field<decimal?>("Вартість") ?? 0m)
                })
                .OrderByDescending(x => x.Sum)
                .Take(12)
                .ToList();

            var s = new Series("Сума вартості")
            {
                ChartType = SeriesChartType.Column
            };

            foreach (var it in groups)
                s.Points.AddXY(it.Key, it.Sum);

            chart1.Series.Add(s);
            chart1.ChartAreas[0].RecalculateAxesScale();
        }
    }
}
