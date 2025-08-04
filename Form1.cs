using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
        // =======================================================
        // 이미지 편집 및 UI 관련 변수들
        // =======================================================

        // --- 이미지 이동 및 선택 관련 변수 ---
        private bool isDragging = false;
        private Point clickOffset;
        private bool showSelectionBorder = false;

        // --- UI 컨트롤 관련 변수 ---
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        private Panel currentVisiblePanel = null;
        private TrackBar opacityTrackBar;
        private Button undoButton;
        private Button cropButton; // 자르기 버튼 추가

        // --- 원본 이미지 및 투명도 관련 변수 ---
        private Image originalImage;
        private float imageOpacity = 1.0f;

        // --- 스포이드 기능 관련 변수 ---
        private bool isEyedropperMode = false;
        private Panel eyedropperPreviewPanel;
        private Label eyedropperInfoLabel;

        // --- 펜 색상 미리보기 관련 변수 ---
        private Panel penColorPreviewPanel;
        private Label penColorInfoLabel;

        // --- 그리기/지우기 기능 관련 변수 ---
        private bool isDrawingMode = false;
        private bool isErasingMode = false;
        private bool isPainting = false;

        private Color penColor = Color.Black;
        private int penWidth = 5;

        // 그려진 선 정보를 담는 클래스
        public class DrawnStroke
        {
            public List<Point> Points { get; set; }
            public Color PenColor { get; set; }
            public int PenWidth { get; set; }
        }
        private List<DrawnStroke> drawnStrokes = new List<DrawnStroke>();
        private List<Point> currentStrokePoints = null;
        private List<DrawnStroke> erasedStrokesInSession = new List<DrawnStroke>();

        // --- 모자이크 기능 관련 변수 ---
        private bool isMosaicMode = false;
        private int mosaicPixelSize = 15;
        private Point startDragPoint;
        private Rectangle currentMosaicRect;

        // 모자이크 정보를 담는 클래스
        public class DrawnMosaic
        {
            public Rectangle Rect { get; set; }
            public int PixelSize { get; set; }
        }
        private List<DrawnMosaic> drawnMosaics = new List<DrawnMosaic>();

        // --- [사진 자르기 기능 추가] ---
        private bool isCropMode = false;
        private bool isDraggingForCrop = false;
        private Rectangle currentCropRect = Rectangle.Empty;
        private Point startCropPoint;
        private PictureBox lastCroppedPictureBox = null;

        // --- [실행 취소(Undo) 기능] ---
        private Stack<EditAction> undoStack = new Stack<EditAction>();



        public abstract class EditAction
        {
            public abstract void Undo(Form1 form);
        }

        public class DrawStrokeAction : EditAction
        {
            private DrawnStroke stroke;
            public DrawStrokeAction(DrawnStroke stroke) { this.stroke = stroke; }
            public override void Undo(Form1 form) { form.drawnStrokes.Remove(stroke); }
        }

        public class MosaicAction : EditAction
        {
            private DrawnMosaic mosaic;
            public MosaicAction(DrawnMosaic mosaic) { this.mosaic = mosaic; }
            public override void Undo(Form1 form) { form.drawnMosaics.Remove(mosaic); }
        }

        public class EraseAction : EditAction
        {
            private List<DrawnStroke> erasedStrokes;
            public EraseAction(List<DrawnStroke> erasedStrokes) { this.erasedStrokes = erasedStrokes; }
            public override void Undo(Form1 form) { form.drawnStrokes.AddRange(erasedStrokes); }
        }

        // [사진 자르기 기능] 자르기 작업을 저장하는 클래스
        public class CropAction : EditAction
        {
            public Image OriginalImageBeforeCrop { get; private set; }
            public List<DrawnStroke> DrawnStrokesBeforeCrop { get; private set; }
            public List<DrawnMosaic> DrawnMosaicsBeforeCrop { get; private set; }
            public PictureBox CroppedPictureBox { get; set; } // private set 제거

            public CropAction(Image originalImage, List<DrawnStroke> drawnStrokes, List<DrawnMosaic> drawnMosaics, PictureBox croppedPictureBox)
            {
                this.OriginalImageBeforeCrop = (Image)originalImage.Clone();
                this.DrawnStrokesBeforeCrop = new List<DrawnStroke>(drawnStrokes);
                this.DrawnMosaicsBeforeCrop = new List<DrawnMosaic>(drawnMosaics);
                this.CroppedPictureBox = croppedPictureBox;
            }

            public override void Undo(Form1 form)
            {
                form.originalImage?.Dispose();
                form.originalImage = (Image)this.OriginalImageBeforeCrop.Clone();
                form.drawnStrokes = new List<DrawnStroke>(this.DrawnStrokesBeforeCrop);
                form.drawnMosaics = new List<DrawnMosaic>(this.DrawnMosaicsBeforeCrop);

                if (this.CroppedPictureBox != null && form.Controls.Contains(this.CroppedPictureBox))
                {
                    form.Controls.Remove(this.CroppedPictureBox);
                    this.CroppedPictureBox.Dispose();

                    if (form.lastCroppedPictureBox == this.CroppedPictureBox)
                    {
                        form.lastCroppedPictureBox = null;
                    }
                }

                form.pictureBox1.Size = form.originalImage.Size;
                form.pictureBox1.Invalidate();
            }
        }
        

        // =======================================================
        // 초기화 및 로드 관련 메서드
        // =======================================================

        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();
            pictureBox1.Paint += pictureBox1_Paint;
            this.WindowState = FormWindowState.Maximized;
            pictureBox1.MouseEnter += PictureBox1_MouseEnter;
            pictureBox1.MouseLeave += PictureBox1_MouseLeave;
            this.DoubleBuffered = true;
        }

        private void InitializeDynamicControls()
        {
            // 동적으로 버튼 생성
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int startY = 95;
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
                btn.Location = new Point(startX + col * (buttonWidth + spacing), startY + row * (buttonHeight + spacing));
                btn.Tag = i;
                btn.Click += Button_Click;
                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // 동적으로 패널 생성
            int panelCount = 10;
            dynamicPanels = new Panel[panelCount];
            Point panelLocation = new Point(1600, 90);
            Size panelSize = new Size(300, 900);

            for (int i = 0; i < panelCount; i++)
            {
                Panel panel = new Panel() { Location = panelLocation, Size = panelSize, Visible = false };
                panel.Controls.Add(new Label() { Text = $"편집 속성 {i + 1}", Location = new Point(10, 10) });

                if (i == 0)
                {
                    opacityTrackBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Location = new Point(10, 40), Size = new Size(200, 40) };
                    opacityTrackBar.Scroll += OpacityTrackBar_Scroll;
                    panel.Controls.Add(opacityTrackBar);

                    Button penColorSelectorButton = new Button { Text = "색상 선택", Location = new Point(10, opacityTrackBar.Bottom + 10), Size = new Size(100, 30) };
                    penColorSelectorButton.Click += PenColorSelectorButton_Click;
                    panel.Controls.Add(penColorSelectorButton);

                    penColorPreviewPanel = new Panel { Location = new Point(penColorSelectorButton.Right + 10, penColorSelectorButton.Top), Size = new Size(50, 30), BorderStyle = BorderStyle.FixedSingle, BackColor = penColor };
                    panel.Controls.Add(penColorPreviewPanel);

                    penColorInfoLabel = new Label { Location = new Point(penColorPreviewPanel.Right + 10, penColorPreviewPanel.Top), Size = new Size(100, 30), Text = $"RGB: {penColor.R}, {penColor.G}, {penColor.B}\nHex: #{penColor.R:X2}{penColor.G:X2}{penColor.B:X2}", BorderStyle = BorderStyle.None, AutoSize = false, TextAlign = ContentAlignment.TopLeft };
                    panel.Controls.Add(penColorInfoLabel);

                    eyedropperPreviewPanel = new Panel { Location = new Point(10, penColorPreviewPanel.Bottom + 10), Size = new Size(50, 30), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
                    panel.Controls.Add(eyedropperPreviewPanel);

                    eyedropperInfoLabel = new Label { Location = new Point(eyedropperPreviewPanel.Right + 10, eyedropperPreviewPanel.Top), Size = new Size(100, 30), Text = "RGB:\nHex:", BorderStyle = BorderStyle.None, AutoSize = false, TextAlign = ContentAlignment.TopLeft };
                    panel.Controls.Add(eyedropperInfoLabel);

                    GroupBox modeGroupBox = new GroupBox
                    {
                        Text = "작업 모드",
                        Location = new Point(10, eyedropperPreviewPanel.Bottom + 10),
                        Size = new Size(280, 130)
                    };
                    panel.Controls.Add(modeGroupBox);

                    RadioButton moveModeRadioButton = new RadioButton { Text = "이동", Location = new Point(10, 20), Checked = true, AutoSize = true };
                    moveModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(moveModeRadioButton);

                    RadioButton eyedropperModeRadioButton = new RadioButton { Text = "스포이드", Location = new Point(moveModeRadioButton.Right + 10, 20), AutoSize = true };
                    eyedropperModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(eyedropperModeRadioButton);

                    RadioButton penModeRadioButton = new RadioButton { Text = "펜", Location = new Point(eyedropperModeRadioButton.Right + 10, 20), AutoSize = true };
                    penModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(penModeRadioButton);

                    RadioButton eraserModeRadioButton = new RadioButton { Text = "지우개", Location = new Point(10, penModeRadioButton.Bottom + 10), AutoSize = true };
                    eraserModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(eraserModeRadioButton);

                    RadioButton mosaicModeRadioButton = new RadioButton { Text = "모자이크", Location = new Point(eraserModeRadioButton.Right + 10, penModeRadioButton.Bottom + 10), AutoSize = true };
                    mosaicModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(mosaicModeRadioButton);

                    RadioButton cropModeRadioButton = new RadioButton { Text = "자르기", Location = new Point(mosaicModeRadioButton.Right + 10, penModeRadioButton.Bottom + 10), AutoSize = true };
                    cropModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(cropModeRadioButton);

                    undoButton = new Button();
                    undoButton.Text = "되돌리기";
                    undoButton.Size = new Size(80, 40);
                    undoButton.Location = new Point(modeGroupBox.Left, modeGroupBox.Bottom + 10);
                    undoButton.Click += UndoButton_Click;
                    panel.Controls.Add(undoButton);

                    cropButton = new Button();
                    cropButton.Text = "자르기 실행";
                    cropButton.Size = new Size(100, 40);
                    cropButton.Location = new Point(undoButton.Right + 10, undoButton.Top);
                    cropButton.Click += CropButton_Click;
                    cropButton.Visible = false;
                    panel.Controls.Add(cropButton);
                }
                else if (i == 1)
                {
                    panel.Controls.Add(new Label { Text = "펜/지우개 속성", Location = new Point(10, 40), AutoSize = true });
                    TrackBar penWidthTrackBar = new TrackBar { Minimum = 1, Maximum = 50, Value = 5, TickFrequency = 5, Location = new Point(10, 60), Size = new Size(200, 45) };
                    penWidthTrackBar.Scroll += PenWidthTrackBar_Scroll;
                    panel.Controls.Add(new Label { Text = "굵기", Location = new Point(10, penWidthTrackBar.Bottom), AutoSize = true });
                    panel.Controls.Add(penWidthTrackBar);

                    panel.Controls.Add(new Label { Text = "모자이크 속성", Location = new Point(10, penWidthTrackBar.Bottom + 50), AutoSize = true });
                    TrackBar mosaicPixelSizeTrackBar = new TrackBar { Minimum = 5, Maximum = 50, Value = mosaicPixelSize, TickFrequency = 5, Location = new Point(10, penWidthTrackBar.Bottom + 70), Size = new Size(200, 45) };
                    mosaicPixelSizeTrackBar.Scroll += MosaicPixelSizeTrackBar_Scroll;
                    panel.Controls.Add(new Label { Text = "블록 크기", Location = new Point(10, mosaicPixelSizeTrackBar.Bottom), AutoSize = true });
                    panel.Controls.Add(mosaicPixelSizeTrackBar);
                }

                panel.Paint += Panel_Paint;
                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // =======================================================
        // 이벤트 핸들러 메서드들
        // =======================================================

        private void ModeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;

            isDrawingMode = false;
            isErasingMode = false;
            isDragging = false;
            isEyedropperMode = false;
            isMosaicMode = false;
            isCropMode = false;

            switch (rb.Text)
            {
                case "이동": break;
                case "스포이드": isEyedropperMode = true; break;
                case "펜": isDrawingMode = true; break;
                case "지우개": isErasingMode = true; break;
                case "모자이크": isMosaicMode = true; break;
                case "자르기": isCropMode = true; break;
            }

            if (cropButton != null)
            {
                cropButton.Visible = isCropMode;
            }

            if (!isCropMode)
            {
                currentCropRect = Rectangle.Empty;
                pictureBox1.Invalidate();
            }

            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (isEyedropperMode || isDrawingMode || isErasingMode || isMosaicMode || isCropMode)
            {
                pictureBox1.Cursor = Cursors.Cross;
            }
            else
            {
                pictureBox1.Cursor = Cursors.Default;
            }
        }

        private void OpacityTrackBar_Scroll(object sender, EventArgs e)
        {
            imageOpacity = ((TrackBar)sender).Value / 100f;
            pictureBox1.Invalidate();
        }

        private void MosaicPixelSizeTrackBar_Scroll(object sender, EventArgs e)
        {
            mosaicPixelSize = ((TrackBar)sender).Value;
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
                    if (currentVisiblePanel != null) currentVisiblePanel.Visible = false;
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }
                if (previousVisiblePanel != null) previousVisiblePanel.Invalidate();
                if (currentVisiblePanel != null) currentVisiblePanel.Invalidate();
            }
        }

        private void PenWidthTrackBar_Scroll(object sender, EventArgs e)
        {
            penWidth = ((TrackBar)sender).Value;
        }

        private void PenColorSelectorButton_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                penColor = colorDialog.Color;
                penColorPreviewPanel.BackColor = penColor;
                penColorInfoLabel.Text = $"RGB: {penColor.R}, {penColor.G}, {penColor.B}\nHex: #{penColor.R:X2}{penColor.G:X2}{penColor.B:X2}";
            }
        }

        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 열기";
            openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    drawnStrokes.Clear();
                    drawnMosaics.Clear();
                    undoStack.Clear();

                    pictureBox1.Image?.Dispose();
                    originalImage?.Dispose();

                    Image img = Image.FromFile(openFileDialog.FileName);
                    originalImage = (Image)img.Clone();
                    img.Dispose();

                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = originalImage.Size;
                    pictureBox1.Location = new Point(10, 10);

                    pictureBox1.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생: " + ex.Message);
                }
            }
        }

        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            originalImage?.Dispose();
            originalImage = null;
            drawnStrokes.Clear();
            drawnMosaics.Clear();
            undoStack.Clear();
            pictureBox1.Invalidate();
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            // TODO: 저장 기능 구현
        }

        private void PictureBox1_MouseEnter(object sender, EventArgs e)
        {
            UpdateCursor();
        }

        private void PictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Cursor = Cursors.Default;
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel paintedPanel = sender as Panel;
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        private void UndoButton_Click(object sender, EventArgs e)
        {
            if (undoStack.Count > 0)
            {
                EditAction lastAction = undoStack.Pop();
                lastAction.Undo(this);
                pictureBox1.Invalidate();
            }
        }

        private void CropButton_Click(object sender, EventArgs e)
        {
            PerformCrop();
        }

        // =======================================================
        // 마우스 이벤트 핸들러
        // =======================================================

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || originalImage == null) return;

            if (isDrawingMode)
            {
                isPainting = true;
                currentStrokePoints = new List<Point> { e.Location };
            }
            else if (isErasingMode)
            {
                isPainting = true;
                erasedStrokesInSession.Clear();
                EraseStrokesAt(e.Location);
            }
            else if (isEyedropperMode)
            {
                if (e.X >= 0 && e.X < originalImage.Width && e.Y >= 0 && e.Y < originalImage.Height)
                {
                    Bitmap bmp = CreateCombinedBitmap();
                    if (bmp != null)
                    {
                        Color pixelColor = bmp.GetPixel(e.X, e.Y);
                        penColor = pixelColor;
                        penColorPreviewPanel.BackColor = penColor;
                        penColorInfoLabel.Text = $"RGB: {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\nHex: #{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}";
                        bmp.Dispose();
                    }
                }
            }
            else if (isMosaicMode)
            {
                isPainting = true;
                startDragPoint = e.Location;
                currentMosaicRect = new Rectangle(e.Location, new Size(0, 0));
            }
            else if (isCropMode)
            {
                isDraggingForCrop = true;
                startCropPoint = e.Location;
                currentCropRect = new Rectangle(e.Location, new Size(0, 0));
            }
            else // 이동 모드일 때
            {
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPainting && isDrawingMode)
            {
                currentStrokePoints.Add(e.Location);
                pictureBox1.Invalidate();
            }
            else if (isPainting && isErasingMode)
            {
                EraseStrokesAt(e.Location);
            }
            else if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;
                pictureBox1.Location = newLocation;
            }
            else if (isEyedropperMode)
            {
                if (originalImage != null && e.X >= 0 && e.X < originalImage.Width && e.Y >= 0 && e.Y < originalImage.Height)
                {
                    Bitmap bmp = CreateCombinedBitmap();
                    if (bmp != null)
                    {
                        Color pixelColor = bmp.GetPixel(e.X, e.Y);
                        eyedropperPreviewPanel.BackColor = pixelColor;
                        eyedropperInfoLabel.Text = $"RGB: {pixelColor.R}, {pixelColor.G}, {pixelColor.B}\nHex: #{pixelColor.R:X2}{pixelColor.G:X2}{pixelColor.B:X2}";
                        bmp.Dispose();
                    }
                }
            }
            else if (isPainting && isMosaicMode)
            {
                int x = Math.Min(startDragPoint.X, e.X);
                int y = Math.Min(startDragPoint.Y, e.Y);
                int width = Math.Abs(startDragPoint.X - e.X);
                int height = Math.Abs(startDragPoint.Y - e.Y);
                currentMosaicRect = new Rectangle(x, y, width, height);
                pictureBox1.Invalidate();
            }
            else if (isDraggingForCrop)
            {
                int x = Math.Min(startCropPoint.X, e.X);
                int y = Math.Min(startCropPoint.Y, e.Y);
                int width = Math.Abs(startCropPoint.X - e.X);
                int height = Math.Abs(startCropPoint.Y - e.Y);
                currentCropRect = new Rectangle(x, y, width, height);
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isPainting && isDrawingMode)
            {
                if (currentStrokePoints != null && currentStrokePoints.Count > 1)
                {
                    var newStroke = new DrawnStroke
                    {
                        Points = new List<Point>(currentStrokePoints),
                        PenColor = penColor,
                        PenWidth = penWidth
                    };
                    drawnStrokes.Add(newStroke);
                    undoStack.Push(new DrawStrokeAction(newStroke));
                }
            }
            else if (isPainting && isErasingMode)
            {
                if (erasedStrokesInSession.Count > 0)
                {
                    undoStack.Push(new EraseAction(new List<DrawnStroke>(erasedStrokesInSession)));
                    erasedStrokesInSession.Clear();
                }
            }
            else if (isPainting && isMosaicMode)
            {
                if (currentMosaicRect.Width > 0 && currentMosaicRect.Height > 0)
                {
                    var newMosaic = new DrawnMosaic { Rect = currentMosaicRect, PixelSize = mosaicPixelSize };
                    drawnMosaics.Add(newMosaic);
                    undoStack.Push(new MosaicAction(newMosaic));
                }
            }
            else if (isDraggingForCrop)
            {
                isDraggingForCrop = false;
                pictureBox1.Invalidate();
            }

            isPainting = false;
            currentStrokePoints = null;
            isDragging = false;
            showSelectionBorder = false;

            pictureBox1.Invalidate();
        }

        // =======================================================
        // 그리기 및 모자이크 관련 메서드
        // =======================================================

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 기존 이미지 그리기
            if (originalImage != null)
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = imageOpacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(originalImage, new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                                 0, 0, originalImage.Width, originalImage.Height,
                                 GraphicsUnit.Pixel, attributes);
            }

            // 모자이크 그리기
            foreach (var mosaic in drawnMosaics)
            {
                ApplyMosaic(g, mosaic.Rect, mosaic.PixelSize);
            }
            if (isPainting && isMosaicMode)
            {
                ApplyMosaic(g, currentMosaicRect, mosaicPixelSize);
            }

            // 펜 선 그리기
            foreach (var stroke in drawnStrokes)
            {
                if (stroke.Points.Count > 1)
                {
                    using (Pen currentPen = new Pen(stroke.PenColor, stroke.PenWidth))
                    {
                        currentPen.StartCap = currentPen.EndCap = LineCap.Round;
                        currentPen.LineJoin = LineJoin.Round;
                        g.DrawLines(currentPen, stroke.Points.ToArray());
                    }
                }
            }
            if (isPainting && isDrawingMode && currentStrokePoints != null && currentStrokePoints.Count > 1)
            {
                using (Pen currentPen = new Pen(penColor, penWidth))
                {
                    currentPen.StartCap = currentPen.EndCap = LineCap.Round;
                    currentPen.LineJoin = LineJoin.Round;
                    g.DrawLines(currentPen, currentStrokePoints.ToArray());
                }
            }

            // 이동 모드 테두리
            if (showSelectionBorder)
            {
                using (Pen borderPen = new Pen(Color.DeepSkyBlue, 2))
                {
                    borderPen.DashStyle = DashStyle.Solid;
                    g.DrawRectangle(borderPen, 0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);
                }
            }

            // [사진 자르기 기능] 자르기 영역 그리기
            if (isCropMode && !currentCropRect.IsEmpty)
            {
                using (Brush semiTransparentBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
                {
                    Region fullRegion = new Region(new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                    fullRegion.Xor(currentCropRect);
                    g.FillRegion(semiTransparentBrush, fullRegion);
                }

                using (Pen cropPen = new Pen(Color.White, 2))
                {
                    g.DrawRectangle(cropPen, currentCropRect);
                }
            }
        }

        private void ApplyMosaic(Graphics g, Rectangle rect, int pixelSize)
        {
            if (originalImage == null || rect.Width <= 0 || rect.Height <= 0) return;

            using (Bitmap tempBitmap = new Bitmap(rect.Width, rect.Height))
            {
                using (Graphics tempG = Graphics.FromImage(tempBitmap))
                {
                    tempG.DrawImage(originalImage, new Rectangle(0, 0, rect.Width, rect.Height),
                                                 rect, GraphicsUnit.Pixel);
                }

                for (int y = 0; y < tempBitmap.Height; y += pixelSize)
                {
                    for (int x = 0; x < tempBitmap.Width; x += pixelSize)
                    {
                        Color avgColor = GetAverageColor(tempBitmap, x, y, pixelSize);

                        using (SolidBrush brush = new SolidBrush(avgColor))
                        {
                            g.FillRectangle(brush, rect.X + x, rect.Y + y, pixelSize, pixelSize);
                        }
                    }
                }
            }
        }

        private Color GetAverageColor(Bitmap bmp, int x, int y, int size)
        {
            long r = 0, g = 0, b = 0;
            int count = 0;

            for (int sy = y; sy < y + size && sy < bmp.Height; sy++)
            {
                for (int sx = x; sx < x + size && sx < bmp.Width; sx++)
                {
                    Color pixel = bmp.GetPixel(sx, sy);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    count++;
                }
            }

            if (count == 0) return Color.Transparent;

            return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
        }

        private Bitmap CreateCombinedBitmap()
        {
            if (originalImage == null) return null;

            Bitmap combinedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                // 원본 이미지 그리기 (투명도 적용)
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = imageOpacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(originalImage, new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                                 0, 0, originalImage.Width, originalImage.Height,
                                 GraphicsUnit.Pixel, attributes);

                // 모자이크 그리기
                foreach (var mosaic in drawnMosaics)
                {
                    ApplyMosaic(g, mosaic.Rect, mosaic.PixelSize);
                }

                // 펜 선 그리기
                foreach (var stroke in drawnStrokes)
                {
                    if (stroke.Points.Count > 1)
                    {
                        using (Pen currentPen = new Pen(stroke.PenColor, stroke.PenWidth))
                        {
                            currentPen.StartCap = currentPen.EndCap = LineCap.Round;
                            currentPen.LineJoin = LineJoin.Round;
                            g.DrawLines(currentPen, stroke.Points.ToArray());
                        }
                    }
                }
            }
            return combinedBitmap;
        }

        private void EraseStrokesAt(Point location)
        {
            var strokesToRemove = new List<DrawnStroke>();
            for (int i = drawnStrokes.Count - 1; i >= 0; i--)
            {
                DrawnStroke stroke = drawnStrokes[i];
                foreach (Point p in stroke.Points)
                {
                    double distance = Math.Sqrt(Math.Pow(p.X - location.X, 2) + Math.Pow(p.Y - location.Y, 2));
                    if (distance < penWidth)
                    {
                        strokesToRemove.Add(stroke);
                        break;
                    }
                }
            }

            foreach (var stroke in strokesToRemove)
            {
                drawnStrokes.Remove(stroke);
                erasedStrokesInSession.Add(stroke);
            }

            pictureBox1.Invalidate();
        }

        // [사진 자르기 기능] 자르기 로직
        private void PerformCrop()
        {
            if (currentCropRect.IsEmpty)
            {
                MessageBox.Show("자르기 영역을 먼저 선택해 주세요.");
                return;
            }

            // 원본 이미지, 펜, 모자이크를 합친 최종 Bitmap 생성 (이 시점의 상태를 저장)
            Bitmap combinedBitmapForUndo = CreateCombinedBitmap();

            // Undo 스택에 CropAction 객체를 추가합니다.
            // 아직 새 PictureBox가 생성되지 않았으므로 null을 전달합니다.
            var cropAction = new CropAction(originalImage, drawnStrokes, drawnMosaics, null);
            undoStack.Push(cropAction);

            // 선택된 영역을 잘라낸 Bitmap 생성
            Bitmap croppedBitmap = new Bitmap(currentCropRect.Width, currentCropRect.Height);
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(combinedBitmapForUndo, 0, 0, currentCropRect, GraphicsUnit.Pixel);
            }

            // 새로운 PictureBox 생성 및 설정
            PictureBox croppedPictureBox = new PictureBox();
            croppedPictureBox.Image = croppedBitmap;
            croppedPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;

            // 위치 계산 로직
            Point newLocation;
            if (lastCroppedPictureBox == null)
            {
                newLocation = new Point(pictureBox1.Location.X + pictureBox1.Width + 10, pictureBox1.Location.Y);
            }
            else
            {
                newLocation = new Point(lastCroppedPictureBox.Location.X + lastCroppedPictureBox.Width + 10, lastCroppedPictureBox.Location.Y);
            }

            croppedPictureBox.Location = newLocation;

            lastCroppedPictureBox = croppedPictureBox;

            this.Controls.Add(croppedPictureBox);

            // Undo 스택에 있는 CropAction 객체에 새로 생성된 PictureBox를 연결합니다.
            cropAction.CroppedPictureBox = croppedPictureBox;

            // 새로운 PictureBox에 이동 기능 추가
            croppedPictureBox.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    clickOffset = e.Location;
                }
            };

            croppedPictureBox.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    PictureBox pb = sender as PictureBox;
                    if (pb != null)
                    {
                        Point pbLocation = pb.Location;
                        pbLocation.X += e.X - clickOffset.X;
                        pbLocation.Y += e.Y - clickOffset.Y;
                        pb.Location = pbLocation;
                    }
                }
            };

            croppedPictureBox.MouseUp += (sender, e) =>
            {
                isDragging = false;
            };

            croppedPictureBox.BringToFront();

            currentCropRect = Rectangle.Empty;
            pictureBox1.Invalidate();

            // 사용되지 않는 Bitmap 객체들 정리
            combinedBitmapForUndo?.Dispose();
        }
    }
}