using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace l13._1
{
        public partial class FDirEdit : Form
        {
            private readonly DataTable _table;
            private readonly string _fieldName;

            public FDirEdit(DataTable table, string fieldName, string title)
            {
                InitializeComponent();
                _table = table;
                _fieldName = fieldName;
                Text = title;

                DG.DataSource = _table;
                DG.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            private void BAdd_Click(object sender, EventArgs e)
            {
                var r = _table.NewRow();
                r[_fieldName] = "Новий";
                _table.Rows.Add(r);
            }

            private void BDel_Click(object sender, EventArgs e)
            {
                if (DG.CurrentRow == null) return;
                DG.Rows.Remove(DG.CurrentRow);
            }

            private void BClose_Click(object sender, EventArgs e) => Close();
        }
    }
