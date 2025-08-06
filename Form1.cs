using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        // 이미지 원본을 저장할 리스트
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();

        // 이미지를 제한 할 변수 추가
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        //새로운 탭 번호를 세어주는 변수
        private int tabCount = 2;

        // --- 상태 관리 변수들 ---
        private bool isDragging = false;
        private Point clickOffset;
        private PictureBox draggingPictureBox = null;
        private Point dragStartMousePosition; // 부모 컨트롤 기준 마우스 시작 위치
        private Dictionary<PictureBox, Point> dragStartPositions = new Dictionary<PictureBox, Point>(); // 드래그 시작 시점의 모든 PictureBox 위치
        private bool isResizing = false;
        private Point resizeStartPoint;
        private Size resizeStartSize;
        private Point resizeStartLocation;
        private string resizeDirection = "";
        private bool showSelectionBorder = false;
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        private Panel currentVisiblePanel = null;
        private List<PictureBox> selectedImages = new List<PictureBox>(); // 여러 이미지를 담을 리스트
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
        private bool isMarqueeSelecting = false;      // 현재 드래그 선택 중인지 여부
        private Point marqueeStartPoint;            // 드래그 시작 지점
        private Rectangle marqueeRect;              // 화면에 그려질 선택 사각형
        private Stack<List<EmojiState>> emojiUndoStack = new Stack<List<EmojiState>>();
        private Stack<List<EmojiState>> emojiRedoStack = new Stack<List<EmojiState>>();


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
            this.BackColor = ColorTranslator.FromHtml("#FFF0F5"); // 라벤더 블러쉬 색상
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
            // 기존 이모티콘 제거
            foreach (Control c in parent.Controls.OfType<PictureBox>().ToList())
                parent.Controls.Remove(c);

            // 저장된 상태대로 복원
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

                    // --- 근본 원인 해결 코드 ---
                    // 1. SizeMode를 StretchImage로 변경 (수동 크기 조절 허용)
                    pb.SizeMode = PictureBoxSizeMode.StretchImage;

                    // 2. Anchor 속성을 Top, Left로 고정하여 자동 레이아웃 충돌 방지
                    pb.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                    // 3. Dock 속성 해제
                    pb.Dock = DockStyle.None;
                    // --- 근본 원인 해결 코드 끝 ---

                    pb.Location = new Point(10, 30);
                    EnableDoubleBuffering(pb);

                    Bitmap originalCopy;
                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original);
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size; // 초기 크기는 이미지 크기로 설정
                    pb.Tag = originalCopy;
                    imageList.Add((pb, originalCopy));

                    // 이벤트 핸들러 연결
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;

                    currentTab.Controls.Add(pb);

                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    selectedImage = pb;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생:\n" + ex.Message);
                }
            }
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                // (다중 선택 로직은 이전과 동일)
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
                    // 만약 클릭한 pb가 이미 선택된 항목 중 하나가 아니라면, 기존 선택을 클리어
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

                // --- 드래그 시작 로직 수정 ---
                if (!string.IsNullOrEmpty(resizeDirection))
                {
                    isResizing = true;
                    isDragging = false;
                }
                else
                {
                    isDragging = true;
                    isResizing = false;

                    // ▼▼▼ 그룹 이동을 위한 초기화 코드 추가 ▼▼▼
                    dragStartPositions.Clear(); // 딕셔너리 초기화
                    dragStartMousePosition = pb.Parent.PointToClient(MousePosition); // 부모 기준 마우스 위치 저장

                    // 선택된 모든 이미지의 현재 위치를 딕셔너리에 저장
                    foreach (var selectedPb in selectedImages)
                    {
                        dragStartPositions.Add(selectedPb, selectedPb.Location);
                    }
                    // ▲▲▲ 여기까지 추가 ▲▲▲
                }
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

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
            }
            else if (isDragging) // <<< 이 부분을 수정합니다.
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
            }
            else
            {
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
                else { pb.Cursor = Cursors.Default; resizeDirection = ""; }
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                if (isResizing)
                {
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();
                    UpdateSelectedImageSize();
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

            // [수정] selectedImages 리스트에 포함되어 있으면 테두리를 그림
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
            // [★추가] 항상 드롭할 때 그 사진이 selectedImage가 되도록!
            selectedImage = basePictureBox;

            // Undo/Redo 스택 관리
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
                var format = System.Drawing.Imaging.ImageFormat.Png;

                switch (ext)
                {
                    case ".jpg": case ".jpeg": format = System.Drawing.Imaging.ImageFormat.Jpeg; break;
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
            // 항상 tabCount를 사용하여 새 탭 생성
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

            tabCount++; // 다음 탭 번호를 위해 1 증가
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
                tabControl1.TabPages.Remove(selectedTab);

                // ▼▼▼ 핵심: 남아있는 탭들을 처음부터 순서대로 번호 재지정 ▼▼▼
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}"; // 보이는 텍스트 변경
                    tab.Name = $"tp{i + 1}";   // 내부 이름 변경
                }

                // 다음에 생성될 탭 번호를 현재 탭 개수 + 1로 설정
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
                    pb.Image?.Dispose();
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // 계산된 크기로 설정
                }
            }

            // UI 텍스트박스에 마지막으로 선택된 이미지의 크기를 업데이트
            if (selectedImage != null)
            {
                textBox1.Text = selectedImage.Width.ToString();
                textBox2.Text = selectedImage.Height.ToString();
            }
        }

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
                    pb.Image?.Dispose();
                    pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                    pb.Size = pb.Image.Size; // 계산된 크기로 설정
                }
            }

            // UI 텍스트박스에 마지막으로 선택된 이미지의 크기를 업데이트
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
                panel.Controls.Add(new Label() { Text = $"편집 속성 {i + 1}", Location = new Point(10, 10) });
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
            // 8번 패널 가져오기
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
            int emojiStartY = 50; // 이모티콘 목록이 시작될 Y 위치
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
                // 이모티콘 클릭 시 드래그 시작 이벤트 연결
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

            // '적용' 버튼 생성
            Button btnApplyEmojis = new Button();
            btnApplyEmojis.Text = "적용";
            btnApplyEmojis.Size = new Size(100, 30);
            // 패널 너비의 중간쯤에 위치하도록 동적 계산
            btnApplyEmojis.Location = new Point((panel8.Width - btnApplyEmojis.Width * 2 - 10) / 2, 850); // Y 위치는 적절히 조정하세요.
            btnApplyEmojis.Click += BtnApplyEmojis_Click; // 클릭 이벤트 핸들러 연결
            panel8.Controls.Add(btnApplyEmojis);

            // '제거' 버튼 생성
            Button btnRemoveLastEmoji = new Button();
            btnRemoveLastEmoji.Text = "끝 제거";
            btnRemoveLastEmoji.Size = new Size(100, 30);
            btnRemoveLastEmoji.Location = new Point(btnApplyEmojis.Right + 10, btnApplyEmojis.Top);
            btnRemoveLastEmoji.Click += BtnRemoveLastEmoji_Click; // 클릭 이벤트 핸들러 연결
            panel8.Controls.Add(btnRemoveLastEmoji);

            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
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

            // 자식 컨트롤 중 PictureBox(이모티콘)만 가져오기
            var emojiControls = selectedImage.Controls.OfType<PictureBox>().ToList();
            if (emojiControls.Count == 0)
            {
                MessageBox.Show("적용할 이모티콘이 없습니다.");
                return;
            }

            // 사용자에게 되돌릴 수 없음을 경고
            var result = MessageBox.Show("이모티콘을 이미지에 영구적으로 합성합니다.\n적용 후에는 이동하거나 수정할 수 없습니다.\n계속하시겠습니까?", "확인", MessageBoxButtons.YesNo);
            if (result == DialogResult.No)
            {
                return;
            }

            // 원본 비트맵을 기반으로 새 비트맵 생성 (여기에 그림)
            Bitmap newBitmap = new Bitmap(selectedImage.Image);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                // 모든 이모티콘 컨트롤을 순회하며 비트맵에 그리기
                foreach (PictureBox emoji in emojiControls)
                {
                    g.DrawImage(emoji.Image, emoji.Bounds); // Bounds는 Location과 Size를 모두 포함
                }
            }

            // 합성된 이미지로 교체
            selectedImage.Image = newBitmap;

            // Tag에 저장된 원본 이미지도 최신화 (매우 중요!)
            // 이렇게 해야 나중에 또 다른 합성을 해도 이전 내용이 유지됨
            if (selectedImage.Tag is Bitmap oldBitmap)
            {
                oldBitmap.Dispose();
            }
            selectedImage.Tag = new Bitmap(newBitmap);


            // 사용이 끝난 이모티콘 컨트롤들은 모두 제거
            foreach (var control in emojiControls)
            {
                selectedImage.Controls.Remove(control);
                control.Dispose();
            }
            selectedEmoji = null; // 선택된 이모티콘 참조 해제

            MessageBox.Show("적용이 완료되었습니다.");
        }

        /// <summary>
        /// '마지막 항목 제거' 버튼 클릭 시, 가장 마지막에 추가된 이모티콘을 제거합니다.
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
                MessageBox.Show("먼저 작업할 이미지를 선택해주세요.");
                return;
            }

            // 자식 컨트롤 중 PictureBox(이모티콘)를 찾음
            var lastEmoji = selectedImage.Controls.OfType<PictureBox>().LastOrDefault();

            if (lastEmoji != null)
            {
                // 컨트롤 목록에서 제거하고 리소스 해제
                selectedImage.Controls.Remove(lastEmoji);
                lastEmoji.Dispose();
            }
            else
            {
                MessageBox.Show("제거할 이모티콘이 없습니다.");
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
                // 기존 선택 항목들의 테두리를 지우기 위해 Invalidate 호출
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

            // 클릭한 위치에 "배경 이미지(Background Image)"가 있는지 검사
            PictureBox clickedBackground = null;
            foreach (Control c in tab.Controls)
            {
                if (c is PictureBox pb && pb.Bounds.Contains(e.Location))
                {
                    clickedBackground = pb;
                    break;
                }
            }

            // === [1] "이미지 바깥" 클릭: 모든 이모티콘 선택 해제 ===
            if (clickedBackground == null)
            {
                foreach (Control c in tab.Controls)
                {
                    if (c is PictureBox bgPb) // 배경 PictureBox
                    {
                        foreach (Control ec in bgPb.Controls) // 이모티콘 PictureBox
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
            // === [2] "배경 이미지" 내부의 빈 곳(이모티콘X) 클릭: 이 배경의 이모티콘만 선택 해제 ===
            else
            {
                // 배경 이미지 내부의 이모티콘 컨트롤 중, 클릭한 위치에 겹치는 게 있는지 확인
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
                // 만약 어떤 이모티콘도 클릭되지 않았다면 -> 해당 이미지의 이모티콘 선택 해제!
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
                // (이모티콘이 클릭됐으면 별도 처리 없음: Emoji_MouseDown에서 처리)
            }

            // 드래그 선택 사각형은 기존 코드처럼 남겨두면 됨!
            if (e.Button == MouseButtons.Left)
            {
                isMarqueeSelecting = true;
                marqueeStartPoint = e.Location;
            }
        }



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
        private void TabPage_MouseUp(object sender, MouseEventArgs e)
        {
            TabPage currentTab = sender as TabPage;
            if (currentTab == null) return;

            // 드래그 선택 상태 종료
            isMarqueeSelecting = false;

            // 드래그로 만들어진 사각형이 아주 작으면(단순 클릭으로 간주)
            if (marqueeRect.Width < 5 && marqueeRect.Height < 5)
            {
                // 모든 선택을 해제
                foreach (var item in selectedImages) { item.Invalidate(); }
                selectedImages.Clear();
                selectedImage = null;
            }
            else
            {
                // 드래그 선택 영역과 겹치는 모든 PictureBox를 찾아서 선택 상태를 토글
                foreach (PictureBox pb in currentTab.Controls.OfType<PictureBox>())
                {
                    if (marqueeRect.IntersectsWith(pb.Bounds))
                    {
                        if (selectedImages.Contains(pb))
                        {
                            selectedImages.Remove(pb); // 이미 선택됐으면 제거
                        }
                        else
                        {
                            selectedImages.Add(pb); // 선택 안됐으면 추가
                        }
                    }
                }

                // 마지막으로 선택된 이미지를 대표 이미지로 설정
                selectedImage = selectedImages.LastOrDefault();

                // 텍스트박스 업데이트
                if (selectedImage != null)
                {
                    textBox1.Text = selectedImage.Width.ToString();
                    textBox2.Text = selectedImage.Height.ToString();
                }

                // 모든 PictureBox를 다시 그려서 테두리 상태 업데이트
                foreach (var pb in currentTab.Controls.OfType<PictureBox>())
                {
                    pb.Invalidate();
                }
            }

            // 화면에 남아있는 선택 사각형을 지우기 위해 마지막으로 Invalidate 호출
            marqueeRect = Rectangle.Empty;
            currentTab.Invalidate();
        }
        // TabPage를 다시 그려야 할 때 (Invalidate 호출 시)
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

        private void btn_leftdegreeClick(object sender, EventArgs e)
        {
            // 선택된 모든 이미지에 적용
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
            // 선택된 모든 이미지에 적용
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

                // 선택된 모든 이미지에 크기 적용
                foreach (var pb in selectedImages)
                {
                    if (pb.Tag is Bitmap originalBitmap)
                    {
                        Bitmap resized = ResizeImageHighQuality(originalBitmap, new Size(width, height));
                        if (resized == null) continue; // 리사이즈 실패 시 건너뛰기

                        pb.Image?.Dispose();
                        pb.Image = resized;
                        pb.Size = new Size(width, height);
                        pb.Invalidate();
                    }
                }

                // 텍스트박스 값도 보정된 값으로 업데이트
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
                    pb.Image.RotateFlip(RotateFlipType.RotateNoneFlipX); // 좌우 반전
                    pb.Invalidate(); // 변경사항을 화면에 반영
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