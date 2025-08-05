using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
        // =======================================================
        // 이미지 편집 및 UI 관련 변수들
        // =======================================================

        // Constants for layout
        private const int LeftMargin = 20; // 폼 왼쪽 여백
        private const int TopMargin = 90; // 폼 상단 여백 (tabControl 아래)
        private const int PanelWidth = 300; // 오른쪽 패널의 고정 너비
        private const int PanelRightMargin = 20; // 오른쪽 패널의 폼 오른쪽 여백
        private const int GapBetweenPictureBoxAndPanel = 20; // pictureBox1과 오른쪽 패널 사이의 간격
        private const int BottomMargin = 20; // 폼 하단 여백
        private const int LeftPanelWidth = 80; // 왼쪽 버튼들이 차지하는 영역 너비

        // 이미지 원본을 저장할 리스트
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();
        // 현재 스케일 비율 (기본 1.0f)
        private float currentScale = 1.0f;
        // 이미지를 제한 할 변수 추가
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        //새로운 탭 번호를 세어주는 변수
        private int tabCount = 2;
        // 삭제된 번호 저장소
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();
        private Stack<int> deletedTabNumbers = new Stack<int>(); // 삭제된 탭 번호만 저장

        // --- 이미지 이동 및 선택 관련 변수 ---
        private bool isDragging = false;
        private PictureBox draggingPictureBox = null;
        private Point clickOffset;
        private bool showSelectionBorder = false; // 이동 모드 테두리
        private bool showSelectionBorderForImage = false; // 이미지 선택 테두리용
        private PictureBox selectedImage = null; // 선택된 이미지 PictureBox

        // 이모지 드래그 앤 드롭
        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;
        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false;
        private const int handleSize = 10;

        // 동적으로 생성할 버튼과 패널 배열
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        // 현재 표시된 패널을 추적하는 변수
        private Panel currentVisiblePanel = null;

        // --- 현수 기능 관련 변수 ---
        private Bitmap originalImage; // 현수 - 원본 이미지 저장용
        private Bitmap _initialImage; // 현수 - 최초 로드된 원본 이미지 저장용
        private TrackBar trackRed, trackGreen, trackBlue; // 현수 - RGB 조절 컨트롤
        private TextBox txtRed, txtGreen, txtBlue; // 현수 - RGB 조절 텍스트박스
        private TrackBar trackBrightness, trackSaturation; // 현수 - 밝기/채도 조절 컨트롤
        private TextBox txtBrightness, txtSaturation; // 현수 - 밝기/채도 텍스트박스
        private Button btnApplyAll, btnResetAll; // 현수 - 모든 조절을 적용/초기화하는 버튼
        private enum FilterState { None, Grayscale, Sepia } // 현수 - 단색 필터 상태
        private FilterState _currentFilter = FilterState.None; // 현수
        private bool isTextChanging = false; // 현수

        // --- 문형 기능 관련 변수 ---
        private float imageOpacity = 1.0f; // 문형
        private bool isEyedropperMode = false; // 문형
        private Panel eyedropperPreviewPanel; // 문형
        private Label eyedropperInfoLabel; // 문형
        private Panel penColorPreviewPanel; // 문형
        private Label penColorInfoLabel; // 문형
        private bool isDrawingMode = false; // 문형
        private bool isErasingMode = false; // 문형
        private bool isPainting = false; // 문형
        private Color penColor = Color.Black; // 문형
        private int penWidth = 5; // 문형
        private bool isMosaicMode = false; // 문형
        private int mosaicPixelSize = 15; // 문형
        private Point startDragPoint; // 문형
        private Rectangle currentMosaicRect; // 문형
        private bool isCropMode = false; // 문형
        private bool isDraggingForCrop = false; // 문형
        private Rectangle currentCropRect = Rectangle.Empty; // 문형
        private Point startCropPoint; // 문형
        private PictureBox lastCroppedPictureBox = null; // 문형
        private bool isDeleteMode = false; // 문형
        private Point mouseLocationOnPanel; // 문형
        private Stack<EditAction> undoStack = new Stack<EditAction>(); // 문형
        private TrackBar opacityTrackBar; // 문형
        private Button undoButton; // 문형
        private Button cropButton; // 문형

        // --- 문형 그리기/모자이크/실행 취소 관련 클래스 ---
        public class DrawnStroke
        {
            public List<Point> Points { get; set; }
            public Color PenColor { get; set; }
            public int PenWidth { get; set; }
        }
        private List<DrawnStroke> drawnStrokes = new List<DrawnStroke>();
        private List<Point> currentStrokePoints = null;
        private List<DrawnStroke> erasedStrokesInSession = new List<DrawnStroke>();

        public class DrawnMosaic
        {
            public Rectangle Rect { get; set; }
            public int PixelSize { get; set; }
        }
        private List<DrawnMosaic> drawnMosaics = new List<DrawnMosaic>();

        public abstract class EditAction
        {
            public abstract void Undo(Form1 form);
        }

        private void PushAction(EditAction action)
        {
            undoStack.Push(action);
        }

        public class DeleteMosaicAction : EditAction
        {
            private DrawnMosaic deletedMosaic;
            public DeleteMosaicAction(DrawnMosaic mosaic)
            {
                this.deletedMosaic = mosaic;
            }
            public override void Undo(Form1 form)
            {
                form.drawnMosaics.Add(deletedMosaic);
            }
        }

        public class DeleteCropAction : EditAction
        {
            private PictureBox deletedPictureBox;
            private Image deletedImage;
            private Point deletedLocation;

            public DeleteCropAction(PictureBox pb)
            {
                this.deletedPictureBox = pb;
                this.deletedImage = (Image)pb.Image.Clone();
                this.deletedLocation = pb.Location;
            }

            public override void Undo(Form1 form)
            {
                PictureBox restoredPictureBox = new PictureBox();
                restoredPictureBox.Image = deletedImage;
                restoredPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
                restoredPictureBox.Location = deletedLocation;

                form.AttachMoveAndDeleteEventsToPictureBox(restoredPictureBox);
                form.Controls.Add(restoredPictureBox);
                restoredPictureBox.BringToFront();
                form.lastCroppedPictureBox = restoredPictureBox;
            }
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

        public class CropAction : EditAction
        {
            public Image OriginalImageBeforeCrop { get; private set; }
            public List<DrawnStroke> DrawnStrokesBeforeCrop { get; private set; }
            public List<DrawnMosaic> DrawnMosaicsBeforeCrop { get; private set; }
            public PictureBox CroppedPictureBox { get; set; }

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
                form.originalImage = (Bitmap?)this.OriginalImageBeforeCrop.Clone();
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

                form.selectedImage.Size = form.originalImage.Size;
                form.selectedImage.Invalidate();
            }
        }


        // =======================================================
        // 초기화 및 로드 관련 메서드
        // =======================================================

        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized;
            this.MouseDown += Form1_MouseDown;

            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;

            // PictureBox의 이벤트 핸들러 연결은 동적으로 이루어지므로, 이 부분은 제거
        }

        // 텍스트 박스에 숫자만 입력 가능하도록 하는 이벤트 핸들러
        private void TextBox_OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // 폼 크기 조절 시 UI 요소 재배치
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

        // 문형 - PictureBox에 마우스 이벤트를 연결하는 헬퍼 메서드
        private void AttachMoveAndDeleteEventsToPictureBox(PictureBox pb)
        {
            pb.MouseDown += (sender, e) =>
            {
                if (isDeleteMode && e.Button == MouseButtons.Left)
                {
                    PictureBox croppedPictureBox = sender as PictureBox;
                    if (croppedPictureBox != null)
                    {
                        undoStack.Push(new DeleteCropAction(croppedPictureBox));
                        this.Controls.Remove(croppedPictureBox);
                        croppedPictureBox.Dispose();
                        if (lastCroppedPictureBox == croppedPictureBox)
                        {
                            lastCroppedPictureBox = null;
                        }
                    }
                    return;
                }

                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    clickOffset = e.Location;
                }
            };
            pb.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    PictureBox picturebox = sender as PictureBox;
                    if (picturebox != null)
                    {
                        Point pbLocation = picturebox.Location;
                        pbLocation.X += e.X - clickOffset.X;
                        pbLocation.Y += e.Y - clickOffset.Y;
                        picturebox.Location = pbLocation;
                    }
                }
            };
            pb.MouseUp += (sender, e) =>
            {
                isDragging = false;
            };
        }

        // [새로 만들기] 버튼 클릭 시 실행
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab != null)
            {
                var pictureBoxesToRemove = currentTab.Controls.OfType<PictureBox>().ToList();
                foreach (var pb in pictureBoxesToRemove)
                {
                    currentTab.Controls.Remove(pb);
                    pb.Dispose();
                }
            }
            // 현수 - 편집 관련 변수 초기화
            originalImage = null;
            _initialImage = null;
            // 문형 - 그리기/모자이크/실행 취소 관련 변수 초기화
            drawnStrokes.Clear();
            drawnMosaics.Clear();
            undoStack.Clear();
            selectedImage = null;
        }

        // 픽쳐박스 자리
        int X = 30;
        private int tabNumber;

        // [열기] 버튼 클릭 시 실행
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 열기";
            openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

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
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 30 + X);
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

                    // 핸들러 연결
                    pb.MouseDown += Image_MouseDown;
                    pb.Paint += Image_Paint;
                    pb.MouseMove += Image_MouseMove;
                    pb.MouseUp += Image_MouseUp;

                    currentTab.Controls.Add(pb);

                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    selectedImage = pb;

                    // 현수 - 이미지 편집용 원본 이미지 저장
                    originalImage = new Bitmap(originalCopy);
                    _initialImage = new Bitmap(originalCopy);
                    btnResetAll_Click(null, null); // 편집 컨트롤 초기화

                    // 문형 - 기능 변수 초기화
                    drawnStrokes.Clear();
                    drawnMosaics.Clear();
                    undoStack.Clear();
                    // 이모지
                    if (currentVisiblePanel != null)
                    {
                        UpdateCursor();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생:\n" + ex.Message);
                }
            }
        }

        // 이모지 드래그 앤 드롭
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

        // [저장] 버튼 클릭 시 실행
        private void btn_Save_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            var pictureBoxes = currentTab.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.Image != null)
                .ToList();

            if (pictureBoxes.Count == 0)
            {
                MessageBox.Show("저장할 이미지가 없습니다.");
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
            saveFileDialog.Title = "이미지 저장";
            saveFileDialog.Filter = "JPEG 파일 (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG 파일 (*.png)|*.png|BMP 파일 (*.bmp)|*.bmp|GIF 파일 (*.gif)|*.gif";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();
                var format = ImageFormat.Png;

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        format = ImageFormat.Jpeg;
                        break;
                    case ".bmp":
                        format = ImageFormat.Bmp;
                        break;
                    case ".gif":
                        format = ImageFormat.Gif;
                        break;
                    case ".png":
                        format = ImageFormat.Png;
                        break;
                    default:
                        MessageBox.Show("지원하지 않는 파일 형식입니다.");
                        return;
                }

                try
                {
                    combinedImage.Save(saveFileDialog.FileName, format);
                    MessageBox.Show("모든 이미지가 하나로 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 저장 중 오류 발생:\n{ex.Message}");
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
        }

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

        // 마우스 이벤트 통합 핸들러
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || selectedImage == null || originalImage == null) return;

            if (isDrawingMode) // 문형
            {
                isPainting = true;
                currentStrokePoints = new List<Point> { e.Location };
            }
            else if (isErasingMode) // 문형
            {
                isPainting = true;
                erasedStrokesInSession.Clear();
                EraseStrokesAt(e.Location);
            }
            else if (isEyedropperMode) // 문형
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
            else if (isMosaicMode) // 문형
            {
                isPainting = true;
                startDragPoint = e.Location;
                currentMosaicRect = new Rectangle(e.Location, new Size(0, 0));
            }
            else if (isCropMode) // 문형
            {
                isDraggingForCrop = true;
                startCropPoint = e.Location;
                currentCropRect = new Rectangle(e.Location, new Size(0, 0));
            }
            else if (isDeleteMode) // 문형
            {
                for (int i = drawnMosaics.Count - 1; i >= 0; i--)
                {
                    DrawnMosaic mosaic = drawnMosaics[i];
                    if (mosaic.Rect.Contains(e.Location))
                    {
                        DrawnMosaic deletedMosaic = drawnMosaics[i];
                        drawnMosaics.RemoveAt(i);
                        undoStack.Push(new DeleteMosaicAction(deletedMosaic));
                        selectedImage.Invalidate();
                        return;
                    }
                }
            }
            else // 이동 모드
            {
                // 기존 PictureBox 선택 로직
                if (sender is PictureBox pb)
                {
                    if (selectedImage != null && selectedImage != pb)
                    {
                        selectedImage.Invalidate();
                    }

                    selectedImage = pb;
                    showSelectionBorderForImage = true;
                    pb.Invalidate();
                    UpdateEditControlsFromSelectedImage();
                }

                // 드래그 시작 로직
                isDragging = true;
                draggingPictureBox = sender as PictureBox;
                clickOffset = e.Location;
                showSelectionBorder = true;
                draggingPictureBox?.Invalidate();
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedImage == null) return;
            if (isPainting && isDrawingMode) // 문형
            {
                currentStrokePoints.Add(e.Location);
                selectedImage.Invalidate();
            }
            else if (isPainting && isErasingMode) // 문형
            {
                EraseStrokesAt(e.Location);
            }
            else if (isDragging)
            {
                Point mousePos = selectedImage.Parent.PointToClient(MousePosition);
                selectedImage.Location = new Point(mousePos.X - clickOffset.X, mousePos.Y - clickOffset.Y);
            }
            else if (isEyedropperMode) // 문형
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
            else if (isPainting && isMosaicMode) // 문형
            {
                int x = Math.Min(startDragPoint.X, e.X);
                int y = Math.Min(startDragPoint.Y, e.Y);
                int width = Math.Abs(startDragPoint.X - e.X);
                int height = Math.Abs(startDragPoint.Y - e.Y);
                currentMosaicRect = new Rectangle(x, y, width, height);
                selectedImage.Invalidate();
            }
            else if (isDraggingForCrop) // 문형
            {
                int x = Math.Min(startCropPoint.X, e.X);
                int y = Math.Min(startCropPoint.Y, e.Y);
                int width = Math.Abs(startCropPoint.X - e.X);
                int height = Math.Abs(startCropPoint.Y - e.Y);
                currentCropRect = new Rectangle(x, y, width, height);
                selectedImage.Invalidate();
            }

            // 커서 모양 변경 로직
            if (sender is PictureBox pic && !isDragging)
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

        private void Image_MouseUp(object sender, MouseEventArgs e)
        {
            if (isPainting && isDrawingMode) // 문형
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
                    PushAction(new DrawStrokeAction(newStroke));
                }
            }
            else if (isPainting && isErasingMode) // 문형
            {
                if (erasedStrokesInSession.Count > 0)
                {
                    PushAction(new EraseAction(new List<DrawnStroke>(erasedStrokesInSession)));
                    erasedStrokesInSession.Clear();
                }
            }
            else if (isPainting && isMosaicMode) // 문형
            {
                if (currentMosaicRect.Width > 0 && currentMosaicRect.Height > 0)
                {
                    var newMosaic = new DrawnMosaic { Rect = currentMosaicRect, PixelSize = mosaicPixelSize };
                    drawnMosaics.Add(newMosaic);
                    PushAction(new MosaicAction(newMosaic));
                }
            }
            else if (isDraggingForCrop) // 문형
            {
                isDraggingForCrop = false;
                selectedImage?.Invalidate();
            }

            isPainting = false;
            currentStrokePoints = null;
            isDragging = false;
            showSelectionBorder = false;

            selectedImage?.Invalidate();
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        private void btnDltTabPage_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count <= 1)
            {
                MessageBox.Show("하나의 탭은 남아있어야 합니다.");
                return;
            }

            TabPage selectedTab = tabControl1.SelectedTab;
            if (selectedTab != null)
            {
                int deletedIndex = tabControl1.TabPages.IndexOf(selectedTab);
                deletedTabNumbers.Push(deletedIndex + 1);

                tabControl1.TabPages.Remove(selectedTab);
                tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;

                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}";
                    tab.Name = $"tp{i + 1}";
                }

                tabCount = tabControl1.TabPages.Count + 1;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // 탭 페이지 안에 텍스트 추가 - 기능 미구현
        }

        private Bitmap ResizeImageHighQuality(Image img, Size size)
        {
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.White);
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return result;
        }

        private void button11_Click(object sender, EventArgs e) //확대
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

        private void button12_Click(object sender, EventArgs e) //축소
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
                if (pb == selectedImage)
                {
                    int newWidth = (int)(original.Width * currentScale);
                    int newHeight = (int)(original.Height * currentScale);
                    pb.Image?.Dispose();
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size;
                }
            }
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

        private void InitializeDynamicControls()
        {
            // 1. 먼저 패널 생성
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

                if (i == 0) // 문형
                {
                    // 문형 - 투명도 조절
                    panel.Controls.Add(new Label() { Text = "이미지 투명도", Location = new Point(10, 10) });
                    opacityTrackBar = new TrackBar { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Location = new Point(10, 40), Size = new Size(200, 40) };
                    opacityTrackBar.Scroll += OpacityTrackBar_Scroll;
                    panel.Controls.Add(opacityTrackBar);

                    // 문형 - 펜 색상 미리보기
                    Button penColorSelectorButton = new Button { Text = "색상 선택", Location = new Point(10, opacityTrackBar.Bottom + 10), Size = new Size(100, 30) };
                    penColorSelectorButton.Click += PenColorSelectorButton_Click;
                    panel.Controls.Add(penColorSelectorButton);
                    penColorPreviewPanel = new Panel { Location = new Point(penColorSelectorButton.Right + 10, penColorSelectorButton.Top), Size = new Size(50, 30), BorderStyle = BorderStyle.FixedSingle, BackColor = penColor };
                    panel.Controls.Add(penColorPreviewPanel);
                    penColorInfoLabel = new Label { Location = new Point(penColorPreviewPanel.Right + 10, penColorPreviewPanel.Top), Size = new Size(100, 30), Text = $"RGB: {penColor.R}, {penColor.G}, {penColor.B}\nHex: #{penColor.R:X2}{penColor.G:X2}{penColor.B:X2}", BorderStyle = BorderStyle.None, AutoSize = false, TextAlign = ContentAlignment.TopLeft };
                    panel.Controls.Add(penColorInfoLabel);

                    // 문형 - 스포이드 색상 미리보기
                    eyedropperPreviewPanel = new Panel { Location = new Point(10, penColorPreviewPanel.Bottom + 10), Size = new Size(50, 30), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
                    panel.Controls.Add(eyedropperPreviewPanel);
                    eyedropperInfoLabel = new Label { Location = new Point(eyedropperPreviewPanel.Right + 10, eyedropperPreviewPanel.Top), Size = new Size(100, 30), Text = "RGB:\nHex:", BorderStyle = BorderStyle.None, AutoSize = false, TextAlign = ContentAlignment.TopLeft };
                    panel.Controls.Add(eyedropperInfoLabel);

                    // 문형 - 작업 모드
                    GroupBox modeGroupBox = new GroupBox { Text = "작업 모드", Location = new Point(10, eyedropperPreviewPanel.Bottom + 10), Size = new Size(280, 130) };
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
                    RadioButton deleteModeRadioButton = new RadioButton { Text = "삭제", Location = new Point(10, cropModeRadioButton.Bottom + 10), AutoSize = true };
                    deleteModeRadioButton.CheckedChanged += ModeRadioButton_CheckedChanged;
                    modeGroupBox.Controls.Add(deleteModeRadioButton);

                    // 문형 - Undo/Crop 버튼
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

                    // 문형 - 펜/지우개 굵기 조절
                    panel.Controls.Add(new Label { Text = "펜/지우개 속성", Location = new Point(10, undoButton.Bottom + 20), AutoSize = true });
                    TrackBar penWidthTrackBar = new TrackBar { Minimum = 1, Maximum = 50, Value = 5, TickFrequency = 5, Location = new Point(10, undoButton.Bottom + 40), Size = new Size(200, 45) };
                    penWidthTrackBar.Scroll += PenWidthTrackBar_Scroll;
                    panel.Controls.Add(new Label { Text = "굵기", Location = new Point(10, penWidthTrackBar.Bottom), AutoSize = true });
                    panel.Controls.Add(penWidthTrackBar);

                    // 문형 - 모자이크 블록 크기 조절
                    panel.Controls.Add(new Label { Text = "모자이크 속성", Location = new Point(10, penWidthTrackBar.Bottom + 50), AutoSize = true });
                    TrackBar mosaicPixelSizeTrackBar = new TrackBar { Minimum = 5, Maximum = 50, Value = mosaicPixelSize, TickFrequency = 5, Location = new Point(10, penWidthTrackBar.Bottom + 70), Size = new Size(200, 45) };
                    mosaicPixelSizeTrackBar.Scroll += MosaicPixelSizeTrackBar_Scroll;
                    panel.Controls.Add(new Label { Text = "블록 크기", Location = new Point(10, mosaicPixelSizeTrackBar.Bottom), AutoSize = true });
                    panel.Controls.Add(mosaicPixelSizeTrackBar);
                }
                else if (i == 1) // 현수 - 두 번째 패널에 이미지 필터 편집 기능 추가
                {
                    AddImageEditControls(panel);
                }
                else if (i == 7) // 이모지 패널 (8번 버튼에 해당)
                {
                    panel.AllowDrop = true;
                    panel.AutoScroll = true;
                    panel.Controls.Add(new Label()
                    {
                        Text = "이모지 선택",
                        Location = new Point(10, 10),
                        Font = new Font(Font, FontStyle.Bold)
                    });
                    AddEmojiControls(panel);
                }
                else
                {
                    panel.Controls.Add(new Label()
                    {
                        Text = $"편집 속성 {i + 1}",
                        Location = new Point(10, 10)
                    });
                }

                panel.Paint += Panel_Paint;
                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            // 2. 버튼 생성
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

            // 3. 기본 패널 보이게 할 수도 있음
            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
        }

        // 이모지 컨트롤을 패널에 추가하는 메서드
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
        }

        // 모든 동적 버튼의 클릭 이벤트를 처리하는 단일 핸들러
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

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate();
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

        private void UpdateSelectedImageSize()
        {
            if (selectedImage == null) return;
            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                width = Math.Max(16, Math.Min(1000, width));
                height = Math.Max(16, Math.Min(1000, height));

                if (textBox1.Text != width.ToString()) textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString()) textBox2.Text = height.ToString();

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

        // 현수 - 특정 패널에 이미지 편집 기능을 위한 컨트롤들을 추가합니다.
        private void AddImageEditControls(Panel targetPanel)
        {
            int currentY = 20;
            int verticalSpacing = 40;
            int sectionSpacing = 30;

            targetPanel.Controls.Add(new Label { Text = "RGB 조절", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddColorControl("Red", ref trackRed, ref txtRed, targetPanel, ref currentY);
            AddColorControl("Green", ref trackGreen, ref txtGreen, targetPanel, ref currentY);
            AddColorControl("Blue", ref trackBlue, ref txtBlue, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "프리셋 필터", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
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

            targetPanel.Controls.Add(new Label { Text = "밝기", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("밝기", ref trackBrightness, ref txtBrightness, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "채도", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("채도", ref trackSaturation, ref txtSaturation, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "단색 필터", Location = new Point(10, currentY), Font = new Font(Font, FontStyle.Bold) });
            currentY += verticalSpacing;
            var btnGray = new Button { Text = "흑백", Location = new Point(10, currentY), Size = new Size(60, 30) };
            btnGray.Click += (s, e) => { ApplyMonochromeFilter(FilterState.Grayscale); };
            targetPanel.Controls.Add(btnGray);
            var btnSepia = new Button { Text = "세피아", Location = new Point(80, currentY), Size = new Size(60, 30) };
            btnSepia.Click += (s, e) => { ApplyMonochromeFilter(FilterState.Sepia); };
            targetPanel.Controls.Add(btnSepia);
            currentY += verticalSpacing + sectionSpacing;

            btnApplyAll = new Button { Text = "적용", Location = new Point(50, currentY), Size = new Size(80, 30) };
            btnApplyAll.Click += btnApplyAll_Click;
            targetPanel.Controls.Add(btnApplyAll);
            btnResetAll = new Button { Text = "초기화", Location = new Point(160, currentY), Size = new Size(80, 30) };
            btnResetAll.Click += btnResetAll_Click;
            targetPanel.Controls.Add(btnResetAll);
        }

        // 현수 - 레이블, 트랙바, 텍스트 박스 컨트롤을 패널에 추가합니다.
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

        // 현수 - 밝기/채도 조절 컨트롤을 패널에 추가합니다.
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

            if (label == "밝기")
            {
                trackBar.Scroll += trackBrightness_Scroll;
                txtBox.TextChanged += txtBrightness_TextChanged;
            }
            else if (label == "채도")
            {
                trackBar.Scroll += trackSaturation_Scroll;
                txtBox.TextChanged += txtSaturation_TextChanged;
            }
            y += 45;
        }

        private void ModeRadioButton_CheckedChanged(object sender, EventArgs e) // 문형
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null || !rb.Checked) return;

            isDrawingMode = false;
            isErasingMode = false;
            isDragging = false;
            isEyedropperMode = false;
            isMosaicMode = false;
            isCropMode = false;
            isDeleteMode = false;

            switch (rb.Text)
            {
                case "이동": break;
                case "스포이드": isEyedropperMode = true; break;
                case "펜": isDrawingMode = true; break;
                case "지우개": isErasingMode = true; break;
                case "모자이크": isMosaicMode = true; break;
                case "자르기": isCropMode = true; break;
                case "삭제": isDeleteMode = true; break;
            }

            if (cropButton != null)
            {
                cropButton.Visible = isCropMode;
            }

            if (!isCropMode)
            {
                currentCropRect = Rectangle.Empty;
                selectedImage?.Invalidate();
            }

            UpdateCursor();
        }

        private void UpdateCursor() // 문형
        {
            if (selectedImage == null) return;
            if (isEyedropperMode || isDrawingMode || isErasingMode || isMosaicMode || isCropMode || isDeleteMode)
            {
                selectedImage.Cursor = Cursors.Cross;
            }
            else
            {
                selectedImage.Cursor = Cursors.Default;
            }
        }

        private void OpacityTrackBar_Scroll(object sender, EventArgs e) // 문형
        {
            imageOpacity = ((TrackBar)sender).Value / 100f;
            selectedImage?.Invalidate();
        }

        private void MosaicPixelSizeTrackBar_Scroll(object sender, EventArgs e) // 문형
        {
            mosaicPixelSize = ((TrackBar)sender).Value;
        }

        private void PenWidthTrackBar_Scroll(object sender, EventArgs e) // 문형
        {
            penWidth = ((TrackBar)sender).Value;
        }

        private void PenColorSelectorButton_Click(object sender, EventArgs e) // 문형
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                penColor = colorDialog.Color;
                penColorPreviewPanel.BackColor = penColor;
                penColorInfoLabel.Text = $"RGB: {penColor.R}, {penColor.G}, {penColor.B}\nHex: #{penColor.R:X2}{penColor.G:X2}{penColor.B:X2}";
            }
        }

        // 현수 - 프리셋 필터를 적용합니다.
        private void ApplyPresetFilter(FilterState filter, string presetType)
        {
            if (originalImage == null) return;
            _currentFilter = filter;

            Bitmap result = (Bitmap)originalImage.Clone();
            switch (presetType)
            {
                case "Warm":
                    result = ApplyWarmFilter(result);
                    trackRed.Value = Math.Min(128 + 30, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 30, 0);
                    break;
                case "Cool":
                    result = ApplyCoolFilter(result);
                    trackRed.Value = Math.Max(128 - 30, 0);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Min(128 + 30, 255);
                    break;
                case "Vintage":
                    result = ApplyVintageFilter(result);
                    trackRed.Value = Math.Min(128 + 20, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 20, 0);
                    break;
            }

            txtRed.Text = trackRed.Value.ToString();
            txtGreen.Text = trackGreen.Value.ToString();
            txtBlue.Text = trackBlue.Value.ToString();
            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";

            if (selectedImage != null)
            {
                selectedImage.Image = result;
            }
        }

        // 현수 - 원본 이미지로 되돌립니다.
        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || _initialImage == null) return;

            _currentFilter = FilterState.None;
            if (selectedImage.Image != null) selectedImage.Image.Dispose();
            selectedImage.Image = (Bitmap)_initialImage.Clone();
            originalImage = (Bitmap)_initialImage.Clone();

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

        // 현수 - Warm 필터 적용
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

        // 현수 - Cool 필터 적용
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

        // 현수 - Vintage 필터 적용
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

        // 현수 - RGB, 밝기, 채도, 단색 필터를 모두 적용한 미리보기 이미지를 생성합니다.
        private void ApplyAllLivePreview()
        {
            if (selectedImage == null || originalImage == null) return;

            Bitmap tempImage = (Bitmap)originalImage.Clone();

            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;
            tempImage = AdjustRGB(tempImage, rAdj, gAdj, bAdj);
            tempImage = AdjustBrightness(tempImage, trackBrightness.Value);
            tempImage = AdjustSaturation(tempImage, trackSaturation.Value);
            if (_currentFilter == FilterState.Grayscale)
            {
                tempImage = ConvertToGrayscale(tempImage);
            }
            else if (_currentFilter == FilterState.Sepia)
            {
                tempImage = ApplySepia(tempImage);
            }

            selectedImage.Image = tempImage;
        }

        private void UpdateEditControlsFromSelectedImage()
        {
            if (selectedImage != null)
            {
                var imageInfo = imageList.FirstOrDefault(item => item.pb == selectedImage);
                if (imageInfo.pb != null)
                {
                    _initialImage = (Bitmap)imageInfo.original.Clone();
                    originalImage = (Bitmap)imageInfo.original.Clone();
                    btnResetAll_Click(null, null);
                }
            }
        }

        private void trackRed_Scroll(object sender, EventArgs e) { txtRed.Text = trackRed.Value.ToString(); ApplyAllLivePreview(); }
        private void trackGreen_Scroll(object sender, EventArgs e) { txtGreen.Text = trackGreen.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBlue_Scroll(object sender, EventArgs e) { txtBlue.Text = trackBlue.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBrightness_Scroll(object sender, EventArgs e) { txtBrightness.Text = trackBrightness.Value.ToString(); ApplyAllLivePreview(); }
        private void trackSaturation_Scroll(object sender, EventArgs e) { txtSaturation.Text = trackSaturation.Value.ToString(); ApplyAllLivePreview(); }

        // 현수 - 최종 이미지 조절값을 원본 이미지에 반영합니다.
        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            originalImage = (Bitmap)selectedImage.Image.Clone();

            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    imageList[i] = (selectedImage, (Bitmap)originalImage.Clone());
                    break;
                }
            }
        }

        // 현수 - 모든 조절값을 초기 상태로 되돌립니다.
        private void btnResetAll_Click(object sender, EventArgs e)
        {
            if (originalImage == null) return;

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
            if (selectedImage != null)
            {
                selectedImage.Image = (Bitmap)originalImage.Clone();
            }
        }

        // 현수 - 흑백 또는 세피아 필터를 적용합니다.
        private void ApplyMonochromeFilter(FilterState filter)
        {
            if (originalImage == null) return;
            _currentFilter = filter;
            ApplyAllLivePreview();
        }

        // 현수 - RGB 값을 조절합니다.
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

        // 현수 - 밝기 조절
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

        // 현수 - 채도 조절
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

        // 현수 - 값을 0-255 범위로 제한
        private int Clamp(int val) => Math.Min(Math.Max(val, 0), 255);

        // 현수 - 흑백 필터 적용
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

        // 현수 - 세피아 필터 적용
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

        // =======================================================
        // 그리기 및 모자이크 관련 메서드 (문형)
        // =======================================================
        private void Image_Paint(object sender, PaintEventArgs e)
        {
            if (sender is PictureBox pb && pb == selectedImage)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (originalImage != null)
                {
                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = imageOpacity;
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    g.DrawImage(selectedImage.Image, new Rectangle(0, 0, selectedImage.Image.Width, selectedImage.Image.Height),
                                     0, 0, selectedImage.Image.Width, selectedImage.Image.Height,
                                     GraphicsUnit.Pixel, attributes);
                }

                foreach (var mosaic in drawnMosaics)
                {
                    ApplyMosaic(g, mosaic.Rect, mosaic.PixelSize);
                }
                if (isPainting && isMosaicMode)
                {
                    ApplyMosaic(g, currentMosaicRect, mosaicPixelSize);
                }
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

                if (showSelectionBorderForImage)
                {
                    using (Pen borderPen = new Pen(Color.DeepSkyBlue, 2))
                    {
                        borderPen.DashStyle = DashStyle.Solid;
                        g.DrawRectangle(borderPen, 0, 0, selectedImage.Width - 1, selectedImage.Height - 1);
                    }
                }

                if (isCropMode && !currentCropRect.IsEmpty)
                {
                    using (Brush semiTransparentBrush = new SolidBrush(Color.FromArgb(128, Color.Black)))
                    {
                        Region fullRegion = new Region(new Rectangle(0, 0, selectedImage.Width, selectedImage.Height));
                        fullRegion.Xor(currentCropRect);
                        g.FillRegion(semiTransparentBrush, fullRegion);
                    }
                    using (Pen cropPen = new Pen(Color.White, 2))
                    {
                        g.DrawRectangle(cropPen, currentCropRect);
                    }
                }
            }
        }

        private void ApplyMosaic(Graphics g, Rectangle rect, int pixelSize) // 문형
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

        private Color GetAverageColor(Bitmap bmp, int x, int y, int size) // 문형
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

        private Bitmap CreateCombinedBitmap() // 문형
        {
            if (originalImage == null) return null;
            Bitmap combinedBitmap = new Bitmap(originalImage.Width, originalImage.Height);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = imageOpacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(originalImage, new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                                 0, 0, originalImage.Width, originalImage.Height,
                                 GraphicsUnit.Pixel, attributes);
                foreach (var mosaic in drawnMosaics)
                {
                    ApplyMosaic(g, mosaic.Rect, mosaic.PixelSize);
                }
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

        private void EraseStrokesAt(Point location) // 문형
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
            selectedImage?.Invalidate();
        }

        private void UndoButton_Click(object sender, EventArgs e) // 문형
        {
            if (undoStack.Count > 0)
            {
                EditAction lastAction = undoStack.Pop();
                lastAction.Undo(this);
                selectedImage?.Invalidate();
            }
        }

        private void CropButton_Click(object sender, EventArgs e) // 문형
        {
            PerformCrop();
        }

        private void PerformCrop() // 문형
        {
            if (currentCropRect.IsEmpty)
            {
                MessageBox.Show("자르기 영역을 먼저 선택해 주세요.");
                return;
            }

            Bitmap combinedBitmapForUndo = CreateCombinedBitmap();
            var cropAction = new CropAction(originalImage, drawnStrokes, drawnMosaics, null);
            undoStack.Push(cropAction);

            Bitmap croppedBitmap = new Bitmap(currentCropRect.Width, currentCropRect.Height);
            using (Graphics g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(combinedBitmapForUndo, 0, 0, currentCropRect, GraphicsUnit.Pixel);
            }

            PictureBox croppedPictureBox = new PictureBox();
            croppedPictureBox.Image = croppedBitmap;
            croppedPictureBox.SizeMode = PictureBoxSizeMode.AutoSize;

            Point newLocation;
            if (lastCroppedPictureBox == null)
            {
                newLocation = new Point(selectedImage.Location.X + selectedImage.Width + 10, selectedImage.Location.Y);
            }
            else
            {
                newLocation = new Point(lastCroppedPictureBox.Location.X + lastCroppedPictureBox.Width + 10, lastCroppedPictureBox.Location.Y);
            }

            croppedPictureBox.Location = newLocation;
            lastCroppedPictureBox = croppedPictureBox;
            this.Controls.Add(croppedPictureBox);
            cropAction.CroppedPictureBox = croppedPictureBox;
            AttachMoveAndDeleteEventsToPictureBox(croppedPictureBox);
            croppedPictureBox.BringToFront();
            currentCropRect = Rectangle.Empty;
            selectedImage?.Invalidate();
            combinedBitmapForUndo?.Dispose();
            //
        }
    }
}
