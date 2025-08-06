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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tapPage = new TabPage();
            tabControl1 = new TabControl();
            menuStrip1 = new MenuStrip();
            toolStrip_File = new ToolStripMenuItem();
            toolStrip_NewFile = new ToolStripMenuItem();
            toolStrip_Open = new ToolStripMenuItem();
            toolStripp_Save = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            groupBox2 = new GroupBox();
            button13 = new Button();
            button10 = new Button();
            button9 = new Button();
            button8 = new Button();
            button7 = new Button();
            button6 = new Button();
            button5 = new Button();
            button4 = new Button();
            button3 = new Button();
            pictureBox3 = new PictureBox();
            pictureBox2 = new PictureBox();
            pictureBox1 = new PictureBox();
            textBox4 = new TextBox();
            button2 = new Button();
            textBox2 = new TextBox();
            button1 = new Button();
            btn_zoomout = new Button();
            btn_zoomin = new Button();
            btnDltTabPage = new Button();
            textBox3 = new TextBox();
            label2 = new Label();
            btnNewTabPage = new Button();
            label3 = new Label();
            btn_Save = new Button();
            label1 = new Label();
            btn_NewFile = new Button();
            label4 = new Label();
            btn_Open = new Button();
            textBox1 = new TextBox();
            tabControl1.SuspendLayout();
            menuStrip1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // tapPage
            // 
            tapPage.BackColor = Color.White;
            tapPage.Location = new Point(4, 24);
            tapPage.Name = "tapPage";
            tapPage.Padding = new Padding(3);
            tapPage.Size = new Size(1401, 798);
            tapPage.TabIndex = 1;
            tapPage.Text = "tp1";
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tapPage);
            tabControl1.Location = new Point(20, 116);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1409, 826);
            tabControl1.TabIndex = 1;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(255, 246, 246);
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStrip_File, toolStripMenuItem3 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1741, 24);
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
            groupBox2.Controls.Add(button13);
            groupBox2.Controls.Add(button10);
            groupBox2.Controls.Add(button9);
            groupBox2.Controls.Add(button8);
            groupBox2.Controls.Add(button7);
            groupBox2.Controls.Add(button6);
            groupBox2.Controls.Add(button5);
            groupBox2.Controls.Add(button4);
            groupBox2.Controls.Add(button3);
            groupBox2.Controls.Add(pictureBox3);
            groupBox2.Controls.Add(pictureBox2);
            groupBox2.Controls.Add(pictureBox1);
            groupBox2.Controls.Add(textBox4);
            groupBox2.Controls.Add(button2);
            groupBox2.Controls.Add(textBox2);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(btn_zoomout);
            groupBox2.Controls.Add(btn_zoomin);
            groupBox2.Controls.Add(btnDltTabPage);
            groupBox2.Controls.Add(textBox3);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(btnNewTabPage);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(btn_Save);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(btn_NewFile);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(btn_Open);
            groupBox2.Controls.Add(textBox1);
            groupBox2.Location = new Point(2, 21);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(1746, 66);
            groupBox2.TabIndex = 3;
            groupBox2.TabStop = false;
            // 
            // button13
            // 
            button13.BackColor = Color.Transparent;
            button13.BackgroundImage = (Image)resources.GetObject("button13.BackgroundImage");
            button13.BackgroundImageLayout = ImageLayout.Stretch;
            button13.FlatAppearance.BorderSize = 0;
            button13.FlatStyle = FlatStyle.Flat;
            button13.Location = new Point(1049, 23);
            button13.Name = "button13";
            button13.Size = new Size(30, 30);
            button13.TabIndex = 10;
            button13.UseVisualStyleBackColor = false;
            // 
            // button10
            // 
            button10.BackColor = Color.Transparent;
            button10.BackgroundImage = (Image)resources.GetObject("button10.BackgroundImage");
            button10.BackgroundImageLayout = ImageLayout.Stretch;
            button10.FlatAppearance.BorderSize = 0;
            button10.FlatStyle = FlatStyle.Flat;
            button10.Location = new Point(985, 23);
            button10.Name = "button10";
            button10.Size = new Size(30, 30);
            button10.TabIndex = 10;
            button10.UseVisualStyleBackColor = false;
            // 
            // button9
            // 
            button9.BackColor = Color.Transparent;
            button9.BackgroundImage = (Image)resources.GetObject("button9.BackgroundImage");
            button9.BackgroundImageLayout = ImageLayout.Stretch;
            button9.FlatAppearance.BorderSize = 0;
            button9.FlatStyle = FlatStyle.Flat;
            button9.Location = new Point(933, 23);
            button9.Name = "button9";
            button9.Size = new Size(30, 30);
            button9.TabIndex = 10;
            button9.UseVisualStyleBackColor = false;
            // 
            // button8
            // 
            button8.BackColor = Color.Transparent;
            button8.BackgroundImage = (Image)resources.GetObject("button8.BackgroundImage");
            button8.BackgroundImageLayout = ImageLayout.Stretch;
            button8.FlatAppearance.BorderSize = 0;
            button8.FlatStyle = FlatStyle.Flat;
            button8.Location = new Point(873, 22);
            button8.Name = "button8";
            button8.Size = new Size(30, 30);
            button8.TabIndex = 10;
            button8.UseVisualStyleBackColor = false;
            // 
            // button7
            // 
            button7.BackColor = Color.Transparent;
            button7.BackgroundImage = (Image)resources.GetObject("button7.BackgroundImage");
            button7.BackgroundImageLayout = ImageLayout.Stretch;
            button7.FlatAppearance.BorderSize = 0;
            button7.FlatStyle = FlatStyle.Flat;
            button7.Location = new Point(806, 23);
            button7.Name = "button7";
            button7.Size = new Size(30, 30);
            button7.TabIndex = 10;
            button7.UseVisualStyleBackColor = false;
            button7.Click += button7_Click;
            // 
            // button6
            // 
            button6.BackColor = Color.Transparent;
            button6.BackgroundImage = (Image)resources.GetObject("button6.BackgroundImage");
            button6.BackgroundImageLayout = ImageLayout.Stretch;
            button6.FlatAppearance.BorderSize = 0;
            button6.FlatStyle = FlatStyle.Flat;
            button6.Location = new Point(742, 21);
            button6.Name = "button6";
            button6.Size = new Size(30, 30);
            button6.TabIndex = 10;
            button6.UseVisualStyleBackColor = false;
            // 
            // button5
            // 
            button5.BackColor = Color.Transparent;
            button5.BackgroundImage = (Image)resources.GetObject("button5.BackgroundImage");
            button5.BackgroundImageLayout = ImageLayout.Stretch;
            button5.FlatAppearance.BorderSize = 0;
            button5.FlatStyle = FlatStyle.Flat;
            button5.Location = new Point(671, 20);
            button5.Name = "button5";
            button5.Size = new Size(30, 30);
            button5.TabIndex = 10;
            button5.UseVisualStyleBackColor = false;
            // 
            // button4
            // 
            button4.BackColor = Color.Transparent;
            button4.BackgroundImage = (Image)resources.GetObject("button4.BackgroundImage");
            button4.BackgroundImageLayout = ImageLayout.Stretch;
            button4.FlatAppearance.BorderSize = 0;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Location = new Point(602, 21);
            button4.Name = "button4";
            button4.Size = new Size(30, 30);
            button4.TabIndex = 10;
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click_1;
            // 
            // button3
            // 
            button3.BackColor = Color.Transparent;
            button3.BackgroundImage = Properties.Resources._10;
            button3.BackgroundImageLayout = ImageLayout.Stretch;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Location = new Point(529, 20);
            button3.Name = "button3";
            button3.Size = new Size(30, 30);
            button3.TabIndex = 10;
            button3.UseVisualStyleBackColor = false;
            // 
            // pictureBox3
            // 
            pictureBox3.BackgroundImage = Properties.Resources.gggggggggggggggggg_removebg_preview_removebg_preview;
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(1325, 13);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(193, 50);
            pictureBox3.TabIndex = 9;
            pictureBox3.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.BackgroundImage = Properties.Resources.gggggggggggggggggg_removebg_preview_removebg_preview;
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.Location = new Point(1513, 11);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(193, 50);
            pictureBox2.TabIndex = 8;
            pictureBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = Properties.Resources.gggggggggggggggggg_removebg_preview_removebg_preview;
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(1700, 10);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(193, 50);
            pictureBox1.TabIndex = 7;
            pictureBox1.TabStop = false;
            // 
            // textBox4
            // 
            textBox4.BackColor = SystemColors.HighlightText;
            textBox4.BorderStyle = BorderStyle.None;
            textBox4.Location = new Point(1260, 39);
            textBox4.Multiline = true;
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(60, 23);
            textBox4.TabIndex = 6;
            textBox4.TextChanged += textBox2_TextChanged;
            // 
            // button2
            // 
            button2.BackgroundImage = Properties.Resources._9;
            button2.BackgroundImageLayout = ImageLayout.Stretch;
            button2.FlatAppearance.BorderColor = Color.LavenderBlush;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Location = new Point(466, 20);
            button2.Name = "button2";
            button2.Size = new Size(30, 30);
            button2.TabIndex = 4;
            button2.UseVisualStyleBackColor = true;
            button2.Click += btn_righthegreeClick;
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.HighlightText;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Location = new Point(1132, 39);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(60, 23);
            textBox2.TabIndex = 6;
            textBox2.TextChanged += textBox2_TextChanged;
            // 
            // button1
            // 
            button1.BackgroundImage = Properties.Resources._8;
            button1.BackgroundImageLayout = ImageLayout.Stretch;
            button1.FlatAppearance.BorderColor = Color.LavenderBlush;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(410, 20);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 3;
            button1.UseVisualStyleBackColor = true;
            button1.Click += btn_leftdegreeClick;
            // 
            // btn_zoomout
            // 
            btn_zoomout.BackgroundImage = Properties.Resources._5;
            btn_zoomout.FlatAppearance.BorderColor = Color.LavenderBlush;
            btn_zoomout.FlatStyle = FlatStyle.Flat;
            btn_zoomout.Location = new Point(349, 23);
            btn_zoomout.Name = "btn_zoomout";
            btn_zoomout.Size = new Size(25, 25);
            btn_zoomout.TabIndex = 2;
            btn_zoomout.UseVisualStyleBackColor = true;
            btn_zoomout.Click += button12_Click;
            // 
            // btn_zoomin
            // 
            btn_zoomin.BackgroundImage = Properties.Resources._7;
            btn_zoomin.FlatAppearance.BorderColor = Color.LavenderBlush;
            btn_zoomin.FlatStyle = FlatStyle.Flat;
            btn_zoomin.Location = new Point(286, 23);
            btn_zoomin.Name = "btn_zoomin";
            btn_zoomin.Size = new Size(25, 25);
            btn_zoomin.TabIndex = 2;
            btn_zoomin.UseVisualStyleBackColor = true;
            btn_zoomin.Click += button11_Click;
            // 
            // btnDltTabPage
            // 
            btnDltTabPage.BackgroundImage = Properties.Resources._11;
            btnDltTabPage.BackgroundImageLayout = ImageLayout.Stretch;
            btnDltTabPage.FlatAppearance.BorderColor = Color.LavenderBlush;
            btnDltTabPage.FlatStyle = FlatStyle.Flat;
            btnDltTabPage.Location = new Point(223, 21);
            btnDltTabPage.Name = "btnDltTabPage";
            btnDltTabPage.Size = new Size(32, 31);
            btnDltTabPage.TabIndex = 1;
            btnDltTabPage.UseVisualStyleBackColor = true;
            btnDltTabPage.Click += btnDltTabPage_Click;
            // 
            // textBox3
            // 
            textBox3.BackColor = SystemColors.HighlightText;
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.Location = new Point(1260, 10);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(60, 23);
            textBox3.TabIndex = 6;
            textBox3.TextChanged += textBox1_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Noto Sans KR Medium", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label2.ForeColor = SystemColors.ControlDarkDark;
            label2.Location = new Point(1111, 43);
            label2.Name = "label2";
            label2.Size = new Size(22, 17);
            label2.TabIndex = 5;
            label2.Text = "Y :";
            // 
            // btnNewTabPage
            // 
            btnNewTabPage.BackgroundImage = Properties.Resources._31;
            btnNewTabPage.BackgroundImageLayout = ImageLayout.Stretch;
            btnNewTabPage.FlatAppearance.BorderColor = Color.LavenderBlush;
            btnNewTabPage.FlatStyle = FlatStyle.Flat;
            btnNewTabPage.Location = new Point(168, 22);
            btnNewTabPage.Name = "btnNewTabPage";
            btnNewTabPage.Size = new Size(29, 29);
            btnNewTabPage.TabIndex = 1;
            btnNewTabPage.UseVisualStyleBackColor = true;
            btnNewTabPage.Click += btnNewTabPage_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Noto Sans KR Medium", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label3.ForeColor = SystemColors.ControlDarkDark;
            label3.Location = new Point(1239, 14);
            label3.Name = "label3";
            label3.Size = new Size(22, 17);
            label3.TabIndex = 5;
            label3.Text = "X :";
            // 
            // btn_Save
            // 
            btn_Save.BackgroundImage = Properties.Resources._111;
            btn_Save.BackgroundImageLayout = ImageLayout.Stretch;
            btn_Save.FlatAppearance.BorderColor = Color.LavenderBlush;
            btn_Save.FlatStyle = FlatStyle.Flat;
            btn_Save.Location = new Point(112, 21);
            btn_Save.Name = "btn_Save";
            btn_Save.Size = new Size(31, 30);
            btn_Save.TabIndex = 0;
            btn_Save.UseVisualStyleBackColor = true;
            btn_Save.Click += btn_Save_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Noto Sans KR Medium", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = SystemColors.ControlDarkDark;
            label1.Location = new Point(1111, 15);
            label1.Name = "label1";
            label1.Size = new Size(22, 17);
            label1.TabIndex = 5;
            label1.Text = "X :";
            // 
            // btn_NewFile
            // 
            btn_NewFile.BackColor = Color.Transparent;
            btn_NewFile.BackgroundImage = Properties.Resources._6;
            btn_NewFile.BackgroundImageLayout = ImageLayout.Stretch;
            btn_NewFile.FlatAppearance.BorderColor = Color.LavenderBlush;
            btn_NewFile.FlatStyle = FlatStyle.Flat;
            btn_NewFile.Location = new Point(12, 23);
            btn_NewFile.Name = "btn_NewFile";
            btn_NewFile.Size = new Size(25, 25);
            btn_NewFile.TabIndex = 0;
            btn_NewFile.UseVisualStyleBackColor = false;
            btn_NewFile.Click += btn_NewFile_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Noto Sans KR Medium", 9F, FontStyle.Bold, GraphicsUnit.Point);
            label4.ForeColor = SystemColors.ControlDarkDark;
            label4.Location = new Point(1239, 43);
            label4.Name = "label4";
            label4.Size = new Size(22, 17);
            label4.TabIndex = 5;
            label4.Text = "Y :";
            // 
            // btn_Open
            // 
            btn_Open.BackgroundImage = Properties.Resources._23;
            btn_Open.BackgroundImageLayout = ImageLayout.Stretch;
            btn_Open.FlatAppearance.BorderColor = Color.LavenderBlush;
            btn_Open.FlatStyle = FlatStyle.Flat;
            btn_Open.Location = new Point(59, 18);
            btn_Open.Name = "btn_Open";
            btn_Open.Size = new Size(31, 35);
            btn_Open.TabIndex = 0;
            btn_Open.UseVisualStyleBackColor = true;
            btn_Open.Click += btn_Open_Click;
            // 
            // textBox1
            // 
            textBox1.BackColor = SystemColors.HighlightText;
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Location = new Point(1132, 10);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(60, 23);
            textBox1.TabIndex = 6;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1741, 954);
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
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
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
        private TextBox textBox4;
        private TextBox textBox3;
        private Label label4;
        private Label label3;
        private PictureBox pictureBox1;
        private PictureBox pictureBox3;
        private PictureBox pictureBox2;
        private Button button3;
        private Button button5;
        private Button button4;
        private Button button13;
        private Button button10;
        private Button button9;
        private Button button8;
        private Button button7;
        private Button button6;
    }
}
