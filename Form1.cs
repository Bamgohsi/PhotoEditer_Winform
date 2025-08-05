using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;  //���ȭ���� �����ϱ� ���� using �߰�
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

        private PictureBox selectedImage = null;
        private bool showSelectionBorderForImage = false;
        private PictureBox draggingPictureBox = null;

        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;

        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false;
        private const int handleSize = 10;

        // Win32 API ���� (���ȭ�� ������ ����)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;


        public Form1()
        {
            InitializeComponent();

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized; // ���� �� ��üȭ��
            this.MouseDown += Form1_MouseDown;

            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;

            
        }
        private void TextBox_OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            // ���� �Ǵ� �齺���̽��� ���
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
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
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin - 15,
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

                    PictureBox pb = new PictureBox();
                    pb.AllowDrop = true;
                    pb.DragEnter += PictureBox_DragEnter;
                    pb.DragOver += PictureBox_DragOver;
                    pb.DragLeave += PictureBox_DragLeave;
                    pb.DragDrop += PictureBox_DragDrop;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 30 + X);
                    EnableDoubleBuffering(pb);

                    Bitmap originalCopy; // ���� ������ ����

                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original); // �Ҵ縸 ���ο���
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size;
                    pb.Tag = originalCopy; // ? ���� ����
                    imageList.Add((pb, originalCopy));

                    // �ڵ鷯 ����
                    pb.MouseDown += Image_MouseDown;
                    pb.Paint += Image_Paint;
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;

                    currentTab.Controls.Add(pb);

                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    selectedImage = pb;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�:\n" + ex.Message);
                }
            }
        }

        private void PictureBox_DragDrop(object sender, DragEventArgs e)
        {
            var basePictureBox = sender as PictureBox;
            if (basePictureBox == null || basePictureBox.Image == null || emojiPreviewImage == null)
            {
                showEmojiPreview = false;
                basePictureBox?.Invalidate();
                return;
            }

            PictureBox newEmoji = new PictureBox
            {
                Image = (Image)emojiPreviewImage.Clone(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Size = new Size(emojiPreviewWidth, emojiPreviewHeight),
                Location = new Point(
                    emojiPreviewLocation.X - emojiPreviewWidth / 2,
                    emojiPreviewLocation.Y - emojiPreviewHeight / 2),
                BackColor = Color.Transparent,
                Cursor = Cursors.SizeAll,
                Tag = "selected"
            };

            newEmoji.MouseDown += Emoji_MouseDown;
            newEmoji.MouseMove += Emoji_MouseMove;
            newEmoji.MouseUp += Emoji_MouseUp;
            newEmoji.Paint += Emoji_Paint;

            basePictureBox.Controls.Add(newEmoji);
            selectedEmoji = newEmoji;
            showEmojiPreview = false;
            basePictureBox.Invalidate();
        }
        private void Emoji_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (Control c in ((Control)sender).Parent.Controls)
            {
                if (c is PictureBox pic && pic != sender)
                {
                    pic.Tag = null;
                    pic.Invalidate();
                }
            }

            selectedEmoji = sender as PictureBox;
            if (selectedEmoji != null)
            {
                selectedEmoji.Tag = "selected";
                selectedEmoji.Invalidate();

                if (e.Button == MouseButtons.Left)
                {
                    Rectangle resizeHandle = new Rectangle(
                        selectedEmoji.Width - handleSize,
                        selectedEmoji.Height - handleSize,
                        handleSize, handleSize);

                    resizing = resizeHandle.Contains(e.Location);
                    if (!resizing)
                        dragOffset = e.Location;
                }
            }
        }

        private void Emoji_MouseMove(object sender, MouseEventArgs e)
        {
            var emoji = sender as PictureBox;
            var parent = emoji?.Parent as PictureBox;

            if (e.Button == MouseButtons.Left && selectedEmoji == emoji && parent != null)
            {
                if (resizing)
                {
                    int newW = Math.Max(32, e.X);
                    int newH = Math.Max(32, e.Y);
                    newW = Math.Min(newW, parent.Width - emoji.Left);
                    newH = Math.Min(newH, parent.Height - emoji.Top);
                    emoji.Size = new Size(newW, newH);
                }
                else
                {
                    Point newLoc = emoji.Location;
                    newLoc.Offset(e.X - dragOffset.X, e.Y - dragOffset.Y);
                    newLoc.X = Math.Max(0, Math.Min(newLoc.X, parent.Width - emoji.Width));
                    newLoc.Y = Math.Max(0, Math.Min(newLoc.Y, parent.Height - emoji.Height));
                    emoji.Location = newLoc;
                }
                emoji.Invalidate();
            }
        }

        private void Emoji_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false;
        }

        private void Emoji_Paint(object sender, PaintEventArgs e)
        {
            var emoji = sender as PictureBox;
            if (emoji.Tag != null && emoji.Tag.ToString() == "selected")
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, emoji.Width - 3, emoji.Height - 3);

                e.Graphics.FillRectangle(Brushes.DeepSkyBlue,
                    emoji.Width - handleSize,
                    emoji.Height - handleSize,
                    handleSize, handleSize);
            }
        }


        private void PictureBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Bitmap)) || e.Data.GetDataPresent(typeof(Image)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void PictureBox_DragOver(object sender, DragEventArgs e)
        {
            Point clientPos = ((PictureBox)sender).PointToClient(new Point(e.X, e.Y));
            emojiPreviewLocation = clientPos;
            showEmojiPreview = true;
            ((PictureBox)sender).Invalidate();
            e.Effect = DragDropEffects.Copy;
        }

        private void PictureBox_DragLeave(object sender, EventArgs e)
        {
            showEmojiPreview = false;
            ((PictureBox)sender).Invalidate();
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
            foreach (TabPage tab in tabControl1.TabPages)
            {
                tab.MouseDown += TabPage_MouseDown;
            }
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

            // ���� ������ �̺�Ʈ ����
            newTabPage.MouseDown += TabPage_MouseDown;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;


        }


        // ���� �ڵ鷯��
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                draggingPictureBox = pb;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pb.Invalidate();
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggingPictureBox != null)
            {
                Point mousePos = draggingPictureBox.Parent.PointToClient(MousePosition);
                draggingPictureBox.Location = new Point(mousePos.X - clickOffset.X, mousePos.Y - clickOffset.Y);
            }

            // Ŀ�� ��� ����� �״�� ����
            if (sender is PictureBox pic)
            {
                const int edge = 5;
                bool atTop = e.Y <= edge;
                bool atBottom = e.Y >= pic.Height - edge;
                bool atLeft = e.X <= edge;
                bool atRight = e.X >= pic.Width - edge;

                if (atTop && atLeft) pic.Cursor = Cursors.SizeNWSE;
                else if (atTop && atRight) pic.Cursor = Cursors.SizeNESW;
                else if (atBottom && atLeft) pic.Cursor = Cursors.SizeNESW;
                else if (atBottom && atRight) pic.Cursor = Cursors.SizeNWSE;
                else if (atTop || atBottom) pic.Cursor = Cursors.SizeNS;
                else if (atLeft || atRight) pic.Cursor = Cursors.SizeWE;
                else pic.Cursor = Cursors.Default;
            }
        }


        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            draggingPictureBox = null;
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
            // 1. ���� �г� ����
            int panelCount = 10;
            dynamicPanels = new Panel[panelCount];

            Point panelLocation = new Point(this.ClientSize.Width - (PanelWidth + PanelRightMargin), TopMargin);
            Size panelSize = new Size(PanelWidth, this.ClientSize.Height - TopMargin - BottomMargin);

            for (int i = 0; i < panelCount; i++)
            {
                Panel panel = new Panel()
                {
                    Location = panelLocation,
                    Size = panelSize,
                    Visible = false,
                    BorderStyle = BorderStyle.FixedSingle
                };

                panel.Controls.Add(new Label()
                {
                    Text = $"���� �Ӽ� {i + 1}",
                    Location = new Point(10, 10)
                });

                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            // 2. ��ư ����
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int buttonStartY = 95;
            int columns = 2;
            int buttonCount = 10;

            dynamicButtons = new Button[buttonCount];

            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = new Button();
                btn.Text = $"{i + 1}";
                btn.Size = new Size(buttonWidth, buttonHeight);

                int col = i % columns;
                int row = i / columns;

                btn.Location = new Point(
                    startX + col * (buttonWidth + spacing),
                    buttonStartY + row * (buttonHeight + spacing));

                btn.Tag = i;
                btn.Click += Button_Click;

                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // 3. �̸��� PictureBox �߰� (�г� 8��)
            Panel panel8 = dynamicPanels[7];
            panel8.AllowDrop = true;
            panel8.AutoScroll = true;

            Image[] emojis = {
        Properties.Resources.Emoji1, Properties.Resources.Emoji2, Properties.Resources.Emoji3, Properties.Resources.Emoji4,
                Properties.Resources.Emoji5, Properties.Resources.Emoji6, Properties.Resources.Emoji7, Properties.Resources.Emoji8,
                Properties.Resources.Emoji9, Properties.Resources.Emoji10, Properties.Resources.Emoji11, Properties.Resources.Emoji12,
                Properties.Resources.Emoji13, Properties.Resources.Emoji14, Properties.Resources.Emoji15, Properties.Resources.Emoji16,
                Properties.Resources.Emoji17, Properties.Resources.Emoji18, Properties.Resources.Emoji19, Properties.Resources.Emoji20,
                Properties.Resources.Emoji21, Properties.Resources.Emoji22, Properties.Resources.Emoji23, Properties.Resources.Emoji24,
                Properties.Resources.Emoji25, Properties.Resources.Emoji26, Properties.Resources.Emoji27, Properties.Resources.Emoji28,
                Properties.Resources.Emoji29, Properties.Resources.Emoji30, Properties.Resources.Emoji31, Properties.Resources.Emoji32,
                Properties.Resources.Emoji33, Properties.Resources.Emoji34, Properties.Resources.Emoji35, Properties.Resources.Emoji36,
                Properties.Resources.Emoji37, Properties.Resources.Emoji38, Properties.Resources.Emoji39, Properties.Resources.Emoji40,
                Properties.Resources.Emoji41, Properties.Resources.Emoji42, Properties.Resources.Emoji43, Properties.Resources.Emoji44,
                Properties.Resources.Emoji45, Properties.Resources.Emoji46, Properties.Resources.Emoji47, Properties.Resources.Emoji48,
                Properties.Resources.Emoji49, Properties.Resources.Emoji50, Properties.Resources.Emoji51, Properties.Resources.Emoji52,
                Properties.Resources.Emoji53, Properties.Resources.Emoji54, Properties.Resources.Emoji55, Properties.Resources.Emoji56,
                Properties.Resources.Emoji57, Properties.Resources.Emoji58, Properties.Resources.Emoji59, Properties.Resources.Emoji60,
                Properties.Resources.Emoji61, Properties.Resources.Emoji62, Properties.Resources.Emoji63, Properties.Resources.Emoji64,
                Properties.Resources.Emoji65, Properties.Resources.Emoji66, Properties.Resources.Emoji67, Properties.Resources.Emoji68,
                Properties.Resources.Emoji69
    };

            int iconSize = 48;
            int emojiPadding = 8;
            int emojiStartY = 50;

            for (int i = 0; i < emojis.Length; i++)
            {
                var pic = new PictureBox
                {
                    Image = emojis[i],
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(iconSize, iconSize),
                    Cursor = Cursors.Hand,
                    Location = new Point(
                        emojiPadding + (i % 5) * 50,
                        emojiStartY + i / 5 * (iconSize + emojiPadding))
                };

                pic.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        emojiPreviewImage = ((PictureBox)s).Image;
                        ((PictureBox)s).DoDragDrop(((PictureBox)s).Image, DragDropEffects.Copy);
                    }
                };

                panel8.Controls.Add(pic);
            }

            // 4. �⺻ �г� ���̰� �� ���� ����
            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
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
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // ���� ���� ����
                if (selectedImage != null && selectedImage != pb)
                {
                    selectedImage.Invalidate(); // ���� �׵θ� ����
                }

                selectedImage = pb;
                showSelectionBorderForImage = true;
                pb.Invalidate(); // �׵θ� �׸��� ���� �ٽ� �׸���
            }
        }
        private void Image_Paint(object sender, PaintEventArgs e)
        {
            if (sender is PictureBox pb && pb == selectedImage && showSelectionBorderForImage)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, pb.Width - 2, pb.Height - 2);
                }
            }
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate(); // �׵θ� ����
                selectedImage = null;
            }
        }
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate();
                selectedImage = null;
            }
        }

        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            if (selectedImage != null && selectedImage.Image != null)
            {
                selectedImage.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                selectedImage.Size = selectedImage.Image.Size;  // ȸ�� �� ũ�� �ݿ�
                selectedImage.Invalidate();
            }
        }

        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            if (selectedImage != null && selectedImage.Image != null)
            {
                selectedImage.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                selectedImage.Size = selectedImage.Image.Size;  // ȸ�� �� ũ�� �ݿ�
                selectedImage.Invalidate();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val)); // 0~1000 ���̷� ����

                if (val != corrected)
                {
                    textBox1.TextChanged -= textBox1_TextChanged;
                    textBox1.Text = corrected.ToString();
                    textBox1.SelectionStart = textBox1.Text.Length; // Ŀ�� ������ �̵�
                    textBox1.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val)); // 0~1000 ���̷� ����

                if (val != corrected)
                {
                    textBox2.TextChanged -= textBox1_TextChanged;
                    textBox2.Text = corrected.ToString();
                    textBox2.SelectionStart = textBox2.Text.Length; // Ŀ�� ������ �̵�
                    textBox2.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void UpdateSelectedImageSize()
        {
            if (selectedImage == null)
                return;

            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                // ���� ����: �ּ� 16, �ִ� 1000
                width = Math.Max(16, Math.Min(1000, width));
                height = Math.Max(16, Math.Min(1000, height));

                //  �ؽ�Ʈ�ڽ��� �ݿ��ǵ���
                if (textBox1.Text != width.ToString())
                    textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString())
                    textBox2.Text = height.ToString();

                if (selectedImage.Tag is Bitmap originalBitmap)
                {
                    Bitmap resized = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.Clear(Color.White);
                        g.DrawImage(originalBitmap, 0, 0, width, height);
                    }

                    selectedImage.Image?.Dispose();
                    selectedImage.Image = resized;
                    selectedImage.Size = resized.Size;
                    selectedImage.Invalidate();
                }
            }
        }

        private void button3_Click_1(object sender, EventArgs e)   //���� ũ��� ����� ��ư
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab == null) return;

            foreach (Control ctrl in currentTab.Controls)
            {
                if (ctrl is PictureBox pb && pb.Tag is Bitmap originalBitmap)
                {
                    // ���� �̹����� ����
                    Bitmap restored = new Bitmap(originalBitmap); // ���纻 ���
                    pb.Image = restored;
                    pb.Size = restored.Size;
                }
            }

            // ������ �ʱ�ȭ
            currentScale = 1.0f;
        }

        private void button4_Click_1(object sender, EventArgs e)   //���ȭ�� ���� ��ư
        {
            TabPage currentTab = tabControl1.SelectedTab;

            if (currentTab == null)
            {
                MessageBox.Show("���� ���õ��� �ʾҽ��ϴ�.");
                return;
            }

            // ù ��° �̹��� �ִ� PictureBox ã��
            PictureBox pb = currentTab.Controls
                .OfType<PictureBox>()
                .FirstOrDefault(p => p.Image != null);

            if (pb == null)
            {
                MessageBox.Show("�̹����� �����ϴ�.");
                return;
            }

            try
            {
                // �ӽ� ��ο� �̹��� ����
                string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.jpg");
                pb.Image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                // ���ȭ�� ����
                bool result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, tempPath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (!result)
                {
                    MessageBox.Show("���ȭ�� ���� ����");
                }
                else
                {
                    MessageBox.Show("���ȭ���� �����Ǿ����ϴ�.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("����: " + ex.Message);
            }
        }
    }
}