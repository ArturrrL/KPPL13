using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;

namespace l13._1
{
    public partial class Form1 : Form
    {
        private TSklad MySklad;

        // ==== друк ====
        private readonly PrintDocument printDocument1 = new PrintDocument();
        private string[] _printLines = new string[0];
        private int _printLineIndex = 0;

        // ==== назви колонок довідників (перші колонки) ====
        private string COL_SKLAD;
        private string COL_GRUPA;
        private string COL_POSTACH;
        private string COL_ODIN;

        // ==== “безпечні” (без крапок) поля для биндингу ComboBox/DataGridViewComboBoxColumn ====
        private string BIND_GRUPA;
        private string BIND_POSTACH;
        private string BIND_ODIN;

        public Form1()
        {
            InitializeComponent();

            // Стабільний друк у PDF/Preview
            printDocument1.OriginAtMargins = true;
            printDocument1.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);
            printDocument1.DefaultPageSettings.Landscape = true; // рядки широкі

            printDocument1.BeginPrint += printDocument1_BeginPrint;
            printDocument1.PrintPage += printDocument1_PrintPage;

            // важливо для ComboBox-колонок у гріді
            DGSklad.CurrentCellDirtyStateChanged += DGSklad_CurrentCellDirtyStateChanged;
            DGSklad.CellValidating += DGSklad_CellValidating;
            DGSklad.CellValueChanged += DGSklad_CellValueChanged;
            DGSklad.DataError += DGSklad_DataError;

            TVSklad.AfterSelect += TVSklad_AfterSelect;
        }

     
        private static string MakeSafeName(string name)
        {
            return (name ?? "")
                .Replace(".", "_")
                .Replace(" ", "_")
                .Replace("-", "_");
        }

        private string EnsureBindableFirstColumn(DataTable dt)
        {
            string col0 = dt.Columns[0].ColumnName;
            if (col0.IndexOf('.') < 0) return col0;

            string alias = MakeSafeName(col0);

            if (!dt.Columns.Contains(alias))
            {
                dt.Columns.Add(alias, typeof(string));

                foreach (DataRow r in dt.Rows)
                    r[alias] = r[col0];

                dt.ColumnChanged += (s, e) =>
                {
                    if (e.Column.ColumnName == col0)
                        e.Row[alias] = e.Row[col0];
                };

                dt.RowChanged += (s, e) =>
                {
                    if (e.Row.RowState != DataRowState.Deleted)
                        e.Row[alias] = e.Row[col0];
                };

                dt.TableNewRow += (s, e) =>
                {
                    try { e.Row[alias] = e.Row[col0]; } catch { }
                };
            }

            return alias;
        }

  
        private void Form1_Load(object sender, EventArgs e)
        {
            MySklad = new TSklad();

            InitDirFields();
            BindTopPanel();
            InitFilterSortControls();

            BuildTree();
            BindGrid();

            ApplyFilterSort(); 
            DGSkladSum.DataSource = MySklad.TabSum;
        }

        private void InitDirFields()
        {
            // перші колонки довідників
            COL_SKLAD = MySklad.DovSklady.Columns[0].ColumnName;
            COL_GRUPA = MySklad.DovGrupa.Columns[0].ColumnName;
            COL_POSTACH = MySklad.DovPostach.Columns[0].ColumnName;
            COL_ODIN = MySklad.DovOdin.Columns[0].ColumnName;

            BIND_GRUPA = EnsureBindableFirstColumn(MySklad.DovGrupa);
            BIND_POSTACH = EnsureBindableFirstColumn(MySklad.DovPostach);
            BIND_ODIN = EnsureBindableFirstColumn(MySklad.DovOdin);
        }

        private void BindTopPanel()
        {
            // ComboBox-и зверху (додавання)
            CBGrupa.DataSource = MySklad.DovGrupa;
            CBGrupa.DisplayMember = BIND_GRUPA;
            CBGrupa.ValueMember = BIND_GRUPA;

            CBPostachalnyk.DataSource = MySklad.DovPostach;
            CBPostachalnyk.DisplayMember = BIND_POSTACH;
            CBPostachalnyk.ValueMember = BIND_POSTACH;

            CBOdynytsi.DataSource = MySklad.DovOdin;
            CBOdynytsi.DisplayMember = BIND_ODIN;
            CBOdynytsi.ValueMember = BIND_ODIN;
        }

