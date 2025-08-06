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
        private List<ClipboardItem> clipboardContent = new List<ClipboardItem>(); // 여러 이미지를 담을 클립보드
        // --- 이모지 Undo/Redo 관련 변수 ---
        private Stack<List<EmojiState>> emojiUndoStack = new Stack<List<EmojiState>>();
        private Stack<List<EmojiState>> emojiRedoStack = new Stack<List<EmojiState>>();

        // --- 클립보드 아이템 저장을 위한 내부 클래스 ---
        private class ClipboardItem
        {
            public Bitmap Image { get; set; }
            public Point RelativeLocation { get; set; }
        }
        // =======================================================
        // 이미지 편집 및 UI 관련 변수들 (현수님 코드 포함)
        // =======================================================

        // Constants for layout
        private const int LeftMargin = 20;
        private const int TopMargin = 90;
        private const int PanelWidth = 300;
        private const int PanelRightMargin = 20;
        private const int GapBetweenPictureBoxAndPanel = 20;
        private const int BottomMargin = 20;
        private const int LeftPanelWidth = 80; // Added from previous context

        // 이미지 원본을 저장할 리스트
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();

        // 현재 스케일 비율 (현수님 코드에서 제거됨, 크기 조절 기능이 텍스트박스/드래그로 대체)
        // private float currentScale = 1.0f; 

        // 이미지를 제한 할 변수 추가
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        // 새로운 탭 번호를 세어주는 변수
        private int tabCount = 2;

        // --- 상태 관리 변수들 ---
        private bool isDragging = false;
        private Point clickOffset; // 현수님 마우스 드래그 오프셋과 중복, 아래 dragStartMousePosition으로 통합 관리
        private PictureBox draggingPictureBox = null;
        private Point dragStartMousePosition; // 부모 컨트롤 기준 마우스 시작 위치
        private Dictionary<PictureBox, Point> dragStartPositions = new Dictionary<PictureBox, Point>(); // 드래그 시작 시점의 모든 PictureBox 위치

        private bool isResizing = false;
        private Point resizeStartPoint; // 사용되지 않음 (새로운 크기 조절 로직에서는)
        private Size resizeStartSize; // 사용되지 않음
        private Point resizeStartLocation; // 사용되지 않음
        private string resizeDirection = "";

        // 기존 showSelectionBorder (문형님 코드)는 삭제
        // private bool showSelectionBorder = false;

        // UI 요소 배열
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        private Panel currentVisiblePanel = null;

        // 이미지 선택 관련
        private List<PictureBox> selectedImages = new List<PictureBox>(); // 여러 이미지를 담을 리스트
        private PictureBox selectedImage = null; // 현재 활성화된(마지막으로 선택된) 이미지
        // private bool showSelectionBorderForImage = false; // PictureBox_Paint에서 selectedImages로 대체

        // 이모지 드래그 앤 드롭
        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;
        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false; // 이모지 리사이징용
        private const int handleSize = 10;

        // 다중 선택 마르키 (Marquee)
        private bool isMarqueeSelecting = false; // 현재 드래그 선택 중인지 여부
        private Point marqueeStartPoint; // 드래그 시작 지점
        private Rectangle marqueeRect; // 화면에 그려질 선택 사각형

        // --- 현수 기능 관련 변수 ---
        private Bitmap originalImage; // 현수 - 원본 이미지 저장용 (현재 편집 중인 이미지)
        private Bitmap _initialImage; // 현수 - 최초 로드된 원본 이미지 저장용 (리셋용)
        private TrackBar trackRed, trackGreen, trackBlue; // 현수 - RGB 조절 컨트롤
        private TextBox txtRed, txtGreen, txtBlue; // 현수 - RGB 조절 텍스트박스
        private TrackBar trackBrightness, trackSaturation; // 현수 - 밝기/채도 조절 컨트롤
        private TextBox txtBrightness, txtSaturation; // 현수 - 밝기/채도 텍스트박스
        private Button btnApplyAll, btnResetAll; // 현수 - 모든 조절을 적용/초기화하는 버튼
        private enum FilterState { None, Grayscale, Sepia } // 현수 - 단색 필터 상태
        private FilterState _currentFilter = FilterState.None; // 현수
        private bool isTextChanging = false; // 현수 - TextBox.TextChanged와 TrackBar.Scroll 무한루프 방지용

        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls(); // 동적 컨트롤 초기화
            InitializeContextMenu(); // <<-- 이 줄을 추가하세요!

            this.Resize += Form1_Resize; // 폼 크기 조절 이벤트
            this.WindowState = FormWindowState.Maximized; // 폼 최대화
            this.MouseDown += Form1_MouseDown; // 폼 전체 마우스 다운 이벤트 (선택 해제 등)

            // 텍스트 박스 숫자만 입력 및 유효성 검사
            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox1.Validating += textBox1_Validating;
            textBox2.Validating += textBox2_Validating;
            textBox1.KeyDown += TextBox_KeyDown_ApplyOnEnter; // 엔터 키로 적용
            textBox2.KeyDown += TextBox_KeyDown_ApplyOnEnter; // 엔터 키로 적용
            textBox3.KeyPress += TextBox_OnlyNumber_KeyPress; // 숫자만 입력
            textBox4.KeyPress += TextBox_OnlyNumber_KeyPress; // 숫자만 입력
            textBox3.Validating += textBox3_Validating;       // 값 검증 및 적용
            textBox4.Validating += textBox4_Validating;       // 값 검증 및 적용
            textBox3.KeyDown += TextBox_KeyDown_ApplyOnEnter; // 엔터 키로 적용
            textBox4.KeyDown += TextBox_KeyDown_ApplyOnEnter; // 엔터 키로 적용

            this.BackColor = ColorTranslator.FromHtml("#FFF0F5"); // 폼 배경색 (라벤더 블러쉬)

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        // 텍스트 박스에서 엔터 키 누르면 다음 컨트롤로 포커스 이동 (크기 변경 적용)
        private void TextBox_KeyDown_ApplyOnEnter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.SelectNextControl((Control)sender, true, true, true, true);
                e.SuppressKeyPress = true;
            }
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
            groupBox2.Width = this.ClientSize.Width - 24; // groupBox2가 있다면 너비 조정 (가정)
        }

        // [새로 만들기] 버튼 클릭 시 실행
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab != null)
            {
                // 현재 탭의 모든 PictureBox 제거
                var pictureBoxesToRemove = currentTab.Controls
                    .OfType<PictureBox>()
                    .ToList();
                foreach (var pb in pictureBoxesToRemove)
                {
                    currentTab.Controls.Remove(pb);
                    pb.Dispose();
                }
            }

            // 현수 - 편집 관련 변수 초기화
            originalImage = null;
            _initialImage = null;
            // 선택된 이미지 및 리스트 초기화
            selectedImage = null;
            selectedImages.Clear();
            imageList.Clear(); // 이미지 리스트도 초기화
            btnResetAll_Click(null, null); // 편집 컨트롤 초기화
        }

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
                    if (currentTab == null) return; // 현재 탭이 없으면 아무것도 하지 않음

                    // 기존 선택된 이미지들 초기화
                    foreach (var item in selectedImages) { item.Invalidate(); }
                    selectedImages.Clear();
                    selectedImage = null;

                    PictureBox pb = new PictureBox();
                    pb.AllowDrop = true; // 이모지 드래그 앤 드롭 허용
                    pb.DragEnter += PictureBox_DragEnter;
                    pb.DragOver += PictureBox_DragOver;
                    pb.DragLeave += PictureBox_DragLeave;
                    pb.DragDrop += PictureBox_DragDrop;

                    // PictureBox 속성 설정
                    pb.SizeMode = PictureBoxSizeMode.StretchImage; // 이미지 크기 조절 가능
                    pb.Anchor = AnchorStyles.Top | AnchorStyles.Left; // 자동 레이아웃 충돌 방지
                    pb.Dock = DockStyle.None; // Dock 속성 해제
                    pb.Location = new Point(10, 30); // 초기 위치
                    EnableDoubleBuffering(pb); // 더블 버퍼링 활성화

                    Bitmap originalCopy;
                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original);
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size; // 초기 크기는 이미지 크기로 설정
                    pb.Tag = originalCopy; // 원본 비트맵을 Tag에 저장
                    imageList.Add((pb, originalCopy)); // imageList에 추가

                    // PictureBox 이벤트 핸들러 연결
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;

                    currentTab.Controls.Add(pb); // 현재 탭에 PictureBox 추가

                    // UI 텍스트박스 업데이트 및 선택 상태 설정
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    selectedImage = pb;
                    selectedImages.Add(pb); // 새 이미지 선택 리스트에 추가
                    pb.Invalidate(); // 테두리 그리기를 위해 Invalidate 호출

                    // 현수 - 이미지 편집용 원본 이미지 저장 및 컨트롤 초기화
                    originalImage = new Bitmap(originalCopy);
                    _initialImage = new Bitmap(originalCopy);
                    btnResetAll_Click(null, null); // 편집 컨트롤 초기화
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생:\n" + ex.Message);
                }
            }
        }

        // PictureBox 마우스 다운 이벤트 핸들러 (선택, 드래그, 리사이즈 시작)
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                // --- 이모티콘 선택 해제 로직 (추가) ---
                bool emojiClicked = false;
                // 클릭된 위치에 자식 컨트롤(이모티콘)이 있는지 확인
                foreach (Control child in pb.Controls)
                {
                    if (child is PictureBox && child.Bounds.Contains(e.Location))
                    {
                        emojiClicked = true;
                        break;
                    }
                }

                // 이모티콘이 아닌 배경을 클릭했다면, 모든 자식 이모티콘의 선택을 해제
                if (!emojiClicked)
                {
                    foreach (Control child in pb.Controls)
                    {
                        if (child is PictureBox emoji)
                        {
                            emoji.Tag = null; // 선택 태그 제거
                            emoji.Invalidate(); // 다시 그려서 테두리 없애기
                        }
                    }
                    selectedEmoji = null; // 전역 선택 변수도 초기화
                }
                // --- 여기까지 추가 ---

                bool isCtrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;

                if (isCtrlPressed) // Ctrl 키 누른 상태에서 클릭 시 다중 선택
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
                else // Ctrl 키 없이 클릭 시 단일 선택
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

                if (!string.IsNullOrEmpty(resizeDirection) && !emojiClicked) // 이모티콘 클릭 시에는 리사이즈/드래그 방지
                {
                    isResizing = true;
                    isDragging = false;
                    resizeStartPoint = e.Location;
                    resizeStartSize = pb.Size;
                    resizeStartLocation = pb.Location;
                }
                else if (!emojiClicked) // 이모티콘 클릭 시에는 리사이즈/드래그 방지
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

        // PictureBox 마우스 이동 이벤트 핸들러 (드래그, 리사이즈 중)
        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            if (isResizing) // 리사이즈 중일 때
            {
                Point mousePosInParent = pb.Parent.PointToClient(MousePosition); // 부모 컨트롤 기준 마우스 위치

                int fixedRight = resizeStartLocation.X + resizeStartSize.Width;
                int fixedBottom = resizeStartLocation.Y + resizeStartSize.Height;
                int fixedLeft = resizeStartLocation.X;
                int fixedTop = resizeStartLocation.Y;

                // 새로운 크기 및 위치 계산
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

                // 실제 PictureBox 속성 업데이트
                pb.SetBounds(newLeft, newTop, newWidth, newHeight);

                // 텍스트박스 업데이트
                textBox1.Text = pb.Width.ToString();
                textBox2.Text = pb.Height.ToString();
            }
            else if (isDragging) // 드래그 중일 때
            {
                // 현재 마우스 위치 (부모 컨트롤 기준)
                Point currentMousePosition = pb.Parent.PointToClient(MousePosition);
                // 드래그 시작 위치로부터의 변화량(델타) 계산
                int deltaX = currentMousePosition.X - dragStartMousePosition.X;
                int deltaY = currentMousePosition.Y - dragStartMousePosition.Y;

                // 딕셔너리에 저장된 모든 선택 이미지들을 순회하며 위치 업데이트
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
            else // 드래그/리사이즈 중이 아닐 때 (커서 모양 변경)
            {
                const int edge = 5; // 테두리 감지 영역
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

        // PictureBox 마우스 업 이벤트 핸들러 (드래그, 리사이즈 종료)
        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // --- 우클릭 시 메뉴 표시 로직 (추가) ---
                if (e.Button == MouseButtons.Right)
                {
                    // 메뉴 아이템 활성화/비활성화
                    menuCopy.Enabled = selectedImages.Count > 0;
                    menuDelete.Enabled = selectedImages.Count > 0;
                    menuPaste.Enabled = clipboardContent.Count > 0;

                    // 메뉴에 어떤 PictureBox에서 이벤트가 발생했는지 알려줌
                    imageContextMenu.Tag = pb;
                    imageContextMenu.Show(pb, e.Location);
                }
                if (isResizing)
                {
                    // 리사이즈 종료 후 최종 크기를 텍스트박스에 반영 (MouseMove에서 이미 반영되므로 선택사항)
                    // textBox1.Text = pb.Width.ToString();
                    // textBox2.Text = pb.Height.ToString();
                    // UpdateSelectedImageSize(); // 필요시 호출하여 원본 이미지 리사이즈 반영
                }

                isDragging = false;
                isResizing = false;
                draggingPictureBox = null;
                resizeDirection = "";

                pb.Invalidate(); // 테두리 업데이트
            }
        }

        // PictureBox 그리기 이벤트 핸들러 (선택 테두리, 이모지 미리보기)
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // 선택된 이미지 테두리 그리기
            if (selectedImages.Contains(pb))
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    // 마지막으로 선택된(활성화된) 이미지는 실선, 나머지는 점선으로 구분
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

            // 이모지 미리보기 그리기
            if (showEmojiPreview && pb == selectedImage && emojiPreviewImage != null)
            {
                e.Graphics.DrawImage(emojiPreviewImage,
                                     emojiPreviewLocation.X - emojiPreviewWidth / 2,
                                     emojiPreviewLocation.Y - emojiPreviewHeight / 2,
                                     emojiPreviewWidth, emojiPreviewHeight);
            }
        }

        // 컨트롤에 더블 버퍼링 활성화
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        // 이모지 드래그 앤 드롭 관련
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

            // --- Undo 스택에 현재 상태 기록 (추가) ---
            emojiUndoStack.Push(CaptureCurrentEmojis(basePictureBox));
            emojiRedoStack.Clear(); // 새로운 작업이므로 Redo 스택은 비움
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
            // 다른 이모지 선택 해제
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
                    resizing = resizeHandle.Contains(e.Location); // 리사이즈 핸들 클릭 여부
                    if (!resizing)
                        dragOffset = e.Location; // 드래그 오프셋 저장
                }
            }
        }

        private void Emoji_MouseMove(object sender, MouseEventArgs e)
        {
            var emoji = sender as PictureBox;
            var parent = emoji?.Parent as PictureBox;

            if (e.Button == MouseButtons.Left && selectedEmoji == emoji && parent != null)
            {
                if (resizing) // 이모지 리사이즈
                {
                    int newW = Math.Max(32, e.X);
                    int newH = Math.Max(32, e.Y);
                    newW = Math.Min(newW, parent.Width - emoji.Left); // 부모 PictureBox 범위 내로 제한
                    newH = Math.Min(newH, parent.Height - emoji.Top);
                    emoji.Size = new Size(newW, newH);
                }
                else // 이모지 이동
                {
                    Point newLoc = emoji.Location;
                    newLoc.Offset(e.X - dragOffset.X, e.Y - dragOffset.Y);
                    newLoc.X = Math.Max(0, Math.Min(newLoc.X, parent.Width - emoji.Width)); // 부모 PictureBox 범위 내로 제한
                    newLoc.Y = Math.Max(0, Math.Min(newLoc.Y, parent.Height - emoji.Height));
                    emoji.Location = newLoc;
                }
                emoji.Invalidate(); // 이모지 다시 그리기
            }
        }

        private void Emoji_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false; // 이모지 리사이즈 종료
        }

        private void Emoji_Paint(object sender, PaintEventArgs e)
        {
            var emoji = sender as PictureBox;
            if (emoji.Tag != null && emoji.Tag.ToString() == "selected")
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                    e.Graphics.DrawRectangle(pen, 1, 1, emoji.Width - 3, emoji.Height - 3); // 이모지 테두리
                e.Graphics.FillRectangle(Brushes.DeepSkyBlue,
                    emoji.Width - handleSize,
                    emoji.Height - handleSize,
                    handleSize, handleSize); // 리사이즈 핸들
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
                g.Clear(Color.White); // 배경을 흰색으로
                foreach (var pb in pictureBoxes)
                {
                    g.DrawImage(pb.Image, pb.Location); // PictureBox의 이미지를 그립니다.
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "이미지 저장";
            saveFileDialog.Filter = "JPEG 파일 (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG 파일 (*.png)|*.png|BMP 파일 (*.bmp)|*.bmp|GIF 파일 (*.gif)|*.gif";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(saveFileDialog.FileName).ToLower();
                var format = System.Drawing.Imaging.ImageFormat.Png; // 기본 형식

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg": format = System.Drawing.Imaging.ImageFormat.Jpeg; break;
                    case ".bmp": format = System.Drawing.Imaging.ImageFormat.Bmp; break;
                    case ".gif": format = System.Drawing.Imaging.ImageFormat.Gif; break;
                    case ".png": format = System.Drawing.Imaging.ImageFormat.Png; break;
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
            combinedImage.Dispose(); // 사용 후 리소스 해제
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

        // 새 탭 추가 버튼 클릭
        private void btnNewTabPage_Click(object sender, EventArgs e)
        {
            TabPage newTabPage = new TabPage($"tp {tabCount}");
            newTabPage.Name = $"tp{tabCount}";
            newTabPage.BackColor = Color.White;

            // 새 탭에도 이벤트 핸들러 연결
            newTabPage.MouseDown += TabPage_MouseDown;
            newTabPage.MouseMove += TabPage_MouseMove;
            newTabPage.MouseUp += TabPage_MouseUp;
            newTabPage.Paint += TabPage_Paint;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage; // 새로 생성된 탭으로 이동

            tabCount++; // 다음 탭 번호를 위해 1 증가
        }

        // 탭 삭제 버튼 클릭
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
                tabControl1.TabPages.Remove(selectedTab);

                // 남아있는 탭들을 처음부터 순서대로 번호 재지정
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}"; // 보이는 텍스트 변경
                    tab.Name = $"tp{i + 1}"; // 내부 이름 변경
                }

                // 다음에 생성될 탭 번호를 현재 탭 개수 + 1로 설정
                tabCount = tabControl1.TabPages.Count + 1;
            }
        }

        // 고품질 이미지 리사이징 헬퍼 메서드
        private Bitmap ResizeImageHighQuality(Image img, Size size)
        {
            if (size.Width <= 0 || size.Height <= 0) return null; // 유효하지 않은 크기 방지
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.Clear(Color.Transparent); // 배경 투명하게
                g.DrawImage(img, new Rectangle(0, 0, size.Width, size.Height));
            }
            return result;
        }

        // 확대 버튼 클릭
        private void button11_Click(object sender, EventArgs e) // 확대
        {
            // 선택된 모든 이미지에 대해 확대 적용
            foreach (var pb in selectedImages)
            {
                // imageList에서 현재 PictureBox에 해당하는 원본 이미지를 찾음
                var imageEntry = imageList.FirstOrDefault(entry => entry.pb == pb);
                if (imageEntry.pb != null)
                {
                    Bitmap original = imageEntry.original;
                    // 현재 크기를 기준으로 1.2배 큰 새로운 크기 계산
                    int newWidth = (int)(pb.Width * 1.2f);
                    int newHeight = (int)(pb.Height * 1.2f);

                    // 최대 크기 제한 (원본 이미지의 MAX_SCALE 배를 넘지 않도록)
                    if (newWidth > original.Width * MAX_SCALE || newHeight > original.Height * MAX_SCALE)
                    {
                        continue; // 너무 커지면 건너뛰기
                    }

                    // 고화질 리사이징
                    pb.Image?.Dispose(); // 기존 이미지 리소스 해제
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // 계산된 크기로 설정
                }
            }

            // UI 텍스트박스에 마지막으로 선택된 이미지의 크기를 업데이트
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
            }
        }

        // 축소 버튼 클릭
        private void button12_Click(object sender, EventArgs e) // 축소
        {
            // 선택된 모든 이미지에 대해 축소 적용
            foreach (var pb in selectedImages)
            {
                // imageList에서 현재 PictureBox에 해당하는 원본 이미지를 찾음
                var imageEntry = imageList.FirstOrDefault(entry => entry.pb == pb);
                if (imageEntry.pb != null)
                {
                    Bitmap original = imageEntry.original;
                    // 현재 크기를 기준으로 0.8배 작은 새로운 크기 계산
                    int newWidth = (int)(pb.Width * 0.8f);
                    int newHeight = (int)(pb.Height * 0.8f);

                    // 최소 크기 제한 (원본 이미지의 MIN_SCALE 배보다 작아지지 않도록)
                    if (newWidth < original.Width * MIN_SCALE || newHeight < original.Height * MIN_SCALE)
                    {
                        continue; // 너무 작아지면 건너뛰기
                    }

                    // 고화질 리사이징
                    pb.Image?.Dispose(); // 기존 이미지 리소스 해제
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // 계산된 크기로 설정
                }
            }

            // UI 텍스트박스에 마지막으로 선택된 이미지의 크기를 업데이트
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
                textBox3.Text = selectedImage.Left.ToString();
                textBox4.Text = selectedImage.Top.ToString();
            }
        }

        // 동적 컨트롤 초기화 (패널 및 버튼)
        private void InitializeDynamicControls()
        {
            // 1. 패널 생성
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
                    Visible = false, // 처음에는 모두 숨김
                    BorderStyle = BorderStyle.FixedSingle
                };

                if (i == 1) // 현수 - 두 번째 패널에 이미지 필터 편집 기능 추가
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
                    AddEmojiControls(panel); // 이모지 컨트롤 추가
                }
                else // 일반적인 패널 (기본 텍스트만)
                {
                    panel.Controls.Add(new Label()
                    {
                        Text = $"편집 속성 {i + 1}",
                        Location = new Point(10, 10)
                    });
                }

                panel.Paint += Panel_Paint; // 패널 테두리 그리기 이벤트
                this.Controls.Add(panel);
                dynamicPanels[i] = panel;
            }

            // 2. 버튼 생성 (왼쪽 사이드바)
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int buttonStartY = 95;
            int columns = 2;
            int buttonCount = 10; // 10개의 버튼
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
                btn.Tag = i; // 버튼 인덱스를 Tag에 저장
                btn.Click += Button_Click; // 클릭 이벤트 핸들러 연결

                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // 3. 기본 패널 보이게 설정 (첫 번째 패널)
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
            int emojiStartY = 50; // 이모티콘 목록이 시작될 Y 위치
            int iconsPerRow = (panel8.Width - emojiPadding * 2) / (iconSize + emojiPadding); // 한 줄에 표시될 아이콘 수 계산

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
                // 이모티콘 클릭 시 드래그 시작 이벤트 연결
                pic.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        emojiPreviewImage = ((PictureBox)s).Image; // 미리보기 이미지 설정
                        (s as PictureBox).DoDragDrop(((PictureBox)s).Image, DragDropEffects.Copy); // 드래그 앤 드롭 시작
                    }
                };
                panel8.Controls.Add(pic);
            }

            // '적용' 버튼 생성
            Button btnApplyEmojis = new Button();
            btnApplyEmojis.Text = "적용";
            btnApplyEmojis.Size = new Size(100, 30);
            btnApplyEmojis.Location = new Point((panel8.Width - btnApplyEmojis.Width * 2 - 10) / 2, emojiStartY + (emojis.Length / iconsPerRow + 1) * (iconSize + emojiPadding) + 20); // Y 위치는 적절히 조정
            btnApplyEmojis.Click += BtnApplyEmojis_Click; // 클릭 이벤트 핸들러 연결
            panel8.Controls.Add(btnApplyEmojis);

            // '끝 제거' 버튼 생성
            Button btnRemoveLastEmoji = new Button();
            btnRemoveLastEmoji.Text = "끝 제거";
            btnRemoveLastEmoji.Size = new Size(100, 30);
            btnRemoveLastEmoji.Location = new Point(btnApplyEmojis.Right + 10, btnApplyEmojis.Top);
            btnRemoveLastEmoji.Click += BtnRemoveLastEmoji_Click; // 클릭 이벤트 핸들러 연결
            panel8.Controls.Add(btnRemoveLastEmoji);
        }

        /// <summary>
        /// '적용' 버튼 클릭 시, 현재 배경 이미지 위의 모든 이모티콘을 합성합니다.
        /// </summary>
        private void BtnApplyEmojis_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null)
            {
                MessageBox.Show("먼저 배경 이미지를 선택해주세요.");
                return;
            }

            var emojiControls = selectedImage.Controls.OfType<PictureBox>().ToList();
            if (emojiControls.Count == 0)
            {
                MessageBox.Show("적용할 이모티콘이 없습니다.");
                return;
            }

            var result = MessageBox.Show("이모티콘을 이미지에 영구적으로 합성합니다.\n적용 후에는 이동하거나 수정할 수 없습니다.\n계속하시겠습니까?", "확인", MessageBoxButtons.YesNo);
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
            selectedImage.Tag = new Bitmap(newBitmap); // Tag에 새 비트맵의 복사본 저장

            foreach (var control in emojiControls)
            {
                selectedImage.Controls.Remove(control);
                control.Dispose();
            }
            selectedEmoji = null;

            // ---  이 부분을 추가해야 합니다!  ---
            // imageList에 저장된 원본 정보도 현재 합성된 이미지로 교체합니다.
            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    // Tag에 저장된 새 비트맵 복사본을 imageList의 원본으로 지정
                    imageList[i] = (selectedImage, (Bitmap)selectedImage.Tag);
                    break;
                }
            }
            // ---  여기까지 추가 ---

            MessageBox.Show("적용이 완료되었습니다.");
        }

        /// <summary>
        /// '마지막 항목 제거' 버튼 클릭 시, 가장 마지막에 추가된 이모티콘을 제거합니다.
        /// </summary>
        private void BtnRemoveLastEmoji_Click(object sender, EventArgs e)
        {
            if (selectedImage == null)
            {
                MessageBox.Show("먼저 작업할 이미지를 선택해주세요.");
                return;
            }

            // --- Undo 스택에 현재 상태 기록 (추가) ---
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
                MessageBox.Show("제거할 이모티콘이 없습니다.");
            }
        }

        // 모든 동적 버튼의 클릭 이벤트를 처리하는 단일 핸들러 (패널 가시성 토글)
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag;
                if (index >= dynamicPanels.Length) return; // 유효하지 않은 인덱스 방지

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel) // 현재 보이는 패널을 다시 클릭하면 숨김
                {
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else // 다른 패널을 클릭하면 현재 패널 숨기고 새 패널 보임
                {
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }
                if (previousVisiblePanel != null) previousVisiblePanel.Invalidate(); // 이전 패널 테두리 업데이트
                if (currentVisiblePanel != null) currentVisiblePanel.Invalidate(); // 새 패널 테두리 업데이트
            }
        }

        // 패널 그리기 이벤트 (테두리)
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

        // 폼 전체 마우스 다운 이벤트 (선택 해제)
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // 클릭된 컨트롤이 TabControl이나 TabPage일 때만 선택 해제 로직 실행
            var clickedControl = this.GetChildAtPoint(e.Location);
            if (clickedControl == null || clickedControl is TabControl || clickedControl is TabPage)
            {
                // 기존 선택 항목들의 테두리를 지우기 위해 Invalidate 호출
                foreach (var item in selectedImages)
                {
                    item.Invalidate();
                }
                selectedImages.Clear();
                selectedImage = null;
                // 현수 - 편집 컨트롤 초기화
                btnResetAll_Click(null, null);
            }
        }

        // TabPage 마우스 다운 이벤트 (마르키 선택 시작)
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            var tab = sender as TabPage;
            if (tab == null) return;

            // --- 이모티콘 선택 해제 로직 (추가) ---
            bool onAnyImage = false;
            foreach (Control c in tab.Controls)
            {
                if (c is PictureBox pb && pb.Bounds.Contains(e.Location))
                {
                    onAnyImage = true;
                    break;
                }
            }

            // 어떤 이미지 위도 클릭하지 않았다면(빈 공간 클릭)
            if (!onAnyImage)
            {
                // 모든 이미지의 모든 자식 이모티콘 선택 해제
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
            // --- 여기까지 추가 ---

            // 왼쪽 버튼 클릭 시에만 드래그 선택 시작
            if (e.Button == MouseButtons.Left)
            {
                isMarqueeSelecting = true;
                marqueeStartPoint = e.Location; // 드래그 시작 지점 저장
            }
        }

        // TabPage 마우스 이동 이벤트 (마르키 사각형 그리기)
        private void TabPage_MouseMove(object sender, MouseEventArgs e)
        {
            // 드래그 선택 중일 때만 실행
            if (isMarqueeSelecting)
            {
                // 드래그 시작점과 현재 위치를 기반으로 사각형의 영역을 계산
                // (어느 방향으로 드래그하든 정상적으로 사각형이 그려지도록 Math.Min/Abs 사용)
                int x = Math.Min(marqueeStartPoint.X, e.X);
                int y = Math.Min(marqueeStartPoint.Y, e.Y);
                int width = Math.Abs(marqueeStartPoint.X - e.X);
                int height = Math.Abs(marqueeStartPoint.Y - e.Y);
                marqueeRect = new Rectangle(x, y, width, height);

                // TabPage를 다시 그리도록 요청 (Paint 이벤트 발생)
                (sender as TabPage).Invalidate();
            }
        }

        // TabPage 마우스 업 이벤트 (마르키 선택 완료 및 우클릭 메뉴)
        private void TabPage_MouseUp(object sender, MouseEventArgs e)
        {
            TabPage currentTab = sender as TabPage;
            if (currentTab == null) return;

            // --- 1. 좌클릭 드래그(사각형) 선택 종료 처리 ---
            if (isMarqueeSelecting)
            {
                isMarqueeSelecting = false;

                if (marqueeRect.Width < 5 && marqueeRect.Height < 5)
                {
                    bool clickedOnImage = currentTab.Controls.OfType<PictureBox>().Any(pb => pb.Bounds.Contains(e.Location));
                    if (!clickedOnImage) // 이미지 위를 클릭한 게 아닐 때만 선택 해제
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
                        textBox3.Text = selectedImage.Left.ToString();   // <-- 이 줄 추가
                        textBox4.Text = selectedImage.Top.ToString();    // <-- 이 줄 추가
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

            // --- 2. 우클릭 빈 공간 메뉴 처리 (추가) ---
            if (e.Button == MouseButtons.Right)
            {
                bool clickedOnImage = currentTab.Controls.OfType<PictureBox>().Any(pb => pb.Bounds.Contains(e.Location));
                if (!clickedOnImage)
                {
                    menuCopy.Enabled = false; // 빈 공간에서는 복사/삭제 비활성화
                    menuDelete.Enabled = false;
                    menuPaste.Enabled = clipboardContent.Count > 0; // 붙여넣기는 가능

                    // 메뉴에 어떤 탭과 위치인지 정보를 저장
                    imageContextMenu.Tag = new Tuple<TabPage, Point>(currentTab, e.Location);
                    imageContextMenu.Show(Cursor.Position);
                }
            }
        }

        // TabPage를 다시 그려야 할 때 (마르키 사각형 그리기)
        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            // 드래그 선택 중일 때만 사각형을 그림
            if (isMarqueeSelecting)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 1))
                {
                    pen.DashStyle = DashStyle.Dash; // 점선 스타일
                    e.Graphics.DrawRectangle(pen, marqueeRect);
                }
            }
        }

        // 왼쪽 90도 회전 버튼 클릭
        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            // 선택된 모든 이미지에 적용
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate270FlipNone); // 시계 반대 방향 90도
                    pb.Size = pb.Image.Size; // 이미지 크기에 맞춰 PictureBox 크기 조정
                    pb.Invalidate(); // 이미지 다시 그리기
                }
            }
        }

        // 오른쪽 90도 회전 버튼 클릭
        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            // 선택된 모든 이미지에 적용
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    pb.Image.RotateFlip(RotateFlipType.Rotate90FlipNone); // 시계 방향 90도
                    pb.Size = pb.Image.Size; // 이미지 크기에 맞춰 PictureBox 크기 조정
                    pb.Invalidate(); // 이미지 다시 그리기
                }
            }
        }

        // 텍스트박스 값으로 선택된 이미지 크기 업데이트
        private void UpdateSelectedImageSize()
        {
            if (selectedImages.Count == 0) return; // 선택된 이미지가 없으면 반환

            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                if (width <= 0 || height <= 0) return; // 유효하지 않은 크기 방지

                // 크기 제한
                width = Math.Max(16, Math.Min(4000, width));
                height = Math.Max(16, Math.Min(4000, height));

                // 선택된 모든 이미지에 크기 적용
                foreach (var pb in selectedImages)
                {
                    if (pb.Tag is Bitmap originalBitmap) // Tag에 저장된 원본 Bitmap 사용
                    {
                        Bitmap resized = ResizeImageHighQuality(originalBitmap, new Size(width, height));
                        if (resized == null) continue; // 리사이즈 실패 시 건너뛰기

                        pb.Image?.Dispose(); // 기존 이미지 리소스 해제
                        pb.Image = resized; // 새 이미지 할당
                        pb.Size = new Size(width, height); // PictureBox 크기 조정
                        pb.Invalidate(); // 이미지 다시 그리기
                    }
                }

                // 텍스트박스 값도 보정된 값으로 업데이트
                if (textBox1.Text != width.ToString()) textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString()) textBox2.Text = height.ToString();
            }
        }

        // textBox1 (너비) 유효성 검사 및 업데이트
        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            // 값이 비어있으면 현재 선택된 이미지의 너비 또는 기본값(100)으로 설정
            if (string.IsNullOrWhiteSpace(textBox1.Text)) textBox1.Text = selectedImage?.Width.ToString() ?? "100";

            // 숫자로 파싱 실패하면 100으로 설정
            if (!int.TryParse(textBox1.Text, out int val)) val = 100;

            // 값 범위 제한 (16 ~ 4000)
            int corrected = Math.Max(16, Math.Min(4000, val));

            // 보정된 값으로 텍스트박스 업데이트 (필요한 경우)
            if (textBox1.Text != corrected.ToString()) textBox1.Text = corrected.ToString();

            UpdateSelectedImageSize(); // 크기 업데이트 적용
        }

        // textBox2 (높이) 유효성 검사 및 업데이트
        private void textBox2_Validating(object sender, CancelEventArgs e)
        {
            // 값이 비어있으면 현재 선택된 이미지의 높이 또는 기본값(100)으로 설정
            if (string.IsNullOrWhiteSpace(textBox2.Text)) textBox2.Text = selectedImage?.Height.ToString() ?? "100";

            // 숫자로 파싱 실패하면 100으로 설정
            if (!int.TryParse(textBox2.Text, out int val)) val = 100;

            // 값 범위 제한 (16 ~ 4000)
            int corrected = Math.Max(16, Math.Min(4000, val));

            // 보정된 값으로 텍스트박스 업데이트 (필요한 경우)
            if (textBox2.Text != corrected.ToString()) textBox2.Text = corrected.ToString();

            UpdateSelectedImageSize(); // 크기 업데이트 적용
        }

        // =======================================================
        // 현수 - 이미지 필터 및 조절 기능 관련 메서드
        // =======================================================

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
                Value = 128, // 기본값
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
                Value = 0, // 기본값
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

        // 선택된 이미지에 따라 편집 컨트롤 상태를 업데이트 (현수)
        private void UpdateEditControlsFromSelectedImage()
        {
            if (selectedImage != null)
            {
                // imageList에서 현재 PictureBox에 해당하는 원본 이미지를 찾음
                var imageInfo = imageList.FirstOrDefault(item => item.pb == selectedImage);
                if (imageInfo.pb != null)
                {
                    _initialImage = (Bitmap)imageInfo.original.Clone(); // 최초 원본 이미지 설정
                    originalImage = (Bitmap)imageInfo.original.Clone(); // 현재 편집 원본 이미지 설정
                    btnResetAll_Click(null, null); // 편집 컨트롤 초기화
                }
            }
            else
            {
                // 선택된 이미지가 없으면 모든 컨트롤 비활성화 또는 기본값으로 설정
                btnResetAll_Click(null, null); // 모든 컨트롤 초기화 및 비활성화 효과
                // 추가적으로 TrackBar와 TextBox를 비활성화할 수도 있습니다.
                // 예: if (trackRed != null) trackRed.Enabled = false;
            }
        }

        // RGB 및 밝기/채도 TrackBar 스크롤 이벤트
        private void trackRed_Scroll(object sender, EventArgs e) { txtRed.Text = trackRed.Value.ToString(); ApplyAllLivePreview(); }
        private void trackGreen_Scroll(object sender, EventArgs e) { txtGreen.Text = trackGreen.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBlue_Scroll(object sender, EventArgs e) { txtBlue.Text = trackBlue.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBrightness_Scroll(object sender, EventArgs e) { txtBrightness.Text = trackBrightness.Value.ToString(); ApplyAllLivePreview(); }
        private void trackSaturation_Scroll(object sender, EventArgs e) { txtSaturation.Text = trackSaturation.Value.ToString(); ApplyAllLivePreview(); }

        // 현수 - 최종 이미지 조절값을 원본 이미지에 반영합니다.
        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            // 현재 PictureBox에 표시된 이미지(필터 및 조절 적용된 상태)를 originalImage로 저장
            originalImage = (Bitmap)selectedImage.Image.Clone();

            // imageList에서도 해당 PictureBox의 원본 이미지를 업데이트
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
            if (originalImage == null || _initialImage == null) // 원본 이미지가 없으면 초기화할 것도 없음
            {
                // 컨트롤들을 기본값으로 설정하거나 비활성화
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
                    // selectedImage가 imageList에 있는 경우에만 이미지를 초기화
                    // (selectedImage가 null이거나 imageList에 없으면 원본 이미지도 없음)
                    var imageEntry = imageList.FirstOrDefault(item => item.pb == selectedImage);
                    if (imageEntry.pb != null)
                    {
                        selectedImage.Image?.Dispose();
                        selectedImage.Image = (Bitmap)imageEntry.original.Clone();
                        originalImage = (Bitmap)imageEntry.original.Clone(); // originalImage도 다시 원본으로
                        _currentFilter = FilterState.None; // 필터 상태도 초기화
                    }
                }
                return;
            }

            // 컨트롤 값을 기본으로 되돌림
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

            _currentFilter = FilterState.None; // 필터 상태 초기화
            if (selectedImage != null)
            {
                selectedImage.Image?.Dispose(); // 기존 이미지 리소스 해제
                selectedImage.Image = (Bitmap)_initialImage.Clone(); // 최초 로드된 이미지로 복원
                originalImage = (Bitmap)_initialImage.Clone(); // originalImage도 최초 이미지로
            }
        }

        // 현수 - 흑백 또는 세피아 필터를 적용합니다.
        private void ApplyMonochromeFilter(FilterState filter)
        {
            if (originalImage == null) return;
            _currentFilter = filter; // 현재 필터 상태 설정
            ApplyAllLivePreview(); // 미리보기 업데이트
        }

        // 현수 - RGB, 밝기, 채도, 단색 필터를 모두 적용한 미리보기 이미지를 생성합니다.
        private void ApplyAllLivePreview()
        {
            if (selectedImage == null || originalImage == null) return;

            // originalImage의 복사본으로 시작하여 각 조절을 순차적으로 적용
            Bitmap tempImage = (Bitmap)originalImage.Clone();

            // 1. RGB 조절
            int rAdj = trackRed.Value - 128; // 128이 0 조절에 해당
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;
            tempImage = AdjustRGB(tempImage, rAdj, gAdj, bAdj);

            // 2. 밝기 조절
            tempImage = AdjustBrightness(tempImage, trackBrightness.Value);

            // 3. 채도 조절
            tempImage = AdjustSaturation(tempImage, trackSaturation.Value);

            // 4. 단색 필터 적용
            if (_currentFilter == FilterState.Grayscale)
            {
                tempImage = ConvertToGrayscale(tempImage);
            }
            else if (_currentFilter == FilterState.Sepia)
            {
                tempImage = ApplySepia(tempImage);
            }

            // 최종 결과 이미지를 PictureBox에 할당
            selectedImage.Image?.Dispose(); // 기존 이미지 리소스 해제
            selectedImage.Image = tempImage;
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

        // 현수 - 프리셋 필터를 적용합니다.
        private void ApplyPresetFilter(FilterState filter, string presetType)
        {
            if (selectedImage == null || originalImage == null) return;
            _currentFilter = filter; // 필터 상태 설정 (여기서는 None으로 유지)

            Bitmap result = (Bitmap)originalImage.Clone(); // 원본 이미지에서 시작

            switch (presetType)
            {
                case "Warm":
                    // RGB 트랙바 값 설정 (예시)
                    trackRed.Value = Math.Min(128 + 30, 255);
                    trackGreen.Value = 128;
                    trackBlue.Value = Math.Max(128 - 30, 0);
                    // 실제 필터 로직은 ApplyAllLivePreview에서 적용될 것이므로 여기서 직접 이미지 조작은 하지 않음
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

            // 트랙바 값 변경에 따라 텍스트 박스 자동 업데이트
            txtRed.Text = trackRed.Value.ToString();
            txtGreen.Text = trackGreen.Value.ToString();
            txtBlue.Text = trackBlue.Value.ToString();

            // 밝기/채도 트랙바 초기화
            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";

            ApplyAllLivePreview(); // 모든 조절 적용 및 미리보기 업데이트
        }

        // 현수 - 원본 이미지로 되돌립니다.
        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || _initialImage == null) return;
            _currentFilter = FilterState.None; // 필터 상태 초기화

            selectedImage.Image?.Dispose(); // 기존 이미지 리소스 해제
            selectedImage.Image = (Bitmap)_initialImage.Clone(); // 최초 로드된 이미지로 복원
            originalImage = (Bitmap)_initialImage.Clone(); // originalImage도 최초 이미지로

            // 모든 컨트롤 값을 기본으로 되돌림
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

        // RGB 텍스트박스 변경 이벤트 (수동 입력)
        private void txtRed_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return; // 무한루프 방지
            isTextChanging = true;
            if (int.TryParse(txtRed.Text, out int val))
            {
                val = Clamp(val); // 0-255 범위 제한
                txtRed.Text = val.ToString(); // 보정된 값으로 업데이트
                trackRed.Value = val; // 트랙바 값 업데이트
                ApplyAllLivePreview(); // 미리보기 업데이트
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                // 숫자가 아니면 이전 트랙바 값으로 되돌림
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

        // 밝기/채도 텍스트박스 변경 이벤트 (수동 입력)
        private void txtBrightness_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;
            isTextChanging = true;
            if (int.TryParse(txtBrightness.Text, out int val))
            {
                val = Math.Min(Math.Max(val, -100), 100); // -100 ~ 100 범위 제한
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
        // 컨텍스트 메뉴 (우클릭) 관련 로직
        // =================================================================

        private void InitializeContextMenu()
        {
            imageContextMenu = new ContextMenuStrip();
            menuCopy = new ToolStripMenuItem("복사");
            menuPaste = new ToolStripMenuItem("붙여넣기");
            menuDelete = new ToolStripMenuItem("삭제");

            imageContextMenu.Items.AddRange(new[] { menuCopy, menuPaste, menuDelete });

            menuCopy.Click += MenuCopy_Click;
            menuPaste.Click += MenuPaste_Click;
            menuDelete.Click += MenuDelete_Click;
        }

        private void MenuCopy_Click(object sender, EventArgs e)
        {
            if (selectedImages.Count == 0) return;

            clipboardContent.Clear();

            // 선택된 이미지 그룹의 좌상단 기준점을 찾습니다.
            int minX = selectedImages.Min(pb => pb.Left);
            int minY = selectedImages.Min(pb => pb.Top);
            Point originPoint = new Point(minX, minY);

            foreach (PictureBox pb in selectedImages)
            {
                clipboardContent.Add(new ClipboardItem
                {
                    Image = new Bitmap(pb.Image),
                    // 그룹의 기준점으로부터의 상대 위치를 저장합니다.
                    RelativeLocation = new Point(pb.Left - originPoint.X, pb.Top - originPoint.Y)
                });
            }
        }

        private void MenuPaste_Click(object sender, EventArgs e)
        {
            if (clipboardContent.Count == 0) return;

            // 메뉴를 띄운 위치 정보를 가져옵니다.
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
                    // 붙여넣을 위치 = 기준점 + 상대 위치
                    Location = new Point(pasteLocation.X + item.RelativeLocation.X, pasteLocation.Y + item.RelativeLocation.Y),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left,
                    BackColor = Color.Transparent,
                    Tag = new Bitmap(item.Image)
                };

                pb.AllowDrop = true; // 드래그 앤 드롭 허용
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

            // 새로 붙여넣은 이미지들을 선택 상태로 만듭니다.
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

            // 리스트를 복사해서 사용 (원본을 순회하며 삭제하면 에러 발생)
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
                // 빈 공간에 붙여넣기
                return (tabInfo.Item1, tabInfo.Item2);
            }
            if (imageContextMenu.Tag is PictureBox pb)
            {
                // 이미지 위에 붙여넣기 (이미지 좌상단 기준 약간 옆)
                return (pb.Parent as TabPage, new Point(pb.Left + 10, pb.Top + 10));
            }
            // 기본값 (현재 탭의 (10,10))
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
            // 기존 이모티콘 제거
            foreach (Control c in parent.Controls.OfType<PictureBox>().ToList())
                parent.Controls.Remove(c);

            // 저장된 상태대로 복원
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
                // 기존 이모티콘 컨트롤에 연결한 이벤트 다시 연결!
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
            // Ctrl 키를 이용한 단축키 처리
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    // --- 복사(Ctrl+C) 단축키 추가 ---
                    case Keys.C:
                        MenuCopy_Click(null, null); // 기존 복사 메서드 호출
                        e.Handled = true;
                        break;

                    // --- 붙여넣기(Ctrl+V) 단축키 추가 ---
                    case Keys.V:
                        MenuPaste_Click(null, null); // 기존 붙여넣기 메서드 호출
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
            // Ctrl 키 없이 방향키 등이 눌렸을 때의 처리
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
        // 텍스트박스 값으로 선택된 이미지 위치 업데이트 (핵심 로직)
        private void UpdateSelectedImageLocation()
        {
            if (selectedImages.Count == 0 || selectedImage == null) return;

            if (int.TryParse(textBox3.Text, out int newX) && int.TryParse(textBox4.Text, out int newY))
            {
                // 기준이 되는 마지막 선택 이미지(selectedImage)의 위치 변화량 계산
                int deltaX = newX - selectedImage.Left;
                int deltaY = newY - selectedImage.Top;

                // 모든 선택된 이미지에 대해 동일한 변화량만큼 이동
                // 이렇게 하면 여러 이미지를 선택해도 상대적인 위치가 유지됩니다.
                foreach (var pb in selectedImages)
                {
                    pb.Location = new Point(pb.Left + deltaX, pb.Top + deltaY);
                }
            }
        }

        // textBox3 (X 좌표) 유효성 검사 및 업데이트
        private void textBox3_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox3.Text)) textBox3.Text = selectedImage?.Left.ToString() ?? "0";
            UpdateSelectedImageLocation();
        }

        // textBox4 (Y 좌표) 유효성 검사 및 업데이트
        private void textBox4_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox4.Text)) textBox4.Text = selectedImage?.Top.ToString() ?? "0";
            UpdateSelectedImageLocation();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // 선택된 모든 이미지에 대해 좌우 반전을 적용합니다.
            foreach (var pb in selectedImages)
            {
                if (pb != null && pb.Image != null)
                {
                    // 이미지에 RotateFlip 메서드를 사용하여 좌우 반전(Horizontal Flip)을 적용합니다.
                    pb.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    // 변경된 이미지를 화면에 다시 그리도록 요청합니다.
                    pb.Invalidate();
                }
            }
        }
    }
    // Form1 클래스 바깥에 추가
    public class EmojiState
    {
        public Image Image { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
    }
}