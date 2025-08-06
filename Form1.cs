using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging; // Added for ColorMatrix, ImageAttributes
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace photo
{

    public partial class Form1 : Form
    {
        private ContextMenuStrip imageContextMenu;
        private ToolStripMenuItem menuCopy;
        private ToolStripMenuItem menuPaste;
        private ToolStripMenuItem menuDelete;
        private List<ClipboardItem> clipboardContent = new List<ClipboardItem>(); // ���� �̹����� ���� Ŭ������
        // --- �̸��� Undo/Redo ���� ���� ---
        private Stack<List<EmojiState>> emojiUndoStack = new Stack<List<EmojiState>>();
        private Stack<List<EmojiState>> emojiRedoStack = new Stack<List<EmojiState>>();

        // --- Ŭ������ ������ ������ ���� ���� Ŭ���� ---
        private class ClipboardItem
        {
            public Bitmap Image { get; set; }
            public Point RelativeLocation { get; set; }
        }
        // =======================================================
        // �̹��� ���� �� UI ���� ������ (������ �ڵ� ����)
        // =======================================================

        // Constants for layout
        private const int LeftMargin = 20;
        private const int TopMargin = 90;
        private const int PanelWidth = 300;
        private const int PanelRightMargin = 20;
        private const int GapBetweenPictureBoxAndPanel = 20;
        private const int BottomMargin = 20;
        private const int LeftPanelWidth = 80; // Added from previous context

        // �̹��� ������ ������ ����Ʈ
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();

        // ���� ������ ���� (������ �ڵ忡�� ���ŵ�, ũ�� ���� ����� �ؽ�Ʈ�ڽ�/�巡�׷� ��ü)
        // private float currentScale = 1.0f; 

        // �̹����� ���� �� ���� �߰�
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        // ���ο� �� ��ȣ�� �����ִ� ����
        private int tabCount = 2;

        // --- ���� ���� ������ ---
        private bool isDragging = false;
        private Point clickOffset; // ������ ���콺 �巡�� �����°� �ߺ�, �Ʒ� dragStartMousePosition���� ���� ����
        private PictureBox draggingPictureBox = null;
        private Point dragStartMousePosition; // �θ� ��Ʈ�� ���� ���콺 ���� ��ġ
        private Dictionary<PictureBox, Point> dragStartPositions = new Dictionary<PictureBox, Point>(); // �巡�� ���� ������ ��� PictureBox ��ġ

        private bool isResizing = false;
        private Point resizeStartPoint; // ������ ���� (���ο� ũ�� ���� ����������)
        private Size resizeStartSize; // ������ ����
        private Point resizeStartLocation; // ������ ����
        private string resizeDirection = "";

        // ���� showSelectionBorder (������ �ڵ�)�� ����
        // private bool showSelectionBorder = false;

        // UI ��� �迭
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        private Panel currentVisiblePanel = null;

        // �̹��� ���� ����
        private List<PictureBox> selectedImages = new List<PictureBox>(); // ���� �̹����� ���� ����Ʈ
        private PictureBox selectedImage = null; // ���� Ȱ��ȭ��(���������� ���õ�) �̹���
        // private bool showSelectionBorderForImage = false; // PictureBox_Paint���� selectedImages�� ��ü

        // �̸��� �巡�� �� ���
        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;
        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false; // �̸��� ������¡��
        private const int handleSize = 10;

        // ���� ���� ����Ű (Marquee)
        private bool isMarqueeSelecting = false; // ���� �巡�� ���� ������ ����
        private Point marqueeStartPoint; // �巡�� ���� ����
        private Rectangle marqueeRect; // ȭ�鿡 �׷��� ���� �簢��

        // --- ���� ��� ���� ���� ---
        private Bitmap originalImage; // ���� - ���� �̹��� ����� (���� ���� ���� �̹���)
        private Bitmap _initialImage; // ���� - ���� �ε�� ���� �̹��� ����� (���¿�)
        private TrackBar trackRed, trackGreen, trackBlue; // ���� - RGB ���� ��Ʈ��
        private TextBox txtRed, txtGreen, txtBlue; // ���� - RGB ���� �ؽ�Ʈ�ڽ�
        private TrackBar trackBrightness, trackSaturation; // ���� - ���/ä�� ���� ��Ʈ��
        private TextBox txtBrightness, txtSaturation; // ���� - ���/ä�� �ؽ�Ʈ�ڽ�
        private Button btnApplyAll, btnResetAll; // ���� - ��� ������ ����/�ʱ�ȭ�ϴ� ��ư
        private enum FilterState { None, Grayscale, Sepia } // ���� - �ܻ� ���� ����
        private FilterState _currentFilter = FilterState.None; // ����
        private bool isTextChanging = false; // ���� - TextBox.TextChanged�� TrackBar.Scroll ���ѷ��� ������

        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls(); // ���� ��Ʈ�� �ʱ�ȭ
            InitializeContextMenu(); // <<-- �� ���� �߰��ϼ���!

            this.Resize += Form1_Resize; // �� ũ�� ���� �̺�Ʈ
            this.WindowState = FormWindowState.Maximized; // �� �ִ�ȭ
            this.MouseDown += Form1_MouseDown; // �� ��ü ���콺 �ٿ� �̺�Ʈ (���� ���� ��)

            // �ؽ�Ʈ �ڽ� ���ڸ� �Է� �� ��ȿ�� �˻�
            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox1.Validating += textBox1_Validating;
            textBox2.Validating += textBox2_Validating;
            textBox1.KeyDown += TextBox_KeyDown_ApplyOnEnter; // ���� Ű�� ����
            textBox2.KeyDown += TextBox_KeyDown_ApplyOnEnter; // ���� Ű�� ����
            textBox3.KeyPress += TextBox_OnlyNumber_KeyPress; // ���ڸ� �Է�
            textBox4.KeyPress += TextBox_OnlyNumber_KeyPress; // ���ڸ� �Է�
            textBox3.Validating += textBox3_Validating;       // �� ���� �� ����
            textBox4.Validating += textBox4_Validating;       // �� ���� �� ����
            textBox3.KeyDown += TextBox_KeyDown_ApplyOnEnter; // ���� Ű�� ����
            textBox4.KeyDown += TextBox_KeyDown_ApplyOnEnter; // ���� Ű�� ����

            this.BackColor = ColorTranslator.FromHtml("#FFF0F5"); // �� ���� (�󺥴� ����)

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        // �ؽ�Ʈ �ڽ����� ���� Ű ������ ���� ��Ʈ�ѷ� ��Ŀ�� �̵� (ũ�� ���� ����)
        private void TextBox_KeyDown_ApplyOnEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.SelectNextControl((Control)sender, true, true, true, true);
                e.SuppressKeyPress = true;
            }
        }

        // �ؽ�Ʈ �ڽ��� ���ڸ� �Է� �����ϵ��� �ϴ� �̺�Ʈ �ڵ鷯
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

            int totalLeft = LeftMargin + LeftPanelWidth + GapBetweenPictureBoxAndPanel;
            tabControl1.Location = new Point(totalLeft, TopMargin);
            tabControl1.Size = new Size(
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin - 15,
                this.ClientSize.Height - TopMargin - BottomMargin
            );
            groupBox2.Width = this.ClientSize.Width - 24; // groupBox2�� �ִٸ� �ʺ� ���� (����)
        }

        // [���� �����] ��ư Ŭ�� �� ����
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab != null)
            {
                // ���� ���� ��� PictureBox ����
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
            originalImage = null;
            _initialImage = null;
            // ���õ� �̹��� �� ����Ʈ �ʱ�ȭ
            selectedImage = null;
            selectedImages.Clear();
            imageList.Clear(); // �̹��� ����Ʈ�� �ʱ�ȭ
            btnResetAll_Click(null, null); // ���� ��Ʈ�� �ʱ�ȭ
        }

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
                    if (currentTab == null) return; // ���� ���� ������ �ƹ��͵� ���� ����

                    // ���� ���õ� �̹����� �ʱ�ȭ
                    foreach (var item in selectedImages) { item.Invalidate(); }
                    selectedImages.Clear();
                    selectedImage = null;

                    PictureBox pb = new PictureBox();
                    pb.AllowDrop = true; // �̸��� �巡�� �� ��� ���
                    pb.DragEnter += PictureBox_DragEnter;
                    pb.DragOver += PictureBox_DragOver;
                    pb.DragLeave += PictureBox_DragLeave;
                    pb.DragDrop += PictureBox_DragDrop;

                    // PictureBox �Ӽ� ����
                    pb.SizeMode = PictureBoxSizeMode.StretchImage; // �̹��� ũ�� ���� ����
                    pb.Anchor = AnchorStyles.Top | AnchorStyles.Left; // �ڵ� ���̾ƿ� �浹 ����
                    pb.Dock = DockStyle.None; // Dock �Ӽ� ����
                    pb.Location = new Point(10, 30); // �ʱ� ��ġ
                    EnableDoubleBuffering(pb); // ���� ���۸� Ȱ��ȭ

                    Bitmap originalCopy;
                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original);
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size; // �ʱ� ũ��� �̹��� ũ��� ����
                    pb.Tag = originalCopy; // ���� ��Ʈ���� Tag�� ����
                    imageList.Add((pb, originalCopy)); // imageList�� �߰�

                    // PictureBox �̺�Ʈ �ڵ鷯 ����
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;

                    currentTab.Controls.Add(pb); // ���� �ǿ� PictureBox �߰�

                    // UI �ؽ�Ʈ�ڽ� ������Ʈ �� ���� ���� ����
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    selectedImage = pb;
                    selectedImages.Add(pb); // �� �̹��� ���� ����Ʈ�� �߰�
                    pb.Invalidate(); // �׵θ� �׸��⸦ ���� Invalidate ȣ��

                    // ���� - �̹��� ������ ���� �̹��� ���� �� ��Ʈ�� �ʱ�ȭ
                    originalImage = new Bitmap(originalCopy);
                    _initialImage = new Bitmap(originalCopy);
                    btnResetAll_Click(null, null); // ���� ��Ʈ�� �ʱ�ȭ
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�:\n" + ex.Message);
                }
            }
        }

        // PictureBox ���콺 �ٿ� �̺�Ʈ �ڵ鷯 (����, �巡��, �������� ����)
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                // --- �̸�Ƽ�� ���� ���� ���� (�߰�) ---
                bool emojiClicked = false;
                // Ŭ���� ��ġ�� �ڽ� ��Ʈ��(�̸�Ƽ��)�� �ִ��� Ȯ��
                foreach (Control child in pb.Controls)
                {
                    if (child is PictureBox && child.Bounds.Contains(e.Location))
                    {
                        emojiClicked = true;
                        break;
                    }
                }

                // �̸�Ƽ���� �ƴ� ����� Ŭ���ߴٸ�, ��� �ڽ� �̸�Ƽ���� ������ ����
                if (!emojiClicked)
                {
                    foreach (Control child in pb.Controls)
                    {
                        if (child is PictureBox emoji)
                        {
                            emoji.Tag = null; // ���� �±� ����
                            emoji.Invalidate(); // �ٽ� �׷��� �׵θ� ���ֱ�
                        }
                    }
                    selectedEmoji = null; // ���� ���� ������ �ʱ�ȭ
                }
                // --- ������� �߰� ---

                bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;

                if (isCtrlPressed) // Ctrl Ű ���� ���¿��� Ŭ�� �� ���� ����
                {
                    if (selectedImages.Contains(pb))
                    {
                        selectedImages.Remove(pb);
                        selectedImage = selectedImages.LastOrDefault();
                    }
                    else
                    {
                        selectedImages.Add(pb);
                        selectedImage = pb;
                    }
                }
                else // Ctrl Ű ���� Ŭ�� �� ���� ����
                {
                    if (!selectedImages.Contains(pb))
                    {
                        foreach (var item in selectedImages) { item.Invalidate(); }
                        selectedImages.Clear();
                    }
                    if (!selectedImages.Contains(pb))
                    {
                        selectedImages.Add(pb);
                    }
                    selectedImage = pb;
                }

                if (selectedImage != null)
                {
                    textBox1.Text = selectedImage.Width.ToString();
                    textBox2.Text = selectedImage.Height.ToString();
                    textBox3.Text = selectedImage.Left.ToString();
                    textBox4.Text = selectedImage.Top.ToString();
                    UpdateEditControlsFromSelectedImage();
                }

                foreach (var item in selectedImages) { item.Invalidate(); }

                if (!string.IsNullOrEmpty(resizeDirection) && !emojiClicked) // �̸�Ƽ�� Ŭ�� �ÿ��� ��������/�巡�� ����
                {
                    isResizing = true;
                    isDragging = false;
                    resizeStartPoint = e.Location;
                    resizeStartSize = pb.Size;
                    resizeStartLocation = pb.Location;
                }
                else if (!emojiClicked) // �̸�Ƽ�� Ŭ�� �ÿ��� ��������/�巡�� ����
                {
                    isDragging = true;
                    isResizing = false;
                    dragStartPositions.Clear();
                    dragStartMousePosition = pb.Parent.PointToClient(MousePosition);
                    foreach (var selectedPb in selectedImages)
                    {
                        dragStartPositions.Add(selectedPb, selectedPb.Location);
                    }
                }
            }
        }

        // PictureBox ���콺 �̵� �̺�Ʈ �ڵ鷯 (�巡��, �������� ��)
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            if (isResizing) // �������� ���� ��
            {
                Point mousePosInParent = pb.Parent.PointToClient(MousePosition); // �θ� ��Ʈ�� ���� ���콺 ��ġ

                int fixedRight = resizeStartLocation.X + resizeStartSize.Width;
                int fixedBottom = resizeStartLocation.Y + resizeStartSize.Height;
                int fixedLeft = resizeStartLocation.X;
                int fixedTop = resizeStartLocation.Y;

                // ���ο� ũ�� �� ��ġ ���
                int newWidth = pb.Width;
                int newHeight = pb.Height;
                int newLeft = pb.Left;
                int newTop = pb.Top;

                if (resizeDirection.Contains("Right"))
                {
                    newWidth = Math.Max(20, mousePosInParent.X - fixedLeft);
                }
                if (resizeDirection.Contains("Left"))
                {
                    newWidth = Math.Max(20, fixedRight - mousePosInParent.X);
                    newLeft = fixedRight - newWidth;
                }
                if (resizeDirection.Contains("Bottom"))
                {
                    newHeight = Math.Max(20, mousePosInParent.Y - fixedTop);
                }
                if (resizeDirection.Contains("Top"))
                {
                    newHeight = Math.Max(20, fixedBottom - mousePosInParent.Y);
                    newTop = fixedBottom - newHeight;
                }

                // ���� PictureBox �Ӽ� ������Ʈ
                pb.SetBounds(newLeft, newTop, newWidth, newHeight);

                // �ؽ�Ʈ�ڽ� ������Ʈ
                textBox1.Text = pb.Width.ToString();
                textBox2.Text = pb.Height.ToString();
            }
            else if (isDragging) // �巡�� ���� ��
            {
                // ���� ���콺 ��ġ (�θ� ��Ʈ�� ����)
                Point currentMousePosition = pb.Parent.PointToClient(MousePosition);
                // �巡�� ���� ��ġ�κ����� ��ȭ��(��Ÿ) ���
                int deltaX = currentMousePosition.X - dragStartMousePosition.X;
                int deltaY = currentMousePosition.Y - dragStartMousePosition.Y;

                // ��ųʸ��� ����� ��� ���� �̹������� ��ȸ�ϸ� ��ġ ������Ʈ
                foreach (var item in dragStartPositions)
                {
                    PictureBox targetPb = item.Key;
                    Point startPosition = item.Value;

                    targetPb.Location = new Point(startPosition.X + deltaX, startPosition.Y + deltaY);
                }
                if (selectedImage != null)
                {
                    textBox3.Text = selectedImage.Left.ToString();
                    textBox4.Text = selectedImage.Top.ToString();
                }
            }
            else // �巡��/�������� ���� �ƴ� �� (Ŀ�� ��� ����)
            {
                const int edge = 5; // �׵θ� ���� ����
                bool atTop = e.Y <= edge;
                bool atBottom = e.Y >= pb.Height - edge;
                bool atLeft = e.X <= edge;
                bool atRight = e.X >= pb.Width - edge;

                if (atTop && atLeft) { pb.Cursor = Cursors.SizeNWSE; resizeDirection = "TopLeft"; }
                else if (atTop && atRight) { pb.Cursor = Cursors.SizeNESW; resizeDirection = "TopRight"; }
                else if (atBottom && atLeft) { pb.Cursor = Cursors.SizeNESW; resizeDirection = "BottomLeft"; }
                else if (atBottom && atRight) { pb.Cursor = Cursors.SizeNWSE; resizeDirection = "BottomRight"; }
                else if (atTop) { pb.Cursor = Cursors.SizeNS; resizeDirection = "Top"; }
                else if (atBottom) { pb.Cursor = Cursors.SizeNS; resizeDirection = "Bottom"; }
                else if (atLeft) { pb.Cursor = Cursors.SizeWE; resizeDirection = "Left"; }
                else if (atRight) { pb.Cursor = Cursors.SizeWE; resizeDirection = "Right"; }
                else { pb.Cursor = Cursors.Default; resizeDirection = ""; }
            }
        }

        // PictureBox ���콺 �� �̺�Ʈ �ڵ鷯 (�巡��, �������� ����)
        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // --- ��Ŭ�� �� �޴� ǥ�� ���� (�߰�) ---
                if (e.Button == MouseButtons.Right)
                {
                    // �޴� ������ Ȱ��ȭ/��Ȱ��ȭ
                    menuCopy.Enabled = selectedImages.Count > 0;
                    menuDelete.Enabled = selectedImages.Count > 0;
                    menuPaste.Enabled = clipboardContent.Count > 0;

                    // �޴��� � PictureBox���� �̺�Ʈ�� �߻��ߴ��� �˷���
                    imageContextMenu.Tag = pb;
                    imageContextMenu.Show(pb, e.Location);
                }
                if (isResizing)
                {
                    // �������� ���� �� ���� ũ�⸦ �ؽ�Ʈ�ڽ��� �ݿ� (MouseMove���� �̹� �ݿ��ǹǷ� ���û���)
                    // textBox1.Text = pb.Width.ToString();
                    // textBox2.Text = pb.Height.ToString();
                    // UpdateSelectedImageSize(); // �ʿ�� ȣ���Ͽ� ���� �̹��� �������� �ݿ�
                }

                isDragging = false;
                isResizing = false;
                draggingPictureBox = null;
                resizeDirection = "";

                pb.Invalidate(); // �׵θ� ������Ʈ
            }
        }

        // PictureBox �׸��� �̺�Ʈ �ڵ鷯 (���� �׵θ�, �̸��� �̸�����)
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // ���õ� �̹��� �׵θ� �׸���
            if (selectedImages.Contains(pb))
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    // ���������� ���õ�(Ȱ��ȭ��) �̹����� �Ǽ�, �������� �������� ����
                    if (pb == selectedImage)
                    {
                        pen.DashStyle = DashStyle.Solid;
                    }
                    else
                    {
                        pen.DashStyle = DashStyle.Dot;
                    }

                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            // �̸��� �̸����� �׸���
            if (showEmojiPreview && pb == selectedImage && emojiPreviewImage != null)
            {
                e.Graphics.DrawImage(emojiPreviewImage,
                                     emojiPreviewLocation.X - emojiPreviewWidth / 2,
                                     emojiPreviewLocation.Y - emojiPreviewHeight / 2,
                                     emojiPreviewWidth, emojiPreviewHeight);
            }
        }

        // ��Ʈ�ѿ� ���� ���۸� Ȱ��ȭ
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        // �̸��� �巡�� �� ��� ����
        private void PictureBox_DragDrop(object sender, DragEventArgs e)
        {
            var basePictureBox = sender as PictureBox;
            if (basePictureBox == null || basePictureBox.Image == null || emojiPreviewImage == null)
            {
                showEmojiPreview = false;
                basePictureBox?.Invalidate();
                return;
            }
            selectedImage = basePictureBox;

            // --- Undo ���ÿ� ���� ���� ��� (�߰�) ---
            emojiUndoStack.Push(CaptureCurrentEmojis(basePictureBox));
            emojiRedoStack.Clear(); // ���ο� �۾��̹Ƿ� Redo ������ ���
            // ------------------------------------

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
            // �ٸ� �̸��� ���� ����
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
                    resizing = resizeHandle.Contains(e.Location); // �������� �ڵ� Ŭ�� ����
                    if (!resizing)
                        dragOffset = e.Location; // �巡�� ������ ����
                }
            }
        }

        private void Emoji_MouseMove(object sender, MouseEventArgs e)
        {
            var emoji = sender as PictureBox;
            var parent = emoji?.Parent as PictureBox;

            if (e.Button == MouseButtons.Left && selectedEmoji == emoji && parent != null)
            {
                if (resizing) // �̸��� ��������
                {
                    int newW = Math.Max(32, e.X);
                    int newH = Math.Max(32, e.Y);
                    newW = Math.Min(newW, parent.Width - emoji.Left); // �θ� PictureBox ���� ���� ����
                    newH = Math.Min(newH, parent.Height - emoji.Top);
                    emoji.Size = new Size(newW, newH);
                }
                else // �̸��� �̵�
                {
                    Point newLoc = emoji.Location;
                    newLoc.Offset(e.X - dragOffset.X, e.Y - dragOffset.Y);
                    newLoc.X = Math.Max(0, Math.Min(newLoc.X, parent.Width - emoji.Width)); // �θ� PictureBox ���� ���� ����
                    newLoc.Y = Math.Max(0, Math.Min(newLoc.Y, parent.Height - emoji.Height));
                    emoji.Location = newLoc;
                }
                emoji.Invalidate(); // �̸��� �ٽ� �׸���
            }
        }

        private void Emoji_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false; // �̸��� �������� ����
        }

        private void Emoji_Paint(object sender, PaintEventArgs e)
        {
            var emoji = sender as PictureBox;
            if (emoji.Tag != null && emoji.Tag.ToString() == "selected")
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, emoji.Width - 3, emoji.Height - 3); // �̸��� �׵θ�
                e.Graphics.FillRectangle(Brushes.DeepSkyBlue,
                    emoji.Width - handleSize,
                    emoji.Height - handleSize,
                    handleSize, handleSize); // �������� �ڵ�
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
                g.Clear(Color.White); // ����� �������
                foreach (var pb in pictureBoxes)
                {
                    g.DrawImage(pb.Image, pb.Location); // PictureBox�� �̹����� �׸��ϴ�.
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "�̹��� ����";
            saveFileDialog.Filter = "JPEG ���� (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG ���� (*.png)|*.png|BMP ���� (*.bmp)|*.bmp|GIF ���� (*.gif)|*.gif";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();
                var format = System.Drawing.Imaging.ImageFormat.Png; // �⺻ ����

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg": format = System.Drawing.Imaging.ImageFormat.Jpeg; break;
                    case ".bmp": format = System.Drawing.Imaging.ImageFormat.Bmp; break;
                    case ".gif": format = System.Drawing.Imaging.ImageFormat.Gif; break;
                    case ".png": format = System.Drawing.Imaging.ImageFormat.Png; break;
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
            combinedImage.Dispose(); // ��� �� ���ҽ� ����
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (TabPage tab in tabControl1.TabPages)
            {
                tab.MouseDown += TabPage_MouseDown;
                tab.MouseMove += TabPage_MouseMove;
                tab.MouseUp += TabPage_MouseUp;
                tab.Paint += TabPage_Paint;
            }
        }

        // �� �� �߰� ��ư Ŭ��
        private void btnNewTabPage_Click(object sender, EventArgs e)
        {
            TabPage newTabPage = new TabPage($"tp {tabCount}");
            newTabPage.Name = $"tp{tabCount}";
            newTabPage.BackColor = Color.White;

            // �� �ǿ��� �̺�Ʈ �ڵ鷯 ����
            newTabPage.MouseDown += TabPage_MouseDown;
            newTabPage.MouseMove += TabPage_MouseMove;
            newTabPage.MouseUp += TabPage_MouseUp;
            newTabPage.Paint += TabPage_Paint;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage; // ���� ������ ������ �̵�

            tabCount++; // ���� �� ��ȣ�� ���� 1 ����
        }

        // �� ���� ��ư Ŭ��
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

                // �����ִ� �ǵ��� ó������ ������� ��ȣ ������
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}"; // ���̴� �ؽ�Ʈ ����
                    tab.Name = $"tp{i + 1}"; // ���� �̸� ����
                }

                // ������ ������ �� ��ȣ�� ���� �� ���� + 1�� ����
                tabCount = tabControl1.TabPages.Count + 1;
            }
        }

        // ��ǰ�� �̹��� ������¡ ���� �޼���
        private Bitmap ResizeImageHighQuality(Image img, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0) return null; // ��ȿ���� ���� ũ�� ����
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.Transparent); // ��� �����ϰ�
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return result;
        }

        // Ȯ�� ��ư Ŭ��
        private void button11_Click(object sender, EventArgs e) // Ȯ��
        {
            // ���õ� ��� �̹����� ���� Ȯ�� ����
            foreach (var pb in selectedImages)
            {
                // imageList���� ���� PictureBox�� �ش��ϴ� ���� �̹����� ã��
                var imageEntry = imageList.FirstOrDefault(entry => entry.pb == pb);
                if (imageEntry.pb != null)
                {
                    Bitmap original = imageEntry.original;
                    // ���� ũ�⸦ �������� 1.2�� ū ���ο� ũ�� ���
                    int newWidth = (int)(pb.Width * 1.2f);
                    int newHeight = (int)(pb.Height * 1.2f);

                    // �ִ� ũ�� ���� (���� �̹����� MAX_SCALE �踦 ���� �ʵ���)
                    if (newWidth > original.Width * MAX_SCALE || newHeight > original.Height * MAX_SCALE)
                    {
                        continue; // �ʹ� Ŀ���� �ǳʶٱ�
                    }

                    // ��ȭ�� ������¡
                    pb.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // ���� ũ��� ����
                }
            }

            // UI �ؽ�Ʈ�ڽ��� ���������� ���õ� �̹����� ũ�⸦ ������Ʈ
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
            }
        }

        // ��� ��ư Ŭ��
        private void button12_Click(object sender, EventArgs e) // ���
        {
            // ���õ� ��� �̹����� ���� ��� ����
            foreach (var pb in selectedImages)
            {
                // imageList���� ���� PictureBox�� �ش��ϴ� ���� �̹����� ã��
                var imageEntry = imageList.FirstOrDefault(entry => entry.pb == pb);
                if (imageEntry.pb != null)
                {
                    Bitmap original = imageEntry.original;
                    // ���� ũ�⸦ �������� 0.8�� ���� ���ο� ũ�� ���
                    int newWidth = (int)(pb.Width * 0.8f);
                    int newHeight = (int)(pb.Height * 0.8f);

                    // �ּ� ũ�� ���� (���� �̹����� MIN_SCALE �躸�� �۾����� �ʵ���)
                    if (newWidth < original.Width * MIN_SCALE || newHeight < original.Height * MIN_SCALE)
                    {
                        continue; // �ʹ� �۾����� �ǳʶٱ�
                    }

                    // ��ȭ�� ������¡
                    pb.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // ���� ũ��� ����
                }
            }

            // UI �ؽ�Ʈ�ڽ��� ���������� ���õ� �̹����� ũ�⸦ ������Ʈ
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
            }
        }

        // ���� ��Ʈ�� �ʱ�ȭ (�г� �� ��ư)
        private void InitializeDynamicControls()
        {
            // 1. �г� ����
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
                    Visible = false, // ó������ ��� ����
                    BorderStyle = BorderStyle.FixedSingle
                };

                if (i == 1) // ���� - �� ��° �гο� �̹��� ���� ���� ��� �߰�
                {
                    AddImageEditControls(panel);
                }
                else if (i == 7) // �̸��� �г� (8�� ��ư�� �ش�)
                {
                    panel.AllowDrop = true;
                    panel.AutoScroll = true;
                    panel.Controls.Add(new Label()
                    {
                        Text = "�̸��� ����",
                        Location = new Point(10, 10),
                        Font = new Font(Font, FontStyle.Bold)
                    });
                    AddEmojiControls(panel); // �̸��� ��Ʈ�� �߰�
                }
                else // �Ϲ����� �г� (�⺻ �ؽ�Ʈ��)
                {
                    panel.Controls.Add(new Label()
                    {
                        Text = $"���� �Ӽ� {i + 1}",
                        Location = new Point(10, 10)
                    });
                }

                panel.Paint += Panel_Paint; // �г� �׵θ� �׸��� �̺�Ʈ
                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            // 2. ��ư ���� (���� ���̵��)
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int buttonStartY = 95;
            int columns = 2;
            int buttonCount = 10; // 10���� ��ư
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
                btn.Tag = i; // ��ư �ε����� Tag�� ����
                btn.Click += Button_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����

                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // 3. �⺻ �г� ���̰� ���� (ù ��° �г�)
            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
        }

        // �̸��� ��Ʈ���� �гο� �߰��ϴ� �޼���
        private void AddEmojiControls(Panel panel8)
        {
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
            int emojiStartY = 50; // �̸�Ƽ�� ����� ���۵� Y ��ġ
            int iconsPerRow = (panel8.Width - emojiPadding * 2) / (iconSize + emojiPadding); // �� �ٿ� ǥ�õ� ������ �� ���

            for (int i = 0; i < emojis.Length; i++)
            {
                var pic = new PictureBox
                {
                    Image = emojis[i],
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(iconSize, iconSize),
                    Cursor = Cursors.Hand,
                    Location = new Point(
                        emojiPadding + (i % iconsPerRow) * (iconSize + emojiPadding),
                        emojiStartY + (i / iconsPerRow) * (iconSize + emojiPadding))
                };
                // �̸�Ƽ�� Ŭ�� �� �巡�� ���� �̺�Ʈ ����
                pic.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        emojiPreviewImage = ((PictureBox)s).Image; // �̸����� �̹��� ����
                        (s as PictureBox).DoDragDrop(((PictureBox)s).Image, DragDropEffects.Copy); // �巡�� �� ��� ����
                    }
                };
                panel8.Controls.Add(pic);
            }

            // '����' ��ư ����
            Button btnApplyEmojis = new Button();
            btnApplyEmojis.Text = "����";
            btnApplyEmojis.Size = new Size(100, 30);
            btnApplyEmojis.Location = new Point((panel8.Width - btnApplyEmojis.Width * 2 - 10) / 2, emojiStartY + (emojis.Length / iconsPerRow + 1) * (iconSize + emojiPadding) + 20); // Y ��ġ�� ������ ����
            btnApplyEmojis.Click += BtnApplyEmojis_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
            panel8.Controls.Add(btnApplyEmojis);

            // '�� ����' ��ư ����
            Button btnRemoveLastEmoji = new Button();
            btnRemoveLastEmoji.Text = "�� ����";
            btnRemoveLastEmoji.Size = new Size(100, 30);
            btnRemoveLastEmoji.Location = new Point(btnApplyEmojis.Right + 10, btnApplyEmojis.Top);
            btnRemoveLastEmoji.Click += BtnRemoveLastEmoji_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
            panel8.Controls.Add(btnRemoveLastEmoji);
        }

        /// <summary>
        /// '����' ��ư Ŭ�� ��, ���� ��� �̹��� ���� ��� �̸�Ƽ���� �ռ��մϴ�.
        /// </summary>
        private void BtnApplyEmojis_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null)
            {
                MessageBox.Show("���� ��� �̹����� �������ּ���.");
                return;
            }

            var emojiControls = selectedImage.Controls.OfType<PictureBox>().ToList();
            if (emojiControls.Count == 0)
            {
                MessageBox.Show("������ �̸�Ƽ���� �����ϴ�.");
                return;
            }

            var result = MessageBox.Show("�̸�Ƽ���� �̹����� ���������� �ռ��մϴ�.\n���� �Ŀ��� �̵��ϰų� ������ �� �����ϴ�.\n����Ͻðڽ��ϱ�?", "Ȯ��", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return;
            }

            Bitmap newBitmap = new Bitmap(selectedImage.Image);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                foreach (PictureBox emoji in emojiControls)
                {
                    g.DrawImage(emoji.Image, emoji.Bounds);
                }
            }

            selectedImage.Image = newBitmap;

            if (selectedImage.Tag is Bitmap oldBitmap)
            {
                oldBitmap.Dispose();
            }
            selectedImage.Tag = new Bitmap(newBitmap); // Tag�� �� ��Ʈ���� ���纻 ����

            foreach (var control in emojiControls)
            {
                selectedImage.Controls.Remove(control);
                control.Dispose();
            }
            selectedEmoji = null;

            // ---  �� �κ��� �߰��ؾ� �մϴ�!  ---
            // imageList�� ����� ���� ������ ���� �ռ��� �̹����� ��ü�մϴ�.
            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    // Tag�� ����� �� ��Ʈ�� ���纻�� imageList�� �������� ����
                    imageList[i] = (selectedImage, (Bitmap)selectedImage.Tag);
                    break;
                }
            }
            // ---  ������� �߰� ---

            MessageBox.Show("������ �Ϸ�Ǿ����ϴ�.");
        }

        /// <summary>
        /// '������ �׸� ����' ��ư Ŭ�� ��, ���� �������� �߰��� �̸�Ƽ���� �����մϴ�.
        /// </summary>
        private void BtnRemoveLastEmoji_Click(object sender, EventArgs e)
        {
            if (selectedImage == null)
            {
                MessageBox.Show("���� �۾��� �̹����� �������ּ���.");
                return;
            }

            // --- Undo ���ÿ� ���� ���� ��� (�߰�) ---
            emojiUndoStack.Push(CaptureCurrentEmojis(selectedImage));
            emojiRedoStack.Clear();
            // ------------------------------------

            var lastEmoji = selectedImage.Controls.OfType<PictureBox>().LastOrDefault();
            if (lastEmoji != null)
            {
                selectedImage.Controls.Remove(lastEmoji);
                lastEmoji.Dispose();
            }
            else
            {
                MessageBox.Show("������ �̸�Ƽ���� �����ϴ�.");
            }
        }

        // ��� ���� ��ư�� Ŭ�� �̺�Ʈ�� ó���ϴ� ���� �ڵ鷯 (�г� ���ü� ���)
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag;
                if (index >= dynamicPanels.Length) return; // ��ȿ���� ���� �ε��� ����

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel) // ���� ���̴� �г��� �ٽ� Ŭ���ϸ� ����
                {
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else // �ٸ� �г��� Ŭ���ϸ� ���� �г� ����� �� �г� ����
                {
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }
                if (previousVisiblePanel != null) previousVisiblePanel.Invalidate(); // ���� �г� �׵θ� ������Ʈ
                if (currentVisiblePanel != null) currentVisiblePanel.Invalidate(); // �� �г� �׵θ� ������Ʈ
            }
        }

        // �г� �׸��� �̺�Ʈ (�׵θ�)
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

        // �� ��ü ���콺 �ٿ� �̺�Ʈ (���� ����)
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // Ŭ���� ��Ʈ���� TabControl�̳� TabPage�� ���� ���� ���� ���� ����
            var clickedControl = this.GetChildAtPoint(e.Location);
            if (clickedControl == null || clickedControl is TabControl || clickedControl is TabPage)
            {
                // ���� ���� �׸���� �׵θ��� ����� ���� Invalidate ȣ��
                foreach (var item in selectedImages)
                {
                    item.Invalidate();
                }
                selectedImages.Clear();
                selectedImage = null;
                // ���� - ���� ��Ʈ�� �ʱ�ȭ
                btnResetAll_Click(null, null);
            }
        }

        // TabPage ���콺 �ٿ� �̺�Ʈ (����Ű ���� ����)
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            var tab = sender as TabPage;
            if (tab == null) return;

            // --- �̸�Ƽ�� ���� ���� ���� (�߰�) ---
            bool onAnyImage = false;
            foreach (Control c in tab.Controls)
            {
                if (c is PictureBox pb && pb.Bounds.Contains(e.Location))
                {
                    onAnyImage = true;
                    break;
                }
            }

            // � �̹��� ���� Ŭ������ �ʾҴٸ�(�� ���� Ŭ��)
            if (!onAnyImage)
            {
                // ��� �̹����� ��� �ڽ� �̸�Ƽ�� ���� ����
                foreach (PictureBox mainPb in tab.Controls.OfType<PictureBox>())
                {
                    foreach (PictureBox emoji in mainPb.Controls.OfType<PictureBox>())
                    {
                        emoji.Tag = null;
                        emoji.Invalidate();
                    }
                }
                selectedEmoji = null;
            }
            // --- ������� �߰� ---

            // ���� ��ư Ŭ�� �ÿ��� �巡�� ���� ����
            if (e.Button == MouseButtons.Left)
            {
                isMarqueeSelecting = true;
                marqueeStartPoint = e.Location; // �巡�� ���� ���� ����
            }
        }

        // TabPage ���콺 �̵� �̺�Ʈ (����Ű �簢�� �׸���)
        private void TabPage_MouseMove(object sender, MouseEventArgs e)
        {
            // �巡�� ���� ���� ���� ����
            if (isMarqueeSelecting)
            {
                // �巡�� �������� ���� ��ġ�� ������� �簢���� ������ ���
                // (��� �������� �巡���ϵ� ���������� �簢���� �׷������� Math.Min/Abs ���)
                int x = Math.Min(marqueeStartPoint.X, e.X);
                int y = Math.Min(marqueeStartPoint.Y, e.Y);
                int width = Math.Abs(marqueeStartPoint.X - e.X);
                int height = Math.Abs(marqueeStartPoint.Y - e.Y);
                marqueeRect = new Rectangle(x, y, width, height);

                // TabPage�� �ٽ� �׸����� ��û (Paint �̺�Ʈ �߻�)
                (sender as TabPage).Invalidate();
            }
        }

        // TabPage ���콺 �� �̺�Ʈ (����Ű ���� �Ϸ� �� ��Ŭ�� �޴�)
        private void TabPage_MouseUp(object sender, MouseEventArgs e)
        {
            TabPage currentTab = sender as TabPage;
            if (currentTab == null) return;

            // --- 1. ��Ŭ�� �巡��(�簢��) ���� ���� ó�� ---
            if (isMarqueeSelecting)
            {
                isMarqueeSelecting = false;

                if (marqueeRect.Width < 5 && marqueeRect.Height < 5)
                {
                    bool clickedOnImage = currentTab.Controls.OfType<PictureBox>().Any(pb => pb.Bounds.Contains(e.Location));
                    if (!clickedOnImage) // �̹��� ���� Ŭ���� �� �ƴ� ���� ���� ����
                    {
                        foreach (var item in selectedImages) { item.Invalidate(); }
                        selectedImages.Clear();
                        selectedImage = null;
                        btnResetAll_Click(null, null);
                    }
                }
                else
                {
                    foreach (PictureBox pb in currentTab.Controls.OfType<PictureBox>())
                    {
                        if (marqueeRect.IntersectsWith(pb.Bounds))
                        {
                            if (!selectedImages.Contains(pb))
                            {
                                selectedImages.Add(pb);
                            }
                        }
                    }

                    selectedImage = selectedImages.LastOrDefault();
                    if (selectedImage != null)
                    {
                        textBox1.Text = selectedImage.Width.ToString();
                        textBox2.Text = selectedImage.Height.ToString();
                        textBox3.Text = selectedImage.Left.ToString();   // <-- �� �� �߰�
                        textBox4.Text = selectedImage.Top.ToString();    // <-- �� �� �߰�
                        UpdateEditControlsFromSelectedImage();
                    }

                    foreach (var pb in currentTab.Controls.OfType<PictureBox>())
                    {
                        pb.Invalidate();
                    }
                }

                marqueeRect = Rectangle.Empty;
                currentTab.Invalidate();
            }

            // --- 2. ��Ŭ�� �� ���� �޴� ó�� (�߰�) ---
            if (e.Button == MouseButtons.Right)
            {
                bool clickedOnImage = currentTab.Controls.OfType<PictureBox>().Any(pb => pb.Bounds.Contains(e.Location));
                if (!clickedOnImage)
                {
                    menuCopy.Enabled = false; // �� ���������� ����/���� ��Ȱ��ȭ
                    menuDelete.Enabled = false;
                    menuPaste.Enabled = clipboardContent.Count > 0; // �ٿ��ֱ�� ����

                    // �޴��� � �ǰ� ��ġ���� ������ ����
                    imageContextMenu.Tag = new Tuple<TabPage, Point>(currentTab, e.Location);
                    imageContextMenu.Show(Cursor.Position);
                }
            }
        }

        // TabPage�� �ٽ� �׷��� �� �� (����Ű �簢�� �׸���)
        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            // �巡�� ���� ���� ���� �簢���� �׸�
            if (isMarqueeSelecting)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 1))
                {
                    pen.DashStyle = DashStyle.Dash; // ���� ��Ÿ��
                    e.Graphics.DrawRectangle(pen, marqueeRect);
                }
            }
        }

        // ���� 90�� ȸ�� ��ư Ŭ��
        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            // ���õ� ��� �̹����� ����
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate270FlipNone); // �ð� �ݴ� ���� 90��
                    pb.Size = pb.Image.Size; // �̹��� ũ�⿡ ���� PictureBox ũ�� ����
                    pb.Invalidate(); // �̹��� �ٽ� �׸���
                }
            }
        }

        // ������ 90�� ȸ�� ��ư Ŭ��
        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            // ���õ� ��� �̹����� ����
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate90FlipNone); // �ð� ���� 90��
                    pb.Size = pb.Image.Size; // �̹��� ũ�⿡ ���� PictureBox ũ�� ����
                    pb.Invalidate(); // �̹��� �ٽ� �׸���
                }
            }
        }

        // �ؽ�Ʈ�ڽ� ������ ���õ� �̹��� ũ�� ������Ʈ
        private void UpdateSelectedImageSize()
        {
            if (selectedImages.Count == 0) return; // ���õ� �̹����� ������ ��ȯ

            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                if (width <= 0 || height <= 0) return; // ��ȿ���� ���� ũ�� ����

                // ũ�� ����
                width = Math.Max(16, Math.Min(4000, width));
                height = Math.Max(16, Math.Min(4000, height));

                // ���õ� ��� �̹����� ũ�� ����
                foreach (var pb in selectedImages)
                {
                    if (pb.Tag is Bitmap originalBitmap) // Tag�� ����� ���� Bitmap ���
                    {
                        Bitmap resized = ResizeImageHighQuality(originalBitmap, new Size(width, height));
                        if (resized == null) continue; // �������� ���� �� �ǳʶٱ�

                        pb.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
                        pb.Image = resized; // �� �̹��� �Ҵ�
                        pb.Size = new Size(width, height); // PictureBox ũ�� ����
                        pb.Invalidate(); // �̹��� �ٽ� �׸���
                    }
                }

                // �ؽ�Ʈ�ڽ� ���� ������ ������ ������Ʈ
                if (textBox1.Text != width.ToString()) textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString()) textBox2.Text = height.ToString();
            }
        }

        // textBox1 (�ʺ�) ��ȿ�� �˻� �� ������Ʈ
        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            // ���� ��������� ���� ���õ� �̹����� �ʺ� �Ǵ� �⺻��(100)���� ����
            if (string.IsNullOrWhiteSpace(textBox1.Text)) textBox1.Text = selectedImage?.Width.ToString() ?? "100";

            // ���ڷ� �Ľ� �����ϸ� 100���� ����
            if (!int.TryParse(textBox1.Text, out int val)) val = 100;

            // �� ���� ���� (16 ~ 4000)
            int corrected = Math.Max(16, Math.Min(4000, val));

            // ������ ������ �ؽ�Ʈ�ڽ� ������Ʈ (�ʿ��� ���)
            if (textBox1.Text != corrected.ToString()) textBox1.Text = corrected.ToString();

            UpdateSelectedImageSize(); // ũ�� ������Ʈ ����
        }

        // textBox2 (����) ��ȿ�� �˻� �� ������Ʈ
        private void textBox2_Validating(object sender, CancelEventArgs e)
        {
            // ���� ��������� ���� ���õ� �̹����� ���� �Ǵ� �⺻��(100)���� ����
            if (string.IsNullOrWhiteSpace(textBox2.Text)) textBox2.Text = selectedImage?.Height.ToString() ?? "100";

            // ���ڷ� �Ľ� �����ϸ� 100���� ����
            if (!int.TryParse(textBox2.Text, out int val)) val = 100;

            // �� ���� ���� (16 ~ 4000)
            int corrected = Math.Max(16, Math.Min(4000, val));

            // ������ ������ �ؽ�Ʈ�ڽ� ������Ʈ (�ʿ��� ���)
            if (textBox2.Text != corrected.ToString()) textBox2.Text = corrected.ToString();

            UpdateSelectedImageSize(); // ũ�� ������Ʈ ����
        }

        // =======================================================
        // ���� - �̹��� ���� �� ���� ��� ���� �޼���
        // =======================================================

        // ���� - Ư�� �гο� �̹��� ���� ����� ���� ��Ʈ�ѵ��� �߰��մϴ�.
        private void AddImageEditControls(Panel targetPanel)
        {
            int currentY = 20;
            int verticalSpacing = 40;
            int sectionSpacing = 30;

            targetPanel.Controls.Add(new Label { Text = "RGB ����", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddColorControl("Red", ref trackRed, ref txtRed, targetPanel, ref currentY);
            AddColorControl("Green", ref trackGreen, ref txtGreen, targetPanel, ref currentY);
            AddColorControl("Blue", ref trackBlue, ref txtBlue, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "������ ����", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
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

            targetPanel.Controls.Add(new Label { Text = "���", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("���", ref trackBrightness, ref txtBrightness, targetPanel, ref currentY);

            currentY += sectionSpacing;
            targetPanel.Controls.Add(new Label { Text = "ä��", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("ä��", ref trackSaturation, ref txtSaturation, targetPanel, ref currentY);

            currentY += sectionSpacing;
            targetPanel.Controls.Add(new Label { Text = "�ܻ� ����", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
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

        // ���� - ���̺�, Ʈ����, �ؽ�Ʈ �ڽ� ��Ʈ���� �гο� �߰��մϴ�.
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
                Value = 128, // �⺻��
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

        // ���� - ���/ä�� ���� ��Ʈ���� �гο� �߰��մϴ�.
        private void AddBrightnessSaturationControl(string label, ref TrackBar trackBar, ref TextBox txtBox, Panel panel, ref int y)
        {
            trackBar = new TrackBar
            {
                Location = new Point(10, y),
                Size = new Size(180, 45),
                Minimum = -100,
                Maximum = 100,
                Value = 0, // �⺻��
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

        // ���õ� �̹����� ���� ���� ��Ʈ�� ���¸� ������Ʈ (����)
        private void UpdateEditControlsFromSelectedImage()
        {
            if (selectedImage != null)
            {
                // imageList���� ���� PictureBox�� �ش��ϴ� ���� �̹����� ã��
                var imageInfo = imageList.FirstOrDefault(item => item.pb == selectedImage);
                if (imageInfo.pb != null)
                {
                    _initialImage = (Bitmap)imageInfo.original.Clone(); // ���� ���� �̹��� ����
                    originalImage = (Bitmap)imageInfo.original.Clone(); // ���� ���� ���� �̹��� ����
                    btnResetAll_Click(null, null); // ���� ��Ʈ�� �ʱ�ȭ
                }
            }
            else
            {
                // ���õ� �̹����� ������ ��� ��Ʈ�� ��Ȱ��ȭ �Ǵ� �⺻������ ����
                btnResetAll_Click(null, null); // ��� ��Ʈ�� �ʱ�ȭ �� ��Ȱ��ȭ ȿ��
                // �߰������� TrackBar�� TextBox�� ��Ȱ��ȭ�� ���� �ֽ��ϴ�.
                // ��: if (trackRed != null) trackRed.Enabled = false;
            }
        }

        // RGB �� ���/ä�� TrackBar ��ũ�� �̺�Ʈ
        private void trackRed_Scroll(object sender, EventArgs e) { txtRed.Text = trackRed.Value.ToString(); ApplyAllLivePreview(); }
        private void trackGreen_Scroll(object sender, EventArgs e) { txtGreen.Text = trackGreen.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBlue_Scroll(object sender, EventArgs e) { txtBlue.Text = trackBlue.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBrightness_Scroll(object sender, EventArgs e) { txtBrightness.Text = trackBrightness.Value.ToString(); ApplyAllLivePreview(); }
        private void trackSaturation_Scroll(object sender, EventArgs e) { txtSaturation.Text = trackSaturation.Value.ToString(); ApplyAllLivePreview(); }

        // ���� - ���� �̹��� �������� ���� �̹����� �ݿ��մϴ�.
        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            // ���� PictureBox�� ǥ�õ� �̹���(���� �� ���� ����� ����)�� originalImage�� ����
            originalImage = (Bitmap)selectedImage.Image.Clone();

            // imageList������ �ش� PictureBox�� ���� �̹����� ������Ʈ
            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    imageList[i] = (selectedImage, (Bitmap)originalImage.Clone());
                    break;
                }
            }
        }

        // ���� - ��� �������� �ʱ� ���·� �ǵ����ϴ�.
        private void btnResetAll_Click(object sender, EventArgs e)
        {
            if (originalImage == null || _initialImage == null) // ���� �̹����� ������ �ʱ�ȭ�� �͵� ����
            {
                // ��Ʈ�ѵ��� �⺻������ �����ϰų� ��Ȱ��ȭ
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

                if (selectedImage != null && imageList.Any(item => item.pb == selectedImage))
                {
                    // selectedImage�� imageList�� �ִ� ��쿡�� �̹����� �ʱ�ȭ
                    // (selectedImage�� null�̰ų� imageList�� ������ ���� �̹����� ����)
                    var imageEntry = imageList.FirstOrDefault(item => item.pb == selectedImage);
                    if (imageEntry.pb != null)
                    {
                        selectedImage.Image?.Dispose();
                        selectedImage.Image = (Bitmap)imageEntry.original.Clone();
                        originalImage = (Bitmap)imageEntry.original.Clone(); // originalImage�� �ٽ� ��������
                        _currentFilter = FilterState.None; // ���� ���µ� �ʱ�ȭ
                    }
                }
                return;
            }

            // ��Ʈ�� ���� �⺻���� �ǵ���
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

            _currentFilter = FilterState.None; // ���� ���� �ʱ�ȭ
            if (selectedImage != null)
            {
                selectedImage.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
                selectedImage.Image = (Bitmap)_initialImage.Clone(); // ���� �ε�� �̹����� ����
                originalImage = (Bitmap)_initialImage.Clone(); // originalImage�� ���� �̹�����
            }
        }

        // ���� - ��� �Ǵ� ���Ǿ� ���͸� �����մϴ�.
        private void ApplyMonochromeFilter(FilterState filter)
        {
            if (originalImage == null) return;
            _currentFilter = filter; // ���� ���� ���� ����
            ApplyAllLivePreview(); // �̸����� ������Ʈ
        }

        // ���� - RGB, ���, ä��, �ܻ� ���͸� ��� ������ �̸����� �̹����� �����մϴ�.
        private void ApplyAllLivePreview()
        {
            if (selectedImage == null || originalImage == null) return;

            // originalImage�� ���纻���� �����Ͽ� �� ������ ���������� ����
            Bitmap tempImage = (Bitmap)originalImage.Clone();

            // 1. RGB ����
            int rAdj = trackRed.Value - 128; // 128�� 0 ������ �ش�
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;
            tempImage = AdjustRGB(tempImage, rAdj, gAdj, bAdj);

            // 2. ��� ����
            tempImage = AdjustBrightness(tempImage, trackBrightness.Value);

            // 3. ä�� ����
            tempImage = AdjustSaturation(tempImage, trackSaturation.Value);

            // 4. �ܻ� ���� ����
            if (_currentFilter == FilterState.Grayscale)
            {
                tempImage = ConvertToGrayscale(tempImage);
            }
            else if (_currentFilter == FilterState.Sepia)
            {
                tempImage = ApplySepia(tempImage);
            }

            // ���� ��� �̹����� PictureBox�� �Ҵ�
            selectedImage.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
            selectedImage.Image = tempImage;
        }

        // ���� - RGB ���� �����մϴ�.
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

        // ���� - ��� ����
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

        // ���� - ä�� ����
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

        // ���� - ���� 0-255 ������ ����
        private int Clamp(int val) => Math.Min(Math.Max(val, 0), 255);

        // ���� - ��� ���� ����
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

        // ���� - ���Ǿ� ���� ����
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

        // ���� - ������ ���͸� �����մϴ�.
        private void ApplyPresetFilter(FilterState filter, string presetType)
        {
            if (selectedImage == null || originalImage == null) return;
            _currentFilter = filter; // ���� ���� ���� (���⼭�� None���� ����)

            Bitmap result = (Bitmap)originalImage.Clone(); // ���� �̹������� ����

            switch (presetType)
            {
                case "Warm":
                    // RGB Ʈ���� �� ���� (����)
                    trackRed.Value = Math.Min(128 + 30, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 30, 0);
                    // ���� ���� ������ ApplyAllLivePreview���� ����� ���̹Ƿ� ���⼭ ���� �̹��� ������ ���� ����
                    break;
                case "Cool":
                    trackRed.Value = Math.Max(128 - 30, 0);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Min(128 + 30, 255);
                    break;
                case "Vintage":
                    trackRed.Value = Math.Min(128 + 20, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 20, 0);
                    break;
            }

            // Ʈ���� �� ���濡 ���� �ؽ�Ʈ �ڽ� �ڵ� ������Ʈ
            txtRed.Text = trackRed.Value.ToString();
            txtGreen.Text = trackGreen.Value.ToString();
            txtBlue.Text = trackBlue.Value.ToString();

            // ���/ä�� Ʈ���� �ʱ�ȭ
            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";

            ApplyAllLivePreview(); // ��� ���� ���� �� �̸����� ������Ʈ
        }

        // ���� - ���� �̹����� �ǵ����ϴ�.
        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || _initialImage == null) return;
            _currentFilter = FilterState.None; // ���� ���� �ʱ�ȭ

            selectedImage.Image?.Dispose(); // ���� �̹��� ���ҽ� ����
            selectedImage.Image = (Bitmap)_initialImage.Clone(); // ���� �ε�� �̹����� ����
            originalImage = (Bitmap)_initialImage.Clone(); // originalImage�� ���� �̹�����

            // ��� ��Ʈ�� ���� �⺻���� �ǵ���
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
        }

        // RGB �ؽ�Ʈ�ڽ� ���� �̺�Ʈ (���� �Է�)
        private void txtRed_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return; // ���ѷ��� ����
            isTextChanging = true;
            if (int.TryParse(txtRed.Text, out int val))
            {
                val = Clamp(val); // 0-255 ���� ����
                txtRed.Text = val.ToString(); // ������ ������ ������Ʈ
                trackRed.Value = val; // Ʈ���� �� ������Ʈ
                ApplyAllLivePreview(); // �̸����� ������Ʈ
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                // ���ڰ� �ƴϸ� ���� Ʈ���� ������ �ǵ���
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
                val = Clamp(val);
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
                val = Clamp(val);
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

        // ���/ä�� �ؽ�Ʈ�ڽ� ���� �̺�Ʈ (���� �Է�)
        private void txtBrightness_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtBrightness.Text, out int val))
            {
                val = Math.Min(Math.Max(val, -100), 100); // -100 ~ 100 ���� ����
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
        // =================================================================
        // ���ؽ�Ʈ �޴� (��Ŭ��) ���� ����
        // =================================================================

        private void InitializeContextMenu()
        {
            imageContextMenu = new ContextMenuStrip();
            menuCopy = new ToolStripMenuItem("����");
            menuPaste = new ToolStripMenuItem("�ٿ��ֱ�");
            menuDelete = new ToolStripMenuItem("����");

            imageContextMenu.Items.AddRange(new[] { menuCopy, menuPaste, menuDelete });

            menuCopy.Click += MenuCopy_Click;
            menuPaste.Click += MenuPaste_Click;
            menuDelete.Click += MenuDelete_Click;
        }

        private void MenuCopy_Click(object sender, EventArgs e)
        {
            if (selectedImages.Count == 0) return;

            clipboardContent.Clear();

            // ���õ� �̹��� �׷��� �»�� �������� ã���ϴ�.
            int minX = selectedImages.Min(pb => pb.Left);
            int minY = selectedImages.Min(pb => pb.Top);
            Point originPoint = new Point(minX, minY);

            foreach (PictureBox pb in selectedImages)
            {
                clipboardContent.Add(new ClipboardItem
                {
                    Image = new Bitmap(pb.Image),
                    // �׷��� ���������κ����� ��� ��ġ�� �����մϴ�.
                    RelativeLocation = new Point(pb.Left - originPoint.X, pb.Top - originPoint.Y)
                });
            }
        }

        private void MenuPaste_Click(object sender, EventArgs e)
        {
            if (clipboardContent.Count == 0) return;

            // �޴��� ��� ��ġ ������ �����ɴϴ�.
            (TabPage targetTab, Point pasteLocation) = GetPasteTarget();
            if (targetTab == null) return;

            List<PictureBox> newPbs = new List<PictureBox>();
            foreach (var item in clipboardContent)
            {
                PictureBox pb = new PictureBox
                {
                    Image = new Bitmap(item.Image),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = item.Image.Size,
                    // �ٿ����� ��ġ = ������ + ��� ��ġ
                    Location = new Point(pasteLocation.X + item.RelativeLocation.X, pasteLocation.Y + item.RelativeLocation.Y),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    BackColor = Color.Transparent,
                    Tag = new Bitmap(item.Image)
                };

                pb.AllowDrop = true; // �巡�� �� ��� ���
                pb.DragEnter += PictureBox_DragEnter;
                pb.DragOver += PictureBox_DragOver;
                pb.DragLeave += PictureBox_DragLeave;
                pb.DragDrop += PictureBox_DragDrop;

                pb.MouseDown += pictureBox_MouseDown;
                pb.MouseMove += pictureBox_MouseMove;
                pb.MouseUp += pictureBox_MouseUp;
                pb.Paint += pictureBox_Paint;

                targetTab.Controls.Add(pb);
                imageList.Add((pb, (Bitmap)pb.Tag));
                newPbs.Add(pb);
            }

            // ���� �ٿ����� �̹������� ���� ���·� ����ϴ�.
            foreach (var item in selectedImages) item.Invalidate();
            selectedImages.Clear();
            selectedImages.AddRange(newPbs);
            selectedImage = newPbs.LastOrDefault();
            if (selectedImage != null)
            {
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
            }
            foreach (var item in selectedImages) item.Invalidate();
        }

        private void MenuDelete_Click(object sender, EventArgs e)
        {
            if (selectedImages.Count == 0) return;

            // ����Ʈ�� �����ؼ� ��� (������ ��ȸ�ϸ� �����ϸ� ���� �߻�)
            var imagesToDelete = selectedImages.ToList();
            foreach (PictureBox pb in imagesToDelete)
            {
                pb.Parent.Controls.Remove(pb);
                imageList.RemoveAll(item => item.pb == pb);
                pb.Dispose();
            }
            selectedImages.Clear();
            selectedImage = null;
        }

        private (TabPage, Point) GetPasteTarget()
        {
            if (imageContextMenu.Tag is Tuple<TabPage, Point> tabInfo)
            {
                // �� ������ �ٿ��ֱ�
                return (tabInfo.Item1, tabInfo.Item2);
            }
            if (imageContextMenu.Tag is PictureBox pb)
            {
                // �̹��� ���� �ٿ��ֱ� (�̹��� �»�� ���� �ణ ��)
                return (pb.Parent as TabPage, new Point(pb.Left + 10, pb.Top + 10));
            }
            // �⺻�� (���� ���� (10,10))
            return (tabControl1.SelectedTab, new Point(10, 10));
        }
        private List<EmojiState> CaptureCurrentEmojis(PictureBox parent)
        {
            return parent.Controls.OfType<PictureBox>()
                .Select(emoji => new EmojiState
                {
                    Image = (Image)emoji.Image.Clone(),
                    Location = emoji.Location,
                    Size = emoji.Size
                }).ToList();
        }

        private void RestoreEmojis(PictureBox parent, List<EmojiState> states)
        {
            // ���� �̸�Ƽ�� ����
            foreach (Control c in parent.Controls.OfType<PictureBox>().ToList())
                parent.Controls.Remove(c);

            // ����� ���´�� ����
            foreach (var emojiState in states)
            {
                var emojiPb = new PictureBox
                {
                    Image = (Image)emojiState.Image.Clone(),
                    Location = emojiState.Location,
                    Size = emojiState.Size,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.SizeAll
                };
                // ���� �̸�Ƽ�� ��Ʈ�ѿ� ������ �̺�Ʈ �ٽ� ����!
                emojiPb.MouseDown += Emoji_MouseDown;
                emojiPb.MouseMove += Emoji_MouseMove;
                emojiPb.MouseUp += Emoji_MouseUp;
                emojiPb.Paint += Emoji_Paint;
                parent.Controls.Add(emojiPb);
            }
            parent.Invalidate();
        }

        private void EmojiUndo()
        {
            if (selectedImage == null) return;
            if (emojiUndoStack.Count > 0)
            {
                var prevState = emojiUndoStack.Pop();
                emojiRedoStack.Push(CaptureCurrentEmojis(selectedImage));
                RestoreEmojis(selectedImage, prevState);
            }
        }

        private void EmojiRedo()
        {
            if (selectedImage == null) return;
            if (emojiRedoStack.Count > 0)
            {
                var nextState = emojiRedoStack.Pop();
                emojiUndoStack.Push(CaptureCurrentEmojis(selectedImage));
                RestoreEmojis(selectedImage, nextState);
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl Ű�� �̿��� ����Ű ó��
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    // --- ����(Ctrl+C) ����Ű �߰� ---
                    case Keys.C:
                        MenuCopy_Click(null, null); // ���� ���� �޼��� ȣ��
                        e.Handled = true;
                        break;

                    // --- �ٿ��ֱ�(Ctrl+V) ����Ű �߰� ---
                    case Keys.V:
                        MenuPaste_Click(null, null); // ���� �ٿ��ֱ� �޼��� ȣ��
                        e.Handled = true;
                        break;

                    case Keys.Z:
                        EmojiUndo();
                        e.Handled = true;
                        break;
                    case Keys.Y:
                        EmojiRedo();
                        e.Handled = true;
                        break;
                    case Keys.Delete:
                        MenuDelete_Click(null, null);
                        e.Handled = true;
                        break;
                }
            }
            // Ctrl Ű ���� ����Ű ���� ������ ���� ó��
            else if (selectedImages.Count > 0)
            {
                bool moved = false;
                switch (e.KeyCode)
                {
                    case Keys.Up:
                        foreach (var pb in selectedImages) pb.Top -= 1;
                        moved = true;
                        break;
                    case Keys.Down:
                        foreach (var pb in selectedImages) pb.Top += 1;
                        moved = true;
                        break;
                    case Keys.Left:
                        foreach (var pb in selectedImages) pb.Left -= 1;
                        moved = true;
                        break;
                    case Keys.Right:
                        foreach (var pb in selectedImages) pb.Left += 1;
                        moved = true;
                        break;
                }

                if (moved)
                {
                    if (selectedImage != null)
                    {
                        textBox3.Text = selectedImage.Left.ToString();
                        textBox4.Text = selectedImage.Top.ToString();
                    }
                    e.Handled = true;
                }
            }
        }
        // �ؽ�Ʈ�ڽ� ������ ���õ� �̹��� ��ġ ������Ʈ (�ٽ� ����)
        private void UpdateSelectedImageLocation()
        {
            if (selectedImages.Count == 0 || selectedImage == null) return;

            if (int.TryParse(textBox3.Text, out int newX) && int.TryParse(textBox4.Text, out int newY))
            {
                // ������ �Ǵ� ������ ���� �̹���(selectedImage)�� ��ġ ��ȭ�� ���
                int deltaX = newX - selectedImage.Left;
                int deltaY = newY - selectedImage.Top;

                // ��� ���õ� �̹����� ���� ������ ��ȭ����ŭ �̵�
                // �̷��� �ϸ� ���� �̹����� �����ص� ������� ��ġ�� �����˴ϴ�.
                foreach (var pb in selectedImages)
                {
                    pb.Location = new Point(pb.Left + deltaX, pb.Top + deltaY);
                }
            }
        }

        // textBox3 (X ��ǥ) ��ȿ�� �˻� �� ������Ʈ
        private void textBox3_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox3.Text)) textBox3.Text = selectedImage?.Left.ToString() ?? "0";
            UpdateSelectedImageLocation();
        }

        // textBox4 (Y ��ǥ) ��ȿ�� �˻� �� ������Ʈ
        private void textBox4_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox4.Text)) textBox4.Text = selectedImage?.Top.ToString() ?? "0";
            UpdateSelectedImageLocation();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // ���õ� ��� �̹����� ���� �¿� ������ �����մϴ�.
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    // �̹����� RotateFlip �޼��带 ����Ͽ� �¿� ����(Horizontal Flip)�� �����մϴ�.
                    pb.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    // ����� �̹����� ȭ�鿡 �ٽ� �׸����� ��û�մϴ�.
                    pb.Invalidate();
                }
            }
        }
    }
    // Form1 Ŭ���� �ٱ��� �߰�
    public class EmojiState
    {
        public Image Image { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
    }
}