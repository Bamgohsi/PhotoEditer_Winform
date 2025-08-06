using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

namespace photo
{
    public partial class Form1 : Form
    {
        // Constants for layout
        private const int LeftMargin = 20;
        private const int TopMargin = 90;
        private const int PanelWidth = 300;
        private const int PanelRightMargin = 20;
        private const int GapBetweenPictureBoxAndPanel = 20;
        private const int BottomMargin = 20;
        private const int LeftPanelWidth = 80; // �� ������ ���� ������ ����

        // �̹��� ������ ������ ����Ʈ. �� PictureBox�� �׿� �ش��ϴ� ���� Bitmap�� �����մϴ�.
        // �� original Bitmap�� ���͸� ���� '������' ������ �����մϴ�.
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();
        // ���� ������ ���� (�⺻ 1.0f)
        private float currentScale = 1.0f;
        // �̹����� ���� �� ���� �߰�
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        //���ο� �� ��ȣ�� �����ִ� ����
        private int tabCount = 2;
        // ������ ��ȣ ����� (���� ������ ����)
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();
        // �̹��� �巡�� �� ���θ� ��Ÿ���� �÷���
        private bool isDragging = false;
        // �巡�� ���� �� ���콺 Ŭ�� ���� ��ǥ
        private Point clickOffset;
        // ���� �׵θ��� ǥ������ ���� (���콺 Ŭ�� �� true) (���� ������ ����)
        private bool showSelectionBorder = false;
        // �������� ������ ��ư�� �г� �迭 (�ּ� ó���� �ڵ忡�� ���)
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        // ���� ǥ�õ� �г��� �����ϴ� ����
        private Panel currentVisiblePanel = null;
        private PictureBox selectedImage = null; // ���� ���õ� PictureBox
        private bool showSelectionBorderForImage = false; // ���õ� �̹����� �׵θ� ǥ�� ����
        private PictureBox draggingPictureBox = null; // �巡�� ���� PictureBox

        // �̸��� ���� ���� (���� �ڵ� ����)
        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;
        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false;
        private const int handleSize = 10;

        // =======================================================
        // ���� - �̹��� ���� ���� ���� �߰�
        // =======================================================
        // originalImage�� ���� ���� ������ '������'�� �Ǵ� ��Ʈ���Դϴ�.
        // ���õ� �̹����� Tag�� ����� '��¥' ������ �����Ǿ�� �մϴ�.
        private Bitmap originalImage; // ���͸��� ���� ���� '�۾�' �̹��� (selectedImage.Tag�� �ִ� �������� �Ļ�)
        private Bitmap _initialImage; // ���� �ε�� ���� �̹��� (����� originalImage�� ������ ����)

        // ���� �� RGB ���� ��Ʈ��
        private TrackBar trackRed, trackGreen, trackBlue;
        private TextBox txtRed, txtGreen, txtBlue;
        private TrackBar trackBrightness, trackSaturation;
        private TextBox txtBrightness, txtSaturation;
        private Button btnApplyAll, btnResetAll;

        private enum FilterState { None, Grayscale, Sepia }
        private FilterState _currentFilter = FilterState.None;
        private bool isTextChanging = false; // �ؽ�Ʈ�ڽ� ���� ���� �� ���� ���� ������

        // �� ���� ��ȣ ���� (���� �ڵ忡 �־���)
        private Stack<int> deletedTabNumbers = new Stack<int>();


        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized;
            this.MouseDown += Form1_MouseDown;
            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;

            this.BackColor = ColorTranslator.FromHtml("#FFF0F5");
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            textBox3.Leave += textBox_Leave;
            textBox4.Leave += textBox_Leave;
        }

        // ���ڸ� �Է� �����ϵ��� �ϴ� �̺�Ʈ �ڵ鷯
        private void TextBox_OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // �� ũ�� ���� �� UI ��� ���ġ
        private void Form1_Resize(object sender, EventArgs e)
        {
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

            int totalLeft = LeftMargin;
            tabControl1.Location = new Point(totalLeft, TopMargin);
            tabControl1.Size = new Size(
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin - 15,
                this.ClientSize.Height - TopMargin - BottomMargin
            );
            groupBox2.Width = this.ClientSize.Width - 24;
        }


        private void button4_Click(object sender, EventArgs e)
        {
            // �̻�� ��ư
        }

