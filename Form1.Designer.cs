namespace photo
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabPage2 = new TabPage();
            tableLayoutPanel1 = new TableLayoutPanel();
            pictureBox1 = new PictureBox();
            tabControl1 = new TabControl();
            menuStrip1 = new MenuStrip();
            toolStrip_File = new ToolStripMenuItem();
            toolStrip_NewFile = new ToolStripMenuItem();
            toolStrip_Open = new ToolStripMenuItem();
            toolStripp_Save = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            groupBox2 = new GroupBox();
            btn_Save = new Button();
            btn_NewFile = new Button();
            btn_Open = new Button();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabControl1.SuspendLayout();
            menuStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(tableLayoutPanel1);
            tabPage2.Controls.Add(pictureBox1);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1379, 826);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Location = new Point(1389, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(172, 427);
            tableLayoutPanel1.TabIndex = 4;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(61, 28);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(472, 427);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Location = new Point(134, 71);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1387, 854);
            tabControl1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStrip_File, toolStripMenuItem3 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1711, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStrip_File
            // 
            toolStrip_File.DropDownItems.AddRange(new ToolStripItem[] { toolStrip_NewFile, toolStrip_Open, toolStripp_Save });
            toolStrip_File.Name = "toolStrip_File";
            toolStrip_File.Size = new Size(43, 20);
            toolStrip_File.Text = "파일";
            // 
            // toolStrip_NewFile
            // 
            toolStrip_NewFile.Name = "toolStrip_NewFile";
            toolStrip_NewFile.ShortcutKeys = Keys.Control | Keys.N;
            toolStrip_NewFile.Size = new Size(181, 22);
            toolStrip_NewFile.Text = "새로 만들기";
            // 
            // toolStrip_Open
            // 
            toolStrip_Open.Name = "toolStrip_Open";
            toolStrip_Open.ShortcutKeys = Keys.Control | Keys.O;
            toolStrip_Open.Size = new Size(181, 22);
            toolStrip_Open.Text = "열기";
            // 
            // toolStripp_Save
            // 
            toolStripp_Save.Name = "toolStripp_Save";
            toolStripp_Save.ShortcutKeys = Keys.Control | Keys.S;
            toolStripp_Save.Size = new Size(181, 22);
            toolStripp_Save.Text = "저장하기";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(127, 20);
            toolStripMenuItem3.Text = "toolStripMenuItem3";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(btn_Save);
            groupBox2.Controls.Add(btn_NewFile);
            groupBox2.Controls.Add(btn_Open);
            groupBox2.Location = new Point(12, 27);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1505, 41);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(73, 12);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(28, 23);
            btn_Save.TabIndex = 0;
            btn_Save.Text = "저";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_NewFile
            // 
            btn_NewFile.Location = new Point(6, 12);
            btn_NewFile.Name = "btn_NewFile";
            btn_NewFile.Size = new Size(28, 23);
            btn_NewFile.TabIndex = 0;
            btn_NewFile.Text = "새";
            btn_NewFile.UseVisualStyleBackColor = true;
            btn_NewFile.Click += btn_NewFile_Click;
            // 
            // btn_Open
            // 
            btn_Open.Location = new Point(39, 12);
            btn_Open.Name = "btn_Open";
            btn_Open.Size = new Size(28, 23);
            btn_Open.TabIndex = 0;
            btn_Open.Text = "옾";
            btn_Open.UseVisualStyleBackColor = true;
            btn_Open.Click += btn_Open_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1711, 954);
            Controls.Add(groupBox2);
            Controls.Add(tabControl1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabControl1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TabPage tabPage2;
        private TabControl tabControl1;
        private Button button12;
        private Button btn_Road;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStrip_File;
        private ToolStripMenuItem toolStrip_Open;
        private ToolStripMenuItem toolStripp_Save;
        private ToolStripMenuItem toolStripMenuItem3;
        private GroupBox groupBox2;
        private Button btn_Open;
        private ToolStripMenuItem toolStrip_NewFile;
        private Button btn_Save;
        private Button btn_NewFile;
        private PictureBox pictureBox1;
        private TableLayoutPanel tableLayoutPanel1;
    }
}
