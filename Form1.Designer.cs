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
            tapPage = new TabPage();
            tabControl1 = new TabControl();
            menuStrip1 = new MenuStrip();
            toolStrip_File = new ToolStripMenuItem();
            toolStrip_NewFile = new ToolStripMenuItem();
            toolStrip_Open = new ToolStripMenuItem();
            toolStripp_Save = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            groupBox2 = new GroupBox();
            button2 = new Button();
            button1 = new Button();
            button12 = new Button();
            button11 = new Button();
            btnDltTabPage = new Button();
            btnNewTabPage = new Button();
            btn_Save = new Button();
            btn_NewFile = new Button();
            btn_Open = new Button();
            label1 = new Label();
            label2 = new Label();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            tabControl1.SuspendLayout();
            menuStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // tapPage
            // 
            tapPage.BackColor = Color.White;
            tapPage.Location = new Point(4, 24);
            tapPage.Name = "tapPage";
            tapPage.Padding = new Padding(3);
            tapPage.Size = new Size(1371, 798);
            tapPage.TabIndex = 1;
            tapPage.Text = "tp1";
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tapPage);
            tabControl1.Location = new Point(134, 71);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1379, 826);
            tabControl1.TabIndex = 1;
            // 
            // menuStrip1
            // 
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
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(textBox2);
            groupBox2.Controls.Add(textBox1);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(button2);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(button12);
            groupBox2.Controls.Add(button11);
            groupBox2.Controls.Add(btnDltTabPage);
            groupBox2.Controls.Add(btnNewTabPage);
            groupBox2.Controls.Add(btn_Save);
            groupBox2.Controls.Add(btn_NewFile);
            groupBox2.Controls.Add(btn_Open);
            groupBox2.Location = new Point(12, 27);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1505, 41);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(318, 12);
            button2.Name = "button2";
            button2.Size = new Size(58, 23);
            button2.TabIndex = 4;
            button2.Text = "오회";
            button2.UseVisualStyleBackColor = true;
            button2.Click += btn_righthegreeClick;
            // 
            // button1
            // 
            button1.Location = new Point(254, 12);
            button1.Name = "button1";
            button1.Size = new Size(58, 23);
            button1.TabIndex = 3;
            button1.Text = "왼회";
            button1.UseVisualStyleBackColor = true;
            button1.Click += btn_leftdegreeClick;
            // 
            // button12
            // 
            button12.Location = new Point(219, 12);
            button12.Name = "button12";
            button12.Size = new Size(29, 23);
            button12.TabIndex = 2;
            button12.Text = "축";
            button12.UseVisualStyleBackColor = true;
            button12.Click += button12_Click;
            // 
            // button11
            // 
            button11.Location = new Point(184, 12);
            button11.Name = "button11";
            button11.Size = new Size(29, 23);
            button11.TabIndex = 2;
            button11.Text = "확";
            button11.UseVisualStyleBackColor = true;
            button11.Click += button11_Click;
            // 
            // btnDltTabPage
            // 
            btnDltTabPage.Location = new Point(151, 12);
            btnDltTabPage.Name = "btnDltTabPage";
            btnDltTabPage.Size = new Size(27, 23);
            btnDltTabPage.TabIndex = 1;
            btnDltTabPage.Text = "텡";
            btnDltTabPage.UseVisualStyleBackColor = true;
            btnDltTabPage.Click += btnDltTabPage_Click;
            // 
            // btnNewTabPage
            // 
            btnNewTabPage.Location = new Point(118, 12);
            btnNewTabPage.Name = "btnNewTabPage";
            btnNewTabPage.Size = new Size(27, 23);
            btnNewTabPage.TabIndex = 1;
            btnNewTabPage.Text = "탯";
            btnNewTabPage.UseVisualStyleBackColor = true;
            btnNewTabPage.Click += btnNewTabPage_Click;
            // 
            // btn_Save
            // 
            btn_Save.Location = new Point(84, 12);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(28, 23);
            btn_Save.TabIndex = 0;
            btn_Save.Text = "저";
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_NewFile
            // 
            btn_NewFile.Location = new Point(20, 12);
            btn_NewFile.Name = "btn_NewFile";
            btn_NewFile.Size = new Size(28, 23);
            btn_NewFile.TabIndex = 0;
            btn_NewFile.Text = "새";
            btn_NewFile.UseVisualStyleBackColor = true;
            btn_NewFile.Click += btn_NewFile_Click;
            // 
            // btn_Open
            // 
            btn_Open.Location = new Point(50, 12);
            btn_Open.Name = "btn_Open";
            btn_Open.Size = new Size(28, 23);
            btn_Open.TabIndex = 0;
            btn_Open.Text = "옾";
            btn_Open.UseVisualStyleBackColor = true;
            btn_Open.Click += btn_Open_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(407, 16);
            label1.Name = "label1";
            label1.Size = new Size(21, 15);
            label1.TabIndex = 5;
            label1.Text = "X :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(506, 16);
            label2.Name = "label2";
            label2.Size = new Size(21, 15);
            label2.TabIndex = 5;
            label2.Text = "Y :";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(434, 13);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(56, 23);
            textBox1.TabIndex = 6;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(533, 13);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(56, 23);
            textBox2.TabIndex = 6;
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
            tabControl1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TabPage tapPage;
        private TabControl tabControl1;
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
        private Button btnNewTabPage;
        private Button btnDltTabPage;
        private Button button12;
        private Button button11;
        private Button button2;
        private Button button1;
        private TextBox textBox2;
        private TextBox textBox1;
        private Label label2;
        private Label label1;
    }
}
