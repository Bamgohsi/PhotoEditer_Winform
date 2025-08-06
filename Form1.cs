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
        private const int LeftPanelWidth = 80; // 이 변수는 현재 사용되지 않음

        // 이미지 원본을 저장할 리스트. 각 PictureBox와 그에 해당하는 원본 Bitmap을 저장합니다.
        // 이 original Bitmap은 필터링 전의 '깨끗한' 원본을 유지합니다.
        private List<(PictureBox pb, Bitmap original)> imageList = new List<(PictureBox, Bitmap)>();
        // 현재 스케일 비율 (기본 1.0f)
        private float currentScale = 1.0f;
        // 이미지를 제한 할 변수 추가
        private const float MIN_SCALE = 0.1f;
        private const float MAX_SCALE = 5.0f;

        //새로운 탭 번호를 세어주는 변수
        private int tabCount = 2;
        // 삭제된 번호 저장소 (현재 사용되지 않음)
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();
        // 이미지 드래그 중 여부를 나타내는 플래그
        private bool isDragging = false;
        // 드래그 시작 시 마우스 클릭 지점 좌표
        private Point clickOffset;
        // 선택 테두리를 표시할지 여부 (마우스 클릭 시 true) (현재 사용되지 않음)
        private bool showSelectionBorder = false;
        // 동적으로 생성할 버튼과 패널 배열 (주석 처리된 코드에서 사용)
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;
        // 현재 표시된 패널을 추적하는 변수
        private Panel currentVisiblePanel = null;
        private PictureBox selectedImage = null; // 현재 선택된 PictureBox
        private bool showSelectionBorderForImage = false; // 선택된 이미지의 테두리 표시 여부
        private PictureBox draggingPictureBox = null; // 드래그 중인 PictureBox

        // 이모지 관련 변수 (기존 코드 유지)
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
        // 현수 - 이미지 편집 관련 변수 추가
        // =======================================================
        // originalImage는 이제 필터 적용의 '기준점'이 되는 비트맵입니다.
        // 선택된 이미지의 Tag에 저장된 '진짜' 원본과 구별되어야 합니다.
        private Bitmap originalImage; // 필터링을 위한 현재 '작업' 이미지 (selectedImage.Tag에 있는 원본에서 파생)
        private Bitmap _initialImage; // 최초 로드된 원본 이미지 (현재는 originalImage와 동일한 역할)

        // 필터 및 RGB 조절 컨트롤
        private TrackBar trackRed, trackGreen, trackBlue;
        private TextBox txtRed, txtGreen, txtBlue;
        private TrackBar trackBrightness, trackSaturation;
        private TextBox txtBrightness, txtSaturation;
        private Button btnApplyAll, btnResetAll;

        private enum FilterState { None, Grayscale, Sepia }
        private FilterState _currentFilter = FilterState.None;
        private bool isTextChanging = false; // 텍스트박스 내용 변경 시 무한 루프 방지용

        // 탭 삭제 번호 저장 (기존 코드에 있었음)
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

        // 숫자만 입력 가능하도록 하는 이벤트 핸들러
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
            // 미사용 버튼
        }

        // [새로 만들기] 버튼 클릭 시 실행
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
            // 현수 - 편집 관련 변수 초기화
            originalImage?.Dispose(); // 기존 이미지 리소스 해제
            originalImage = null;
            _initialImage?.Dispose(); // 기존 이미지 리소스 해제
            _initialImage = null;

            selectedImage = null; // 선택된 이미지 초기화
            btnResetAll_Click(null, null); // UI 컨트롤 초기화
        }

        int X = 30; // PictureBox의 Y 오프셋 (테스트용)

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
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 30 + X); // PictureBox 위치
                    EnableDoubleBuffering(pb);

                    Bitmap originalBitmapFromFile;
                    // 파일에서 비트맵을 생성하고 원본 복사본을 유지합니다.
                    using (var tempImage = Image.FromFile(filePath))
                    {
                        originalBitmapFromFile = new Bitmap(tempImage);
                    }

                    pb.Image = new Bitmap(originalBitmapFromFile); // PictureBox에 이미지 할당
                    pb.Size = pb.Image.Size;
                    pb.Tag = originalBitmapFromFile; // PictureBox의 Tag에 '진짜 원본' Bitmap 저장
                    imageList.Add((pb, originalBitmapFromFile)); // imageList에 (PictureBox, 원본 Bitmap) 추가

                    // 이벤트 핸들러 연결
                    pb.MouseDown += Image_MouseDown;
                    pb.Paint += Image_Paint;
                    pb.MouseDown += pictureBox_MouseDown; // 드래그용
                    pb.MouseMove += pictureBox_MouseMove; // 드래그용
                    pb.MouseUp += pictureBox_MouseUp;     // 드래그용

                    currentTab.Controls.Add(pb);

                    // 현재 열린 이미지를 selectedImage로 설정
                    selectedImage = pb;
                    textBox1.Text = pb.Width.ToString();
                    textBox2.Text = pb.Height.ToString();

                    // 현수 - 이미지 편집용 원본 이미지 저장 및 UI 초기화
                    // 이제 originalImage와 _initialImage는 selectedImage의 Tag에 있는 원본을 참조
                    originalImage?.Dispose();
                    originalImage = (Bitmap)originalBitmapFromFile.Clone();
                    _initialImage?.Dispose();
                    _initialImage = (Bitmap)originalBitmapFromFile.Clone();
                    btnResetAll_Click(null, null); // 편집 컨트롤 초기화
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생:\n" + ex.Message);
                }
            }
            this.ActiveControl = null; // 버튼 포커스 제거
        }

        // 이모지 드래그 앤 드롭 관련 메서드 (기존 코드 유지)
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

            // 가장 큰 PictureBox를 기준으로 저장합니다. (현재는 단일 이미지 처리 중)
            // 만약 여러 PictureBox를 하나의 이미지로 병합하려면 아래 로직을 수정해야 합니다.
            // 현재 selectedImage가 필터링된 결과 이미지를 가지고 있다고 가정합니다.
            if (selectedImage?.Image == null)
            {
                MessageBox.Show("선택된 이미지가 없거나 이미지가 유효하지 않습니다.");
                return;
            }

            // selectedImage의 현재 렌더링 상태를 포함하는 비트맵 생성
            Bitmap combinedImage = new Bitmap(selectedImage.Image.Width, selectedImage.Image.Height);
            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                // selectedImage의 현재 Image를 그대로 그립니다.
                // 이는 ApplyAllLivePreview에 의해 필터링된 결과일 것입니다.
                g.DrawImage(selectedImage.Image, 0, 0, selectedImage.Image.Width, selectedImage.Image.Height);
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
                        MessageBox.Show("지원하지 않는 파일 형식입니다.");
                        return;
                }

                try
                {
                    combinedImage.Save(saveFileDialog.FileName, format);
                    MessageBox.Show("이미지가 저장되었습니다.");
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
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(btn_NewFile, "새로만들기(Ctrl+N)");
            toolTip.SetToolTip(btn_Open, "파일 열기(Ctrl+O)");
            toolTip.SetToolTip(btn_Save, "파일 저장(Ctrl+S)");
            toolTip.SetToolTip(btnNewTabPage, "탭 페이지 추가");
            toolTip.SetToolTip(btnDltTabPage, "탭 페이지 삭제");
            toolTip.SetToolTip(btn_zoomin, "확대");
            toolTip.SetToolTip(btn_zoomout, "축소");
            toolTip.SetToolTip(button1, "왼쪽으로 90º회전");
            toolTip.SetToolTip(button2, "오른쪽으로 90º회전");
            toolTip.SetToolTip(button3, "좌우반전");
            toolTip.SetToolTip(button4, "펜");
            toolTip.SetToolTip(button5, "자르기");
            toolTip.SetToolTip(button6, "스포이드");
            toolTip.SetToolTip(button7, "이모티콘");
            toolTip.SetToolTip(button8, "모자이크");
            toolTip.SetToolTip(button9, "모자이크 해제");
            toolTip.SetToolTip(button10, "필터");
            toolTip.SetToolTip(button13, "지우개");

        }

        


        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                draggingPictureBox = pb;
                clickOffset = e.Location;
                showSelectionBorder = true; // 이 변수는 현재 Image_Paint에서 사용되지 않음
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
            showSelectionBorder = false; // 이 변수는 현재 Image_Paint에서 사용되지 않음

            if (sender is PictureBox pb)
                pb.Invalidate();

            // 옮긴 이미지의 좌표 업데이트
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

        // pictureBox_Paint는 이미지에 테두리를 그리는 역할만 합니다.
        // 필터링된 이미지는 ApplyAllLivePreview에서 selectedImage.Image에 직접 할당됩니다.
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
        private PictureBox selectedPictureBox = null; // 이 변수는 현재 사용되지 않는 것으로 보임

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
            // 기능 미구현 버튼
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


        private void button11_Click(object sender, EventArgs e) // 확대
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

        private void button12_Click(object sender, EventArgs e) // 축소
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
                // 스케일링은 선택된 이미지에만 적용되도록 수정 (또는 전체에 적용)
                // 현재는 모든 이미지에 스케일링을 적용하는 로직이나, 필터는 selectedImage에만 적용되므로
                // 이 부분을 주의해야 합니다. 필터된 이미지를 다시 스케일링하는 것이 아니라,
                // 항상 original을 기준으로 스케일링하도록 유지합니다.
                int newWidth = (int)(original.Width * currentScale);
                int newHeight = (int)(original.Height * currentScale);

                // selectedImage의 경우, 필터링이 적용된 현재 Image를 복사하여 스케일링합니다.
                // 다른 PictureBox의 경우, original (태그에 저장된 원본)을 사용합니다.
                Bitmap imageToResize;
                if (pb == selectedImage && pb.Image != null)
                {
                    imageToResize = new Bitmap(pb.Image); // 필터링된 현재 이미지를 스케일링
                }
                else
                {
                    imageToResize = original; // Tag에 저장된 원본 사용
                }

                pb.Image?.Dispose(); // 이전 이미지 제거
                pb.Image = ResizeImageHighQuality(imageToResize, new Size(newWidth, newHeight));
                pb.Size = pb.Image.Size;

                imageToResize.Dispose(); // 임시 비트맵 해제
            }

            // 탭 스크롤 갱신
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
        /// 버튼과 패널을 동적으로 생성하고 초기화합니다.
        /// </summary>
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
                // Panel 0에 현수님 필터/RGB 컨트롤 추가
                if (i == 0)
                {
                    AddImageEditControls(panel);
                }
                else
                {
                    // 다른 패널은 기본 레이블 유지 (혹은 원하는대로 구성)
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

            // 2. 버튼 생성 (주석 처리된 코드 유지)
            // dynamicButtons = new Button[buttonCount];
            // ... (생략) ...

            // 3. 이모지 PictureBox 추가 (패널 8번) - 문형님 기능이지만, 기존 코드에 있어 유지
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

            // 4. 기본 패널 보이게 할 수도 있음
            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
        }

        // 모든 동적 버튼의 클릭 이벤트를 처리하는 단일 핸들러 (현재 주석 처리된 버튼 생성 코드와 함께 사용)
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
        /// 패널의 Paint 이벤트 핸들러: 활성화된 패널에 테두리를 그립니다.
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

        // 이미지 클릭 시 이벤트 핸들러
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // 기존에 선택된 이미지가 있다면 테두리 제거
                if (selectedImage != null && selectedImage != pb)
                {
                    selectedImage.Invalidate();
                }

                // 새로 선택된 이미지 설정 및 테두리 표시
                selectedImage = pb;
                showSelectionBorderForImage = true;
                pb.Invalidate();

                // 현수 - 선택된 이미지가 변경될 때 편집 컨트롤 UI를 업데이트하고 원본 이미지를 동기화
                UpdateEditControlsFromSelectedImage();
            }
        }

        // 이미지 Paint 이벤트 핸들러
        private void Image_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;

            // 선택된 이미지 테두리 그리기
            if (pb != null && pb == selectedImage && showSelectionBorderForImage)
            {
                using (Pen pen = new Pen(Color.LightSkyBlue, 2))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, 1, 1, pb.Width - 2, pb.Height - 2);
                }
            }

            // 현수 - 이미지가 PictureBox에 할당되어 있다면 그립니다.
            // 필터링된 이미지는 ApplyAllLivePreview에서 selectedImage.Image에 직접 할당되므로
            // 여기서는 selectedImage.Image를 그리기만 하면 됩니다.
            if (pb != null && pb.Image != null)
            {
                // 추가적인 필터링 로직 없이 현재 PictureBox의 이미지를 그립니다.
                e.Graphics.DrawImage(pb.Image, 0, 0, pb.Width, pb.Height);
            }
        }

        // 폼의 빈 공간 클릭 시 selectedImage 선택 해제
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate(); // 테두리 제거
                selectedImage = null; // 선택된 이미지 해제
                // 선택 해제 시 필터 UI도 초기화합니다.
                btnResetAll_Click(null, null);
            }
        }

        // 탭 페이지 빈 공간 클릭 시 selectedImage 선택 해제
        private void TabPage_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate();
                selectedImage = null;
                // 선택 해제 시 필터 UI도 초기화합니다.
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

                // selectedImage.Tag에 저장된 원본 Bitmap을 사용합니다.
                if (selectedImage.Tag is Bitmap originalBitmapFromTag)
                {
                    // 현재 selectedImage.Image를 가져와서 스케일링합니다.
                    // 이렇게 해야 필터가 적용된 상태에서 스케일링이 됩니다.
                    Bitmap imageToResize = null;
                    if (selectedImage.Image != null)
                    {
                        imageToResize = new Bitmap(selectedImage.Image);
                    }
                    else
                    {
                        // 만약 selectedImage.Image가 null이면, Tag의 원본을 사용합니다.
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

                    imageToResize?.Dispose(); // 임시 비트맵 해제
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
            // 기존 코드 유지 (내용 없음)
        }

        // button7_Click은 이모티콘 기능 (기존 코드 유지)
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
        // 현수 - 필터 부분 기능 추가
        // =======================================================

        private void AddImageEditControls(Panel targetPanel)
        {
            int currentY = 10;
            int verticalSpacing = 40;
            int sectionSpacing = 30;

            targetPanel.Controls.Add(new Label { Text = "RGB 조절", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddColorControl("Red", ref trackRed, ref txtRed, targetPanel, ref currentY);
            AddColorControl("Green", ref trackGreen, ref txtGreen, targetPanel, ref currentY);
            AddColorControl("Blue", ref trackBlue, ref txtBlue, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "프리셋 필터", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
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

            targetPanel.Controls.Add(new Label { Text = "밝기", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("밝기", ref trackBrightness, ref txtBrightness, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "채도", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
            currentY += verticalSpacing;
            AddBrightnessSaturationControl("채도", ref trackSaturation, ref txtSaturation, targetPanel, ref currentY);
            currentY += sectionSpacing;

            targetPanel.Controls.Add(new Label { Text = "단색 필터", Location = new Point(10, currentY), Font = new Font(Font, System.Drawing.FontStyle.Bold) });
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

        private void ApplyPresetFilter(FilterState filter, string presetType)
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            // _currentFilter를 설정합니다.
            _currentFilter = filter;

            // 원본 비트맵으로부터 클론을 생성하여 필터링 작업에 사용합니다.
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
                    result = tempOriginal; // 프리셋이 없는 경우 원본 그대로
                    break;
            }

            // originalImage를 새롭게 필터링된 이미지로 업데이트합니다.
            originalImage?.Dispose();
            originalImage = result;

            txtRed.Text = trackRed.Value.ToString();
            txtGreen.Text = trackGreen.Value.ToString();
            txtBlue.Text = trackBlue.Value.ToString();
            trackBrightness.Value = 0;
            txtBrightness.Text = "0";
            trackSaturation.Value = 0;
            txtSaturation.Text = "0";

            // PictureBox의 이미지를 업데이트하고 화면을 갱신합니다.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)originalImage.Clone(); // 작업 이미지를 PictureBox에 할당
            selectedImage.Invalidate();
        }

        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Tag is not Bitmap originalBitmap) return;

            _currentFilter = FilterState.None;

            // selectedImage.Tag에 저장된 원본 Bitmap을 _initialImage와 originalImage에 다시 할당합니다.
            _initialImage?.Dispose();
            _initialImage = (Bitmap)originalBitmap.Clone();
            originalImage?.Dispose();
            originalImage = (Bitmap)originalBitmap.Clone();

            // PictureBox의 이미지를 원본으로 되돌립니다.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)originalBitmap.Clone();
            selectedImage.Invalidate();

            // UI 컨트롤을 기본값으로 초기화합니다.
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

            // selectedImage의 Tag에 있는 원본 비트맵으로부터 시작합니다.
            Bitmap tempImage = (Bitmap)originalBitmap.Clone();

            // RGB 조절 적용
            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;
            tempImage = AdjustRGB(tempImage, rAdj, gAdj, bAdj);

            // 밝기 조절 적용
            tempImage = AdjustBrightness(tempImage, trackBrightness.Value);

            // 채도 조절 적용
            tempImage = AdjustSaturation(tempImage, trackSaturation.Value);

            // 단색 필터 적용
            if (_currentFilter == FilterState.Grayscale)
            {
                tempImage = ConvertToGrayscale(tempImage);
            }
            else if (_currentFilter == FilterState.Sepia)
            {
                tempImage = ApplySepia(tempImage);
            }

            // 이전 이미지를 해제하고 새로운 이미지를 할당합니다.
            selectedImage.Image?.Dispose();
            selectedImage.Image = tempImage;
            selectedImage.Invalidate(); // 변경 사항 즉시 반영
        }

        // selectedImage가 변경될 때 편집 컨트롤 UI를 업데이트
        private void UpdateEditControlsFromSelectedImage()
        {
            if (selectedImage != null)
            {
                // 선택된 PictureBox의 Tag에서 원본 Bitmap을 가져옵니다.
                if (selectedImage.Tag is Bitmap currentOriginalBitmap)
                {
                    _initialImage?.Dispose();
                    _initialImage = (Bitmap)currentOriginalBitmap.Clone(); // 진짜 원본
                    originalImage?.Dispose();
                    originalImage = (Bitmap)currentOriginalBitmap.Clone(); // 필터의 기준이 될 원본
                    btnResetAll_Click(null, null); // UI 컨트롤 초기화
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
        // 트랙바 스크롤 이벤트 핸들러
        private void trackRed_Scroll(object sender, EventArgs e) { txtRed.Text = trackRed.Value.ToString(); ApplyAllLivePreview(); }
        private void trackGreen_Scroll(object sender, EventArgs e) { txtGreen.Text = trackGreen.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBlue_Scroll(object sender, EventArgs e) { txtBlue.Text = trackBlue.Value.ToString(); ApplyAllLivePreview(); }
        private void trackBrightness_Scroll(object sender, EventArgs e) { txtBrightness.Text = txtBrightness.ToString(); ApplyAllLivePreview(); }
        private void trackSaturation_Scroll(object sender, EventArgs e) { txtSaturation.Text = trackSaturation.Value.ToString(); ApplyAllLivePreview(); }

        // 최종 이미지 조절값을 원본 이미지에 반영합니다.
        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            if (selectedImage == null || selectedImage.Image == null) return;

            // 현재 PictureBox에 표시된 최종 결과 이미지(필터 적용됨)를 새로운 '원본'으로 설정합니다.
            originalImage?.Dispose();
            originalImage = (Bitmap)selectedImage.Image.Clone();

            // imageList에 있는 해당 PictureBox의 'original' 비트맵도 업데이트합니다.
            // 이렇게 함으로써 확대/축소 시에도 필터가 적용된 이미지를 기준으로 동작합니다.
            for (int i = 0; i < imageList.Count; i++)
            {
                if (imageList[i].pb == selectedImage)
                {
                    imageList[i].original?.Dispose(); // 기존 원본 해제
                    imageList[i] = (selectedImage, (Bitmap)originalImage.Clone()); // 새 원본 할당
                    break;
                }
            }
            // _initialImage는 원래 로드된 원본 이미지를 유지해야 하므로 여기서는 업데이트하지 않습니다.
        }

        // 모든 조절값을 초기 상태로 되돌립니다.
        private void btnResetAll_Click(object sender, EventArgs e)
        {
            // selectedImage.Tag에 저장된 '진짜' 원본 이미지를 가져옵니다.
            if (selectedImage == null || selectedImage.Tag is not Bitmap trueOriginalBitmap)
            {
                // 선택된 이미지가 없다면 UI만 초기화합니다.
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

            // _initialImage와 originalImage를 selectedImage의 Tag에 있는 원본으로 복원합니다.
            _initialImage?.Dispose();
            _initialImage = (Bitmap)trueOriginalBitmap.Clone();
            originalImage?.Dispose();
            originalImage = (Bitmap)trueOriginalBitmap.Clone();

            // PictureBox의 이미지를 원본으로 되돌립니다.
            selectedImage.Image?.Dispose();
            selectedImage.Image = (Bitmap)trueOriginalBitmap.Clone();
            selectedImage.Invalidate();

            // UI 컨트롤을 초기화합니다.
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

            // _currentFilter를 설정합니다.
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

        // 텍스트박스 변경 이벤트 핸들러 (실시간 업데이트 및 범위 제한)
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

        // button10_Click: "필터" 버튼 클릭 이벤트 핸들러
        private void button10_Click(object sender, EventArgs e)
        {
            Panel targetPanel = dynamicPanels[0]; // 필터 및 RGB 기능이 추가된 첫 번째 패널
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