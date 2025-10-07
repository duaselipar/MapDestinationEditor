using System.Windows.Forms;
using System.Drawing;

namespace MapDestinationEditorA
{
    partial class Form1 : Form
    {
        private System.ComponentModel.IContainer components = null;

        // Controls (free layout)
        private Button btnLoad;

        private Label lblName;
        private TextBox txtName;
        private Label lblInfo;
        private TextBox txtInfo;
        private Label lblMenu;
        private TextBox txtMenu;
        private Label lblParent;
        private TextBox txtParent;
        private Label lblX;
        private TextBox txtX;
        private Label lblY;
        private TextBox txtY;
        private Label lblMap;
        private TextBox txtMap;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            btnLoad = new Button();
            lblName = new Label();
            txtName = new TextBox();
            lblInfo = new Label();
            txtInfo = new TextBox();
            lblMenu = new Label();
            txtMenu = new TextBox();
            lblParent = new Label();
            txtParent = new TextBox();
            lblX = new Label();
            txtX = new TextBox();
            lblY = new Label();
            txtY = new TextBox();
            lblMap = new Label();
            txtMap = new TextBox();
            dataGridView1 = new DataGridView();
            Column1 = new DataGridViewTextBoxColumn();
            Column2 = new DataGridViewTextBoxColumn();
            dataGridView2 = new DataGridView();
            Column3 = new DataGridViewTextBoxColumn();
            dataGridView3 = new DataGridView();
            Column4 = new DataGridViewTextBoxColumn();
            textBox1 = new TextBox();
            label1 = new Label();
            btnFind = new Button();
            btnSave = new Button();
            btnUpdate = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).BeginInit();
            SuspendLayout();
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(706, 11);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(75, 25);
            btnLoad.TabIndex = 0;
            btnLoad.Text = "Load";
            btnLoad.Click += btnLoad_Click;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(675, 56);
            lblName.Name = "lblName";
            lblName.Size = new Size(45, 15);
            lblName.TabIndex = 2;
            lblName.Text = "Name :";
            // 
            // txtName
            // 
            txtName.Location = new Point(726, 53);
            txtName.Name = "txtName";
            txtName.Size = new Size(269, 23);
            txtName.TabIndex = 3;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(686, 88);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(34, 15);
            lblInfo.TabIndex = 4;
            lblInfo.Text = "Info :";
            // 
            // txtInfo
            // 
            txtInfo.Location = new Point(726, 85);
            txtInfo.Multiline = true;
            txtInfo.Name = "txtInfo";
            txtInfo.ScrollBars = ScrollBars.Vertical;
            txtInfo.Size = new Size(269, 130);
            txtInfo.TabIndex = 5;
            // 
            // lblMenu
            // 
            lblMenu.AutoSize = true;
            lblMenu.Location = new Point(662, 282);
            lblMenu.Name = "lblMenu";
            lblMenu.Size = new Size(58, 15);
            lblMenu.TabIndex = 6;
            lblMenu.Text = "Menu ID :";
            // 
            // txtMenu
            // 
            txtMenu.Location = new Point(726, 279);
            txtMenu.Name = "txtMenu";
            txtMenu.Size = new Size(91, 23);
            txtMenu.TabIndex = 7;
            // 
            // lblParent
            // 
            lblParent.AutoSize = true;
            lblParent.Location = new Point(837, 254);
            lblParent.Name = "lblParent";
            lblParent.Size = new Size(61, 15);
            lblParent.TabIndex = 8;
            lblParent.Text = "Parent ID :";
            // 
            // txtParent
            // 
            txtParent.Location = new Point(904, 250);
            txtParent.Name = "txtParent";
            txtParent.Size = new Size(91, 23);
            txtParent.TabIndex = 9;
            // 
            // lblX
            // 
            lblX.AutoSize = true;
            lblX.Location = new Point(700, 224);
            lblX.Name = "lblX";
            lblX.Size = new Size(20, 15);
            lblX.TabIndex = 10;
            lblX.Text = "X :";
            // 
            // txtX
            // 
            txtX.Location = new Point(726, 221);
            txtX.Name = "txtX";
            txtX.Size = new Size(91, 23);
            txtX.TabIndex = 11;
            // 
            // lblY
            // 
            lblY.AutoSize = true;
            lblY.Location = new Point(878, 224);
            lblY.Name = "lblY";
            lblY.Size = new Size(20, 15);
            lblY.TabIndex = 12;
            lblY.Text = "Y :";
            // 
            // txtY
            // 
            txtY.Location = new Point(904, 221);
            txtY.Name = "txtY";
            txtY.Size = new Size(91, 23);
            txtY.TabIndex = 13;
            // 
            // lblMap
            // 
            lblMap.AutoSize = true;
            lblMap.Location = new Point(669, 254);
            lblMap.Name = "lblMap";
            lblMap.Size = new Size(51, 15);
            lblMap.TabIndex = 14;
            lblMap.Text = "Map ID :";
            // 
            // txtMap
            // 
            txtMap.Location = new Point(726, 250);
            txtMap.Name = "txtMap";
            txtMap.Size = new Size(91, 23);
            txtMap.TabIndex = 15;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Column1, Column2 });
            dataGridView1.Location = new Point(17, 40);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridView1.ScrollBars = ScrollBars.Vertical;
            dataGridView1.Size = new Size(223, 422);
            dataGridView1.TabIndex = 18;
            // 
            // Column1
            // 
            Column1.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Column1.Frozen = true;
            Column1.HeaderText = "ID";
            Column1.Name = "Column1";
            Column1.ReadOnly = true;
            Column1.Resizable = DataGridViewTriState.False;
            Column1.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column1.Width = 70;
            // 
            // Column2
            // 
            Column2.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Column2.Frozen = true;
            Column2.HeaderText = "Name";
            Column2.Name = "Column2";
            Column2.ReadOnly = true;
            Column2.Resizable = DataGridViewTriState.False;
            Column2.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column2.Width = 150;
            // 
            // dataGridView2
            // 
            dataGridView2.AllowUserToAddRows = false;
            dataGridView2.AllowUserToDeleteRows = false;
            dataGridView2.AllowUserToResizeColumns = false;
            dataGridView2.AllowUserToResizeRows = false;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Columns.AddRange(new DataGridViewColumn[] { Column3 });
            dataGridView2.Location = new Point(246, 40);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.ReadOnly = true;
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.ScrollBars = ScrollBars.Vertical;
            dataGridView2.Size = new Size(202, 422);
            dataGridView2.TabIndex = 19;
            // 
            // Column3
            // 
            Column3.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Column3.Frozen = true;
            Column3.HeaderText = "Path";
            Column3.Name = "Column3";
            Column3.ReadOnly = true;
            Column3.Resizable = DataGridViewTriState.False;
            Column3.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column3.Width = 200;
            // 
            // dataGridView3
            // 
            dataGridView3.AllowUserToAddRows = false;
            dataGridView3.AllowUserToDeleteRows = false;
            dataGridView3.AllowUserToResizeColumns = false;
            dataGridView3.AllowUserToResizeRows = false;
            dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView3.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView3.Columns.AddRange(new DataGridViewColumn[] { Column4 });
            dataGridView3.Location = new Point(454, 40);
            dataGridView3.Name = "dataGridView3";
            dataGridView3.ReadOnly = true;
            dataGridView3.RowHeadersVisible = false;
            dataGridView3.ScrollBars = ScrollBars.Vertical;
            dataGridView3.Size = new Size(202, 422);
            dataGridView3.TabIndex = 20;
            // 
            // Column4
            // 
            Column4.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            Column4.Frozen = true;
            Column4.HeaderText = "Path";
            Column4.Name = "Column4";
            Column4.ReadOnly = true;
            Column4.Resizable = DataGridViewTriState.False;
            Column4.SortMode = DataGridViewColumnSortMode.NotSortable;
            Column4.Width = 200;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(91, 11);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(528, 23);
            textBox1.TabIndex = 21;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 14);
            label1.Name = "label1";
            label1.Size = new Size(71, 15);
            label1.TabIndex = 22;
            label1.Text = "Client Path :";
            // 
            // btnFind
            // 
            btnFind.Location = new Point(625, 11);
            btnFind.Name = "btnFind";
            btnFind.Size = new Size(75, 25);
            btnFind.TabIndex = 23;
            btnFind.Text = "Find";
            btnFind.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(902, 404);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(91, 58);
            btnSave.TabIndex = 24;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new Point(904, 279);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(91, 25);
            btnUpdate.TabIndex = 25;
            btnUpdate.Text = "Update";
            btnUpdate.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1011, 477);
            Controls.Add(btnUpdate);
            Controls.Add(btnSave);
            Controls.Add(btnFind);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(dataGridView3);
            Controls.Add(dataGridView2);
            Controls.Add(dataGridView1);
            Controls.Add(btnLoad);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblInfo);
            Controls.Add(txtInfo);
            Controls.Add(lblMenu);
            Controls.Add(txtMenu);
            Controls.Add(lblParent);
            Controls.Add(txtParent);
            Controls.Add(lblX);
            Controls.Add(txtX);
            Controls.Add(lblY);
            Controls.Add(txtY);
            Controls.Add(lblMap);
            Controls.Add(txtMap);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "MapDestination.dat Editor - By DuaSelipar";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView3).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private DataGridView dataGridView1;
        private DataGridView dataGridView2;
        private DataGridView dataGridView3;
        private TextBox textBox1;
        private Label label1;
        private Button btnFind;
        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewTextBoxColumn Column3;
        private DataGridViewTextBoxColumn Column4;
        private Button btnSave;
        private Button btnUpdate;
    }
}
