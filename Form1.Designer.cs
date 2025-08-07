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
            textBox3 = new TextBox();
            tabControl1 = new TabControl();
            menuStrip1 = new MenuStrip();
            toolStrip_File = new ToolStripMenuItem();
            toolStrip_NewFile = new ToolStripMenuItem();
            toolStrip_Open = new ToolStripMenuItem();
            toolStripp_Save = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            groupBox2 = new GroupBox();
            label6 = new Label();
            label5 = new Label();
            button13 = new Button();
            textBox4 = new TextBox();
            button10 = new Button();
            label4 = new Label();
            button9 = new Button();
            label3 = new Label();
            button8 = new Button();
            button7 = new Button();
            button6 = new Button();
            button5 = new Button();
            button4 = new Button();
            button3 = new Button();
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            label2 = new Label();
            label1 = new Label();
            button2 = new Button();
            button1 = new Button();
            btn_zoomout = new Button();
            btn_zoomin = new Button();
            btnDltTabPage = new Button();
            btnNewTabPage = new Button();
            btn_Save = new Button();
            btn_NewFile = new Button();
            btn_Open = new Button();
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
            tapPage.Size = new Size(1584, 798);
            tapPage.TabIndex = 1;
            tapPage.Text = "tp1";
            // 
            // textBox3
            // 
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.Location = new Point(1514, 11);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(56, 23);
            textBox3.TabIndex = 2;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tapPage);
            tabControl1.Location = new Point(134, 89);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1592, 826);
            tabControl1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(255, 246, 246);
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStrip_File, toolStripMenuItem3 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1924, 24);
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
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(button13);
            groupBox2.Controls.Add(textBox4);
            groupBox2.Controls.Add(textBox3);
            groupBox2.Controls.Add(button10);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(button9);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(button8);
            groupBox2.Controls.Add(button7);
            groupBox2.Controls.Add(button6);
            groupBox2.Controls.Add(button5);
            groupBox2.Controls.Add(button4);
            groupBox2.Controls.Add(button3);
            groupBox2.Controls.Add(textBox2);
            groupBox2.Controls.Add(textBox1);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(button2);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(btn_zoomout);
            groupBox2.Controls.Add(btn_zoomin);
            groupBox2.Controls.Add(btnDltTabPage);
            groupBox2.Controls.Add(btnNewTabPage);
            groupBox2.Controls.Add(btn_Save);
            groupBox2.Controls.Add(btn_NewFile);
            groupBox2.Controls.Add(btn_Open);
            groupBox2.Location = new Point(12, 27);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1882, 62);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(1410, 14);
            label6.Name = "label6";
            label6.Size = new Size(71, 15);
            label6.TabIndex = 0;
            label6.Text = "이미지 위치";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(1215, 15);
            label5.Name = "label5";
            label5.Size = new Size(71, 15);
            label5.TabIndex = 0;
            label5.Text = "이미지 크기";
            // 
            // button13
            // 
            button13.BackColor = Color.Transparent;
            button13.BackgroundImage = Properties.Resources.m_15;
            button13.BackgroundImageLayout = ImageLayout.Stretch;
            button13.FlatAppearance.BorderSize = 0;
            button13.FlatStyle = FlatStyle.Flat;
            button13.Location = new Point(873, 17);
            button13.Name = "button13";
            button13.Size = new Size(35, 35);
            button13.TabIndex = 7;
            button13.UseVisualStyleBackColor = false;
            button13.Click += button13_Click;
            // 
            // textBox4
            // 
            textBox4.BorderStyle = BorderStyle.None;
            textBox4.Location = new Point(1514, 36);
            textBox4.Multiline = true;
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(56, 23);
            textBox4.TabIndex = 2;
            // 
            // button10
            // 
            button10.BackColor = Color.Transparent;
            button10.BackgroundImage = Properties.Resources.m_18;
            button10.BackgroundImageLayout = ImageLayout.Stretch;
            button10.FlatAppearance.BorderSize = 0;
            button10.FlatStyle = FlatStyle.Flat;
            button10.Location = new Point(1056, 17);
            button10.Name = "button10";
            button10.Size = new Size(35, 35);
            button10.TabIndex = 7;
            button10.UseVisualStyleBackColor = false;
            button10.Click += button10_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(1487, 39);
            label4.Name = "label4";
            label4.Size = new Size(21, 15);
            label4.TabIndex = 0;
            label4.Text = "Y :";
            // 
            // button9
            // 
            button9.BackColor = Color.Transparent;
            button9.BackgroundImage = Properties.Resources.m_17;
            button9.BackgroundImageLayout = ImageLayout.Stretch;
            button9.FlatAppearance.BorderSize = 0;
            button9.FlatStyle = FlatStyle.Flat;
            button9.Location = new Point(995, 17);
            button9.Name = "button9";
            button9.Size = new Size(35, 35);
            button9.TabIndex = 7;
            button9.UseVisualStyleBackColor = false;
            button9.Click += button9_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(1487, 15);
            label3.Name = "label3";
            label3.Size = new Size(21, 15);
            label3.TabIndex = 0;
            label3.Text = "X :";
            // 
            // button8
            // 
            button8.BackColor = Color.Transparent;
            button8.BackgroundImage = Properties.Resources.m_16;
            button8.BackgroundImageLayout = ImageLayout.Stretch;
            button8.FlatAppearance.BorderSize = 0;
            button8.FlatStyle = FlatStyle.Flat;
            button8.Location = new Point(934, 17);
            button8.Name = "button8";
            button8.Size = new Size(35, 35);
            button8.TabIndex = 7;
            button8.UseVisualStyleBackColor = false;
            button8.Click += button8_Click;
            // 
            // button7
            // 
            button7.BackColor = Color.Transparent;
            button7.BackgroundImage = Properties.Resources.m_21;
            button7.BackgroundImageLayout = ImageLayout.Stretch;
            button7.FlatAppearance.BorderSize = 0;
            button7.FlatStyle = FlatStyle.Flat;
            button7.Location = new Point(812, 17);
            button7.Name = "button7";
            button7.Size = new Size(35, 35);
            button7.TabIndex = 7;
            button7.UseVisualStyleBackColor = false;
            button7.Click += button7_Click;
            // 
            // button6
            // 
            button6.BackColor = Color.Transparent;
            button6.BackgroundImage = Properties.Resources.m_19;
            button6.BackgroundImageLayout = ImageLayout.Stretch;
            button6.FlatAppearance.BorderSize = 0;
            button6.FlatStyle = FlatStyle.Flat;
            button6.Location = new Point(751, 17);
            button6.Name = "button6";
            button6.Size = new Size(35, 35);
            button6.TabIndex = 7;
            button6.UseVisualStyleBackColor = false;
            button6.Click += button6_Click;
            // 
            // button5
            // 
            button5.BackColor = Color.Transparent;
            button5.BackgroundImage = Properties.Resources.m_14;
            button5.BackgroundImageLayout = ImageLayout.Stretch;
            button5.FlatAppearance.BorderSize = 0;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Location = new Point(690, 17);
            button5.Name = "button5";
            button5.Size = new Size(35, 35);
            button5.TabIndex = 7;
            button5.UseVisualStyleBackColor = false;
            button5.Click += button5_Click;
            // 
            // button4
            // 
            button4.BackColor = Color.Transparent;
            button4.BackgroundImage = Properties.Resources.m_20;
            button4.BackgroundImageLayout = ImageLayout.Stretch;
            button4.FlatAppearance.BorderSize = 0;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Location = new Point(629, 17);
            button4.Name = "button4";
            button4.Size = new Size(35, 35);
            button4.TabIndex = 7;
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // button3
            // 
            button3.BackColor = Color.Transparent;
            button3.BackgroundImage = Properties.Resources.m_10;
            button3.BackgroundImageLayout = ImageLayout.Stretch;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Location = new Point(568, 17);
            button3.Name = "button3";
            button3.Size = new Size(35, 35);
            button3.TabIndex = 7;
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // textBox2
            // 
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Location = new Point(1319, 36);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(56, 23);
            textBox2.TabIndex = 6;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Location = new Point(1319, 11);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(56, 23);
            textBox1.TabIndex = 6;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(1292, 39);
            label2.Name = "label2";
            label2.Size = new Size(21, 15);
            label2.TabIndex = 5;
            label2.Text = "Y :";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(1292, 14);
            label1.Name = "label1";
            label1.Size = new Size(21, 15);
            label1.TabIndex = 5;
            label1.Text = "X :";
            // 
            // button2
            // 
            button2.BackColor = Color.Transparent;
            button2.BackgroundImage = Properties.Resources.m_9;
            button2.BackgroundImageLayout = ImageLayout.Stretch;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Location = new Point(507, 17);
            button2.Name = "button2";
            button2.Size = new Size(35, 35);
            button2.TabIndex = 4;
            button2.UseVisualStyleBackColor = false;
            button2.Click += btn_righthegreeClick;
            // 
            // button1
            // 
            button1.BackColor = Color.Transparent;
            button1.BackgroundImage = Properties.Resources.m_8;
            button1.BackgroundImageLayout = ImageLayout.Stretch;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(446, 17);
            button1.Name = "button1";
            button1.Size = new Size(35, 35);
            button1.TabIndex = 3;
            button1.UseVisualStyleBackColor = false;
            button1.Click += btn_leftdegreeClick;
            // 
            // btn_zoomout
            // 
            btn_zoomout.BackgroundImage = Properties.Resources.m_5;
            btn_zoomout.BackgroundImageLayout = ImageLayout.Stretch;
            btn_zoomout.FlatAppearance.BorderSize = 0;
            btn_zoomout.FlatStyle = FlatStyle.Flat;
            btn_zoomout.Location = new Point(385, 17);
            btn_zoomout.Name = "btn_zoomout";
            btn_zoomout.Size = new Size(35, 35);
            btn_zoomout.TabIndex = 2;
            btn_zoomout.UseVisualStyleBackColor = true;
            btn_zoomout.Click += button12_Click;
            // 
            // btn_zoomin
            // 
            btn_zoomin.BackgroundImage = Properties.Resources.m_7;
            btn_zoomin.BackgroundImageLayout = ImageLayout.Stretch;
            btn_zoomin.FlatAppearance.BorderSize = 0;
            btn_zoomin.FlatStyle = FlatStyle.Flat;
            btn_zoomin.Location = new Point(324, 17);
            btn_zoomin.Name = "btn_zoomin";
            btn_zoomin.Size = new Size(35, 35);
            btn_zoomin.TabIndex = 2;
            btn_zoomin.UseVisualStyleBackColor = true;
            btn_zoomin.Click += button11_Click;
            // 
            // btnDltTabPage
            // 
            btnDltTabPage.BackgroundImage = Properties.Resources.m_1;
            btnDltTabPage.BackgroundImageLayout = ImageLayout.Stretch;
            btnDltTabPage.FlatAppearance.BorderSize = 0;
            btnDltTabPage.FlatStyle = FlatStyle.Flat;
            btnDltTabPage.Location = new Point(263, 17);
            btnDltTabPage.Name = "btnDltTabPage";
            btnDltTabPage.Size = new Size(35, 35);
            btnDltTabPage.TabIndex = 1;
            btnDltTabPage.UseVisualStyleBackColor = true;
            btnDltTabPage.Click += btnDltTabPage_Click;
            // 
            // btnNewTabPage
            // 
            btnNewTabPage.BackColor = Color.Transparent;
            btnNewTabPage.BackgroundImage = Properties.Resources.m_3;
            btnNewTabPage.BackgroundImageLayout = ImageLayout.Stretch;
            btnNewTabPage.FlatAppearance.BorderSize = 0;
            btnNewTabPage.FlatStyle = FlatStyle.Flat;
            btnNewTabPage.Location = new Point(202, 17);
            btnNewTabPage.Name = "btnNewTabPage";
            btnNewTabPage.Size = new Size(35, 35);
            btnNewTabPage.TabIndex = 1;
            btnNewTabPage.UseVisualStyleBackColor = false;
            btnNewTabPage.Click += btnNewTabPage_Click;
            // 
            // btn_Save
            // 
            btn_Save.BackColor = Color.Transparent;
            btn_Save.BackgroundImage = Properties.Resources.m_11;
            btn_Save.BackgroundImageLayout = ImageLayout.Stretch;
            btn_Save.FlatAppearance.BorderSize = 0;
            btn_Save.FlatStyle = FlatStyle.Flat;
            btn_Save.Location = new Point(141, 17);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(35, 35);
            btn_Save.TabIndex = 0;
            btn_Save.UseVisualStyleBackColor = false;
            btn_Save.Click += btn_Save_Click;
            // 
            // btn_NewFile
            // 
            btn_NewFile.BackColor = Color.Transparent;
            btn_NewFile.BackgroundImage = Properties.Resources.m_6;
            btn_NewFile.BackgroundImageLayout = ImageLayout.Stretch;
            btn_NewFile.FlatAppearance.BorderSize = 0;
            btn_NewFile.FlatStyle = FlatStyle.Flat;
            btn_NewFile.Location = new Point(19, 17);
            btn_NewFile.Name = "btn_NewFile";
            btn_NewFile.Size = new Size(35, 35);
            btn_NewFile.TabIndex = 0;
            btn_NewFile.UseVisualStyleBackColor = false;
            btn_NewFile.Click += btn_NewFile_Click;
            // 
            // btn_Open
            // 
            btn_Open.BackColor = Color.Transparent;
            btn_Open.BackgroundImage = Properties.Resources.m_2;
            btn_Open.BackgroundImageLayout = ImageLayout.Stretch;
            btn_Open.FlatAppearance.BorderSize = 0;
            btn_Open.FlatStyle = FlatStyle.Flat;
            btn_Open.Location = new Point(80, 17);
            btn_Open.Name = "btn_Open";
            btn_Open.Size = new Size(35, 35);
            btn_Open.TabIndex = 0;
            btn_Open.UseVisualStyleBackColor = false;
            btn_Open.Click += btn_Open_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1924, 954);
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
        private Button btn_zoomout;
        private Button btn_zoomin;
        private Button button2;
        private Button button1;
        private TextBox textBox2;
        private TextBox textBox1;
        private Label label2;
        private Label label1;
        private Button button10;
        private Button button9;
        private Button button8;
        private Button button7;
        private Button button6;
        private Button button5;
        private Button button4;
        private Button button3;
        private Button button13;
        private TextBox textBox3;
        private Label label3;
        private TextBox textBox4;
        private Label label4;
        private Label label6;
        private Label label5;
    }
}
