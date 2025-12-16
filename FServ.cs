using System;
using System.Windows.Forms;

namespace l13._1
{
    public partial class FServ : Form
    {
        public string ValueText { get; private set; } = "";

        public FServ()
        {
            InitializeComponent();
        }

        private void FServ_Load(object sender, EventArgs e)
        {
        }

        private void BOk_Click(object sender, EventArgs e)
        {
            ValueText = TBValue.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
