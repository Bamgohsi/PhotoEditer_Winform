using System.Reflection;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
        // Constants for layout
        private const int LeftMargin = 20; // �� ���� ����
        private const int TopMargin = 90; // �� ��� ���� (tabControl �Ʒ�)
        private const int PanelWidth = 300; // ������ �г��� ���� �ʺ�
        private const int PanelRightMargin = 20; // ������ �г��� �� ������ ����
        private const int GapBetweenPictureBoxAndPanel = 20; // pictureBox1�� ������ �г� ������ ����
        private const int BottomMargin = 20; // �� �ϴ� ����
        // �̹��� ������ ������ ����Ʈ
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();

        // ���� ������ ���� (�⺻ 1.0f)
        private float currentScale = 1.0f;

        // �̹����� ���� �� ���� �߰�
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;


        //���ο� �� ��ȣ�� �����ִ� ����
        private int tabCount = 2;
        // ������ ��ȣ �����
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();


        // �̹��� �巡�� �� ���θ� ��Ÿ���� �÷���
        private bool isDragging = false;

        // �巡�� ���� �� ���콺 Ŭ�� ���� ��ǥ
        private Point clickOffset;

        // ���� �׵θ��� ǥ������ ���� (���콺 Ŭ�� �� true)
        private bool showSelectionBorder = false;

        // �������� ������ ��ư�� �г� �迭
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;

        // ���� ǥ�õ� �г��� �����ϴ� ����
        private Panel currentVisiblePanel = null;
        public Form1()
        {
            InitializeComponent();

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized; // ���� �� ��üȭ��



        }
        private const int LeftPanelWidth = 80; // ���� ��ư���� �����ϴ� ���� �ʺ�

        private void Form1_Resize(object sender, EventArgs e)
        {
            // ������ �г� ũ�� �� ��ġ ����
            if (dynamicPanels != null)
            {
                Point panelLocation = new Point(
                    this.ClientSize.Width - (PanelWidth + PanelRightMargin),
                    TopMargin
                );
                Size panelSize = new Size(
                    PanelWidth,
                    this.ClientSize.Height - TopMargin - BottomMargin
                );

                foreach (var panel in dynamicPanels)
                {
                    panel.Location = panelLocation;
                    panel.Size = panelSize;
                }
            }

            // ���� ���� Ȯ��: LeftMargin + ���� ��ư�� + �߰� ����
            int totalLeft = LeftMargin + LeftPanelWidth + GapBetweenPictureBoxAndPanel;

            // ����Ʈ�� ��ġ �� ũ�� ����
            tabControl1.Location = new Point(totalLeft, TopMargin);
            tabControl1.Size = new Size(
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin-15,
                this.ClientSize.Height - TopMargin - BottomMargin
            );

            // ��� �׷�ڽ� �ʺ� ����
            groupBox2.Width = this.ClientSize.Width - 24;
        }



        private void button4_Click(object sender, EventArgs e)
        {
            // �̻�� ��ư - ���� ��� ���� ����
        }

        // [���� �����] ��ư Ŭ�� �� ����
        // pictureBox�� �̹��� �ʱ�ȭ
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;

            if (currentTab != null)
            {
                // �� ���� ��� PictureBox ����
                var pictureBoxesToRemove = currentTab.Controls
                    .OfType<PictureBox>()
                    .ToList(); // �÷��� ���� ���� ���� ���� ����Ʈ�� ����

                foreach (var pb in pictureBoxesToRemove)
                {
                    currentTab.Controls.Remove(pb);
                    pb.Dispose(); // ���ҽ� ����
                }

            }
        }

        // ���Ĺڽ� �ڸ�
        int X = 30;
        // [����] ��ư Ŭ�� �� ����
        // �̹��� ������ �����ϰ� pictureBox�� �ε�
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    TabPage currentTab = tabControl1.SelectedTab;

                    // �� PictureBox ����
                    PictureBox pb = new PictureBox();
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 30 + X); // ��ġ�� �Ʒ� �Լ� ����
                    EnableDoubleBuffering(pb);

                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        Bitmap originalCopy = new Bitmap(original); // �̹��� ���� ����
                        pb.Image = new Bitmap(originalCopy);        // ȭ�� ǥ�ÿ� �̹���
                        pb.Size = pb.Image.Size;

                        // ����Ʈ�� ���� ����
                        imageList.Add((pb, originalCopy));
                    }

                    pb.Image = Image.FromFile(filePath);
                    pb.Size = pb.Image.Size;

                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;



                    // ���� �ǿ� �߰�
                    currentTab.Controls.Add(pb);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�:\n" + ex.Message);
                }
            }
        }


        // [����] ��ư Ŭ�� �� ���� (���� ���� ����)
        private void btn_Save_Click(object sender, EventArgs e)        // TODO: ���� ��� ���� (����)
        {
            TabPage currentTab = tabControl1.SelectedTab;

            // ���� �� �� ��� PictureBox ����
            var pictureBoxes = currentTab.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.Image != null)
                .ToList();

            if (pictureBoxes.Count == 0)
            {
                MessageBox.Show("������ �̹����� �����ϴ�.");
                return;
            }

            // ��ü ���� �̹����� ũ�⸦ ��� (��� PictureBox�� ��ġ + ũ�� ���)
            int maxRight = 0;
            int maxBottom = 0;
            foreach (var pb in pictureBoxes)
            {
                maxRight = Math.Max(maxRight, pb.Right);
                maxBottom = Math.Max(maxBottom, pb.Bottom);
            }

            Bitmap combinedImage = new Bitmap(maxRight, maxBottom);
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                g.Clear(Color.White); // ��� ���

                foreach (var pb in pictureBoxes)
                {
                    g.DrawImage(pb.Image, pb.Location);
                }
            }

            // ���� ���̾�α�
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "�̹��� ����";
            saveFileDialog.Filter = "JPEG ���� (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG ���� (*.png)|*.png|BMP ���� (*.bmp)|*.bmp|GIF ���� (*.gif)|*.gif";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();
                var format = System.Drawing.Imaging.ImageFormat.Png;

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        format = System.Drawing.Imaging.ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = System.Drawing.Imaging.ImageFormat.Bmp;
                        break;
                    case ".gif":
                        format = System.Drawing.Imaging.ImageFormat.Gif;
                        break;
                    case ".png":
                        format = System.Drawing.Imaging.ImageFormat.Png;
                        break;
                    default:
                        MessageBox.Show("�������� �ʴ� ���� �����Դϴ�.");
                        return;
                }

                try
                {
                    combinedImage.Save(saveFileDialog.FileName, format);
                    MessageBox.Show("��� �̹����� �ϳ��� ����Ǿ����ϴ�.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�̹��� ���� �� ���� �߻�:\n{ex.Message}");
                }
            }

            combinedImage.Dispose(); // ���ҽ� ����
        }





        // �� �ε� �� ���� (�ʿ� �� �ʱ�ȭ ó�� ����)
        private void Form1_Load(object sender, EventArgs e)
        {
            // ����� ��� ����
        }

        int tabNumber;

        private Stack<int> deletedTabNumbers = new Stack<int>();  // ������ �� ��ȣ�� ����
        private void btnNewTabPage_Click(object sender, EventArgs e)       //�������� �߰� ��ư �̺�Ʈ         
        {


            // ������ ��ȣ �켱 ����
            if (deletedTabNumbers.Count > 0)
            {
                tabNumber = deletedTabNumbers.Pop();
            }
            else
            {
                tabNumber = tabCount++;
            }

            TabPage newTabPage = new TabPage($"tp {tabNumber}");
            newTabPage.Name = $"tp{tabNumber}";
            newTabPage.BackColor = Color.White;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;


        }


        // ���� �ڵ鷯��
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pb.Invalidate(); // �ٽ� �׸���
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is PictureBox pb)
            {
                Point newLocation = pb.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;

                pb.Location = newLocation;
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            showSelectionBorder = false;

            if (sender is PictureBox pb)
                pb.Invalidate();
        }
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder && sender is PictureBox pb)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
        private PictureBox selectedPictureBox = null;

        private void btnDltTabPage_Click(object sender, EventArgs e)   //�������� ���� ��ư �̺�Ʈ
        {
            if (tabControl1.TabPages.Count <= 1)
            {
                MessageBox.Show("�ϳ��� ���� �����־�� �մϴ�.");
                return;
            }

            TabPage selectedTab = tabControl1.SelectedTab;

            if (selectedTab != null)
            {
                tabControl1.TabPages.Remove(selectedTab);
                tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;

                // �� �̸��� ���� Name �Ӽ� ����
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}";
                    tab.Name = $"tp{i + 1}";
                }

                // �� ���� ������Ʈ
                tabCount = tabControl1.TabPages.Count + 1;

                // ������ ��ȣ ���� ��� (���� �������̹Ƿ� ���� �ʿ� ����)
                deletedTabNumbers.Clear();
            }

        }

        private void button3_Click(object sender, EventArgs e)  // �� ������ �ȿ� �ؽ�Ʈ �߰�
        {

        }

        private Bitmap ResizeImageHighQuality(Image img, Size size)
        {
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.White);
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return result;
        }




        private void button11_Click(object sender, EventArgs e)     //Ȯ��
        {
            float nextScale = currentScale * 1.2f;
            if (nextScale > MAX_SCALE)
            {
                return;
            }

            currentScale = nextScale;
            ApplyScaling();

        }






        private void button12_Click(object sender, EventArgs e)      //���
        {
            float nextScale = currentScale * 0.8f;
            if (nextScale < MIN_SCALE)
                return;

            currentScale = nextScale;
            ApplyScaling();
        }

        private void ApplyScaling()
        {
            foreach (var (pb, original) in imageList)
            {
                int newWidth = (int)(original.Width * currentScale);
                int newHeight = (int)(original.Height * currentScale);

                pb.Image?.Dispose(); // ���� �̹��� ����
                pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                pb.Size = pb.Image.Size;


            }

            // �� ��ũ�� ����
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab != null)
            {
                int maxRight = 0, maxBottom = 0;
                foreach (Control ctrl in currentTab.Controls)
                {
                    maxRight = Math.Max(maxRight, ctrl.Right);
                    maxBottom = Math.Max(maxBottom, ctrl.Bottom);
                }

                currentTab.AutoScroll = true;
                currentTab.AutoScrollMinSize = new Size(maxRight + 50, maxBottom + 50);
            }
        }
            /// <summary>
            /// ��ư�� �г��� �������� �����ϰ� �ʱ�ȭ�մϴ�.
            /// </summary>
        private void InitializeDynamicControls()
        {
            // ��ư ���� ����
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int startY = 95;
            int columns = 2; // 2���� ��ġ
            int buttonCount = 10; // �� ��ư ����

            dynamicButtons = new Button[buttonCount];

            // 2�� 5������ ��ư ��ġ
            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = new Button();
                btn.Text = $"{i + 1}"; // ��ư �ؽ�Ʈ�� ���ڷ� ����
                btn.Size = new Size(buttonWidth, buttonHeight);

                // ��ư ��ġ ��� (2�� 5��)
                int col = i % columns;
                int row = i / columns;
                btn.Location = new Point(startX + col * (buttonWidth + spacing),
                                         startY + row * (buttonHeight + spacing));

                btn.Tag = i; // ��ư�� �ε��� ����
                btn.Click += Button_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // �г� ���� ����
            int panelCount = 10;
            dynamicPanels = new Panel[panelCount];

            // �г� ��ġ�� ������ ������� ����
            // Calculate panel location based on current client size
            Point panelLocation = new Point(this.ClientSize.Width - (PanelWidth + PanelRightMargin), TopMargin);
            Size panelSize = new Size(PanelWidth, this.ClientSize.Height - TopMargin - BottomMargin);

            for (int i = 0; i < panelCount; i++)
            {
                Panel panel = new Panel()
                {
                    Location = panelLocation, // Use the calculated location
                    Size = panelSize,         // Use the calculated size
                    Visible = false,
                    BorderStyle = BorderStyle.FixedSingle // �г� ��輱 �߰�
                };

                // �гο� �� �߰�
                panel.Controls.Add(new Label() { Text = $"���� �Ӽ� {i + 1}", Location = new Point(10, 10) });

                // �гο� Paint �̺�Ʈ �ڵ鷯 �߰�
                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel; // ������ �г��� �迭�� ����
            }
        }
        // ��� ���� ��ư�� Ŭ�� �̺�Ʈ�� ó���ϴ� ���� �ڵ鷯
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag; // ��ư�� Tag���� �ε��� ��������

                // �г��� ���� ��ư�� �ƹ� ���۵� ���� ����
                if (index >= dynamicPanels.Length)
                {
                    return;
                }

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel)
                {
                    // ���� ���̴� �гΰ� Ŭ���� �г��� ������ ��� (����)
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else
                {
                    // �ٸ� �г��� ���̰� �ִٸ� �����
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }

                    // Ŭ���� ��ư�� �ش��ϴ� �гθ� ���̰� �ϱ�
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }

                // ���� �гΰ� �� �г��� Paint �̺�Ʈ�� ������ ȣ���Ͽ� �׵θ��� ����
                if (previousVisiblePanel != null)
                {
                    previousVisiblePanel.Invalidate();
                }
                if (currentVisiblePanel != null)
                {
                    currentVisiblePanel.Invalidate();
                }
            }
        }
        /// <summary>
        /// �г��� Paint �̺�Ʈ �ڵ鷯: Ȱ��ȭ�� �гο� �׵θ��� �׸��ϴ�.
        /// </summary>
        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel paintedPanel = sender as Panel;

            // ���� ���̴� �г��� ��쿡�� �׵θ� �׸���
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                // �׵θ� ������ ���������� ����
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // �г� ��迡 �׵θ� �׸���
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

    }
}

