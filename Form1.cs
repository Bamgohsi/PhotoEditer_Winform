using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D; // DashStyle ����� ���� �߰�

namespace photo
{
    public partial class Form1 : Form
    {
        // Constants for layout
        private const int LeftMargin = 20; // �� ���� ����
        private const int TopMargin = 90; // �� ��� ���� (tabControl �Ʒ�)
        private const int PanelWidth = 300; // ������ �г��� ���� �ʺ�
        private const int PanelRightMargin = 10; // ������ �г��� �� ������ ����
        private const int GapBetweenPictureBoxAndPanel = 20; // pictureBox1�� ������ �г� ������ ����
        private const int BottomMargin = 20; // �� �ϴ� ����

        // �̹��� �巡�� �� ���θ� ��Ÿ���� �÷��� (��� �̹�����)
        private bool isDragging = false;

        // �巡�� ���� �� ���콺 Ŭ�� ���� ��ǥ (��� �̹�����)
        private Point clickOffset;

        // ���� �׵θ��� ǥ������ ���� (���콺 Ŭ�� �� true) (��� �̹�����)
        private bool showSelectionBorder = false;

        // �������� ������ ��ư�� �г� �迭
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;

        // ���� ǥ�õ� �г��� �����ϴ� ����
        private Panel currentVisiblePanel = null;

        // --- [NEW] �ռ�/�̸������ ���� ---
        private Image emojiPreviewImage = null;     // ���� �巡�� �� �̸�Ƽ�� (���� �̹���)
        // �̸�Ƽ�� ũ�� �Է� �ʵ尡 ���ŵǾ����Ƿ�, �⺻ ũ�⸦ �����մϴ�.
        private int emojiPreviewWidth = 64;         // �⺻ ũ��
        private int emojiPreviewHeight = 64;        // �⺻ ũ��
        private Point emojiPreviewLocation = Point.Empty; // ���� ��ġ
        private bool showEmojiPreview = false;      // ���� �̸����� ǥ�� ����

        // ���õ� �̸�Ƽ�� PictureBox (�巡��, ũ�� ������)
        private PictureBox selectedEmoji = null;
        private Point dragOffset; // �̸�Ƽ�� �巡�� ���� �� ������
        private bool resizing = false; // �̸�Ƽ�� ũ�� ���� �� ����
        private const int handleSize = 10; // ũ�� ���� �ڵ� ũ��


        public Form1()
        {
            InitializeComponent();

            // Initial setup for pictureBox1
            // Set initial location and size to fill the available space, respecting margins and panel area
            pictureBox1.Location = new Point(LeftMargin, TopMargin);
            int availableWidthForPb1 = this.ClientSize.Width - LeftMargin - (PanelWidth + PanelRightMargin + GapBetweenPictureBoxAndPanel);
            int availableHeightForPb1 = this.ClientSize.Height - TopMargin - BottomMargin;
            pictureBox1.Size = new Size(Math.Max(100, availableWidthForPb1), Math.Max(100, availableHeightForPb1)); // �ּ� ũ�� ����
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Use Zoom to fit image within bounds

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning

            // PictureBox1�� �巡�� �� ��� �̺�Ʈ ����
            pictureBox1.AllowDrop = true;
            pictureBox1.DragEnter += pictureBox1_DragEnter;
            pictureBox1.DragOver += pictureBox1_DragOver;
            pictureBox1.DragLeave += pictureBox1_DragLeave;
            pictureBox1.DragDrop += pictureBox1_DragDrop;

            // pictureBox1�� Ŀ���� �׸���(Paint) �̺�Ʈ ����
            pictureBox1.Paint += pictureBox1_Paint;

            this.WindowState = FormWindowState.Maximized; // ��üȭ������ ����
            this.Resize += Form1_Resize; // Add resize event handler for responsive layout

            // �� ��� Ŭ�� �� ��� ���� ���� (�̸�Ƽ�� �� ��� �̹���)
            this.MouseDown += Form1_MouseDown;
            // �� ������ Ŭ�� �õ� ��� ���� ���� (tabPage2�� Form1�� ���� �ڽ��̶�� ����)
            // ���� TabControl ���ο� �ִٸ�, TabControl�� MouseDown �̺�Ʈ�� Ȱ���ϰų�
            // Form1_MouseDown�� ��κ��� �� ���� Ŭ���� ó���� ���Դϴ�.
            tabPage2.MouseDown += Form1_MouseDown;
        }