        // [���� �����] ��ư Ŭ�� �� ����
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab != null)
            {
                var pictureBoxesToRemove = currentTab.Controls
                    .OfType<PictureBox>()
                    .ToList();

                foreach (var pb in pictureBoxesToRemove)
                {
                    currentTab.Controls.Remove(pb);
                    pb.Dispose();
                }
            }
            // ���� - ���� ���� ���� �ʱ�ȭ
            originalImage?.Dispose(); // ���� �̹��� ���ҽ� ����
            originalImage = null;
            _initialImage?.Dispose(); // ���� �̹��� ���ҽ� ����
            _initialImage = null;

            selectedImage = null; // ���õ� �̹��� �ʱ�ȭ
            btnResetAll_Click(null, null); // UI ��Ʈ�� �ʱ�ȭ
        }

        int X = 30; // PictureBox�� Y ������ (�׽�Ʈ��)

        // [����] ��ư Ŭ�� �� ����
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
                    pb.Location = new Point(10, 30 + X); // PictureBox ��ġ
                    EnableDoubleBuffering(pb);

                    Bitmap originalBitmapFromFile;
                    // ���Ͽ��� ��Ʈ���� �����ϰ� ���� ���纻�� �����մϴ�.
                    using (var tempImage = Image.FromFile(filePath))
                    {
                        originalBitmapFromFile = new Bitmap(tempImage);
                    }

                    pb.Image = new Bitmap(originalBitmapFromFile); // PictureBox�� �̹��� �Ҵ�
                    pb.Size = pb.Image.Size;
                    pb.Tag = originalBitmapFromFile; // PictureBox�� Tag�� '��¥ ����' Bitmap ����
                    imageList.Add((pb, originalBitmapFromFile)); // imageList�� (PictureBox, ���� Bitmap) �߰�

                    // �̺�Ʈ �ڵ鷯 ����
                    pb.MouseDown += Image_MouseDown;
                    pb.Paint += Image_Paint;
                    pb.MouseDown += pictureBox_MouseDown; // �巡�׿�
                    pb.MouseMove += pictureBox_MouseMove; // �巡�׿�
                    pb.MouseUp += pictureBox_MouseUp;     // �巡�׿�

                    currentTab.Controls.Add(pb);

                    // ���� ���� �̹����� selectedImage�� ����
                    selectedImage = pb;
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();

                    // ���� - �̹��� ������ ���� �̹��� ���� �� UI �ʱ�ȭ
                    // ���� originalImage�� _initialImage�� selectedImage�� Tag�� �ִ� ������ ����
                    originalImage?.Dispose();
                    originalImage = (Bitmap)originalBitmapFromFile.Clone();
                    _initialImage?.Dispose();
                    _initialImage = (Bitmap)originalBitmapFromFile.Clone();
                    btnResetAll_Click(null, null); // ���� ��Ʈ�� �ʱ�ȭ
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�:\n" + ex.Message);
                }
            }
            this.ActiveControl = null; // ��ư ��Ŀ�� ����
        }

        // �̸��� �巡�� �� ��� ���� �޼��� (���� �ڵ� ����)
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

        // [����] ��ư Ŭ�� �� ����
        private void btn_Save_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            var pictureBoxes = currentTab.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.Image != null)
                .ToList();
            if (pictureBoxes.Count == 0)
            {
                MessageBox.Show("������ �̹����� �����ϴ�.");
                return;
            }

            // ���� ū PictureBox�� �������� �����մϴ�. (����� ���� �̹��� ó�� ��)
            // ���� ���� PictureBox�� �ϳ��� �̹����� �����Ϸ��� �Ʒ� ������ �����ؾ� �մϴ�.
            // ���� selectedImage�� ���͸��� ��� �̹����� ������ �ִٰ� �����մϴ�.
            if (selectedImage?.Image == null)
            {
                MessageBox.Show("���õ� �̹����� ���ų� �̹����� ��ȿ���� �ʽ��ϴ�.");
                return;
            }

            // selectedImage�� ���� ������ ���¸� �����ϴ� ��Ʈ�� ����
            Bitmap combinedImage = new Bitmap(selectedImage.Image.Width, selectedImage.Image.Height);
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                // selectedImage�� ���� Image�� �״�� �׸��ϴ�.
                // �̴� ApplyAllLivePreview�� ���� ���͸��� ����� ���Դϴ�.
                g.DrawImage(selectedImage.Image, 0, 0, selectedImage.Image.Width, selectedImage.Image.Height);
            }


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
                    MessageBox.Show("�̹����� ����Ǿ����ϴ�.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�̹��� ���� �� ���� �߻�:\n{ex.Message}");
                }
            }

            combinedImage.Dispose();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (TabPage tab in tabControl1.TabPages)
            {
                tab.MouseDown += TabPage_MouseDown;
            }
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(btn_NewFile, "���θ����(Ctrl+N)");
            toolTip.SetToolTip(btn_Open, "���� ����(Ctrl+O)");
            toolTip.SetToolTip(btn_Save, "���� ����(Ctrl+S)");
            toolTip.SetToolTip(btnNewTabPage, "�� ������ �߰�");
            toolTip.SetToolTip(btnDltTabPage, "�� ������ ����");
            toolTip.SetToolTip(btn_zoomin, "Ȯ��");
            toolTip.SetToolTip(btn_zoomout, "���");
            toolTip.SetToolTip(button1, "�������� 90��ȸ��");
            toolTip.SetToolTip(button2, "���������� 90��ȸ��");
            toolTip.SetToolTip(button3, "�¿����");
            toolTip.SetToolTip(button4, "��");
            toolTip.SetToolTip(button5, "�ڸ���");
            toolTip.SetToolTip(button6, "�����̵�");
            toolTip.SetToolTip(button7, "�̸�Ƽ��");
            toolTip.SetToolTip(button8, "������ũ");
            toolTip.SetToolTip(button9, "������ũ ����");
            toolTip.SetToolTip(button10, "����");
            toolTip.SetToolTip(button13, "���찳");

        }

        


        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                draggingPictureBox = pb;
                clickOffset = e.Location;
                showSelectionBorder = true; // �� ������ ���� Image_Paint���� ������ ����
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
            showSelectionBorder = false; // �� ������ ���� Image_Paint���� ������ ����

            if (sender is PictureBox pb)
                pb.Invalidate();

            // �ű� �̹����� ��ǥ ������Ʈ
            PictureBox movedPictureBox = sender as PictureBox;
            if (movedPictureBox != null)
            {
                movedPictureBox.Invalidate();
                textBox3.Text = movedPictureBox.Location.X.ToString();
                textBox4.Text = movedPictureBox.Location.Y.ToString();
            }
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        // pictureBox_Paint�� �̹����� �׵θ��� �׸��� ���Ҹ� �մϴ�.
        // ���͸��� �̹����� ApplyAllLivePreview���� selectedImage.Image�� ���� �Ҵ�˴ϴ�.
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder && sender is PictureBox pb)
            {
                using (Pen pen = new Pen(Color.LightSkyBlue, 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
        private PictureBox selectedPictureBox = null; // �� ������ ���� ������ �ʴ� ������ ����

        private void btnDltTabPage_Click(object sender, EventArgs e)
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

                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}";
                    tab.Name = $"tp{i + 1}";
                }

                tabCount = tabControl1.TabPages.Count + 1;
                deletedTabNumbers.Clear();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            // ��� �̱��� ��ư
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


        private void button11_Click(object sender, EventArgs e) // Ȯ��
        {
            if (selectedImage == null) return;
            float nextScale = currentScale * 1.2f;
            if (nextScale > MAX_SCALE)
            {
                return;
            }

            currentScale = nextScale;
            ApplyScaling();
        }

        private void button12_Click(object sender, EventArgs e) // ���
        {
            if (selectedImage == null) return;
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
                // �����ϸ��� ���õ� �̹������� ����ǵ��� ���� (�Ǵ� ��ü�� ����)
                // ����� ��� �̹����� �����ϸ��� �����ϴ� �����̳�, ���ʹ� selectedImage���� ����ǹǷ�
                // �� �κ��� �����ؾ� �մϴ�. ���͵� �̹����� �ٽ� �����ϸ��ϴ� ���� �ƴ϶�,
                // �׻� original�� �������� �����ϸ��ϵ��� �����մϴ�.
                int newWidth = (int)(original.Width * currentScale);
                int newHeight = (int)(original.Height * currentScale);

                // selectedImage�� ���, ���͸��� ����� ���� Image�� �����Ͽ� �����ϸ��մϴ�.
                // �ٸ� PictureBox�� ���, original (�±׿� ����� ����)�� ����մϴ�.
                Bitmap imageToResize;
                if (pb == selectedImage && pb.Image != null)
                {
                    imageToResize = new Bitmap(pb.Image); // ���͸��� ���� �̹����� �����ϸ�
                }
                else
                {
                    imageToResize = original; // Tag�� ����� ���� ���
                }

                pb.Image?.Dispose(); // ���� �̹��� ����
                pb.Image = ResizeImageHighQuality(imageToResize, new Size(newWidth, newHeight));
                pb.Size = pb.Image.Size;

                imageToResize.Dispose(); // �ӽ� ��Ʈ�� ����
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
                // Panel 0�� ������ ����/RGB ��Ʈ�� �߰�
                if (i == 0)
                {
                    AddImageEditControls(panel);
                }
                else
                {
                    // �ٸ� �г��� �⺻ ���̺� ���� (Ȥ�� ���ϴ´�� ����)
                    panel.Controls.Add(new Label()
                    {
                        Text = $"���� �Ӽ� {i + 1}",
                        Location = new Point(10, 10)
                    });
                }
                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            // 2. ��ư ���� (�ּ� ó���� �ڵ� ����)
            // dynamicButtons = new Button[buttonCount];
            // ... (����) ...

            // 3. �̸��� PictureBox �߰� (�г� 8��) - ������ ���������, ���� �ڵ忡 �־� ����
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

        // ��� ���� ��ư�� Ŭ�� �̺�Ʈ�� ó���ϴ� ���� �ڵ鷯 (���� �ּ� ó���� ��ư ���� �ڵ�� �Բ� ���)
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag;

                if (index >= dynamicPanels.Length)
                {
                    return;
                }

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel)
                {
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else
                {
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }

                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }

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
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                using (Pen pen = new Pen(Color.LightGray, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        // �̹��� Ŭ�� �� �̺�Ʈ �ڵ鷯
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // ������ ���õ� �̹����� �ִٸ� �׵θ� ����
                if (selectedImage != null && selectedImage != pb)
                {
                    selectedImage.Invalidate();
                }

                // ���� ���õ� �̹��� ���� �� �׵θ� ǥ��
                selectedImage = pb;
                showSelectionBorderForImage = true;
                pb.Invalidate();

                // ���� - ���õ� �̹����� ����� �� ���� ��Ʈ�� UI�� ������Ʈ�ϰ� ���� �̹����� ����ȭ
                UpdateEditControlsFromSelectedImage();
            }
        }

        // �̹��� Paint �̺�Ʈ �ڵ鷯
        private void Image_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;

            // ���õ� �̹��� �׵θ� �׸���
            if (pb != null && pb == selectedImage && showSelectionBorderForImage)
            {
                using (Pen pen = new Pen(Color.LightSkyBlue, 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 1, 1, pb.Width - 2, pb.Height - 2);
                }
            }

            // ���� - �̹����� PictureBox�� �Ҵ�Ǿ� �ִٸ� �׸��ϴ�.
            // ���͸��� �̹����� ApplyAllLivePreview���� selectedImage.Image�� ���� �Ҵ�ǹǷ�
            // ���⼭�� selectedImage.Image�� �׸��⸸ �ϸ� �˴ϴ�.
            if (pb != null && pb.Image != null)
            {
                // �߰����� ���͸� ���� ���� ���� PictureBox�� �̹����� �׸��ϴ�.
                e.Graphics.DrawImage(pb.Image, 0, 0, pb.Width, pb.Height);
            }
        }

        // ���� �� ���� Ŭ�� �� selectedImage ���� ����
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate(); // �׵θ� ����
                selectedImage = null; // ���õ� �̹��� ����
                // ���� ���� �� ���� UI�� �ʱ�ȭ�մϴ�.
                btnResetAll_Click(null, null);
            }
        }

        // �� ������ �� ���� Ŭ�� �� selectedImage ���� ����
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate();
                selectedImage = null;
                // ���� ���� �� ���� UI�� �ʱ�ȭ�մϴ�.
                btnResetAll_Click(null, null);
            }
        }

        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            if (selectedImage != null && selectedImage.Image != null)
            {
                selectedImage.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                selectedImage.Size = selectedImage.Image.Size;
                selectedImage.Invalidate();
            }
        }

        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            if (selectedImage != null && selectedImage.Image != null)
            {
                selectedImage.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                selectedImage.Size = selectedImage.Image.Size;
                selectedImage.Invalidate();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val));

                if (val != corrected)
                {
                    textBox1.TextChanged -= textBox1_TextChanged;
                    textBox1.Text = corrected.ToString();
                    textBox1.SelectionStart = textBox1.Text.Length;
                    textBox1.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val));

                if (val != corrected)
                {
                    textBox2.TextChanged -= textBox1_TextChanged;
                    textBox2.Text = corrected.ToString();
                    textBox2.SelectionStart = textBox2.Text.Length;
                    textBox2.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBox3.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val));

                if (val != corrected)
                {
                    textBox3.TextChanged -= textBox3_TextChanged;
                    textBox3.Text = corrected.ToString();
                    textBox3.SelectionStart = textBox3.Text.Length;
                    textBox3.TextChanged += textBox3_TextChanged;
                }
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBox4.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val));

                if (val != corrected)
                {
                    textBox4.TextChanged -= textBox4_TextChanged;
                    textBox4.Text = corrected.ToString();
                    textBox4.SelectionStart = textBox4.Text.Length;
                    textBox4.TextChanged += textBox4_TextChanged;
                }
            }
        }

        private void textBox_Leave(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
        }

        private void UpdateSelectedImageSize()
        {
            if (selectedImage == null)
                return;
            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                width = Math.Max(16, Math.Min(1000, width));
                height = Math.Max(16, Math.Min(1000, height));

                if (textBox1.Text != width.ToString())
                    textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString())
                    textBox2.Text = height.ToString();

                // selectedImage.Tag�� ����� ���� Bitmap�� ����մϴ�.
                if (selectedImage.Tag is Bitmap originalBitmapFromTag)
                {
                    // ���� selectedImage.Image�� �����ͼ� �����ϸ��մϴ�.
                    // �̷��� �ؾ� ���Ͱ� ����� ���¿��� �����ϸ��� �˴ϴ�.
                    Bitmap imageToResize = null;
                    if (selectedImage.Image != null)
                    {
                        imageToResize = new Bitmap(selectedImage.Image);
                    }
                    else
                    {
                        // ���� selectedImage.Image�� null�̸�, Tag�� ������ ����մϴ�.
                        imageToResize = new Bitmap(originalBitmapFromTag);
                    }


                    Bitmap resized = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.Clear(Color.White);
                        g.DrawImage(imageToResize, 0, 0, width, height);
                    }

                    selectedImage.Image?.Dispose();
                    selectedImage.Image = resized;
                    selectedImage.Size = resized.Size;
                    selectedImage.Invalidate();

                    imageToResize?.Dispose(); // �ӽ� ��Ʈ�� ����
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (selectedImage != null)
            {
                Point loc = selectedImage.Location;
                switch (e.KeyCode)
                {
                    case Keys.Left:
                        loc.X = Math.Max(0, loc.X - 1);
                        break;
                    case Keys.Right:
                        loc.X = Math.Min(selectedImage.Parent.Width - selectedImage.Width, loc.X + 1);
                        break;
                    case Keys.Up:
                        loc.Y = Math.Max(0, loc.Y - 1);
                        break;
                    case Keys.Down:
                        loc.Y = Math.Min(selectedImage.Parent.Height - selectedImage.Height, loc.Y + 1);
                        break;
                }

                selectedImage.Location = loc;
                textBox3.Text = loc.X.ToString();
                textBox4.Text = loc.Y.ToString();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            // ���� �ڵ� ���� (���� ����)
        }

        // button7_Click�� �̸�Ƽ�� ��� (���� �ڵ� ����)
        private void button7_Click(object sender, EventArgs e)
        {
            Panel targetPanel = dynamicPanels[7];
            if (currentVisiblePanel == targetPanel)
            {
                targetPanel.Visible = false;
                currentVisiblePanel = null;
            }
            else
            {
                foreach (Panel panel in dynamicPanels)
                {
                    panel.Visible = false;
                }

                targetPanel.Visible = true;
                targetPanel.BringToFront();
                currentVisiblePanel = targetPanel;
                targetPanel.Invalidate();
            }
        }

        // =======================================================
        // ���� - ���� �κ� ��� �߰�
        // =======================================================

        private void AddImageEditControls(Panel targetPanel)
        {
            int currentY = 10;
            int verticalSpacing = 40;
            int sectionSpacing = 30;

            targetPanel.Controls.Add(new Label { Text = "RGB ����", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddColorControl("Red", ref trackRed, ref txtRed, targetPanel, ref currentY);
            AddColorControl("Green", ref trackGreen, ref txtGreen, targetPanel, ref currentY);
            AddColorControl("Blue", ref trackBlue, ref txtBlue, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "������ ����", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            var warmBtn = new Button { Text = "Warm", Location = new Point(10, currentY), Size = new Size(60, 30), Tag = "Warm" };
            warmBtn.Click += (s, e) => { ApplyPresetFilter(FilterState.None, "Warm"); };
            targetPanel.Controls.Add(warmBtn);
            var coolBtn = new Button { Text = "Cool", Location = new Point(80, currentY), Size = new Size(60, 30), Tag = "Cool" };
            coolBtn.Click += (s, e) => { ApplyPresetFilter(FilterState.None, "Cool"); };
            targetPanel.Controls.Add(coolBtn);
            var vintageBtn = new Button { Text = "Vintage", Location = new Point(150, currentY), Size = new Size(60, 30), Tag = "Vintage" };
            vintageBtn.Click += (s, e) => { ApplyPresetFilter(FilterState.None, "Vintage"); };
            targetPanel.Controls.Add(vintageBtn);
            var originalBtn = new Button { Text = "Original", Location = new Point(220, currentY), Size = new Size(60, 30), Tag = "Original" };
            originalBtn.Click += (s, e) => { btnOriginal_Click(s, e); };
            targetPanel.Controls.Add(originalBtn);
            currentY += verticalSpacing + sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "���", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("���", ref trackBrightness, ref txtBrightness, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "ä��", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("ä��", ref trackSaturation, ref txtSaturation, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "�ܻ� ����", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            var btnGray = new Button { Text = "���", Location = new Point(10, currentY), Size = new Size(60, 30) };
            btnGray.Click += (s, e) => { ApplyMonochromeFilter(FilterState.Grayscale); };
            targetPanel.Controls.Add(btnGray);
            var btnSepia = new Button { Text = "���Ǿ�", Location = new Point(80, currentY), Size = new Size(60, 30) };
            btnSepia.Click += (s, e) => { ApplyMonochromeFilter(FilterState.Sepia); };
            targetPanel.Controls.Add(btnSepia);
            currentY += verticalSpacing + sectionSpacing;

            btnApplyAll = new Button { Text = "����", Location = new Point(50, currentY), Size = new Size(80, 30) };
            btnApplyAll.Click += btnApplyAll_Click;
            targetPanel.Controls.Add(btnApplyAll);
            btnResetAll = new Button { Text = "�ʱ�ȭ", Location = new Point(160, currentY), Size = new Size(80, 30) };
            btnResetAll.Click += btnResetAll_Click;
            targetPanel.Controls.Add(btnResetAll);
        }

        private void AddColorControl(string label, ref TrackBar trackBar, ref TextBox txtBox, Panel panel, ref int y)
        {
            Label colorLabel = new Label { Text = label + ":", Location = new Point(10, y) };
            panel.Controls.Add(colorLabel);
            trackBar = new TrackBar
            {
                Location = new Point(10, y + 25),
                Size = new Size(180, 45),
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                TickFrequency = 10
            };
            panel.Controls.Add(trackBar);
            txtBox = new TextBox
            {
                Location = new Point(200, y + 30),
                Size = new Size(45, 25),
                Text = "128"
            };
            panel.Controls.Add(txtBox);

            if (label == "Red")
            {
                trackBar.Scroll += trackRed_Scroll;
                txtBox.TextChanged += txtRed_TextChanged;
            }
            else if (label == "Green")
            {
                trackBar.Scroll += trackGreen_Scroll;
                txtBox.TextChanged += txtGreen_TextChanged;
            }
            else if (label == "Blue")
            {
                trackBar.Scroll += trackBlue_Scroll;
                txtBox.TextChanged += txtBlue_TextChanged;
            }
            y += 70;
        }

        private void AddBrightnessSaturationControl(string label, ref TrackBar trackBar, ref TextBox txtBox, Panel panel, ref int y)
        {
            trackBar = new TrackBar
            {
                Location = new Point(10, y),
                Size = new Size(180, 45),
                Minimum = -100,
                Maximum = 100,
                Value = 0,
                TickFrequency = 10
            };
            panel.Controls.Add(trackBar);
            txtBox = new TextBox
            {
                Location = new Point(200, y + 5),
                Size = new Size(45, 25),
                Text = "0"
            };
            panel.Controls.Add(txtBox);

            if (label == "���")
            {
                trackBar.Scroll += trackBrightness_Scroll;
                txtBox.TextChanged += txtBrightness_TextChanged;
            }
            else if (label == "ä��")
            {
                trackBar.Scroll += trackSaturation_Scroll;
                txtBox.TextChanged += txtSaturation_TextChanged;
            }
            y += 45;
        }

        private void ApplyPresetFilter(FilterState filter, string presetType)
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            // _currentFilter�� �����մϴ�.
            _currentFilter = filter;

            // ���� ��Ʈ�����κ��� Ŭ���� �����Ͽ� ���͸� �۾��� ����մϴ�.
            Bitmap tempOriginal = (Bitmap)originalBitmap.Clone();
            Bitmap result = null;

            switch (presetType)
            {
                case "Warm":
                    result = ApplyWarmFilter(tempOriginal);
                    trackRed.Value = Math.Min(128 + 30, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 30, 0);
                    break;
                case "Cool":
                    result = ApplyCoolFilter(tempOriginal);
                    trackRed.Value = Math.Max(128 - 30, 0);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Min(128 + 30, 255);
                    break;
                case "Vintage":
                    result = ApplyVintageFilter(tempOriginal);
                    trackRed.Value = Math.Min(128 + 20, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 20, 0);
                    break;
                default:
                    result = tempOriginal; // �������� ���� ��� ���� �״��
                    break;
            }

            // originalImage�� ���Ӱ� ���͸��� �̹����� ������Ʈ�մϴ�.
            originalImage?.Dispose();
            originalImage = result;

            txtRed.Text = trackRed.Value.ToString();
            txtGreen.Text = trackGreen.Value.ToString();
            txtBlue.Text = trackBlue.Value.ToString();
            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";

            // PictureBox�� �̹����� ������Ʈ�ϰ� ȭ���� �����մϴ�.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)originalImage.Clone(); // �۾� �̹����� PictureBox�� �Ҵ�
            selectedImage.Invalidate();
        }

        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            _currentFilter = FilterState.None;

            // selectedImage.Tag�� ����� ���� Bitmap�� _initialImage�� originalImage�� �ٽ� �Ҵ��մϴ�.
            _initialImage?.Dispose();
            _initialImage = (Bitmap)originalBitmap.Clone();
            originalImage?.Dispose();
            originalImage = (Bitmap)originalBitmap.Clone();

            // PictureBox�� �̹����� �������� �ǵ����ϴ�.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)originalBitmap.Clone();
            selectedImage.Invalidate();

            // UI ��Ʈ���� �⺻������ �ʱ�ȭ�մϴ�.
            trackRed.Value = 128;
            trackGreen.Value = 128;
            trackBlue.Value = 128;
            txtRed.Text = "128";
            txtGreen.Text = "128";
            txtBlue.Text = "128";

            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";
        }

        private Bitmap ApplyWarmFilter(Bitmap img)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int r = Math.Min(pixel.R + 30, 255);
                    int g = pixel.G;
                    int b = Math.Max(pixel.B - 30, 0);
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        private Bitmap ApplyCoolFilter(Bitmap img)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int r = Math.Max(pixel.R - 30, 0);
                    int g = pixel.G;
                    int b = Math.Min(pixel.B + 30, 255);
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        private Bitmap ApplyVintageFilter(Bitmap img)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int avg = (pixel.R + pixel.G + pixel.B) / 3;
                    int r = Math.Min(avg + 20, 255);
                    int g = avg;
                    int b = Math.Max(avg - 20, 0);
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        private void ApplyAllLivePreview()
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            // selectedImage�� Tag�� �ִ� ���� ��Ʈ�����κ��� �����մϴ�.
            Bitmap tempImage = (Bitmap)originalBitmap.Clone();

            // RGB ���� ����
            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;
            tempImage = AdjustRGB(tempImage, rAdj, gAdj, bAdj);

            // ��� ���� ����
            tempImage = AdjustBrightness(tempImage, trackBrightness.Value);

            // ä�� ���� ����
            tempImage = AdjustSaturation(tempImage, trackSaturation.Value);

            // �ܻ� ���� ����
            if (_currentFilter == FilterState.Grayscale)
            {
                tempImage = ConvertToGrayscale(tempImage);
            }
            else if (_currentFilter == FilterState.Sepia)
            {
                tempImage = ApplySepia(tempImage);
            }

            // ���� �̹����� �����ϰ� ���ο� �̹����� �Ҵ��մϴ�.
            selectedImage.Image?.Dispose();
            selectedImage.Image = tempImage;
            selectedImage.Invalidate(); // ���� ���� ��� �ݿ�
        }

        // selectedImage�� ����� �� ���� ��Ʈ�� UI�� ������Ʈ
        private void UpdateEditControlsFromSelectedImage()
        {
            if (selectedImage != null)
            {
                // ���õ� PictureBox�� Tag���� ���� Bitmap�� �����ɴϴ�.
                if (selectedImage.Tag is Bitmap currentOriginalBitmap)
                {
                    _initialImage?.Dispose();
                    _initialImage = (Bitmap)currentOriginalBitmap.Clone(); // ��¥ ����
                    originalImage?.Dispose();
                    originalImage = (Bitmap)currentOriginalBitmap.Clone(); // ������ ������ �� ����
                    btnResetAll_Click(null, null); // UI ��Ʈ�� �ʱ�ȭ
                }
            }
        }
        int tabNumber;
        private static readonly Stack<int> stack = new Stack<int>();
      
        private void btnNewTabPage_Click(object sender, EventArgs e)
        {
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

            newTabPage.MouseDown += TabPage_MouseDown;
            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;
        }
        // Ʈ���� ��ũ�� �̺�Ʈ �ڵ鷯
        private void trackRed_Scroll(object sender, EventArgs e) { txtRed.Text = trackRed.Value.ToString(); ApplyAllLivePreview(); }
        private void trackGreen_Scroll(object sender, EventArgs e) { txtGreen.Text = trackGreen.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBlue_Scroll(object sender, EventArgs e) { txtBlue.Text = trackBlue.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBrightness_Scroll(object sender, EventArgs e) { txtBrightness.Text = txtBrightness.ToString(); ApplyAllLivePreview(); }
        private void trackSaturation_Scroll(object sender, EventArgs e) { txtSaturation.Text = trackSaturation.Value.ToString(); ApplyAllLivePreview(); }

        // ���� �̹��� �������� ���� �̹����� �ݿ��մϴ�.
        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            // ���� PictureBox�� ǥ�õ� ���� ��� �̹���(���� �����)�� ���ο� '����'���� �����մϴ�.
            originalImage?.Dispose();
            originalImage = (Bitmap)selectedImage.Image.Clone();

            // imageList�� �ִ� �ش� PictureBox�� 'original' ��Ʈ�ʵ� ������Ʈ�մϴ�.
            // �̷��� �����ν� Ȯ��/��� �ÿ��� ���Ͱ� ����� �̹����� �������� �����մϴ�.
            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    imageList[i].original?.Dispose(); // ���� ���� ����
                    imageList[i] = (selectedImage, (Bitmap)originalImage.Clone()); // �� ���� �Ҵ�
                    break;
                }
            }
            // _initialImage�� ���� �ε�� ���� �̹����� �����ؾ� �ϹǷ� ���⼭�� ������Ʈ���� �ʽ��ϴ�.
        }

        // ��� �������� �ʱ� ���·� �ǵ����ϴ�.
        private void btnResetAll_Click(object sender, EventArgs e)
        {
            // selectedImage.Tag�� ����� '��¥' ���� �̹����� �����ɴϴ�.
            if (selectedImage == null || selectedImage.Tag is not Bitmap trueOriginalBitmap)
            {
                // ���õ� �̹����� ���ٸ� UI�� �ʱ�ȭ�մϴ�.
                if (trackRed != null) trackRed.Value = 128;
                if (trackGreen != null) trackGreen.Value = 128;
                if (trackBlue != null) trackBlue.Value = 128;
                if (txtRed != null) txtRed.Text = "128";
                if (txtGreen != null) txtGreen.Text = "128";
                if (txtBlue != null) txtBlue.Text = "128";

                if (trackBrightness != null) trackBrightness.Value = 0;
                if (txtBrightness != null) txtBrightness.Text = "0";
                if (trackSaturation != null) trackSaturation.Value = 0;
                if (txtSaturation != null) txtSaturation.Text = "0";
                _currentFilter = FilterState.None;
                return;
            }

            // _initialImage�� originalImage�� selectedImage�� Tag�� �ִ� �������� �����մϴ�.
            _initialImage?.Dispose();
            _initialImage = (Bitmap)trueOriginalBitmap.Clone();
            originalImage?.Dispose();
            originalImage = (Bitmap)trueOriginalBitmap.Clone();

            // PictureBox�� �̹����� �������� �ǵ����ϴ�.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)trueOriginalBitmap.Clone();
            selectedImage.Invalidate();

            // UI ��Ʈ���� �ʱ�ȭ�մϴ�.
            if (trackRed != null) trackRed.Value = 128;
            if (trackGreen != null) trackGreen.Value = 128;
            if (trackBlue != null) trackBlue.Value = 128;
            if (txtRed != null) txtRed.Text = "128";
            if (txtGreen != null) txtGreen.Text = "128";
            if (txtBlue != null) txtBlue.Text = "128";

            if (trackBrightness != null) trackBrightness.Value = 0;
            if (txtBrightness != null) txtBrightness.Text = "0";
            if (trackSaturation != null) trackSaturation.Value = 0;
            if (txtSaturation != null) txtSaturation.Text = "0";
            _currentFilter = FilterState.None;
        }

        private void ApplyMonochromeFilter(FilterState filter)
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            // _currentFilter�� �����մϴ�.
            _currentFilter = filter;
            ApplyAllLivePreview();
        }

        private Bitmap AdjustRGB(Bitmap img, int rAdj, int gAdj, int bAdj)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int r = Clamp(pixel.R + rAdj);
                    int g = Clamp(pixel.G + gAdj);
                    int b = Clamp(pixel.B + bAdj);
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        // �ؽ�Ʈ�ڽ� ���� �̺�Ʈ �ڵ鷯 (�ǽð� ������Ʈ �� ���� ����)
        private void txtRed_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtRed.Text, out int val))
            {
                val = Math.Min(Math.Max(val, 0), 255);
                txtRed.Text = val.ToString();
                trackRed.Value = val;
                ApplyAllLivePreview();
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                txtRed.Text = trackRed.Value.ToString();
            }
            isTextChanging = false;
        }

        private void txtGreen_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtGreen.Text, out int val))
            {
                val = Math.Min(Math.Max(val, 0), 255);
                txtGreen.Text = val.ToString();
                trackGreen.Value = val;
                ApplyAllLivePreview();
            }
            else if (!string.IsNullOrEmpty(txtGreen.Text))
            {
                txtGreen.Text = trackGreen.Value.ToString();
            }
            isTextChanging = false;
        }

        private void txtBlue_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtBlue.Text, out int val))
            {
                val = Math.Min(Math.Max(val, 0), 255);
                txtBlue.Text = val.ToString();
                trackBlue.Value = val;
                ApplyAllLivePreview();
            }
            else if (!string.IsNullOrEmpty(txtBlue.Text))
            {
                txtBlue.Text = trackBlue.Value.ToString();
            }
            isTextChanging = false;
        }

        private void txtBrightness_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtBrightness.Text, out int val))
            {
                val = Math.Min(Math.Max(val, -100), 100);
                txtBrightness.Text = val.ToString();
                trackBrightness.Value = val;
                ApplyAllLivePreview();
            }
            else if (!string.IsNullOrEmpty(txtBrightness.Text))
            {
                txtBrightness.Text = trackBrightness.Value.ToString();
            }
            isTextChanging = false;
        }

        private void txtSaturation_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtSaturation.Text, out int val))
            {
                val = Math.Min(Math.Max(val, -100), 100);
                txtSaturation.Text = val.ToString();
                trackSaturation.Value = val;
                ApplyAllLivePreview();
            }
            else if (!string.IsNullOrEmpty(txtSaturation.Text))
            {
                txtSaturation.Text = trackSaturation.Value.ToString();
            }
            isTextChanging = false;
        }

        private Bitmap AdjustBrightness(Bitmap img, int brightness)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int r = Clamp(pixel.R + brightness);
                    int g = Clamp(pixel.G + brightness);
                    int b = Clamp(pixel.B + brightness);
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        private Bitmap AdjustSaturation(Bitmap img, float saturationFactor)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    float gray = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f);
                    int r = Clamp((int)(gray + (pixel.R - gray) * (1 + saturationFactor / 100)));
                    int g = Clamp((int)(gray + (pixel.G - gray) * (1 + saturationFactor / 100)));
                    int b = Clamp((int)(gray + (pixel.B - gray) * (1 + saturationFactor / 100)));
                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        private int Clamp(int val) => Math.Min(Math.Max(val, 0), 255);
        private Bitmap ConvertToGrayscale(Bitmap img)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int gray = (int)(0.3 * pixel.R + 0.59 * pixel.G + 0.11 * pixel.B);
                    newImg.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
            return newImg;
        }

        private Bitmap ApplySepia(Bitmap img)
        {
            Bitmap newImg = (Bitmap)img.Clone();
            for (int y = 0; y < newImg.Height; y++)
            {
                for (int x = 0; x < newImg.Width; x++)
                {
                    Color pixel = newImg.GetPixel(x, y);
                    int tr = (int)(0.393 * pixel.R + 0.769 * pixel.G + 0.189 * pixel.B);
                    int tg = (int)(0.349 * pixel.R + 0.686 * pixel.G + 0.168 * pixel.B);
                    int tb = (int)(0.272 * pixel.R + 0.534 * pixel.G + 0.131 * pixel.B);

                    int r = Math.Min(255, tr);
                    int g = Math.Min(255, tg);
                    int b = Math.Min(255, tb);

                    newImg.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return newImg;
        }

        // button10_Click: "����" ��ư Ŭ�� �̺�Ʈ �ڵ鷯
        private void button10_Click(object sender, EventArgs e)
        {
            Panel targetPanel = dynamicPanels[0]; // ���� �� RGB ����� �߰��� ù ��° �г�
            if (currentVisiblePanel == targetPanel)
            {
                targetPanel.Visible = false;
                currentVisiblePanel = null;
            }
            else
            {
                foreach (Panel panel in dynamicPanels)
                {
                    panel.Visible = false;
                }

                targetPanel.Visible = true;
                targetPanel.BringToFront();
                currentVisiblePanel = targetPanel;
                targetPanel.Invalidate();
            }
        }
    }
}