using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace l13._1
{
    public class TSklad
    {
        public DataTable TabSklad = new DataTable("TabSklad");
        public DataView ViewSklad;

        public DataTable TabSum = new DataTable("TabSum");

        public DataTable DovSklady = new DataTable("DovSklady");
        public DataTable DovGrupa = new DataTable("DovGrupa");
        public DataTable DovPostach = new DataTable("DovPostach");
        public DataTable DovOdin = new DataTable("DovOdin");
        public DataTable DovVyrobnyk = new DataTable("DovVyrobnyk");

        public TSklad()
        {
            CreateDovids();
            CreateMainTable();
            CreateSumTable();

            ViewSklad = new DataView(TabSklad);
        }

        private static DataColumn Col(string name, Type t) => new DataColumn(name, t);

        private void CreateMainTable()
        {
            TabSklad.Columns.Clear();

            TabSklad.Columns.Add(Col("№", typeof(int)));
            TabSklad.Columns.Add(Col("Склад", typeof(string)));
            TabSklad.Columns.Add(Col("Група", typeof(string)));
            TabSklad.Columns.Add(Col("Назва", typeof(string)));
            TabSklad.Columns.Add(Col("Виробник", typeof(string)));
            TabSklad.Columns.Add(Col("Постачальник", typeof(string)));
            TabSklad.Columns.Add(Col("Од.виміру", typeof(string)));
            TabSklad.Columns.Add(Col("Ціна", typeof(decimal)));
            TabSklad.Columns.Add(Col("Кількість", typeof(int)));
            TabSklad.Columns.Add(Col("Вартість", typeof(decimal)));
            TabSklad.Columns.Add(Col("Дата", typeof(DateTime)));

            TabSklad.Columns["Дата"].DefaultValue = DateTime.Today;
        }

        private void CreateSumTable()
        {
            TabSum.Columns.Clear();
            TabSum.Columns.Add(Col("Показник", typeof(string)));
            TabSum.Columns.Add(Col("Значення", typeof(string)));

            TabSum.Rows.Clear();
            TabSum.Rows.Add("К-сть позицій", "0");
            TabSum.Rows.Add("Сумарна кількість", "0");
            TabSum.Rows.Add("Сумарна вартість", "0");
        }

        private void CreateDovids()
        {
            DovSklady.Columns.Clear();
            DovSklady.Columns.Add(Col("Склад", typeof(string)));
            DovSklady.Rows.Clear();
            DovSklady.Rows.Add("Склад №1");
            DovSklady.Rows.Add("Склад №2");
            DovSklady.Rows.Add("Склад №3");

            DovGrupa.Columns.Clear();
            DovGrupa.Columns.Add(Col("Група", typeof(string)));
            DovGrupa.Rows.Clear();

            // Категорії з методички/твого скріну
            string[] groups =
            {
                "Книги","CD","DVD","Мобілки","Плеєри","Аксесуари","Дисплеї","Корпуси","Блоки живлення","Клавіатури"
            };
            foreach (var g in groups) DovGrupa.Rows.Add(g);

            DovPostach.Columns.Clear();
            DovPostach.Columns.Add(Col("Постачальник", typeof(string)));
            DovPostach.Rows.Clear();
            DovPostach.Rows.Add("ТОВ Інтерсервіс");
            DovPostach.Rows.Add("ФОП Петренко");
            DovPostach.Rows.Add("Імпорт Плюс");

            DovOdin.Columns.Clear();
            DovOdin.Columns.Add(Col("Од.виміру", typeof(string)));
            DovOdin.Rows.Clear();
            DovOdin.Rows.Add("шт.");
            DovOdin.Rows.Add("кг");
            DovOdin.Rows.Add("л");
            DovOdin.Rows.Add("м");

            DovVyrobnyk.Columns.Clear();
            DovVyrobnyk.Columns.Add(Col("Виробник", typeof(string)));
            DovVyrobnyk.Rows.Clear();
            DovVyrobnyk.Rows.Add("Samsung");
            DovVyrobnyk.Rows.Add("Apple");
            DovVyrobnyk.Rows.Add("Sony");
            DovVyrobnyk.Rows.Add("Xiaomi");
        }

        public void ApplyFilter(string sklad, string grupa)
        {
            string f = "";

            if (!string.IsNullOrWhiteSpace(sklad))
                f = $"[Склад] = '{Escape(sklad)}'";

            if (!string.IsNullOrWhiteSpace(grupa))
            {
                if (f.Length > 0) f += " AND ";
                f += $"[Група] = '{Escape(grupa)}'";
            }

            ViewSklad.RowFilter = f;
        }

        private static string Escape(string s) => s.Replace("'", "''");

        public void RecalcRow(DataRow r)
        {
            decimal c = 0m;
            int k = 0;
            try { c = Convert.ToDecimal(r["Ціна"]); } catch { }
            try { k = Convert.ToInt32(r["Кількість"]); } catch { }

            r["Вартість"] = c * k;
        }

        public void UpdateSums(DataView view = null)
        {
            var v = view ?? ViewSklad;

            int pos = v.Count;
            int sumK = 0;
            decimal sumV = 0m;

            foreach (DataRowView rv in v)
            {
                try { sumK += Convert.ToInt32(rv["Кількість"]); } catch { }
                try { sumV += Convert.ToDecimal(rv["Вартість"]); } catch { }
            }

            TabSum.Rows[0]["Значення"] = pos.ToString();
            TabSum.Rows[1]["Значення"] = sumK.ToString();
            TabSum.Rows[2]["Значення"] = sumV.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static DataGridViewComboBoxColumn MakeCombo(string header, string dataProp, DataTable src, string field, string name, int width, bool readOnly = false)
        {
            return new DataGridViewComboBoxColumn
            {
                HeaderText = header,
                DataPropertyName = dataProp,
                DataSource = src,
                DisplayMember = field,
                ValueMember = field,
                Name = name,
                Width = width,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                ReadOnly = readOnly
            };
        }

        public void SaveToFile(string path)
        {
            using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
            {
                var cols = TabSklad.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
                sw.WriteLine(string.Join(";", cols));

                foreach (DataRow r in TabSklad.Rows)
                {
                    var vals = cols.Select(c => ToText(r[c]));
                    sw.WriteLine(string.Join(";", vals));
                }
            }
        }


        public void LoadFromFile(string path)
        {
            if (!File.Exists(path)) return;

            TabSklad.Rows.Clear();

            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8))
            {
                string header = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(header)) return;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var a = line.Split(';');
                    if (a.Length < TabSklad.Columns.Count) continue;

                    var r = TabSklad.NewRow();
                    for (int i = 0; i < TabSklad.Columns.Count; i++)
                    {
                        var col = TabSklad.Columns[i];
                        r[col.ColumnName] = FromText(a[i], col.DataType);
                    }

                    TabSklad.Rows.Add(r);
                }
            }
        }


        private static string ToText(object v)
        {
            if (v == null) return "";
            if (v is DateTime dt) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (v is decimal dc) return dc.ToString("0.00", CultureInfo.InvariantCulture);
            return v.ToString();
        }

        private static object FromText(string s, Type t)
        {
            s = (s ?? "").Trim();
            if (t == typeof(int))
            {
                int.TryParse(s, out int x);
                return x;
            }
            if (t == typeof(decimal))
            {
                decimal.TryParse(s.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal x);
                return x;
            }
            if (t == typeof(DateTime))
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    return dt;
                return DateTime.Today;
            }
            return s;
        }
    }
}