        private void InitFilterSortControls()
        {
            CBSortField.Items.Clear();
            CBSortField.Items.Add("Назва");
            CBSortField.Items.Add("Ціна");
            CBSortField.Items.Add("Кількість");
            CBSortField.Items.Add("Вартість");
            CBSortField.SelectedIndex = 0;


        }



        private void BuildTree()
        {
            TVSklad.Nodes.Clear();

            TreeNode rootAll = new TreeNode("Всі склади");
            rootAll.Tag = new Tuple<string, string>(null, null);
            TVSklad.Nodes.Add(rootAll);

            foreach (DataRow w in MySklad.DovSklady.Rows)
            {
                string sklad = Convert.ToString(w[COL_SKLAD]);
                TreeNode nS = new TreeNode(sklad);
                nS.Tag = new Tuple<string, string>(sklad, null);

                foreach (DataRow g in MySklad.DovGrupa.Rows)
                {
                    string grupa = Convert.ToString(g[COL_GRUPA]);
                    TreeNode nG = new TreeNode(grupa);
                    nG.Tag = new Tuple<string, string>(sklad, grupa);
                    nS.Nodes.Add(nG);
                }

                TVSklad.Nodes.Add(nS);
            }

            TVSklad.ExpandAll();
            TVSklad.SelectedNode = rootAll;
        }

