using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
        // Constants for layout
        private const int LeftMargin = 20;
        private const int TopMargin = 90;
        private const int PanelWidth = 300;
        private const int PanelRightMargin = 20;
        private const int GapBetweenPictureBoxAndPanel = 20;
        private const int BottomMargin = 20;

        // �̹��� ������ ������ ����Ʈ
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();

        // �̹����� ���� �� ���� �߰�
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        //���ο� �� ��ȣ�� �����ִ� ����
        private int tabCount = 2;

        // --- ���� ���� ������ ---
        private bool isDragging = false;
        private Point clickOffset;
        private PictureBox draggingPictureBox = null;
        private Point dragStartMousePosition; // �θ� ��Ʈ�� ���� ���콺 ���� ��ġ
        private Dictionary<PictureBox, Point> dragStartPositions = new Dictionary<PictureBox, Point>(); // �巡�� ���� ������ ��� PictureBox ��ġ
        private bool isResizing = false;
        private Point resizeStartPoint;
        private Size resizeStartSize;
        private Point resizeStartLocation;
        private string resizeDirection = "";
        private bool showSelectionBorder = false;
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        private Panel currentVisiblePanel = null;
        private List<PictureBox> selectedImages = new List<PictureBox>(); // ���� �̹����� ���� ����Ʈ
        private PictureBox selectedImage = null;
        private bool showSelectionBorderForImage = false;
        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;
        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false;
        private const int handleSize = 10;
        private bool isMarqueeSelecting = false;      // ���� �巡�� ���� ������ ����
        private Point marqueeStartPoint;            // �巡�� ���� ����
        private Rectangle marqueeRect;              // ȭ�鿡 �׷��� ���� �簢��
        private Stack<List<EmojiState>> emojiUndoStack = new Stack<List<EmojiState>>();
        private Stack<List<EmojiState>> emojiRedoStack = new Stack<List<EmojiState>>();


        // ���� ���ο� �߰� �������� �� Ŭ����
        private bool isCropping = false;
        private Point cropStartPoint;
        private Rectangle cropRect;
        private Dictionary<PictureBox, Bitmap> originalImages = new();
        private TrackBar tbMosaicSize;
        private Panel panelColorSelected;   // "���� ����" �̸�����
        private Panel panelColorPicked;     // �����̵�� ���õ� ���� �̸�����
        private Label lblRGB;               // RGB + Hex ǥ��
        private Dictionary<PictureBox, int> imageTransparencyMap = new Dictionary<PictureBox, int>();
        private TrackBar tbTransparencyGlobal;
        // ���� ����: �ǵ�����/�ٽý��� ����
        private Stack<EditAction> undoStack = new Stack<EditAction>(); // �߰���
        private Stack<EditAction> redoStack = new Stack<EditAction>(); //  �߰���
        private bool isDrawing = false;                // ���콺 �巡�� ������ ����
        private Point lastDrawPoint = Point.Empty;     // ������ �׷ȴ� ��
        private bool isMosaicing = false; // ������ũ ������ ����
        private Point mosaicStartPoint;   // �巡�� ���� ��ġ
        private Rectangle mosaicRect;     // �巡�׵� �簢��


        // �� PictureBox ���� �׷��� ������ ����
        private Dictionary<PictureBox, List<PenStroke>> penStrokesMap = new Dictionary<PictureBox, List<PenStroke>>();

        // ���� �׸��� �ִ� ��
        private PenStroke currentStroke = null;
        private string currentWorkMode = "�̵�";

        private TrackBar tbPenSize; // �� ��� ���찳�� ��� ����
        private int EraserRadius => tbEraserSize?.Value ?? 10;
        private TrackBar tbEraserSize; // ���찳 ũ�� ������ Ʈ����

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


        // �̹������������� Ŭ���� �߰� ��
        //  ���� ���õ� �۾� ���
       


        public class EditAction
        {
            public PictureBox Target { get; set; }
            public Bitmap Before { get; set; }
            public Bitmap After { get; set; }
            public string ActionType { get; set; }
            public Rectangle? AffectedArea { get; set; }  // �� �߿�! Ŭ�� ��ǥ �񱳿�

            public EditAction(PictureBox target, Bitmap before, Bitmap after, string actionType, Rectangle? affectedArea = null)
            {
                Target = target;
                Before = before;
                After = after;
                ActionType = actionType;
                AffectedArea = affectedArea;
            }
        }
        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized;
            this.MouseDown += Form1_MouseDown;
            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox1.Validating += textBox1_Validating;
            textBox2.Validating += textBox2_Validating;
            textBox1.KeyDown += TextBox_KeyDown_ApplyOnEnter;
            textBox2.KeyDown += TextBox_KeyDown_ApplyOnEnter;
            this.BackColor = ColorTranslator.FromHtml("#FFF0F5"); // �󺥴� ���� ����
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

        }

        private void TextBox_KeyDown_ApplyOnEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.SelectNextControl((Control)sender, true, true, true, true);
                e.SuppressKeyPress = true;
            }
        }

        private void TextBox_OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private const int LeftPanelWidth = 80;

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

            groupBox2.Width = this.ClientSize.Width - 24;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                EmojiUndo();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                EmojiRedo();
                e.Handled = true;
            }
        }


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
            foreach (var emoji in states)
            {
                var emojiPb = new PictureBox
                {
                    Image = (Image)emoji.Image.Clone(),
                    Location = emoji.Location,
                    Size = emoji.Size,
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

                    // --- �̹��� ���� ---
                    pb.SizeMode = PictureBoxSizeMode.StretchImage;
                    pb.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    pb.Dock = DockStyle.None;
                    pb.Location = new Point(10, 30);
                    EnableDoubleBuffering(pb);

                    Bitmap originalCopy;
                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original);
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size;
                    pb.Tag = originalCopy;

                    imageList.Add((pb, originalCopy));

                    // [������ �κ� 1] ���� �⺻�� ���
                    imageTransparencyMap[pb] = 255;

                    // --- �̺�Ʈ �ڵ鷯 ���� ---
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;

                    currentTab.Controls.Add(pb);

                    // ���õ� �̹��� ����
                    selectedImage = pb;
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();

                    // [������ �κ� 2] ���õ� �̹����� ���� Ʈ���� ��ġ �ݿ�
                    if (imageTransparencyMap.TryGetValue(pb, out int alpha))
                    {
                        tbTransparencyGlobal.Value = alpha;
                    }
                    else
                    {
                        tbTransparencyGlobal.Value = 255;
                        imageTransparencyMap[pb] = 255;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�:\n" + ex.Message);
                }
            }
        }


        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is not PictureBox pb || pb.Image == null || e.Button != MouseButtons.Left)
                return;

            // --- [�� ���] ---
            if (currentWorkMode == "��")
            {
                isDrawing = true;

                currentStroke = new PenStroke
                {
                    StrokeColor = panelColorSelected.BackColor,
                    StrokeWidth = tbPenSize.Value
                };
                currentStroke.Points.Add(e.Location);

                if (!penStrokesMap.ContainsKey(pb))
                {
                    penStrokesMap[pb] = new List<PenStroke>();
                }

                pb.Cursor = Cursors.Cross;
                return;
            }

            // --- [���찳 ���] ---
            if (currentWorkMode == "���찳")
            {
                isDrawing = true;
                pb.Cursor = Cursors.Cross;
                return;
            }

            // --- [������ũ ���] ---
            if (currentWorkMode == "������ũ")
            {
                mosaicStartPoint = e.Location;
                isMosaicing = true;
                return;
            }

            // --- [�����̵� ���] ---
            if (currentWorkMode == "�����̵�")
            {
                Bitmap bmp = pb.Image as Bitmap;
                if (bmp != null && e.X >= 0 && e.Y >= 0 && e.X < bmp.Width && e.Y < bmp.Height)
                {
                    Color picked = bmp.GetPixel(e.X, e.Y);
                    panelColorPicked.BackColor = picked;
                    panelColorSelected.BackColor = picked;

                    lblRGB.Text = $"RGB: {picked.R}, {picked.G}, {picked.B}\nHex: #{picked.R:X2}{picked.G:X2}{picked.B:X2}";
                }
                return;
            }
            if (currentWorkMode == "�ڸ���" && e.Button == MouseButtons.Left && sender is PictureBox pbCrop && pbCrop.Image != null)
            {
                isCropping = true;
                cropStartPoint = e.Location;
                pbCrop.Cursor = Cursors.Cross;
                return;
            }
            if (currentWorkMode == "����" && e.Button == MouseButtons.Left)
            {
                Point clickPoint = e.Location;

                var targetAction = undoStack
                    .Where(a => a.ActionType == "Mosaic" &&
                                a.Target == pb &&
                                a.AffectedArea.HasValue &&
                                Enlarge(a.AffectedArea.Value, 10).Contains(clickPoint)) // �� �˳��ϰ� 10�ȼ� Ȯ��
                    .LastOrDefault();

                if (targetAction != null)
                {
                    RestoreMosaicArea(pb, targetAction); // �� ��ü ���� ��� ������ ����
                    undoStack = new Stack<EditAction>(undoStack.Where(a => a != targetAction));
                }

                return;
            }

            // --- [�⺻ ����/�̵�/������¡ ���] ---
            bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (isCtrlPressed)
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
            else
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
            }

            foreach (var item in selectedImages) { item.Invalidate(); }

            if (!string.IsNullOrEmpty(resizeDirection))
            {
                isResizing = true;
                isDragging = false;
            }
            else
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

            if (imageTransparencyMap.TryGetValue(selectedImage, out int selectedAlpha))
            {
                tbTransparencyGlobal.Value = selectedAlpha;
            }
            else
            {
                tbTransparencyGlobal.Value = 255;
                imageTransparencyMap[selectedImage] = 255;
            }
        }






        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not PictureBox pb || pb.Image == null) return;

            // --- [������ũ ���: �巡�� �� �ǽð� ����] ---
            if (currentWorkMode == "������ũ" && isMosaicing)
            {
                Point mosaicEndPoint = e.Location;

                mosaicRect = new Rectangle(
                    Math.Min(mosaicStartPoint.X, mosaicEndPoint.X),
                    Math.Min(mosaicStartPoint.Y, mosaicEndPoint.Y),
                    Math.Abs(mosaicStartPoint.X - mosaicEndPoint.X),
                    Math.Abs(mosaicStartPoint.Y - mosaicEndPoint.Y)
                );

                if (!originalImages.ContainsKey(pb))
                {
                    originalImages[pb] = new Bitmap(pb.Image); // ���� ����
                }

                Bitmap original = originalImages[pb];

                // ���� �̹����� ���� (��ü ���� X �� ������ũ �簢���� �׸����)
                Bitmap preview = new Bitmap(original);

                // ���� ������ũ ������ �̹��� ��踦 ��ġ�� ����
                Rectangle mosaicRectClamped = Rectangle.Intersect(
                    new Rectangle(0, 0, preview.Width, preview.Height),
                    mosaicRect
                );

                using (Graphics g = Graphics.FromImage(preview))
                {
                    // ApplyMosaic�� preview ��ü�� �ٲٰ� ��
                    ApplyMosaic(preview, mosaicRectClamped, tbMosaicSize.Value);
                }

                pb.Image = preview;
                pb.Invalidate();
                return;
            }

            // --- [���찳 ���: �� ��Ʈ��ũ ����] ---
            if (currentWorkMode == "���찳" && isDrawing)
            {
                if (penStrokesMap.TryGetValue(pb, out var strokes))
                {
                    Point eraserCenter = e.Location;
                    float threshold = EraserRadius * 1.2f;

                    var toRemove = new List<PenStroke>();

                    foreach (var stroke in strokes)
                    {
                        for (int i = 0; i < stroke.Points.Count - 1; i++)
                        {
                            Point p1 = stroke.Points[i];
                            Point p2 = stroke.Points[i + 1];

                            float distance = DistanceFromPointToSegment(eraserCenter, p1, p2);
                            if (distance <= threshold)
                            {
                                toRemove.Add(stroke);
                                break;
                            }
                        }
                    }

                    foreach (var stroke in toRemove)
                        strokes.Remove(stroke);

                    pb.Invalidate();
                }
                return;
            }

            // --- [�� ���: �� �׸���] ---
            if (currentWorkMode == "��" && isDrawing && currentStroke != null)
            {
                currentStroke.Points.Add(e.Location);
                pb.Invalidate();
                return;
            }

            // --- [�����̵� ���: ���� ����] ---
            if (currentWorkMode == "�����̵�")
            {
                Point mousePos = e.Location;
                Bitmap bmp = pb.Image as Bitmap;

                if (bmp != null && mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X < bmp.Width && mousePos.Y < bmp.Height)
                {
                    Color colorUnderCursor = bmp.GetPixel(mousePos.X, mousePos.Y);
                    panelColorPicked.BackColor = colorUnderCursor;
                }

                pb.Cursor = Cursors.Cross;
                return;
            }
            if (currentWorkMode == "�ڸ���" && isCropping && sender is PictureBox pbCrop && pbCrop.Image != null)
            {
                Point cropEnd = e.Location;
                cropRect = new Rectangle(
                    Math.Min(cropStartPoint.X, cropEnd.X),
                    Math.Min(cropStartPoint.Y, cropEnd.Y),
                    Math.Abs(cropStartPoint.X - cropEnd.X),
                    Math.Abs(cropStartPoint.Y - cropEnd.Y)
                );

                pbCrop.Invalidate(); // Paint�� �ڸ��� �̸����� ǥ��
                return;
            }

            // --- [������¡ ��] ---
            if (isResizing)
            {
                Point mousePosInParent = pb.Parent.PointToClient(MousePosition);
                int fixedRight = pb.Right;
                int fixedBottom = pb.Bottom;
                int fixedLeft = pb.Left;
                int fixedTop = pb.Top;

                if (resizeDirection.Contains("Right"))
                {
                    pb.Width = Math.Max(20, mousePosInParent.X - fixedLeft);
                }
                if (resizeDirection.Contains("Left"))
                {
                    int newWidth = Math.Max(20, fixedRight - mousePosInParent.X);
                    pb.Left = fixedRight - newWidth;
                    pb.Width = newWidth;
                }
                if (resizeDirection.Contains("Bottom"))
                {
                    pb.Height = Math.Max(20, mousePosInParent.Y - fixedTop);
                }
                if (resizeDirection.Contains("Top"))
                {
                    int newHeight = Math.Max(20, fixedBottom - mousePosInParent.Y);
                    pb.Top = fixedBottom - newHeight;
                    pb.Height = newHeight;
                }
                return;
            }

            // --- [�巡�� ��: �̵�] ---
            if (isDragging)
            {
                Point currentMousePosition = pb.Parent.PointToClient(MousePosition);
                int deltaX = currentMousePosition.X - dragStartMousePosition.X;
                int deltaY = currentMousePosition.Y - dragStartMousePosition.Y;

                foreach (var item in dragStartPositions)
                {
                    PictureBox targetPb = item.Key;
                    Point startPosition = item.Value;
                    targetPb.Location = new Point(startPosition.X + deltaX, startPosition.Y + deltaY);
                }
                return;
            }

            // --- [Ŀ�� ��� ����] ---
            const int edge = 5;
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
            else
            {
                pb.Cursor = currentWorkMode == "�����̵�" ? Cursors.Cross : Cursors.Default;
                resizeDirection = "";
            }
        }






        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                if (currentWorkMode == "��" && currentStroke != null)
                {
                    if (penStrokesMap.TryGetValue(pb, out var strokes))
                    {
                        strokes.Add(currentStroke);
                    }
                    currentStroke = null;
                    isDrawing = false;
                    return;
                }

                if (isResizing)
                {
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    UpdateSelectedImageSize();
                }

                if (currentWorkMode == "���찳")
                {
                    isDrawing = false;
                    return;
                }

                if (currentWorkMode == "������ũ" && isMosaicing)
                {
                    isMosaicing = false;

                    Point end = e.Location;
                    Rectangle rect = NormalizeRectangle(new Rectangle(mosaicStartPoint.X, mosaicStartPoint.Y, end.X - mosaicStartPoint.X, end.Y - mosaicStartPoint.Y));

                    if (!originalImages.ContainsKey(pb)) return;

                    Bitmap before = originalImages[pb];
                    Bitmap working = new Bitmap(pb.Image);

                    ApplyMosaic(working, rect, tbMosaicSize.Value);
                    pb.Image = working;
                    pb.Invalidate();

                    undoStack.Push(new EditAction(pb, before, (Bitmap)working.Clone(), "Mosaic", rect));
                    originalImages.Remove(pb);
                }

                if (currentWorkMode == "�ڸ���" && isCropping && sender is PictureBox pbCrop && pbCrop.Image != null)
                {
                    isCropping = false;

                    Bitmap rendered = new Bitmap(pbCrop.Width, pbCrop.Height);
                    pbCrop.DrawToBitmap(rendered, pbCrop.ClientRectangle); // ��� ���� �����Ͽ� ĸó

                    Rectangle intersected = Rectangle.Intersect(cropRect, new Rectangle(Point.Empty, rendered.Size));
                    if (intersected.Width > 0 && intersected.Height > 0)
                    {
                        Bitmap cropped = rendered.Clone(intersected, rendered.PixelFormat);
                        ShowCroppedImage(cropped); // �� PictureBox ����
                    }

                    pbCrop.Invalidate(); // �ڸ��� ���� ������� �ٽ� �׸���
                    return;
                }

                isDragging = false;
                isResizing = false;
                draggingPictureBox = null;
                resizeDirection = "";

                pb.Invalidate();
            }
        }


        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // ���� �׵θ� �׸���
            if (selectedImages.Contains(pb))
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = (pb == selectedImage) ? DashStyle.Solid : DashStyle.Dot;
                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }

            // �� �� �׸���
            if (penStrokesMap.TryGetValue(pb, out var strokes))
            {
                foreach (var stroke in strokes)
                {
                    using (Pen pen = new Pen(stroke.StrokeColor, stroke.StrokeWidth))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        pen.LineJoin = LineJoin.Round;
                        pen.DashStyle = DashStyle.Solid;
                        if (stroke.Points.Count >= 2)
                        {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            e.Graphics.DrawLines(pen, stroke.Points.ToArray());
                        }
                    }
                }
            }

            // ���� �׸��� ������ ��
            if (isDrawing && currentWorkMode == "��" && currentStroke != null && currentStroke.Points.Count >= 2)
            {
                using (Pen pen = new Pen(currentStroke.StrokeColor, currentStroke.StrokeWidth))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    pen.DashStyle = DashStyle.Solid;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawLines(pen, currentStroke.Points.ToArray());
                }
            }
            if (currentWorkMode == "�ڸ���" && isCropping && cropRect.Width > 0 && cropRect.Height > 0)
            {
                using (Pen cropPen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(cropPen, cropRect);
                }
            }
        }


        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
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
            // [���߰�] �׻� ����� �� �� ������ selectedImage�� �ǵ���!
            selectedImage = basePictureBox;

            // Undo/Redo ���� ����
            emojiUndoStack.Push(CaptureCurrentEmojis(basePictureBox));
            emojiRedoStack.Clear();

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
                g.Clear(Color.White);

                foreach (var pb in pictureBoxes)
                {
                    g.DrawImage(pb.Image, pb.Location);
                }
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
                    case ".jpg": case ".jpeg": format = System.Drawing.Imaging.ImageFormat.Jpeg; break;
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
            combinedImage.Dispose();
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

        int tabNumber;

        private void btnNewTabPage_Click(object sender, EventArgs e)
        {
            // �׻� tabCount�� ����Ͽ� �� �� ����
            TabPage newTabPage = new TabPage($"tp {tabCount}");
            newTabPage.Name = $"tp{tabCount}";
            newTabPage.BackColor = Color.White;
            newTabPage.MouseDown += TabPage_MouseDown;

            newTabPage.MouseDown += TabPage_MouseDown;
            newTabPage.MouseMove += TabPage_MouseMove;
            newTabPage.MouseUp += TabPage_MouseUp;
            newTabPage.Paint += TabPage_Paint;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;

            tabCount++; // ���� �� ��ȣ�� ���� 1 ����
        }

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

                // ���� �ٽ�: �����ִ� �ǵ��� ó������ ������� ��ȣ ������ ����
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}"; // ���̴� �ؽ�Ʈ ����
                    tab.Name = $"tp{i + 1}";   // ���� �̸� ����
                }

                // ������ ������ �� ��ȣ�� ���� �� ���� + 1�� ����
                tabCount = tabControl1.TabPages.Count + 1;
            }
        }

        private Bitmap ResizeImageHighQuality(Image img, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0) return null;

            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return result;
        }

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
                    pb.Image?.Dispose();
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // ���� ũ��� ����
                }
            }

            // UI �ؽ�Ʈ�ڽ��� ���������� ���õ� �̹����� ũ�⸦ ������Ʈ
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
            }
        }

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
                    pb.Image?.Dispose();
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // ���� ũ��� ����
                }
            }

            // UI �ؽ�Ʈ�ڽ��� ���������� ���õ� �̹����� ũ�⸦ ������Ʈ
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
            }
        }


        private void InitializeDynamicControls()
        {
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
                panel.Controls.Add(new Label() { Text = $"���� �Ӽ� {i + 1}", Location = new Point(10, 10) });
                panel.Paint += Panel_Paint;
                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }
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
                btn.Location = new Point(startX + col * (buttonWidth + spacing), buttonStartY + row * (buttonHeight + spacing));
                btn.Tag = i;
                btn.Click += Button_Click;
                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }
            // 1���гΰ�������


            // 8�� �г� ��������
            Panel panel8 = dynamicPanels[7];
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
            int emojiStartY = 50; // �̸�Ƽ�� ����� ���۵� Y ��ġ
            int iconsPerRow = (panel8.Width - emojiPadding * 2) / (iconSize + emojiPadding);

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
                        emojiPreviewImage = ((PictureBox)s).Image;
                        (s as PictureBox).DoDragDrop(((PictureBox)s).Image, DragDropEffects.Copy);
                    }
                };
                panel8.Controls.Add(pic);
            }

            // '����' ��ư ����
            Button btnApplyEmojis = new Button();
            btnApplyEmojis.Text = "����";
            btnApplyEmojis.Size = new Size(100, 30);
            // �г� �ʺ��� �߰��뿡 ��ġ�ϵ��� ���� ���
            btnApplyEmojis.Location = new Point((panel8.Width - btnApplyEmojis.Width * 2 - 10) / 2, 850); // Y ��ġ�� ������ �����ϼ���.
            btnApplyEmojis.Click += BtnApplyEmojis_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
            panel8.Controls.Add(btnApplyEmojis);

            // '����' ��ư ����
            Button btnRemoveLastEmoji = new Button();
            btnRemoveLastEmoji.Text = "�� ����";
            btnRemoveLastEmoji.Size = new Size(100, 30);
            btnRemoveLastEmoji.Location = new Point(btnApplyEmojis.Right + 10, btnApplyEmojis.Top);
            btnRemoveLastEmoji.Click += BtnRemoveLastEmoji_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
            panel8.Controls.Add(btnRemoveLastEmoji);

            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                InitializePanel1(); // 1���г� ����
                currentVisiblePanel.Invalidate();

            }
        }
        //==============================================//==============================================//==============================================//==============================================
        //==============================================//==============================================//==============================================//==============================================
        //==============================================//==============================================//==============================================//==============================================
        // �̹��� �޼ҵ�
        private void InitializePanel1()
        {
            Panel panel1 = dynamicPanels[0];
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
            RadioButton[] modeButtons = new RadioButton[modes.Length];
            for (int i = 0; i < modes.Length; i++)
            {
                int index = i;
                modeButtons[i] = new RadioButton
                {
                    Text = modes[i],
                    Location = new Point(10 + (index % 3) * 80, 20 + (index / 3) * 25),
                    AutoSize = true,
                    Checked = (modes[index] == "�̵�")
                };
                modeButtons[i].CheckedChanged += (s, e) =>
                {
                    if (modeButtons[index].Checked)
                    {
                        currentWorkMode = modeButtons[index].Text;
                        this.Cursor = (currentWorkMode == "�����̵�") ? Cursors.Cross : Cursors.Default;
                    }
                };
                gbModes.Controls.Add(modeButtons[i]);
            }
            panel1.Controls.Add(gbModes);
            currentY = gbModes.Bottom + spacing;

            // 4. �ǵ����� ��ư
            Button btnUndo = new Button { Text = "�ǵ�����", Location = new Point(10, currentY), Width = 100 };
            panel1.Controls.Add(btnUndo);
            currentY = btnUndo.Bottom + spacing;

            // 5. ��/���찳 �Ӽ�
            Label lblPen = new Label { Text = "��/���찳 �Ӽ�", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblPen);
            currentY = lblPen.Bottom + spacing;

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

            Label lblPenSize = new Label { Text = "����", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblPenSize);
            currentY = lblPenSize.Bottom + spacing;

            // 6. ������ũ �Ӽ�
            Label lblMosaic = new Label { Text = "������ũ �Ӽ�", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblMosaic);
            currentY = lblMosaic.Bottom + spacing;

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

            Label lblMosaicBlock = new Label { Text = "��� ũ��", Location = new Point(10, currentY) };
            panel1.Controls.Add(lblMosaicBlock);
            currentY += 40;
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

                    // RGB �� HEX ǥ�� ������Ʈ
                    lblRGB.Text = $"RGB: {selected.R}, {selected.G}, {selected.B}\nHex: #{selected.R:X2}{selected.G:X2}{selected.B:X2}";
                }
            }
        }
        private void TbTransparencyGlobal_ValueChanged(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null)
                return;

            int alpha = tbTransparencyGlobal.Value;

            // ���� ���̴� �̹��� �������� ���纻 ���� (������ �׸� �׸� ���� �ݿ��� ���¸� �����ϱ� ����)
            Bitmap currentImage = selectedImage.Image as Bitmap;
            if (currentImage == null)
                return;

            Bitmap transparentCopy = new Bitmap(currentImage.Width, currentImage.Height);

            // ���� �̹����� �� �ȼ��� ���� ���İ��� ���� ����
            for (int y = 0; y < currentImage.Height; y++)
            {
                for (int x = 0; x < currentImage.Width; x++)
                {
                    Color originalColor = currentImage.GetPixel(x, y);
                    Color transparentColor = Color.FromArgb(alpha, originalColor.R, originalColor.G, originalColor.B);
                    transparentCopy.SetPixel(x, y, transparentColor);
                }
            }

            // ���� �̹��� �޸� ���� �� �� �̹����� ��ü
            selectedImage.Image?.Dispose();
            selectedImage.Image = transparentCopy;

            // ���� �̹����� ���� ���� ��� ����
            imageTransparencyMap[selectedImage] = alpha;
        }

        private void PerformUndo()
        {
            if (undoStack.Count == 0) return;

            EditAction action = undoStack.Pop();

            // Redo�� ���� ���¸� push
            redoStack.Push(new EditAction(
                action.Target,
                (Bitmap)action.Target.Image.Clone(),
                action.Before,
                action.ActionType));

            // �ǵ����� ����
            action.Target.Image?.Dispose();
            action.Target.Image = (Bitmap)action.Before.Clone();
            action.Target.Invalidate();
        }
        private float DistanceFromPointToSegment(Point pt, Point p1, Point p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;

            if (dx == 0 && dy == 0)
                return Distance(pt, p1);

            float t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            float projX = p1.X + t * dx;
            float projY = p1.Y + t * dy;

            return Distance(pt, new Point((int)projX, (int)projY));
        }

        private float Distance(Point a, Point b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// ������Ʈ  poto�Ӽ����� ���� �����ؾ���
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="rect"></param>
        /// <param name="blockSize"></param>
        private unsafe void ApplyMosaic(Bitmap bmp, Rectangle rect, int blockSize)
        {
            // rect�� �̹��� ��踦 ���� �ʵ��� ����
            rect.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height));
            if (rect.IsEmpty) return;

            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);  // 24��Ʈ RGB (�ȼ��� 3����Ʈ)

            byte* ptrBase = (byte*)data.Scan0;
            int stride = data.Stride;

            for (int y = rect.Top; y < rect.Bottom; y += blockSize)
            {
                for (int x = rect.Left; x < rect.Right; x += blockSize)
                {
                    int r = 0, g = 0, b = 0, count = 0;

                    // ��� �� ���
                    for (int j = 0; j < blockSize && (y + j) < rect.Bottom; j++)
                    {
                        if ((y + j) >= bmp.Height) continue;
                        byte* row = ptrBase + (y + j) * stride;
                        for (int i = 0; i < blockSize && (x + i) < rect.Right; i++)
                        {
                            if ((x + i) >= bmp.Width) continue;
                            byte* pixel = row + (x + i) * 3;
                            b += pixel[0];
                            g += pixel[1];
                            r += pixel[2];
                            count++;
                        }
                    }

                    if (count == 0) continue;

                    byte avgR = (byte)(r / count);
                    byte avgG = (byte)(g / count);
                    byte avgB = (byte)(b / count);

                    // ����� ��� ������ ä���
                    for (int j = 0; j < blockSize && (y + j) < rect.Bottom; j++)
                    {
                        if ((y + j) >= bmp.Height) continue;
                        byte* row = ptrBase + (y + j) * stride;
                        for (int i = 0; i < blockSize && (x + i) < rect.Right; i++)
                        {
                            if ((x + i) >= bmp.Width) continue;
                            byte* pixel = row + (x + i) * 3;
                            pixel[0] = avgB;
                            pixel[1] = avgG;
                            pixel[2] = avgR;
                        }
                    }
                }
            }

            bmp.UnlockBits(data);
        }



        private Color GetAverageColor(Bitmap bmp, int x, int y, int w, int h)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    Color c = bmp.GetPixel(x + dx, y + dy);
                    r += c.R;
                    g += c.G;
                    b += c.B;
                    count++;
                }
            }

            if (count == 0) return Color.Black;
            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }
        private void ShowCroppedImage(Bitmap cropped)
        {
            PictureBox pbNew = new PictureBox
            {
                Image = cropped,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 200,
                Height = 200,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(10, this.Height - 250) // �ӽ� ��ġ
            };

            this.Controls.Add(pbNew);
            pbNew.BringToFront();
        }
        private void BtnConfirmCrop_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null || cropRect.Width == 0 || cropRect.Height == 0)
            {
                MessageBox.Show("�ڸ� �̹����� ���� �����ϰ� �巡�׷� �ڸ� ������ �����ϼ���.");
                return;
            }

            Bitmap rendered = new Bitmap(selectedImage.Width, selectedImage.Height);
            selectedImage.DrawToBitmap(rendered, selectedImage.ClientRectangle);

            Rectangle intersected = Rectangle.Intersect(cropRect, new Rectangle(Point.Empty, rendered.Size));
            if (intersected.Width > 0 && intersected.Height > 0)
            {
                Bitmap cropped = rendered.Clone(intersected, rendered.PixelFormat);

                PictureBox pbNew = new PictureBox
                {
                    Image = cropped,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(cropped.Width, cropped.Height),
                    Location = new Point(selectedImage.Right + 20, selectedImage.Top),
                    BorderStyle = BorderStyle.FixedSingle
                };

                EnableDoubleBuffering(pbNew);

                imageList.Add((pbNew, new Bitmap(cropped)));
                imageTransparencyMap[pbNew] = 255;

                // --- �׸� ������ ���� �̺�Ʈ ---
                pbNew.MouseDown += pictureBox_MouseDown;
                pbNew.MouseMove += pictureBox_MouseMove;
                pbNew.MouseUp += pictureBox_MouseUp;
                pbNew.Paint += pictureBox_Paint;

                // --- �̸�Ƽ�� �巡�� ������ ���� �̺�Ʈ ---
                pbNew.AllowDrop = true;
                pbNew.DragEnter += PictureBox_DragEnter;
                pbNew.DragOver += PictureBox_DragOver;
                pbNew.DragDrop += PictureBox_DragDrop;
                pbNew.DragLeave += PictureBox_DragLeave;

                var currentTab = tabControl1.SelectedTab;
                currentTab.Controls.Add(pbNew);
                pbNew.BringToFront();

                selectedImage = pbNew;
                selectedImages.Clear();
                selectedImages.Add(pbNew);

                textBox1.Text = pbNew.Width.ToString();
                textBox2.Text = pbNew.Height.ToString();
                tbTransparencyGlobal.Value = 255;

                pbNew.Invalidate();
            }
        }
        private void ApplyMosaicEffect(PictureBox pb, Rectangle rect, int mosaicSize)
        {
            if (pb.Image == null) return;

            Bitmap bmp = (Bitmap)pb.Image;
            Bitmap backup = (Bitmap)bmp.Clone();

            // [1] ��ǥ ����ȭ (�巡�� �ݴ�� �� ��� ���)
            Rectangle normalizedRect = new Rectangle(
                Math.Max(0, Math.Min(rect.Left, rect.Right)),
                Math.Max(0, Math.Min(rect.Top, rect.Bottom)),
                Math.Abs(rect.Width),
                Math.Abs(rect.Height)
            );

            for (int y = normalizedRect.Top; y < normalizedRect.Bottom; y += mosaicSize)
            {
                for (int x = normalizedRect.Left; x < normalizedRect.Right; x += mosaicSize)
                {
                    int pixelX = Math.Min(x, bmp.Width - 1);
                    int pixelY = Math.Min(y, bmp.Height - 1);
                    Color pixelColor = bmp.GetPixel(pixelX, pixelY);

                    for (int j = 0; j < mosaicSize && y + j < normalizedRect.Bottom && y + j < bmp.Height; j++)
                    {
                        for (int i = 0; i < mosaicSize && x + i < normalizedRect.Right && x + i < bmp.Width; i++)
                        {
                            int drawX = x + i;
                            int drawY = y + j;
                            if (drawX < bmp.Width && drawY < bmp.Height)
                                bmp.SetPixel(drawX, drawY, pixelColor);
                        }
                    }
                }
            }

            undoStack.Push(new EditAction(pb, backup, (Bitmap)bmp.Clone(), "Mosaic", normalizedRect));


            // [3] �̹��� ����
            pb.Image = bmp;
            pb.Invalidate();
        }

        private Rectangle Enlarge(Rectangle rect, int margin)
        {
            return new Rectangle(
                rect.X - margin,
                rect.Y - margin,
                rect.Width + margin * 2,
                rect.Height + margin * 2
            );
        }

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
        private Rectangle NormalizeRectangle(Rectangle rect)
        {
            int x = Math.Min(rect.Left, rect.Right);
            int y = Math.Min(rect.Top, rect.Bottom);
            int width = Math.Abs(rect.Width);
            int height = Math.Abs(rect.Height);
            return new Rectangle(x, y, width, height);
        }
        // �̹��� �޼ҵ�
        //==============================================//==============================================//==============================================//==============================================
        //==============================================//==============================================//==============================================//==============================================
        //==============================================//==============================================//==============================================//==============================================
        private void BtnApplyEmojis_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null)
            {
                MessageBox.Show("���� ��� �̹����� �������ּ���.");
                return;
            }

            // �ڽ� ��Ʈ�� �� PictureBox(�̸�Ƽ��)�� ��������
            var emojiControls = selectedImage.Controls.OfType<PictureBox>().ToList();
            if (emojiControls.Count == 0)
            {
                MessageBox.Show("������ �̸�Ƽ���� �����ϴ�.");
                return;
            }

            // ����ڿ��� �ǵ��� �� ������ ���
            var result = MessageBox.Show("�̸�Ƽ���� �̹����� ���������� �ռ��մϴ�.\n���� �Ŀ��� �̵��ϰų� ������ �� �����ϴ�.\n����Ͻðڽ��ϱ�?", "Ȯ��", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return;
            }

            // ���� ��Ʈ���� ������� �� ��Ʈ�� ���� (���⿡ �׸�)
            Bitmap newBitmap = new Bitmap(selectedImage.Image);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                // ��� �̸�Ƽ�� ��Ʈ���� ��ȸ�ϸ� ��Ʈ�ʿ� �׸���
                foreach (PictureBox emoji in emojiControls)
                {
                    g.DrawImage(emoji.Image, emoji.Bounds); // Bounds�� Location�� Size�� ��� ����
                }
            }

            // �ռ��� �̹����� ��ü
            selectedImage.Image = newBitmap;

            // Tag�� ����� ���� �̹����� �ֽ�ȭ (�ſ� �߿�!)
            // �̷��� �ؾ� ���߿� �� �ٸ� �ռ��� �ص� ���� ������ ������
            if (selectedImage.Tag is Bitmap oldBitmap)
            {
                oldBitmap.Dispose();
            }
            selectedImage.Tag = new Bitmap(newBitmap);


            // ����� ���� �̸�Ƽ�� ��Ʈ�ѵ��� ��� ����
            foreach (var control in emojiControls)
            {
                selectedImage.Controls.Remove(control);
                control.Dispose();
            }
            selectedEmoji = null; // ���õ� �̸�Ƽ�� ���� ����

            MessageBox.Show("������ �Ϸ�Ǿ����ϴ�.");
        }

        /// <summary>
        /// '������ �׸� ����' ��ư Ŭ�� ��, ���� �������� �߰��� �̸�Ƽ���� �����մϴ�.
        /// </summary>
        private void BtnRemoveLastEmoji_Click(object sender, EventArgs e)
        {
            if (selectedImage != null)
            {
                emojiUndoStack.Push(CaptureCurrentEmojis(selectedImage));
                emojiRedoStack.Clear();
            }
            if (selectedImage == null)
            {
                MessageBox.Show("���� �۾��� �̹����� �������ּ���.");
                return;
            }

            // �ڽ� ��Ʈ�� �� PictureBox(�̸�Ƽ��)�� ã��
            var lastEmoji = selectedImage.Controls.OfType<PictureBox>().LastOrDefault();

            if (lastEmoji != null)
            {
                // ��Ʈ�� ��Ͽ��� �����ϰ� ���ҽ� ����
                selectedImage.Controls.Remove(lastEmoji);
                lastEmoji.Dispose();
            }
            else
            {
                MessageBox.Show("������ �̸�Ƽ���� �����ϴ�.");
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag;
                if (index >= dynamicPanels.Length) return;

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
                if (previousVisiblePanel != null) previousVisiblePanel.Invalidate();
                if (currentVisiblePanel != null) currentVisiblePanel.Invalidate();
            }
        }

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

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
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
            }
        }

        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            var tab = sender as TabPage;
            if (tab == null) return;

            // Ŭ���� ��ġ�� "��� �̹���(Background Image)"�� �ִ��� �˻�
            PictureBox clickedBackground = null;
            foreach (Control c in tab.Controls)
            {
                if (c is PictureBox pb && pb.Bounds.Contains(e.Location))
                {
                    clickedBackground = pb;
                    break;
                }
            }

            // === [1] "�̹��� �ٱ�" Ŭ��: ��� �̸�Ƽ�� ���� ���� ===
            if (clickedBackground == null)
            {
                foreach (Control c in tab.Controls)
                {
                    if (c is PictureBox bgPb) // ��� PictureBox
                    {
                        foreach (Control ec in bgPb.Controls) // �̸�Ƽ�� PictureBox
                        {
                            if (ec is PictureBox emojiPb)
                            {
                                emojiPb.Tag = null;
                                emojiPb.Invalidate();
                            }
                        }
                    }
                }
                selectedEmoji = null;
            }
            // === [2] "��� �̹���" ������ �� ��(�̸�Ƽ��X) Ŭ��: �� ����� �̸�Ƽ�ܸ� ���� ���� ===
            else
            {
                // ��� �̹��� ������ �̸�Ƽ�� ��Ʈ�� ��, Ŭ���� ��ġ�� ��ġ�� �� �ִ��� Ȯ��
                bool emojiClicked = false;
                foreach (Control ec in clickedBackground.Controls)
                {
                    if (ec is PictureBox emojiPb && emojiPb.Bounds.Contains(
                        clickedBackground.PointToClient(tab.PointToScreen(e.Location))))
                    {
                        emojiClicked = true;
                        break;
                    }
                }
                // ���� � �̸�Ƽ�ܵ� Ŭ������ �ʾҴٸ� -> �ش� �̹����� �̸�Ƽ�� ���� ����!
                if (!emojiClicked)
                {
                    foreach (Control ec in clickedBackground.Controls)
                    {
                        if (ec is PictureBox emojiPb)
                        {
                            emojiPb.Tag = null;
                            emojiPb.Invalidate();
                        }
                    }
                    selectedEmoji = null;
                }
                // (�̸�Ƽ���� Ŭ�������� ���� ó�� ����: Emoji_MouseDown���� ó��)
            }

            // �巡�� ���� �簢���� ���� �ڵ�ó�� ���ܵθ� ��!
            if (e.Button == MouseButtons.Left)
            {
                isMarqueeSelecting = true;
                marqueeStartPoint = e.Location;
            }
        }



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
        private void TabPage_MouseUp(object sender, MouseEventArgs e)
        {
            TabPage currentTab = sender as TabPage;
            if (currentTab == null) return;

            // �巡�� ���� ���� ����
            isMarqueeSelecting = false;

            // �巡�׷� ������� �簢���� ���� ������(�ܼ� Ŭ������ ����)
            if (marqueeRect.Width < 5 && marqueeRect.Height < 5)
            {
                // ��� ������ ����
                foreach (var item in selectedImages) { item.Invalidate(); }
                selectedImages.Clear();
                selectedImage = null;
            }
            else
            {
                // �巡�� ���� ������ ��ġ�� ��� PictureBox�� ã�Ƽ� ���� ���¸� ���
                foreach (PictureBox pb in currentTab.Controls.OfType<PictureBox>())
                {
                    if (marqueeRect.IntersectsWith(pb.Bounds))
                    {
                        if (selectedImages.Contains(pb))
                        {
                            selectedImages.Remove(pb); // �̹� ���õ����� ����
                        }
                        else
                        {
                            selectedImages.Add(pb); // ���� �ȵ����� �߰�
                        }
                    }
                }

                // ���������� ���õ� �̹����� ��ǥ �̹����� ����
                selectedImage = selectedImages.LastOrDefault();

                // �ؽ�Ʈ�ڽ� ������Ʈ
                if (selectedImage != null)
                {
                    textBox1.Text = selectedImage.Width.ToString();
                    textBox2.Text = selectedImage.Height.ToString();
                }

                // ��� PictureBox�� �ٽ� �׷��� �׵θ� ���� ������Ʈ
                foreach (var pb in currentTab.Controls.OfType<PictureBox>())
                {
                    pb.Invalidate();
                }
            }

            // ȭ�鿡 �����ִ� ���� �簢���� ����� ���� ���������� Invalidate ȣ��
            marqueeRect = Rectangle.Empty;
            currentTab.Invalidate();
        }
        // TabPage�� �ٽ� �׷��� �� �� (Invalidate ȣ�� ��)
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

        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            // ���õ� ��� �̹����� ����
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    pb.Size = pb.Image.Size;
                    pb.Invalidate();
                }
            }
        }

        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            // ���õ� ��� �̹����� ����
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    pb.Size = pb.Image.Size;
                    pb.Invalidate();
                }
            }
        }

        private void UpdateSelectedImageSize()
        {
            if (selectedImages.Count == 0) return;

            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                if (width <= 0 || height <= 0) return;
                width = Math.Max(16, Math.Min(4000, width));
                height = Math.Max(16, Math.Min(4000, height));

                // ���õ� ��� �̹����� ũ�� ����
                foreach (var pb in selectedImages)
                {
                    if (pb.Tag is Bitmap originalBitmap)
                    {
                        Bitmap resized = ResizeImageHighQuality(originalBitmap, new Size(width, height));
                        if (resized == null) continue; // �������� ���� �� �ǳʶٱ�

                        pb.Image?.Dispose();
                        pb.Image = resized;
                        pb.Size = new Size(width, height);
                        pb.Invalidate();
                    }
                }

                // �ؽ�Ʈ�ڽ� ���� ������ ������ ������Ʈ
                if (textBox1.Text != width.ToString()) textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString()) textBox2.Text = height.ToString();
            }
        }

        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text)) textBox1.Text = selectedImage?.Width.ToString() ?? "100";
            if (!int.TryParse(textBox1.Text, out int val)) val = 100;
            int corrected = Math.Max(16, Math.Min(4000, val));
            if (textBox1.Text != corrected.ToString()) textBox1.Text = corrected.ToString();
            UpdateSelectedImageSize();
        }

        private void textBox2_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text)) textBox2.Text = selectedImage?.Height.ToString() ?? "100";
            if (!int.TryParse(textBox2.Text, out int val)) val = 100;
            int corrected = Math.Max(16, Math.Min(4000, val));
            if (textBox2.Text != corrected.ToString()) textBox2.Text = corrected.ToString();
            UpdateSelectedImageSize();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.RotateNoneFlipX); // �¿� ����
                    pb.Invalidate(); // ��������� ȭ�鿡 �ݿ�
                }
            }
        }
    }
    public class EmojiState
    {
        public Image Image { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
    }
}