        // �� ũ�� ���� �� ��Ʈ�ѵ��� ��ġ�� ũ�⸦ ������
        private void Form1_Resize(object sender, EventArgs e)
        {
            // Recalculate and set the size and location of pictureBox1
            int availableWidthForPb1 = this.ClientSize.Width - LeftMargin - (PanelWidth + PanelRightMargin + GapBetweenPictureBoxAndPanel);
            int availableHeightForPb1 = this.ClientSize.Height - TopMargin - BottomMargin;
            pictureBox1.Size = new Size(Math.Max(100, availableWidthForPb1), Math.Max(100, availableHeightForPb1));
            pictureBox1.Location = new Point(LeftMargin, TopMargin);

            // Recalculate and set the location and size of dynamicPanels
            Point panelLocation = new Point(this.ClientSize.Width - (PanelWidth + PanelRightMargin), TopMargin);
            Size panelSize = new Size(PanelWidth, this.ClientSize.Height - TopMargin - BottomMargin);

            foreach (Panel panel in dynamicPanels)
            {
                panel.Location = panelLocation;
                panel.Size = panelSize;
                panel.Invalidate(); // Redraw panel borders if visible
            }

            // tabControl1�� ��ġ�� �ʿ��ϴٸ� ������ (����� ���� ��ġ�� ����)
            // tabControl1.Location = new Point(12, 12);
        }

        // ��� �Ǵ� ���� ��� �ȼ��� �����ϰ� ����� �Լ� (���� �ڵ� ����, ���� ������ ����)
        private Bitmap MakeWhiteLikeTransparent(Bitmap src, int tolerance = 50)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(src, 0, 0);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            int transparentCount = 0;
            unsafe
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + y * data.Stride;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        // byte a = row[x * 4 + 3]; // 'a'�� ������ �����Ƿ� �ּ� ó��

                        if (
                            Math.Abs(r - 255) <= tolerance &&
                            Math.Abs(g - 255) <= tolerance &&
                            Math.Abs(b - 255) <= tolerance
                        )
                        {
                            row[x * 4 + 3] = 0; // ���� ��������!
                            transparentCount++;
                        }
                    }
                }
            }
            bmp.UnlockBits(data);
            MessageBox.Show($"����ȭ�� �ȼ� ��: {transparentCount}");
            return bmp;
        }

        // PictureBox1 ������ �巡�� ���� �� ȣ��� (���� �̸����� ������Ʈ)
        private void pictureBox1_DragOver(object sender, DragEventArgs e)
        {
            Point clientPos = pictureBox1.PointToClient(new Point(e.X, e.Y));
            emojiPreviewLocation = clientPos; // ���콺 ��ġ�� �̸����� ��ġ�� ����
            showEmojiPreview = true; // �̸����� ǥ��
            pictureBox1.Invalidate(); // PictureBox1�� �ٽ� �׷��� �̸����� ������Ʈ
            e.Effect = DragDropEffects.Copy; // ��� ȿ���� ����� ����
        }

        // PictureBox1���� �巡�װ� ����� �� ȣ��� (���� �̸����� ����)
        private void pictureBox1_DragLeave(object sender, EventArgs e)
        {
            showEmojiPreview = false; // �̸����� ����
            pictureBox1.Invalidate(); // PictureBox1�� �ٽ� �׷��� �̸����� ����
        }

        // �� ���� (�Ǵ� �� ������) Ŭ�� �� ��ü ���� ����
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // 1. ��� �̸�Ƽ�� PictureBox ���� ����
            // PictureBox1�� �ڽ� ��Ʈ�ѵ��� ��ȸ�ϸ� �̸�Ƽ�� PictureBox�� ã��
            if (pictureBox1 != null) // pictureBox1�� null�� �ƴ� ���� ����
            {
                foreach (Control c in pictureBox1.Controls)
                {
                    if (c is PictureBox pic)
                    {
                        pic.Tag = null; // ���� ����
                        pic.Invalidate(); // �ٽ� �׷��� �׵θ� ����
                    }
                }
            }
            selectedEmoji = null; // ���õ� �̸�Ƽ�� ���� ����

            // 2. ��� �̹����� �Ķ� �׵θ� ����
            showSelectionBorder = false;
            pictureBox1.Invalidate(); // PictureBox1�� �ٽ� �׷��� �׵θ� ����
        }

        // �巡�� �� ���콺�� pictureBox1 ���� ������ ��
        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            // �巡���ϴ� �����Ͱ� �̹������� Ȯ��
            if (e.Data.GetDataPresent(typeof(Bitmap)) || e.Data.GetDataPresent(typeof(Image)))
                e.Effect = DragDropEffects.Copy; // �̹����� ���� ȿ�� ���
            else
                e.Effect = DragDropEffects.None; // �ƴϸ� ��� �� ��
        }

        // PictureBox1�� �̸�Ƽ���� ������� ��
        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            // ��� �̹����� ������ ��� �Ұ�
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("���� ��� �̹����� �����ּ���!");
                showEmojiPreview = false; // �̸����� ����
                pictureBox1.Invalidate(); // �̸����� ���Ÿ� ���� �ٽ� �׸���
                return;
            }

            // �巡�� ���� �̸�Ƽ�� �̹����� ������ ����
            if (emojiPreviewImage == null)
            {
                showEmojiPreview = false; // �̸����� ����
                pictureBox1.Invalidate(); // �̸����� ���Ÿ� ���� �ٽ� �׸���
                return;
            }

            // ���ο� PictureBox ��Ʈ���� �����Ͽ� ��ӵ� �̸�Ƽ���� ǥ��
            PictureBox newEmojiPic = new PictureBox
            {
                Image = (Image)emojiPreviewImage.Clone(), // ���� �̹��� ���� (�߿�! ���� ���ҽ��� �������� �ʵ���)
                SizeMode = PictureBoxSizeMode.StretchImage, // ������ ũ�⿡ ���� �ø�
                Size = new Size(emojiPreviewWidth, emojiPreviewHeight), // �Էµ� ũ�� ���� (�⺻�� 64x64)
                Location = new Point(
                    emojiPreviewLocation.X - emojiPreviewWidth / 2, // ��� ��ġ�� �̸�Ƽ�� �߾� ����
                    emojiPreviewLocation.Y - emojiPreviewHeight / 2
                ),
                BackColor = Color.Transparent, // PictureBox�� ����� �����ϰ� ����
                Cursor = Cursors.SizeAll, // �巡�� ������ Ŀ���� ����
                Tag = "selected" // �ʱ⿡�� ���õ� ���·� ����
            };

            // ���� ������ �̸�Ƽ�� PictureBox�� �̺�Ʈ �ڵ鷯 ����
            newEmojiPic.MouseDown += Emoji_MouseDown;
            newEmojiPic.MouseMove += Emoji_MouseMove;
            newEmojiPic.MouseUp += Emoji_MouseUp;
            newEmojiPic.Paint += Emoji_Paint;

            // PictureBox1�� �ڽ� ��Ʈ�ѷ� �߰��Ͽ� ��� �̹��� ���� ���ٴϰ� ��
            pictureBox1.Controls.Add(newEmojiPic);

            // ������ ���õ� �̸�Ƽ���� �ִٸ� ���� ����
            if (selectedEmoji != null && selectedEmoji != newEmojiPic)
            {
                selectedEmoji.Tag = null;
                selectedEmoji.Invalidate();
            }
            selectedEmoji = newEmojiPic; // ���� ��ӵ� �̸�Ƽ���� ���õ� �̸�Ƽ������ ����

            // ���� �̸����� ����
            showEmojiPreview = false;
            pictureBox1.Invalidate(); // PictureBox1�� �ٽ� �׷��� ���� �̸����� ����
        }

        // �̸�Ƽ�� PictureBox�� ���콺 �ٿ� �̺�Ʈ (����, �巡��, ũ�� ���� ����)
        private void Emoji_MouseDown(object sender, MouseEventArgs e)
        {
            // �ٸ� ��� �̸�Ƽ�� PictureBox�� ������ ����
            foreach (Control c in pictureBox1.Controls)
            {
                if (c is PictureBox pic)
                {
                    pic.Tag = null;
                    pic.Invalidate();
                }
            }

            selectedEmoji = sender as PictureBox; // Ŭ���� �̸�Ƽ���� ���õ� �̸�Ƽ������ ����
            if (selectedEmoji != null)
            {
                selectedEmoji.Tag = "selected"; // ���õ����� ǥ��
                selectedEmoji.Invalidate(); // �ٽ� �׷��� �׵θ� ǥ��

                if (e.Button == MouseButtons.Left)
                {
                    // ũ�� ���� �ڵ� ���� Ȯ�� (������ �Ʒ� �𼭸�)
                    Rectangle resizeHandle = new Rectangle(
                        selectedEmoji.Width - handleSize,
                        selectedEmoji.Height - handleSize,
                        handleSize,
                        handleSize
                    );

                    if (resizeHandle.Contains(e.Location))
                    {
                        resizing = true; // ũ�� ���� ���
                    }
                    else
                    {
                        resizing = false; // �巡�� ���
                        dragOffset = e.Location; // �巡�� ���� �� ���콺 ������ ����
                    }
                }
            }
        }

        // �̸�Ƽ�� PictureBox�� ���콺 �̵� �̺�Ʈ (�巡�� �Ǵ� ũ�� ����)
        private void Emoji_MouseMove(object sender, MouseEventArgs e)
        {
            var emoji = sender as PictureBox;
            if (e.Button == MouseButtons.Left && selectedEmoji == emoji)
            {
                if (resizing)
                {
                    // �ּ� ũ�� 32, �ִ� ũ�� PictureBox1�� ũ�� (�Ǵ� ������ �ִ밪)
                    int newW = Math.Max(32, e.X);
                    int newH = Math.Max(32, e.Y);

                    // PictureBox1 ��踦 ���� �ʵ��� ũ�� ����
                    newW = Math.Min(newW, pictureBox1.Width - emoji.Left);
                    newH = Math.Min(newH, pictureBox1.Height - emoji.Top);

                    emoji.Size = new Size(newW, newH);
                }
                else
                {
                    // ��ġ �̵�
                    Point newLoc = emoji.Location;
                    newLoc.Offset(e.X - dragOffset.X, e.Y - dragOffset.Y);

                    // PictureBox1 ��踦 ���� �ʵ��� ��ġ ����
                    newLoc.X = Math.Max(0, Math.Min(newLoc.X, pictureBox1.Width - emoji.Width));
                    newLoc.Y = Math.Max(0, Math.Min(newLoc.Y, pictureBox1.Height - emoji.Height));

                    emoji.Location = newLoc;
                }
                emoji.Invalidate(); // ����� �̸�Ƽ�� PictureBox�� �ٽ� �׷��� ������Ʈ
            }
        }

        // �̸�Ƽ�� PictureBox�� ���콺 �� �̺�Ʈ (�巡�� �Ǵ� ũ�� ���� ����)
        private void Emoji_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false; // ũ�� ���� ��� ����
        }

        // �̸�Ƽ�� PictureBox�� Paint �̺�Ʈ (���� �׵θ� �� ũ�� ���� �ڵ� �׸���)
        private void Emoji_Paint(object sender, PaintEventArgs e)
        {
            var emoji = sender as PictureBox;
            // Tag�� "selected"�� ������ ��쿡�� �׵θ� �� �ڵ� �׸���
            if (emoji.Tag != null && emoji.Tag.ToString() == "selected")
            {
                // �Ķ� �׵θ� �׸���
                using (Pen p = new Pen(Color.DeepSkyBlue, 2))
                    e.Graphics.DrawRectangle(p, 1, 1, emoji.Width - 3, emoji.Height - 3); // �ȼ� ������ ���

                // ũ�� ������ �ڵ� (������ �Ʒ� �簢��) �׸���
                e.Graphics.FillRectangle(Brushes.DeepSkyBlue, emoji.Width - handleSize, emoji.Height - handleSize, handleSize, handleSize);
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

            // 8��° �г� (�ε��� 7)�� �̸�Ƽ�� ���� ��Ʈ�� �߰�
            var panel8 = dynamicPanels[7];
            // �̸�Ƽ�� ũ�� �Է� �ʵ� ����
            // panel8.Controls.Add(new Label { Text = "�̸�Ƽ�� ũ��:", Location = new Point(10, 40) });
            // TextBox txtEmojiWidth = new TextBox { Name = "txtEmojiWidth", Text = "64", Location = new Point(10, 60), Width = 50 };
            // TextBox txtEmojiHeight = new TextBox { Name = "txtEmojiHeight", Text = "64", Location = new Point(70, 60), Width = 50 };
            // panel8.Controls.Add(new Label { Text = "W:", Location = new Point(10, 80) });
            // panel8.Controls.Add(new Label { Text = "H:", Location = new Point(70, 80) });
            // panel8.Controls.Add(txtEmojiWidth);
            // panel8.Controls.Add(txtEmojiHeight);

            // ũ�� �Է��� �ٲ� ������ �� ����! (���� ��ȯ ���� �� �⺻�� ����)
            // txtEmojiWidth.TextChanged += (s, e) => { if (!int.TryParse(txtEmojiWidth.Text, out emojiPreviewWidth)) emojiPreviewWidth = 64; };
            // txtEmojiHeight.TextChanged += (s, e) => { if (!int.TryParse(txtEmojiHeight.Text, out emojiPreviewHeight)) emojiPreviewHeight = 64; };

            panel8.AllowDrop = true;  // �巡�� ����
            panel8.AutoScroll = true; // ��ũ�� ����

            // ���ҽ��� �߰��� �̸�Ƽ�� �̹��� �̸����� �迭 ����
            // (Properties.Resources.EmojiX�� ����ڰ� ������Ʈ ���ҽ��� �߰��ߴٰ� ����)
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

            int iconSize = 48, padding = 8; // �̸�Ƽ�� ������ ũ�� �� �е�
            // �̸�Ƽ�� ũ�� �Է� �ʵ尡 ���ŵǾ����Ƿ�, �̸�Ƽ�� ����� ���� Y ��ġ�� �����մϴ�.
            int startYForEmojis = 40; // �г� ��ܿ��� ������ ���� ��ġ
            int iconsPerRow = (panel8.Width - padding * 2) / (iconSize + padding); // �� �ٿ� ǥ�õ� ������ �� ���

            for (int i = 0; i < emojis.Length; i++)
            {
                var pic = new PictureBox
                {
                    Image = emojis[i],
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(iconSize, iconSize),
                    Cursor = Cursors.Hand, // �巡�� ������ Ŀ��
                    Location = new Point(
                        padding + (i % iconsPerRow) * (iconSize + padding),
                        startYForEmojis + (i / iconsPerRow) * (iconSize + padding)
                    )
                };
                // �̸�Ƽ�� PictureBox�� ���콺 �ٿ� �̺�Ʈ: �巡�� ����
                pic.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        emojiPreviewImage = ((PictureBox)s).Image; // �巡���� �̸�Ƽ�� �̹��� ����
                        // DoDragDrop ȣ���Ͽ� �巡�� �� ��� �۾� ����
                        (s as PictureBox).DoDragDrop((s as PictureBox).Image, DragDropEffects.Copy);
                    }
                };

                panel8.Controls.Add(pic);
            }

            // ù ��° �г��� �ʱ� ���¿��� ���̰� �����ϰ�, �׵θ��� �׸��� ���� Invalidate ȣ��
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

        // [���� �����] ��ư Ŭ�� �� ����
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            // ��� �̹��� ����
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;

            // �߰��� ��� �̸�Ƽ�� PictureBox ����
            foreach (Control c in pictureBox1.Controls)
            {
                if (c is PictureBox pic)
                {
                    pic.Dispose();
                }
            }
            pictureBox1.Controls.Clear(); // ��� �ڽ� ��Ʈ�� ����
            selectedEmoji = null; // ���õ� �̸�Ƽ�� �ʱ�ȭ
            showSelectionBorder = false; // ��� �̹��� �׵θ� ����
            pictureBox1.Invalidate(); // PictureBox1 �ٽ� �׸���
        }

        // [����] ��ư Ŭ�� �� ����
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose(); // ���� �̹��� ����
                    Image img = Image.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = img;
                    // PictureBox�� ũ��� ��ġ�� Form1_Resize �Ǵ� �ʱ� �������� �̹� ó���ǹǷ�,
                    // AutoSize�� �������� Size/Location ������ �����մϴ�.
                    // pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize; // ����
                    // pictureBox1.Size = img.Size; // ����
                    // pictureBox1.Location = new Point(10, 10); // ����

                    // ��� �̹��� �ε� �� ���� �̸�Ƽ�� ��� ����
                    foreach (Control c in pictureBox1.Controls)
                    {
                        if (c is PictureBox pic)
                        {
                            pic.Dispose();
                        }
                    }
                    pictureBox1.Controls.Clear();
                    selectedEmoji = null;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�: " + ex.Message);
                }
            }
        }

        // [����] ��ư Ŭ�� �� ���� (�ռ��� ���� �̹����� ����)
        private void btn_Save_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("������ �̹����� �����ϴ�.");
                return;
            }

            // ���� �ռ��� �̹����� ���� Bitmap ����
            // PictureBox1�� ���� ũ�⸦ ��� (�̹��� ũ�Ⱑ �ƴ�)
            Bitmap finalImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                // PictureBox1�� ��� �̹��� �׸���
                if (pictureBox1.Image != null)
                {
                    g.DrawImage(pictureBox1.Image, 0, 0, pictureBox1.Width, pictureBox1.Height);
                }

                // PictureBox1 ���� �ִ� ��� �̸�Ƽ�� PictureBox �׸���
                foreach (Control control in pictureBox1.Controls)
                {
                    if (control is PictureBox emojiPic)
                    {
                        // �̸�Ƽ�� PictureBox�� �̹����� ���� ��ġ�� ũ��� �׸�
                        g.DrawImage(emojiPic.Image, emojiPic.Location.X, emojiPic.Location.Y, emojiPic.Width, emojiPic.Height);
                    }
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "�̹��� ����";
            saveFileDialog.Filter = "PNG �̹���|*.png|JPEG �̹���|*.jpg|BMP �̹���|*.bmp";
            saveFileDialog.FileName = "�ռ���_�̹���.png"; // �⺻ ���� �̸�

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // ���õ� ���� ���Ŀ� ���� ����
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1: // PNG
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            break;
                        case 2: // JPEG
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;
                        case 3: // BMP
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                            break;
                    }
                    MessageBox.Show("�̹����� ���������� ����Ǿ����ϴ�.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹��� ���� �� ���� �߻�: " + ex.Message);
                }
            }
            finalImage.Dispose(); // ��� �� Bitmap ����
        }


        // ��� �̹��� (pictureBox1)�� ���콺 ��ư�� ���� �� ȣ���
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // �̸�Ƽ���� �ƴ� ��� �̹����� �巡�� ����
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                // �ٸ� ��� �̸�Ƽ�� PictureBox�� ������ ����
                foreach (Control c in pictureBox1.Controls)
                {
                    if (c is PictureBox pic)
                    {
                        pic.Tag = null;
                        pic.Invalidate();
                    }
                }
                selectedEmoji = null; // ���õ� �̸�Ƽ�� ���� ����

                isDragging = true; // ��� �̹��� �巡�� ���� �÷���
                clickOffset = e.Location; // Ŭ�� ������ ����
                showSelectionBorder = true; // ��� �̹��� ���� �׵θ� ǥ��
                pictureBox1.Invalidate(); // PictureBox1�� �ٽ� �׷��� �׵θ� ������Ʈ
            }
        }

        // ��� �̹��� (pictureBox1) ���콺�� �̵��� �� ȣ���
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;
                pictureBox1.Location = newLocation; // PictureBox1 ��ġ �̵�
            }
        }

        // ��� �̹��� (pictureBox1) ���콺 ��ư�� ���� �� ȣ���
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false; // ��� �̹��� �巡�� ����
            // showSelectionBorder = false; // ��� �̹��� ���� �����ϰ� ���� ��� �ּ� ���� (Ŭ�� �� �����ǵ��� Form1_MouseDown���� ó��)
            pictureBox1.Invalidate(); // PictureBox1 �ٽ� �׸���
        }

        // �� �ε� �� ���� (�ʿ� �� �ʱ�ȭ ó�� ����)
        private void Form1_Load(object sender, EventArgs e)
        {
            // �ʱ�ȭ ���� (����� Ư���� ����)
        }

        // pictureBox1�� �ٽ� �׷��� �� ȣ��� (���� �̸����� �� ���� �׵θ� �׸�)
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // --- ���� �̸����� �׸��� ---
            if (showEmojiPreview && emojiPreviewImage != null)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = DashStyle.Dash; // ���� ��Ÿ��
                    Rectangle rect = new Rectangle(
                        emojiPreviewLocation.X - emojiPreviewWidth / 2, // �̸����� ��ġ ���
                        emojiPreviewLocation.Y - emojiPreviewHeight / 2,
                        emojiPreviewWidth, emojiPreviewHeight);
                    e.Graphics.DrawRectangle(pen, rect); // ���� �簢�� �׸���
                }
            }

            // --- ��� �̹��� ���� �׵θ� �׸��� ---
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.Blue, 3)) // �Ķ��� �β��� �׵θ�
                {
                    // PictureBox1�� ��迡 �׵θ� �׸���
                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);
                }
            }
        }
    }
}