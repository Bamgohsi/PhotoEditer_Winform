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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace photo
{

    public partial class Form1 : Form
    {
        private ToolTip toolTip1; // �̸��� �̸����� ����
        private PictureBox cropPreviewBox; // <<< �̸����� PictureBox ���� ����
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

        // Win32 API ���� (���ȭ�� ������ ����)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        // ���� showSelectionBorder (������ �ڵ�)�� ����
        // private bool showSelectionBorder = false;

        // UI ��� �迭

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
        private bool filterApplied = false; // ���� ���� ���� ����

        private string currentWorkMode = "�̵�"; // �⺻ ��带 '�̵�'���� ����

        // �ڸ��� ����
        private bool isCropping = false;
        private Point cropStartPoint;
        private Rectangle cropRect;

        // ������ũ/�� �� ���� ���� �����
        private Dictionary<PictureBox, Bitmap> originalImages = new();

        // UI ��Ʈ��
        private TrackBar tbMosaicSize;
        private Panel panelColorSelected;   // "���� ����" �̸�����
        private Panel panelColorPicked;     // �����̵�� ���õ� ���� �̸�����
        private Label lblRGB;               // RGB + Hex ǥ��
        private Dictionary<PictureBox, int> imageTransparencyMap = new Dictionary<PictureBox, int>();
        private TrackBar tbTransparencyGlobal;
        private TrackBar tbPenSize;
        private TrackBar tbEraserSize; // ���찳�� ���� Ʈ���ٰ� �ʿ��� �� ������, �켱 tbPenSize ����

        // ���� ���/�ٽ� ���� (Undo/Redo) ���� (������ũ, �� � ���)
        private Stack<EditAction> undoStack = new Stack<EditAction>();
        private Stack<EditAction> redoStack = new Stack<EditAction>();

        // �׸���/������ũ ���� ����
        private bool isDrawing = false;           // ��/���찳 �巡�� ������ ����
        private Point lastDrawPoint = Point.Empty; // ������ �׷ȴ� �� (���� �ڵ忡���� ������ ����)
        private bool isMosaicing = false;          // ������ũ �巡�� ������ ����
        private Point mosaicStartPoint;            // ������ũ �巡�� ���� ��ġ
        private Rectangle mosaicRect;              // ������ũ �巡�׵� �簢��

        // �� �׸��� ������ �����
        private Dictionary<PictureBox, List<PenStroke>> penStrokesMap = new Dictionary<PictureBox, List<PenStroke>>();
        private PenStroke currentStroke = null; // ���� �׸��� �ִ� ��

        private int EraserRadius => tbPenSize?.Value ?? 10; // ���찳 ũ��� �� ũ��� ����
        private void TogglePanelVisibility(int index)
        {
            if (index >= dynamicPanels.Length) return;

            Panel targetPanel = dynamicPanels[index];

            // �ڡڡڡڡ� ����� �κ� �ڡڡڡڡ�
            // �����ַ��� �г��� �̹� ȭ�鿡 �ִٸ�,
            // �ƹ��͵� ���� �ʰ� �׳� �Լ��� �����մϴ�.
            if (currentVisiblePanel == targetPanel)
            {
                return;
            }
            // �ڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡ�

            // �ٸ� �гη� ��ȯ�� ��, ������ ���� �г��� �����־��ٸ� ������� ����
            if (currentVisiblePanel == dynamicPanels[1] && selectedImage != null && _initialImage != null && !filterApplied)
            {
                selectedImage.Image = new Bitmap(_initialImage);
                selectedImage.Invalidate();
            }

            // ���� �����ִ� �г��� �ִٸ� �ݱ�
            if (currentVisiblePanel != null)
                currentVisiblePanel.Visible = false;

            // ���� ������ �г� ����
            currentVisiblePanel = targetPanel;
            currentVisiblePanel.Visible = true;

            // ���� �г�(�ε��� 1)�� �� ��, ���� �̹��� ���¸� ���
            if (index == 1 && selectedImage != null)
            {
                filterApplied = false;
                UpdateEditControlsFromSelectedImage();
            }
        }
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
            toolTip1 = new ToolTip(); // ���� ToolTip �ν��Ͻ� ����

            // (����) ǥ�� ����
            toolTip1.InitialDelay = 300;
            toolTip1.ReshowDelay = 100;
            toolTip1.AutoPopDelay = 5000;
            toolTip1.ShowAlways = true;
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
            toolTip.SetToolTip(button4, "�ڸ���");
            toolTip.SetToolTip(button5, "��");
            toolTip.SetToolTip(button6, "���찳");
            toolTip.SetToolTip(button7, "�����̵�");
            toolTip.SetToolTip(button13, "�̸�Ƽ��");
            toolTip.SetToolTip(button8, "������ũ");
            toolTip.SetToolTip(button9, "������ũ ����");
            toolTip.SetToolTip(button10, "����");
            InitializeCropPreview();
        }
        private void InitializeCropPreview()
        {
            cropPreviewBox = new PictureBox
            {
                // �� ���� �ϴܿ� ��ġ��ŵ�ϴ�.
                Size = new Size(200, 200),
                Location = new Point(LeftMargin, this.ClientSize.Height - 200 - BottomMargin),
                SizeMode = PictureBoxSizeMode.Zoom, // �̹����� ��Ʈ�ѿ� �°� ������
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false, // ��ҿ��� ���� ��
                BackColor = Color.LightGray,
                // �� ũ�Ⱑ ����� �� ��ġ�� �����ϵ��� ����
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            this.Controls.Add(cropPreviewBox);
            cropPreviewBox.BringToFront(); // �ٸ� ��Ʈ�Ѻ��� �׻� ���� ���̵��� ����
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

            int totalLeft = LeftMargin; //+ LeftPanelWidth + GapBetweenPictureBoxAndPanel;
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
            if (sender is not PictureBox pb || pb.Image == null) return;

            // ��Ŭ���� �׻� ���ؽ�Ʈ �޴��� ���� ���ǹǷ�, ��Ŭ���� ���� �Ʒ� ���� ����
            if (e.Button != MouseButtons.Left) return;

            // ======== �۾� ��忡 ���� ��� �б� ========
            if (currentWorkMode == "��")
            {
                isDrawing = true;
                currentStroke = new PenStroke
                {
                    StrokeColor = panelColorSelected.BackColor,
                    StrokeWidth = tbPenSize.Value
                };
                currentStroke.Points.Add(e.Location);
                if (!penStrokesMap.ContainsKey(pb)) penStrokesMap[pb] = new List<PenStroke>();
                pb.Cursor = Cursors.Cross;
                return; // "��" �۾� �� �ٸ� �۾�(�̵� ��)�� ���� ���� return
            }
            if (currentWorkMode == "���찳")
            {
                isDrawing = true;
                pb.Cursor = Cursors.Cross;
                // ���� ����� ������ MouseMove���� ó��
                return; // "���찳" �۾� �� return
            }
            if (currentWorkMode == "������ũ")
            {
                mosaicStartPoint = e.Location;
                isMosaicing = true;
                originalImages[pb] = new Bitmap(pb.Image); // �̸����⸦ ���� ���� ���
                return; // "������ũ" �۾� �� return
            }
            if (currentWorkMode == "�����̵�")
            {
                // Ŭ�� ��, '�����̵� ����'(�̸�����)�� '���� ����'(����)���� Ȯ���մϴ�.
                panelColorSelected.BackColor = panelColorPicked.BackColor;
                return; // �����̵� �۾� �� �ٸ� �۾��� ���� ���� ���⼭ �����մϴ�.
            }
            if (currentWorkMode == "�ڸ���")
            {
                isCropping = true;
                cropStartPoint = e.Location;
                pb.Cursor = Cursors.Cross;
                return; // "�ڸ���" �۾� �� return
            }
            if (currentWorkMode == "����")
            {
                Point clickPoint = e.Location;
                var targetAction = undoStack
                    .Where(a => a.ActionType == "Mosaic" && a.Target == pb &&
                                a.AffectedArea.HasValue && Enlarge(a.AffectedArea.Value, 10).Contains(clickPoint))
                    .LastOrDefault();

                if (targetAction != null)
                {
                    RestoreMosaicArea(pb, targetAction);
                    undoStack = new Stack<EditAction>(undoStack.Where(a => a != targetAction));
                }
                return; // "����" �۾� �� return
            }

            // "�̵�" ����� ���� �Ʒ� ������ ����˴ϴ�.
            bool emojiClicked = false;
            foreach (Control child in pb.Controls)
            {
                if (child is PictureBox && child.Bounds.Contains(e.Location))
                {
                    emojiClicked = true;
                    break;
                }
            }
            if (!emojiClicked)
            {
                foreach (Control child in pb.Controls)
                {
                    if (child is PictureBox emoji)
                    {
                        emoji.Tag = null;
                        emoji.Invalidate();
                    }
                }
                selectedEmoji = null;
            }

            bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (isCtrlPressed)
            {
                if (selectedImages.Contains(pb)) { selectedImages.Remove(pb); selectedImage = selectedImages.LastOrDefault(); }
                else { selectedImages.Add(pb); selectedImage = pb; }
            }
            else
            {
                if (!selectedImages.Contains(pb))
                {
                    foreach (var item in selectedImages) { item.Invalidate(); }
                    selectedImages.Clear();
                }
                if (!selectedImages.Contains(pb)) { selectedImages.Add(pb); }
                selectedImage = pb;
            }

            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
                UpdateEditControlsFromSelectedImage();
                if (imageTransparencyMap.TryGetValue(selectedImage, out int alpha)) { tbTransparencyGlobal.Value = alpha; }
                else { tbTransparencyGlobal.Value = 255; imageTransparencyMap[selectedImage] = 255; }
            }

            foreach (var item in this.Controls.OfType<PictureBox>()) { item.Invalidate(); }
            if (tabControl1.SelectedTab != null)
            {
                foreach (var item in tabControl1.SelectedTab.Controls.OfType<PictureBox>()) { item.Invalidate(); }
            }


            if (!string.IsNullOrEmpty(resizeDirection) && !emojiClicked)
            {
                isResizing = true;
                isDragging = false;
                resizeStartPoint = e.Location;
                resizeStartSize = pb.Size;
                resizeStartLocation = pb.Location;
            }
            else if (!emojiClicked)
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

        // PictureBox ���콺 �̵� �̺�Ʈ �ڵ鷯 (�巡��, �������� ��)
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not PictureBox pb || pb.Image == null) return;

            // ======== �۾� ��忡 ���� ��� �б� ========
            if (currentWorkMode == "��" && isDrawing && currentStroke != null)
            {
                currentStroke.Points.Add(e.Location);
                pb.Invalidate();
                return;
            }
            if (currentWorkMode == "���찳" && isDrawing)
            {
                if (penStrokesMap.TryGetValue(pb, out var strokes))
                {
                    var toRemove = strokes.Where(stroke =>
                        stroke.Points.Any(p => Distance(p, e.Location) < EraserRadius)
                    ).ToList();

                    if (toRemove.Any())
                    {
                        foreach (var stroke in toRemove) strokes.Remove(stroke);
                        pb.Invalidate();
                    }
                }
                return;
            }
            if (currentWorkMode == "������ũ" && isMosaicing)
            {
                Point mosaicEndPoint = e.Location;
                mosaicRect = new Rectangle(
                    Math.Min(mosaicStartPoint.X, mosaicEndPoint.X),
                    Math.Min(mosaicStartPoint.Y, mosaicEndPoint.Y),
                    Math.Abs(mosaicStartPoint.X - mosaicEndPoint.X),
                    Math.Abs(mosaicStartPoint.Y - mosaicEndPoint.Y)
                );
                pb.Image?.Dispose();
                pb.Image = ApplyMosaicToPreview(originalImages[pb], mosaicRect, tbMosaicSize.Value);
                return;
            }
            if (currentWorkMode == "�ڸ���" && isCropping)
            {
                Point cropEnd = e.Location;
                cropRect = new Rectangle(
                    Math.Min(cropStartPoint.X, cropEnd.X),
                    Math.Min(cropStartPoint.Y, cropEnd.Y),
                    Math.Abs(cropStartPoint.X - cropEnd.X),
                    Math.Abs(cropStartPoint.Y - cropEnd.Y)
                );
                pb.Invalidate(); // ���� �̹����� ���� �簢���� �ٽ� �׸����� ��û

                // ================== [���� �߰��� ����] ==================
                // ��ȿ�� �ڸ��� ������ ��� �̸����⸦ ������Ʈ�մϴ�.
                if (selectedImage != null && selectedImage.Image != null && cropRect.Width > 1 && cropRect.Height > 1)
                {
                    // ���� �̹��� ũ�⸦ ����� �ʵ��� �ڸ��� ������ �����մϴ�.
                    Rectangle validCropRect = Rectangle.Intersect(cropRect, new Rectangle(0, 0, selectedImage.Image.Width, selectedImage.Image.Height));

                    if (validCropRect.Width > 1 && validCropRect.Height > 1)
                    {
                        // ���� �̹������� �ش� ������ �����Ͽ� �̸����� �̹����� ����ϴ�.
                        Bitmap sourceBmp = (Bitmap)selectedImage.Image;
                        Bitmap preview = sourceBmp.Clone(validCropRect, sourceBmp.PixelFormat);

                        // �̸����� PictureBox�� �̹����� ǥ���ϰ� ���̰� �մϴ�.
                        cropPreviewBox.Image?.Dispose(); // ���� �̸����� �̹��� ���ҽ� ����
                        cropPreviewBox.Image = preview;
                        cropPreviewBox.Visible = true;
                    }
                    else
                    {
                        cropPreviewBox.Visible = false; // ������ ��ȿ���� ������ ����
                    }
                }
                else
                {
                    cropPreviewBox.Visible = false; // ������ ��ȿ���� ������ ����
                }
                // ========================================================
                return;
            }
            if (currentWorkMode == "�����̵�")
            {
                pb.Cursor = Cursors.Cross;
                try
                {
                    Bitmap bmp = pb.Image as Bitmap;
                    if (bmp != null && e.X >= 0 && e.Y >= 0 && e.X < bmp.Width && e.Y < bmp.Height)
                    {
                        // ���콺 �Ʒ� �ȼ� ������ �����ɴϴ�.
                        Color picked = bmp.GetPixel(e.X, e.Y);

                        // '�����̵� ����' �̸����� �г��� ������ �ǽð����� ������Ʈ�մϴ�.
                        panelColorPicked.BackColor = picked;

                        // RGB ���� ���̺� �Բ� ������Ʈ�մϴ�.
                        lblRGB.Text = $"RGB: {picked.R}, {picked.G}, {picked.B}\nHex: #{picked.R:X2}{picked.G:X2}{picked.B:X2}";
                    }
                }
                catch (Exception)
                {
                    // GetPixel�� Ư�� �̹��� ���Ŀ��� ���ܸ� �߻���ų �� �����Ƿ� ������ġ�� �Ӵϴ�.
                }
                return; // �����̵� ����� ���� �ٸ� ����(�巡�� ��)�� ���� ���� ���⼭ �����մϴ�.
            }

            // "�̵�" ����� ���� �Ʒ� ������ ����˴ϴ�.
            if (isResizing)
            {
                Point mousePosInParent = pb.Parent.PointToClient(MousePosition);
                int fixedRight = resizeStartLocation.X + resizeStartSize.Width;
                int fixedBottom = resizeStartLocation.Y + resizeStartSize.Height;
                int fixedLeft = resizeStartLocation.X;
                int fixedTop = resizeStartLocation.Y;
                int newWidth = pb.Width, newHeight = pb.Height, newLeft = pb.Left, newTop = pb.Top;

                if (resizeDirection.Contains("Right")) newWidth = Math.Max(20, mousePosInParent.X - fixedLeft);
                if (resizeDirection.Contains("Left")) { newWidth = Math.Max(20, fixedRight - mousePosInParent.X); newLeft = fixedRight - newWidth; }
                if (resizeDirection.Contains("Bottom")) newHeight = Math.Max(20, mousePosInParent.Y - fixedTop);
                if (resizeDirection.Contains("Top")) { newHeight = Math.Max(20, fixedBottom - mousePosInParent.Y); newTop = fixedBottom - newHeight; }

                pb.SetBounds(newLeft, newTop, newWidth, newHeight);
                textBox1.Text = pb.Width.ToString();
                textBox2.Text = pb.Height.ToString();
            }
            else if (isDragging)
            {
                Point currentMousePosition = pb.Parent.PointToClient(MousePosition);
                int deltaX = currentMousePosition.X - dragStartMousePosition.X;
                int deltaY = currentMousePosition.Y - dragStartMousePosition.Y;
                foreach (var item in dragStartPositions)
                {
                    item.Key.Location = new Point(item.Value.X + deltaX, item.Value.Y + deltaY);
                }
                if (selectedImage != null)
                {
                    textBox3.Text = selectedImage.Left.ToString();
                    textBox4.Text = selectedImage.Top.ToString();
                }
            }
            else
            {
                const int edge = 5;
                bool atTop = e.Y <= edge, atBottom = e.Y >= pb.Height - edge, atLeft = e.X <= edge, atRight = e.X >= pb.Width - edge;
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
            if (sender is not PictureBox pb) return;

            // ======== �۾� ��忡 ���� ��� �б� ========
            // [���� 1] �� �۾� �Ϸ� ��
            if (currentWorkMode == "��" && isDrawing && currentStroke != null)
            {
                if (penStrokesMap.TryGetValue(pb, out var strokes))
                {
                    strokes.Add(currentStroke);
                }
                else
                {
                    var newStrokes = new List<PenStroke> { currentStroke };
                    penStrokesMap[pb] = newStrokes;
                }
                currentStroke = null;
                isDrawing = false;
                pb.Invalidate(); // ApplyStrokesToImage(pb) ��� Invalidate()�� ȣ��
                return;
            }

            // [���� 2] ���찳 �۾� �Ϸ� ��
            if (currentWorkMode == "���찳" && isDrawing)
            {
                isDrawing = false;
                // if (penStrokesMap.ContainsKey(pb)) ApplyStrokesToImage(pb); <- �ٷ� �� �ڵ带 �����ؾ� �մϴ�!
                pb.Invalidate(); // ȭ�鸸 �����ϵ��� ����
                return;
            }

            // ======== ���� ���� �ڵ� ========
            if (currentWorkMode == "������ũ" && isMosaicing)
            {
                isMosaicing = false;
                if (!originalImages.ContainsKey(pb)) return;
                Bitmap before = originalImages[pb];
                Bitmap after = new Bitmap(before);
                Rectangle finalRect = NormalizeRectangle(mosaicRect);
                ApplyMosaic(after, finalRect, tbMosaicSize.Value);
                pb.Image = after;
                undoStack.Push(new EditAction(pb, before, (Bitmap)after.Clone(), "Mosaic", finalRect));
                originalImages.Remove(pb);
                pb.Invalidate();
                return;
            }
            if (currentWorkMode == "�ڸ���" && isCropping)
            {
                isCropping = false;
                pb.Cursor = Cursors.Default;
                pb.Invalidate();
                return;
            }

            // "�̵�" ����� ���� �Ʒ� ������ ����˴ϴ�.
            if (e.Button == MouseButtons.Right)
            {
                menuCopy.Enabled = selectedImages.Count > 0;
                menuDelete.Enabled = selectedImages.Count > 0;
                menuPaste.Enabled = clipboardContent.Count > 0;
                imageContextMenu.Tag = pb;
                imageContextMenu.Show(pb, e.Location);
            }
            if (isResizing)
            {
                UpdateSelectedImageSize();
            }

            isDragging = false;
            isResizing = false;
            draggingPictureBox = null;
            resizeDirection = "";

            pb.Invalidate();
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
                    pen.DashStyle = (pb == selectedImage) ? DashStyle.Solid : DashStyle.Dot;
                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            // �̸��� �̸����� �׸���
            if (showEmojiPreview && emojiPreviewImage != null)
            {
                var basePb = (PictureBox)sender;
                if (basePb.AllowDrop)
                {
                    e.Graphics.DrawImage(emojiPreviewImage,
                        emojiPreviewLocation.X - emojiPreviewWidth / 2,
                        emojiPreviewLocation.Y - emojiPreviewHeight / 2,
                        emojiPreviewWidth, emojiPreviewHeight);
                }
            }

            // ======== [������ �κ�] �� �� �� �ڸ��� ���� �׸��� ========
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. ����� ��� �� ������ �׸��ϴ�.
            // �� ������ �߰��Ǿ�, �������� ���� ��� ������ ȭ�鿡 ��� ǥ�õ˴ϴ�.
            if (penStrokesMap.TryGetValue(pb, out var strokes))
            {
                foreach (var stroke in strokes)
                {
                    if (stroke.Points.Count >= 2)
                    {
                        using (Pen pen = new Pen(stroke.StrokeColor, stroke.StrokeWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        {
                            e.Graphics.DrawLines(pen, stroke.Points.ToArray());
                        }
                    }
                }
            }

            // 2. ���� �׸��� �ִ� �� �� �׸��� (�ǽð� �̸�����)
            if (isDrawing && currentWorkMode == "��" && currentStroke != null && currentStroke.Points.Count >= 2)
            {
                using (Pen pen = new Pen(currentStroke.StrokeColor, currentStroke.StrokeWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    e.Graphics.DrawLines(pen, currentStroke.Points.ToArray());
                }
            }

            // 3. �ڸ��� ���� �簢�� �׸���
            if (currentWorkMode == "�ڸ���" && isCropping && pb == selectedImage && cropRect.Width > 0 && cropRect.Height > 0)
            {
                using (Pen cropPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(cropPen, cropRect);
                }
            }
            // =============================================================
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
        // ========== [1�� �ڵ� ��� �߰�] UI �ʱ�ȭ �� �̺�Ʈ �ڵ鷯 ==========

        // 1�� �г�(���� ����)�� ��Ʈ�ѵ��� �߰��ϴ� �޼���
        private void InitializePanel1()
        {
            Panel panel1 = dynamicPanels[0]; // 0�� �ε��� �г��� ���
            panel1.Controls.Clear();
            panel1.AutoScroll = true;

            int marginTop = 20;
            int spacing = 15;
            int controlWidth = 200;
            int currentY = marginTop;

            // 1. �̹��� ����
            Label lblTransparency = new Label { Text = "�̹��� ����", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblTransparency);
            currentY = lblTransparency.Bottom + spacing;

            tbTransparencyGlobal = new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                Value = 255,
                TickFrequency = 10,
                Width = controlWidth,
                Location = new Point(10, currentY)
            };
            panel1.Controls.Add(tbTransparencyGlobal);
            tbTransparencyGlobal.ValueChanged += TbTransparencyGlobal_ValueChanged;
            currentY = tbTransparencyGlobal.Bottom + spacing;

            // 2. ���� ����
            Button btnColorSelect = new Button { Text = "���� ����", Location = new Point(10, currentY), Size = new Size(80, 30) };
            panel1.Controls.Add(btnColorSelect);
            btnColorSelect.Click += BtnColorSelect_Click;

            panelColorSelected = new Panel
            {
                BackColor = Color.Black,
                Location = new Point(btnColorSelect.Right + 10, btnColorSelect.Top),
                Size = new Size(40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel1.Controls.Add(panelColorSelected);

            lblRGB = new Label
            {
                Text = "RGB: 0, 0, 0\nHex: #000000",
                Location = new Point(panelColorSelected.Right + 10, btnColorSelect.Top + 5),
                AutoSize = true
            };
            panel1.Controls.Add(lblRGB);

            Label lblPicked = new Label { Text = "�����̵� ����", Location = new Point(10, btnColorSelect.Bottom + spacing), AutoSize = true };
            panel1.Controls.Add(lblPicked);

            panelColorPicked = new Panel
            {
                BackColor = Color.White,
                Location = new Point(lblPicked.Right + 10, lblPicked.Top - 2),
                Size = new Size(40, 20),
                BorderStyle = BorderStyle.FixedSingle
            };
            panel1.Controls.Add(panelColorPicked);
            currentY = lblPicked.Bottom + spacing;

            // 3. �۾� ���
            GroupBox gbModes = new GroupBox
            {
                Text = "�۾� ���",
                Location = new Point(10, currentY),
                Size = new Size(250, 120)
            };
            string[] modes = { "�̵�", "�����̵�", "��", "���찳", "������ũ", "�ڸ���", "����" };
            for (int i = 0; i < modes.Length; i++)
            {
                int index = i;
                var rb = new RadioButton
                {
                    Text = modes[i],
                    Location = new Point(10 + (index % 3) * 80, 20 + (index / 3) * 25),
                    AutoSize = true,
                    Checked = (modes[index] == "�̵�")
                };
                rb.CheckedChanged += (s, e) =>
                {
                    if (rb.Checked)
                    {
                        currentWorkMode = rb.Text;
                        this.Cursor = (currentWorkMode == "�����̵�") ? Cursors.Cross : Cursors.Default;
                    }
                };
                gbModes.Controls.Add(rb);
            }
            panel1.Controls.Add(gbModes);
            currentY = gbModes.Bottom + spacing;

            // 4. �ǵ����� ��ư (������ũ ��)
            Button btnUndo = new Button { Text = "���� �ǵ�����", Location = new Point(10, currentY), Width = 100 };
            btnUndo.Click += (s, e) => PerformUndo(); // Undo �޼��� ����
            panel1.Controls.Add(btnUndo);
            currentY = btnUndo.Bottom + spacing;

            // 5. ��/���찳 �Ӽ�
            Label lblPen = new Label { Text = "��/���찳 ����", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblPen);
            currentY = lblPen.Bottom + 5;

            tbPenSize = new TrackBar
            {
                Minimum = 1,
                Maximum = 50,
                Value = 5,
                TickFrequency = 5,
                Width = controlWidth,
                Location = new Point(10, currentY)
            };
            panel1.Controls.Add(tbPenSize);
            currentY = tbPenSize.Bottom + spacing;

            // 6. ������ũ �Ӽ�
            Label lblMosaic = new Label { Text = "������ũ ��� ũ��", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblMosaic);
            currentY = lblMosaic.Bottom + 5;

            tbMosaicSize = new TrackBar
            {
                Minimum = 2,
                Maximum = 50,
                Value = 10,
                TickFrequency = 5,
                Width = controlWidth,
                Location = new Point(10, currentY)
            };
            panel1.Controls.Add(tbMosaicSize);
            currentY = tbMosaicSize.Bottom + spacing;

            // 7. �ڸ��� Ȯ�� ��ư
            Button btnConfirmCrop = new Button
            {
                Text = "�ڸ��� Ȯ��",
                Location = new Point(10, currentY),
                Size = new Size(100, 30)
            };
            btnConfirmCrop.Click += BtnConfirmCrop_Click;
            panel1.Controls.Add(btnConfirmCrop);
        }

        // ���� ���� ���̾�α� ����
        private void BtnColorSelect_Click(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.FullOpen = true;
                cd.Color = panelColorSelected.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    Color selected = cd.Color;
                    panelColorSelected.BackColor = selected;
                    lblRGB.Text = $"RGB: {selected.R}, {selected.G}, {selected.B}\nHex: #{selected.R:X2}{selected.G:X2}{selected.B:X2}";
                }
            }
        }

        // ���� ���� Ʈ���� �̺�Ʈ
        private void TbTransparencyGlobal_ValueChanged(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;
            int alpha = tbTransparencyGlobal.Value;
            if (!(selectedImage.Tag is Bitmap originalBitmap)) return;

            Bitmap transparentCopy = new Bitmap(originalBitmap.Width, originalBitmap.Height);
            using (Graphics g = Graphics.FromImage(transparentCopy))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = (float)alpha / 255; // Alpha �� ����
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(originalBitmap, new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height), 0, 0, originalBitmap.Width, originalBitmap.Height, GraphicsUnit.Pixel, attributes);
            }
            selectedImage.Image?.Dispose();
            selectedImage.Image = transparentCopy;
            imageTransparencyMap[selectedImage] = alpha;
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
                    Visible = false,
                    BorderStyle = BorderStyle.FixedSingle
                };

                // �ڡڡڡڡ� �ٷ� �� �κ��Դϴ�! �ڡڡڡڡ�
                // �г��� �迭�� ���� �Ҵ��ؾ�, InitializePanel1 ��� �ش� �г��� ����� �� �ֽ��ϴ�.
                dynamicPanels[i] = panel;
                panel.Paint += Panel_Paint;
                // �ڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡڡ�

                if (i == 0) // 1�� ��ư: ��, ������ũ, �ڸ��� �� ���� ���� �г�
                {
                    // ������ dynamicPanels[0]�� panel�� �Ҵ��߱� ������
                    // ���� InitializePanel1() �ȿ��� dynamicPanels[0]�� null�� �ƴմϴ�.
                    InitializePanel1();
                }
                else if (i == 1) // 2�� ��ư: �̹��� ���� ���� ��� �г�
                {
                    AddImageEditControls(panel);
                }
                else if (i == 7) // 8�� ��ư: �̸��� �г�
                {
                    panel.AllowDrop = true;
                    panel.AutoScroll = true;
                    panel.Controls.Add(new Label()
                    {
                        Text = "�̸��� ����",
                        Location = new Point(10, 10),
                        Font = new Font(this.Font, FontStyle.Bold)
                    });
                    AddEmojiControls(panel);
                }
                else // ������ �Ϲ� �г�
                {
                    panel.Controls.Add(new Label()
                    {
                        Text = $"���� �Ӽ� {i + 1}",
                        Location = new Point(10, 10)
                    });
                }

                this.Controls.Add(panel);
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
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    pb.Size = pb.Image.Size;
                    pb.Invalidate();

                    //  Tag�� imageList ����ȭ
                    if (pb.Tag is Bitmap oldTag) oldTag.Dispose();
                    pb.Tag = new Bitmap(pb.Image);

                    for (int i = 0; i < imageList.Count; i++)
                    {
                        if (imageList[i].pb == pb)
                        {
                            imageList[i] = (pb, new Bitmap(pb.Image));
                            break;
                        }
                    }
                }
            }
        }

        // ������ 90�� ȸ�� ��ư Ŭ��
        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    pb.Size = pb.Image.Size;
                    pb.Invalidate();

                    // ?? Tag�� imageList ����ȭ
                    if (pb.Tag is Bitmap oldTag) oldTag.Dispose();
                    pb.Tag = new Bitmap(pb.Image);

                    for (int i = 0; i < imageList.Count; i++)
                    {
                        if (imageList[i].pb == pb)
                        {
                            imageList[i] = (pb, new Bitmap(pb.Image));
                            break;
                        }
                    }
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
                var imageInfo = imageList.FirstOrDefault(item => item.pb == selectedImage);
                if (imageInfo.pb != null)
                {
                    try
                    {
                        // �׻� ���� �̹����� �����ؼ� _initialImage�� ����
                        _initialImage = new Bitmap(selectedImage.Image);  // �� ���� ���̴� ���¸� ���
                        originalImage = new Bitmap(selectedImage.Image);  // �� ���� �����뵵 ���� ����

                        btnResetAll_Click(null, null); // �� UI �ʱ�ȭ
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("�̹��� ���� ����: " + ex.Message);
                        return;
                    }
                }
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
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    pb.Invalidate();

                    // Tag�� imageList ����ȭ
                    if (pb.Tag is Bitmap oldTag) oldTag.Dispose();
                    pb.Tag = new Bitmap(pb.Image);

                    for (int i = 0; i < imageList.Count; i++)
                    {
                        if (imageList[i].pb == pb)
                        {
                            imageList[i] = (pb, new Bitmap(pb.Image));
                            break;
                        }
                    }
                }
            }
        }
        private void PerformUndo()
        {
            if (undoStack.Count == 0) return;
            EditAction action = undoStack.Pop();
            redoStack.Push(new EditAction(action.Target, (Bitmap)action.Target.Image.Clone(), action.Before, action.ActionType, action.AffectedArea));
            action.Target.Image?.Dispose();
            action.Target.Image = (Bitmap)action.Before.Clone();
            action.Target.Invalidate();
        }

        // ���� ���� ������ �Ÿ� ��� (���찳��)
        private float Distance(Point a, Point b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // ������ũ �̸������ �޼��� (���� �̹����� �������� ����)
        private Bitmap ApplyMosaicToPreview(Bitmap original, Rectangle rect, int blockSize)
        {
            Bitmap preview = (Bitmap)original.Clone();
            ApplyMosaic(preview, rect, blockSize); // unsafe �޼��� ȣ��
            return preview;
        }

        // Unsafe �ڵ带 ����� ��� ������ũ ����
        private unsafe void ApplyMosaic(Bitmap bmp, Rectangle rect, int blockSize)
        {
            if (blockSize <= 1) return;
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            if (rect.IsEmpty) return;

            Bitmap bmp32 = null;
            try
            {
                // 32bppArgb�� ��ȯ�Ͽ� ���� ä���� ������ ó��
                bmp32 = bmp.PixelFormat == PixelFormat.Format32bppArgb ? bmp : new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
                if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                {
                    using (Graphics g = Graphics.FromImage(bmp32))
                    {
                        g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    }
                }

                BitmapData data = bmp32.LockBits(new Rectangle(0, 0, bmp32.Width, bmp32.Height),
                                                 ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

                byte* ptrBase = (byte*)data.Scan0;
                int stride = data.Stride;

                for (int y = rect.Top; y < rect.Bottom; y += blockSize)
                {
                    for (int x = rect.Left; x < rect.Right; x += blockSize)
                    {
                        long r = 0, g = 0, b = 0, a = 0;
                        int count = 0;
                        int blockEndY = Math.Min(y + blockSize, rect.Bottom);
                        int blockEndX = Math.Min(x + blockSize, rect.Right);

                        for (int j = y; j < blockEndY; j++)
                        {
                            byte* row = ptrBase + j * stride;
                            for (int i = x; i < blockEndX; i++)
                            {
                                byte* pixel = row + i * 4;
                                b += pixel[0]; g += pixel[1]; r += pixel[2]; a += pixel[3];
                                count++;
                            }
                        }

                        if (count == 0) continue;
                        byte avgR = (byte)(r / count);
                        byte avgG = (byte)(g / count);
                        byte avgB = (byte)(b / count);
                        byte avgA = (byte)(a / count);

                        for (int j = y; j < blockEndY; j++)
                        {
                            byte* row = ptrBase + j * stride;
                            for (int i = x; i < blockEndX; i++)
                            {
                                byte* pixel = row + i * 4;
                                pixel[0] = avgB; pixel[1] = avgG; pixel[2] = avgR; pixel[3] = avgA;
                            }
                        }
                    }
                }
                bmp32.UnlockBits(data);

                if (bmp != bmp32) // ��ȯ�� ��쿡�� ������ �ٽ� �׸���
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(bmp32, 0, 0);
                    }
                }
            }
            finally
            {
                if (bmp != bmp32) bmp32?.Dispose();
            }
        }


        // �ڸ��� Ȯ�� ��ư Ŭ�� �̺�Ʈ
        private void BtnConfirmCrop_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null || cropRect.Width <= 0 || cropRect.Height <= 0)
            {
                MessageBox.Show("�ڸ� �̹����� �����ϰ� �巡�׷� ������ �����ϼ���.");
                return;
            }

            // ���� �̹����� �����Ͽ� �ӽ� ��Ʈ���� ����ϴ�.
            Bitmap bitmapWithStrokes = new Bitmap(selectedImage.Image);

            // ���õ� �̹����� �׷��� �� ������ �ִ��� Ȯ���ϰ� �ӽ� ��Ʈ�ʿ� ��Ĩ�ϴ�.
            if (penStrokesMap.TryGetValue(selectedImage, out var strokes) && strokes.Any())
            {
                using (Graphics g = Graphics.FromImage(bitmapWithStrokes))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    foreach (var stroke in strokes)
                    {
                        if (stroke.Points.Count >= 2)
                        {
                            using (Pen pen = new Pen(stroke.StrokeColor, stroke.StrokeWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            {
                                g.DrawLines(pen, stroke.Points.ToArray());
                            }
                        }
                    }
                }
            }

            // �׸��� ������ ��Ʈ���� ������� �ڸ��⸦ �����մϴ�.
            Rectangle intersected = Rectangle.Intersect(cropRect, new Rectangle(Point.Empty, bitmapWithStrokes.Size));
            if (intersected.Width > 0 && intersected.Height > 0)
            {
                Bitmap cropped = bitmapWithStrokes.Clone(intersected, bitmapWithStrokes.PixelFormat);
                PictureBox pbNew = new PictureBox
                {
                    Image = cropped,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = cropped.Size,
                    Location = new Point(selectedImage.Right + 20, selectedImage.Top),
                    Tag = new Bitmap(cropped)
                };
                EnableDoubleBuffering(pbNew);
                imageList.Add((pbNew, (Bitmap)pbNew.Tag));
                imageTransparencyMap[pbNew] = 255;

                pbNew.MouseDown += pictureBox_MouseDown;
                pbNew.MouseMove += pictureBox_MouseMove;
                pbNew.MouseUp += pictureBox_MouseUp;
                pbNew.Paint += pictureBox_Paint;
                pbNew.AllowDrop = true;
                pbNew.DragEnter += PictureBox_DragEnter;
                pbNew.DragOver += PictureBox_DragOver;
                pbNew.DragDrop += PictureBox_DragDrop;
                pbNew.DragLeave += PictureBox_DragLeave;

                tabControl1.SelectedTab.Controls.Add(pbNew);
                pbNew.BringToFront();

                selectedImages.Clear();
                selectedImages.Add(pbNew);
                selectedImage = pbNew;
                pbNew.Invalidate();
            }

            // ================== [������ �κ�] ==================
            // �ڸ��� �۾��� �����ߵ� �����ߵ�, �����ִ� �̸����� â�� ����� ���ҽ��� �����մϴ�.
            if (cropPreviewBox != null)
            {
                cropPreviewBox.Visible = false;
                cropPreviewBox.Image?.Dispose();
                cropPreviewBox.Image = null;
            }
            // =================================================

            // �ӽ÷� ����� ��Ʈ�� ���ҽ��� �����մϴ�.
            bitmapWithStrokes.Dispose();
        }
        // �׷��� ������ �̹����� ���������� �ռ��ϴ� �޼���
        private void ApplyStrokesToImage(PictureBox pb)
        {
            if (penStrokesMap.TryGetValue(pb, out var strokes) && strokes.Any())
            {
                using (Graphics g = Graphics.FromImage(pb.Image))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    foreach (var stroke in strokes)
                    {
                        if (stroke.Points.Count >= 2)
                        {
                            using (Pen pen = new Pen(stroke.StrokeColor, stroke.StrokeWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                            {
                                g.DrawLines(pen, stroke.Points.ToArray());
                            }
                        }
                    }
                }
                strokes.Clear();
                pb.Invalidate();
            }
        }


        // �簢�� ���� Ȯ�� (���� ������)
        private Rectangle Enlarge(Rectangle rect, int margin)
        {
            return new Rectangle(rect.X - margin, rect.Y - margin, rect.Width + margin * 2, rect.Height + margin * 2);
        }

        // ������ ������ũ ���� ����
        private void RestoreMosaicArea(PictureBox pb, EditAction action)
        {
            if (pb.Image is Bitmap current && action.Before is Bitmap before && action.AffectedArea.HasValue)
            {
                Rectangle area = action.AffectedArea.Value;
                using (Graphics g = Graphics.FromImage(current))
                {
                    g.DrawImage(before, area, area, GraphicsUnit.Pixel);
                }
                pb.Invalidate();
            }
        }

        // �簢�� ��ǥ ����ȭ (���� �ʺ�/���� ����)
        private Rectangle NormalizeRectangle(Rectangle rect)
        {
            return new Rectangle(
                Math.Min(rect.Left, rect.Right),
                Math.Min(rect.Top, rect.Bottom),
                Math.Abs(rect.Width),
                Math.Abs(rect.Height)
            );
        }

        private void button5_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(7);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(0);
        }

        private void toolStrip_NewFile_Click(object sender, EventArgs e)   //�� ���θ����
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

        private void toolStrip_Open_Click(object sender, EventArgs e)   //�� ���Ͽ���
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

        private void toolStripp_Save_Click(object sender, EventArgs e)   //�� �����ϱ�
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

        private void toolStripMenuItem1_Click(object sender, EventArgs e)  //���ȭ�� �����ϱ� ��
        {
            TabPage currentTab = tabControl1.SelectedTab;

            if (currentTab == null)
            {
                MessageBox.Show("���� ���õ��� �ʾҽ��ϴ�.");
                return;
            }

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

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.jpg");
                combinedImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);

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
            finally
            {
                combinedImage.Dispose(); // ���ҽ� ����
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)  //Ȯ�� ��
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

        private void toolStripMenuItem4_Click(object sender, EventArgs e)  //��� ��
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
    }

    // Form1 Ŭ���� �ٱ��� �߰�
    public class EmojiState
    {
        public Image Image { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
    }
    public class PenStroke
    {
        public List<Point> Points = new List<Point>();
        public Color StrokeColor;
        public float StrokeWidth;

        public Rectangle GetBoundingBox()
        {
            if (Points.Count == 0) return Rectangle.Empty;
            int minX = Points.Min(p => p.X);
            int minY = Points.Min(p => p.Y);
            int maxX = Points.Max(p => p.X);
            int maxY = Points.Max(p => p.Y);
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public class EditAction
    {
        public PictureBox Target { get; set; }
        public Bitmap Before { get; set; }
        public Bitmap After { get; set; }
        public string ActionType { get; set; }
        public Rectangle? AffectedArea { get; set; } // ������ũ ���� ��

        public EditAction(PictureBox target, Bitmap before, Bitmap after, string actionType, Rectangle? affectedArea = null)
        {
            Target = target;
            Before = before;
            After = after;
            ActionType = actionType;
            AffectedArea = affectedArea;
        }
    }
}