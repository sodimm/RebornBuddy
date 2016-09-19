using System;
using System.Windows.Forms;

namespace Sparrow
{
    public partial class SparrowForm : Form
    {
        private ComboBox cbox_Feeds;
        private Button btn_Cancel;
        private Button btn_Confirm;
        private ComboBox cbox_Stances;

        public SparrowForm()
        {
            InitializeComponent();
        }

        private void SparrowForm_Load(object sender, EventArgs e)
        {
            cbox_Stances.Text = SparrowSettings.Instance.Stance;
            cbox_Feeds.Text = SparrowSettings.Instance.Feed;
        }

        private void btn_Confirm_Click(object sender, EventArgs e)
        {
            SparrowSettings.Instance.Stance = cbox_Stances.Text;
            SparrowSettings.Instance.Feed = cbox_Feeds.Text;
            Close();
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void InitializeComponent()
        {
            cbox_Stances = new ComboBox();
            cbox_Feeds = new ComboBox();
            btn_Cancel = new Button();
            btn_Confirm = new Button();
            SuspendLayout();
            // 
            // cbox_Stances
            // 
            cbox_Stances.BackColor = System.Drawing.Color.Aquamarine;
            cbox_Stances.FlatStyle = FlatStyle.Flat;
            cbox_Stances.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, (((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((0)));
            cbox_Stances.ForeColor = System.Drawing.Color.Teal;
            cbox_Stances.FormattingEnabled = true;
            cbox_Stances.Items.AddRange(new object[] {
            "Attacker",
            "Defender",
            "Free",
            "Healer"});
            cbox_Stances.Location = new System.Drawing.Point(10, 10);
            cbox_Stances.Name = "cbox_Stances";
            cbox_Stances.Size = new System.Drawing.Size(275, 21);
            cbox_Stances.Sorted = true;
            cbox_Stances.TabIndex = 0;
            cbox_Stances.Text = "Which Stance?";
            // 
            // cbox_Feeds
            // 
            cbox_Feeds.BackColor = System.Drawing.Color.Aquamarine;
            cbox_Feeds.FlatStyle = FlatStyle.Flat;
            cbox_Feeds.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, (((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((0)));
            cbox_Feeds.ForeColor = System.Drawing.Color.Teal;
            cbox_Feeds.FormattingEnabled = true;
            cbox_Feeds.Items.AddRange(new object[] {
            "Curiel Root",
            "Mimett Gourd",
            "None",
            "Pahsana Fruit",
            "Sylkis Bud",
            "Tantalplant"});
            cbox_Feeds.Location = new System.Drawing.Point(10, 37);
            cbox_Feeds.Name = "cbox_Feeds";
            cbox_Feeds.Size = new System.Drawing.Size(275, 21);
            cbox_Feeds.Sorted = true;
            cbox_Feeds.TabIndex = 1;
            cbox_Feeds.Text = "Which Feed?";
            // 
            // btn_Cancel
            // 
            btn_Cancel.BackColor = System.Drawing.Color.Aquamarine;
            btn_Cancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((((192)))), ((((255)))), ((((255)))));
            btn_Cancel.FlatStyle = FlatStyle.Flat;
            btn_Cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, (((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((0)));
            btn_Cancel.ForeColor = System.Drawing.Color.Teal;
            btn_Cancel.Location = new System.Drawing.Point(185, 64);
            btn_Cancel.Name = "btn_Cancel";
            btn_Cancel.Size = new System.Drawing.Size(100, 22);
            btn_Cancel.TabIndex = 2;
            btn_Cancel.Text = "Cancel";
            btn_Cancel.UseVisualStyleBackColor = false;
            btn_Cancel.Click += new EventHandler(this.btn_Cancel_Click);
            // 
            // btn_Confirm
            // 
            btn_Confirm.BackColor = System.Drawing.Color.Aquamarine;
            btn_Confirm.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((((192)))), ((((255)))), ((((255)))));
            btn_Confirm.FlatStyle = FlatStyle.Flat;
            btn_Confirm.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, (((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((0)));
            btn_Confirm.ForeColor = System.Drawing.Color.Teal;
            btn_Confirm.Location = new System.Drawing.Point(10, 64);
            btn_Confirm.Name = "btn_Confirm";
            btn_Confirm.Size = new System.Drawing.Size(100, 22);
            btn_Confirm.TabIndex = 3;
            btn_Confirm.Text = "Confirm";
            btn_Confirm.UseVisualStyleBackColor = false;
            btn_Confirm.Click += new EventHandler(this.btn_Confirm_Click);
            // 
            // SparrowForm
            // 
            BackColor = System.Drawing.Color.Teal;
            ClientSize = new System.Drawing.Size(295, 100);
            Controls.Add(btn_Confirm);
            Controls.Add(btn_Cancel);
            Controls.Add(cbox_Feeds);
            Controls.Add(cbox_Stances);
            Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((0)));
            FormBorderStyle = FormBorderStyle.None;
            Name = "SparrowForm";
            Opacity = 0.75D;
            ShowIcon = false;
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            TopMost = true;
            TransparencyKey = System.Drawing.Color.Green;
            Load += new EventHandler(SparrowForm_Load);
            ResumeLayout(false);

        }
    }
}