        private void TVSklad_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Tuple<string, string> t = e.Node != null ? e.Node.Tag as Tuple<string, string> : null;
            if (t != null)
            {
                MySklad.ApplyFilter(t.Item1, t.Item2);
                ApplyFilterSort();
            }
        }

        private string GetSelectedSklad()
        {
            Tuple<string, string> t = TVSklad.SelectedNode != null ? TVSklad.SelectedNode.Tag as Tuple<string, string> : null;
            return t != null ? t.Item1 : null;
        }


        private void BindGrid()
        {
            DGSklad.Columns.Clear();
            DGSklad.AutoGenerateColumns = false;

            // №
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "№",
                DataPropertyName = "№",
                Name = "№",
                Width = 50
            });

            // Склад (ReadOnly щоб не телепортувались рядки у фільтрі дерева)
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Склад",
                DataPropertyName = "Склад",
                Name = "Склад",
                ReadOnly = true,
                Width = 110
            });

            // Група (Combo)
            DGSklad.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Група",
                Name = "Група",
                DataPropertyName = "Група",
                DataSource = MySklad.DovGrupa,
                DisplayMember = BIND_GRUPA,
                ValueMember = BIND_GRUPA,
                Width = 120,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            });

            // Назва
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Назва",
                DataPropertyName = "Назва",
                Name = "Назва",
                Width = 170
            });

            // Виробник — ТЕКСТ (ти так хотіла)
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Виробник",
                DataPropertyName = "Виробник",
                Name = "Виробник",
                Width = 150
            });

            // Постачальник (Combo)
            DGSklad.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Постачальник",
                Name = "Постачальник",
                DataPropertyName = "Постачальник",
                DataSource = MySklad.DovPostach,
                DisplayMember = BIND_POSTACH,
                ValueMember = BIND_POSTACH,
                Width = 160,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            });

            // Од.виміру (Combo) — DisplayMember без крапки
            DGSklad.Columns.Add(new DataGridViewComboBoxColumn
            {
                HeaderText = "Од.виміру",
                Name = "Од.виміру",
                DataPropertyName = "Од.виміру",
                DataSource = MySklad.DovOdin,
                DisplayMember = BIND_ODIN,
                ValueMember = BIND_ODIN,
                Width = 90,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            });

            // Ціна, Кількість, Вартість
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ціна", DataPropertyName = "Ціна", Name = "Ціна", Width = 80 });
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Кількість", DataPropertyName = "Кількість", Name = "Кількість", Width = 90 });
            DGSklad.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Вартість", DataPropertyName = "Вартість", Name = "Вартість", ReadOnly = true, Width = 100 });

            DGSklad.DataSource = MySklad.ViewSklad;
        }

        private void DGSklad_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (DGSklad.IsCurrentCellDirty)
                DGSklad.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DGSklad_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        
            e.ThrowException = false;
        }

        private void DGSklad_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (DGSklad.Rows[e.RowIndex].IsNewRow) return;

            string col = DGSklad.Columns[e.ColumnIndex].Name;

            if (col == "Ціна")
            {
                decimal tmp;
                if (!decimal.TryParse(Convert.ToString(e.FormattedValue).Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out tmp))
                {
                    MessageBox.Show("Введіть числове значення у поле 'Ціна'.");
                    e.Cancel = true;
                }
            }

            if (col == "Кількість")
            {
                int tmp;
                if (!int.TryParse(Convert.ToString(e.FormattedValue), out tmp))
                {
                    MessageBox.Show("Введіть цілочислове значення у поле 'Кількість'.");
                    e.Cancel = true;
                }
            }
        }

        private void DGSklad_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (DGSklad.Rows[e.RowIndex].IsNewRow) return;

            DataRowView rv = DGSklad.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (rv == null) return;

            string name = DGSklad.Columns[e.ColumnIndex].Name;
            if (name == "Ціна" || name == "Кількість")
            {
                MySklad.RecalcRow(rv.Row);
                MySklad.UpdateSums(MySklad.ViewSklad);
            }
        }

        

        private void BApplyFS_Click(object sender, EventArgs e)
        {
            ApplyFilterSort();
        }

        private void BClearFS_Click(object sender, EventArgs e)
        {
            TBFilterName.Text = "";
            CHSortDesc.Checked = false;
            if (CBSortField.Items.Count > 0) CBSortField.SelectedIndex = 0;

            Tuple<string, string> t = TVSklad.SelectedNode != null ? TVSklad.SelectedNode.Tag as Tuple<string, string> : null;
            if (t != null) MySklad.ApplyFilter(t.Item1, t.Item2);

            ApplyFilterSort();
        }

        private void ApplyFilterSort()
        {
            // базовий фільтр дерева уже встановив ApplyFilter(...)
            string baseFilter = MySklad.ViewSklad.RowFilter ?? "";

            // додатковий фільтр по назві
            string extra = "";
            string q = (TBFilterName.Text ?? "").Trim();
            if (q.Length > 0)
            {
                q = q.Replace("'", "''");
                extra = string.Format("Назва LIKE '%{0}%'", q);
            }

            string combined;
            if (baseFilter.Length > 0 && extra.Length > 0)
                combined = "(" + baseFilter + ") AND (" + extra + ")";
            else
                combined = baseFilter.Length > 0 ? baseFilter : extra;

            MySklad.ViewSklad.RowFilter = combined;

            // сортування
            string field = CBSortField.SelectedItem != null ? CBSortField.SelectedItem.ToString() : "Назва";
            string dir = CHSortDesc.Checked ? " DESC" : " ASC";
            MySklad.ViewSklad.Sort = field + dir;

            MySklad.UpdateSums(MySklad.ViewSklad);
        }

        // ------------------------------------------------------------
        //  Add (верхня панель)
        // ------------------------------------------------------------

        private void BAdd_Click(object sender, EventArgs e)
        {
            string sklad = GetSelectedSklad() ?? "Склад №1";
            string grupa = CBGrupa.SelectedValue != null ? CBGrupa.SelectedValue.ToString() : "";
            string nazva = (TBNazva.Text ?? "").Trim();

            // виробник вводимо текстом
            string vyr = (TBVyrobnyk.Text ?? "").Trim();

            string post = CBPostachalnyk.SelectedValue != null ? CBPostachalnyk.SelectedValue.ToString() : "";
            string od = CBOdynytsi.SelectedValue != null ? CBOdynytsi.SelectedValue.ToString() : "";

            if (nazva.Length == 0)
            {
                MessageBox.Show("Введи назву товару.");
                return;
            }

            decimal cin;
            if (!decimal.TryParse((TBCina.Text ?? "").Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out cin))
            {
                MessageBox.Show("Ціна має бути числом (decimal).");
                return;
            }

            int kilk;
            if (!int.TryParse(TBKilkist.Text ?? "", out kilk))
            {
                MessageBox.Show("Кількість має бути цілим числом (int).");
                return;
            }

            DataRow r = MySklad.TabSklad.NewRow();
            r["№"] = MySklad.TabSklad.Rows.Count + 1;
            r["Склад"] = sklad;
            r["Група"] = grupa;
            r["Назва"] = nazva;
            r["Виробник"] = vyr;
            r["Постачальник"] = post;
            r["Од.виміру"] = od;
            r["Ціна"] = cin;
            r["Кількість"] = kilk;

            MySklad.RecalcRow(r);
            MySklad.TabSklad.Rows.Add(r);

            ApplyFilterSort();
        }

   

        private void miOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Sklad files (*.sklad)|*.sklad|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    MySklad.LoadFromFile(ofd.FileName);

                   
                    InitDirFields();
                    BindTopPanel();
                    BuildTree();
                    BindGrid();

              
                    Tuple<string, string> t = TVSklad.SelectedNode != null ? TVSklad.SelectedNode.Tag as Tuple<string, string> : null;
                    if (t != null) MySklad.ApplyFilter(t.Item1, t.Item2);

                    ApplyFilterSort();
                }
            }
        }

        private void miSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Sklad files (*.sklad)|*.sklad|All files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    MySklad.SaveToFile(sfd.FileName);
                    MessageBox.Show("Збережено.");
                }
            }
        }

        private void miPrint_Click(object sender, EventArgs e)
        {
            _printLineIndex = 0;

            using (PrintPreviewDialog ppd = new PrintPreviewDialog())
            {
                ppd.Document = printDocument1;
                ppd.Width = 1100;
                ppd.Height = 750;
                ppd.ShowDialog();
            }
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Close();
        }



        private void printDocument1_BeginPrint(object sender, PrintEventArgs e)
        {
            var list = new System.Collections.Generic.List<string>();

            // друкуємо саме поточний відбір (після ApplyFilterSort)
            for (int i = 0; i < MySklad.ViewSklad.Count; i++)
            {
                DataRowView rv = MySklad.ViewSklad[i];

                string line = string.Format(
                    "{0,3} | {1,-10} | {2,-14} | {3,-20} | {4,-14} | {5,-14} | {6,-6} | {7,4} | {8,8} | {9,10}",
                    rv["№"],
                    rv["Склад"],
                    rv["Група"],
                    rv["Назва"],
                    rv["Виробник"],
                    rv["Постачальник"],
                    rv["Од.виміру"],
                    rv["Кількість"],
                    rv["Ціна"],
                    rv["Вартість"]
                );

                list.Add(line);
            }

            if (list.Count == 0)
                list.Add("Немає даних для друку.");

            _printLines = list.ToArray();
            _printLineIndex = 0;
        }

        private void printDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            Font font = new Font("Consolas", 11);
            try
            {
                float x = 0; // OriginAtMargins = true
                float y = 0;

                float lineH = font.GetHeight(e.Graphics) + 2;
                float maxY = e.MarginBounds.Height - lineH;

                while (_printLineIndex < _printLines.Length)
                {
                    if (y > maxY)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    e.Graphics.DrawString(_printLines[_printLineIndex], font, Brushes.Black, x, y);
                    y += lineH;
                    _printLineIndex++;
                }

                e.HasMorePages = false;
            }
            finally
            {
                font.Dispose();
            }
        }

  

        private void miDovids_Click(object sender, EventArgs e)
        {
            ContextMenuStrip m = new ContextMenuStrip();

            m.Items.Add("Склади", null, (s, a) =>
            {
                using (FDirEdit f = new FDirEdit(MySklad.DovSklady, COL_SKLAD, "Довідник складів"))
                    f.ShowDialog();
                BuildTree();
            });

            m.Items.Add("Групи", null, (s, a) =>
            {
                using (FDirEdit f = new FDirEdit(MySklad.DovGrupa, COL_GRUPA, "Довідник груп"))
                    f.ShowDialog();
                InitDirFields();
                BindTopPanel();
                BuildTree();
                BindGrid();
                ApplyFilterSort();
            });

            m.Items.Add("Постачальники", null, (s, a) =>
            {
                using (FDirEdit f = new FDirEdit(MySklad.DovPostach, COL_POSTACH, "Довідник постачальників"))
                    f.ShowDialog();
                InitDirFields();
                BindTopPanel();
                BindGrid();
                ApplyFilterSort();
            });

            m.Items.Add("Одиниці виміру", null, (s, a) =>
            {
                using (FDirEdit f = new FDirEdit(MySklad.DovOdin, COL_ODIN, "Довідник одиниць виміру"))
                    f.ShowDialog();
                InitDirFields();
                BindTopPanel();
                BindGrid();
                ApplyFilterSort();
            });

            m.Show(Cursor.Position);
        }

        private void miStats_Click(object sender, EventArgs e)
        {
            using (FStats f = new FStats(MySklad))
                f.ShowDialog();
        }
    }
}

