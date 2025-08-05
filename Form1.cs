using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;  //배경화면을 설정하기 위해 using 추가
namespace photo
{
    public partial class Form1 : Form
    {
        // Constants for layout
        private const int LeftMargin = 20; // 폼 왼쪽 여백
        private const int TopMargin = 90; // 폼 상단 여백 (tabControl 아래)
        private const int PanelWidth = 300; // 오른쪽 패널의 고정 너비
        private const int PanelRightMargin = 20; // 오른쪽 패널의 폼 오른쪽 여백
        private const int GapBetweenPictureBoxAndPanel = 20; // pictureBox1과 오른쪽 패널 사이의 간격
        private const int BottomMargin = 20; // 폼 하단 여백
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


        // 이미지 드래그 중 여부를 나타내는 플래그
        private bool isDragging = false;

        // 드래그 시작 시 마우스 클릭 지점 좌표
        private Point clickOffset;

        // 선택 테두리를 표시할지 여부 (마우스 클릭 시 true)
        private bool showSelectionBorder = false;

        // 동적으로 생성할 버튼과 패널 배열
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;

        // 현재 표시된 패널을 추적하는 변수
        private Panel currentVisiblePanel = null;

        private PictureBox selectedImage = null;
        private bool showSelectionBorderForImage = false;
        private PictureBox draggingPictureBox = null;

        private Image emojiPreviewImage = null;
        private int emojiPreviewWidth = 64;
        private int emojiPreviewHeight = 64;
        private Point emojiPreviewLocation = Point.Empty;
        private bool showEmojiPreview = false;

        private PictureBox selectedEmoji = null;
        private Point dragOffset;
        private bool resizing = false;
        private const int handleSize = 10;

        // Win32 API 선언 (배경화면 설정을 위해)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;


        public Form1()
        {
            InitializeComponent();

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized; // 실행 시 전체화면
            this.MouseDown += Form1_MouseDown;

            textBox1.KeyPress += TextBox_OnlyNumber_KeyPress;
            textBox2.KeyPress += TextBox_OnlyNumber_KeyPress;

            
        }
        private void TextBox_OnlyNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 숫자 또는 백스페이스만 허용
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private const int LeftPanelWidth = 80; // 왼쪽 버튼들이 차지하는 영역 너비

        private void Form1_Resize(object sender, EventArgs e)
        {
            // 오른쪽 패널 크기 및 위치 조정
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

            // 왼쪽 공간 확보: LeftMargin + 왼쪽 버튼들 + 중간 여백
            int totalLeft = LeftMargin + LeftPanelWidth + GapBetweenPictureBoxAndPanel;

            // 탭컨트롤 위치 및 크기 조정
            tabControl1.Location = new Point(totalLeft, TopMargin);
            tabControl1.Size = new Size(
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin - 15,
                this.ClientSize.Height - TopMargin - BottomMargin
            );

            // 상단 그룹박스 너비 조정
            groupBox2.Width = this.ClientSize.Width - 24;
        }



        private void button4_Click(object sender, EventArgs e)
        {
            // 미사용 버튼 - 추후 기능 연결 가능
        }

        // [새로 만들기] 버튼 클릭 시 실행
        // pictureBox의 이미지 초기화
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            TabPage currentTab = tabControl1.SelectedTab;

            if (currentTab != null)
            {
                // 탭 안의 모든 PictureBox 제거
                var pictureBoxesToRemove = currentTab.Controls
                    .OfType<PictureBox>()
                    .ToList(); // 컬렉션 수정 오류 방지 위해 리스트로 복사

                foreach (var pb in pictureBoxesToRemove)
                {
                    currentTab.Controls.Remove(pb);
                    pb.Dispose(); // 리소스 해제
                }

            }
        }

        // 픽쳐박스 자리
        int X = 30;
        // [열기] 버튼 클릭 시 실행
        // 이미지 파일을 선택하고 pictureBox에 로드
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
                    pb.Location = new Point(10, 30 + X);
                    EnableDoubleBuffering(pb);

                    Bitmap originalCopy; // 변수 선언을 먼저

                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        originalCopy = new Bitmap(original); // 할당만 내부에서
                    }

                    pb.Image = new Bitmap(originalCopy);
                    pb.Size = pb.Image.Size;
                    pb.Tag = originalCopy; // ? 원본 저장
                    imageList.Add((pb, originalCopy));

                    // 핸들러 연결
                    pb.MouseDown += Image_MouseDown;
                    pb.Paint += Image_Paint;
                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;

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

        // [저장] 버튼 클릭 시 실행 (추후 구현 예정)
        private void btn_Save_Click(object sender, EventArgs e)        // TODO: 저장 기능 구현 (찬송)
        {
            TabPage currentTab = tabControl1.SelectedTab;

            // 현재 탭 내 모든 PictureBox 수집
            var pictureBoxes = currentTab.Controls
                .OfType<PictureBox>()
                .Where(pb => pb.Image != null)
                .ToList();

            if (pictureBoxes.Count == 0)
            {
                MessageBox.Show("저장할 이미지가 없습니다.");
                return;
            }

            // 전체 병합 이미지의 크기를 계산 (모든 PictureBox의 위치 + 크기 고려)
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
                g.Clear(Color.White); // 배경 흰색

                foreach (var pb in pictureBoxes)
                {
                    g.DrawImage(pb.Image, pb.Location);
                }
            }

            // 저장 다이얼로그
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
                    MessageBox.Show("모든 이미지가 하나로 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 저장 중 오류 발생:\n{ex.Message}");
                }
            }

            combinedImage.Dispose(); // 리소스 해제
        }





        // 폼 로드 시 실행 (필요 시 초기화 처리 가능)
        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (TabPage tab in tabControl1.TabPages)
            {
                tab.MouseDown += TabPage_MouseDown;
            }
        }

        int tabNumber;

        private Stack<int> deletedTabNumbers = new Stack<int>();  // 삭제된 탭 번호만 저장
        private void btnNewTabPage_Click(object sender, EventArgs e)       //탭페이지 추가 버튼 이벤트         
        {


            // 삭제된 번호 우선 재사용
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

            // 선택 해제용 이벤트 연결
            newTabPage.MouseDown += TabPage_MouseDown;

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;


        }


        // 공통 핸들러들
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                draggingPictureBox = pb;
                clickOffset = e.Location;
                showSelectionBorder = true;
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

            // 커서 모양 계산은 그대로 유지
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
            showSelectionBorder = false;

            if (sender is PictureBox pb)
                pb.Invalidate();
        }
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder && sender is PictureBox pb)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, pb.Width - 1, pb.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
        private PictureBox selectedPictureBox = null;

        private void btnDltTabPage_Click(object sender, EventArgs e)   //탭페이지 삭제 버튼 이벤트
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

                // 탭 이름과 내부 Name 속성 정렬
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    TabPage tab = tabControl1.TabPages[i];
                    tab.Text = $"tp {i + 1}";
                    tab.Name = $"tp{i + 1}";
                }

                // 탭 개수 업데이트
                tabCount = tabControl1.TabPages.Count + 1;

                // 삭제된 번호 스택 비움 (순차 재정렬이므로 재사용 필요 없음)
                deletedTabNumbers.Clear();
            }

        }

        private void button3_Click(object sender, EventArgs e)  // 탭 페이지 안에 텍스트 추가
        {

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




        private void button11_Click(object sender, EventArgs e)     //확대
        {
            float nextScale = currentScale * 1.2f;
            if (nextScale > MAX_SCALE)
            {
                return;
            }

            currentScale = nextScale;
            ApplyScaling();

        }






        private void button12_Click(object sender, EventArgs e)      //축소
        {
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
                int newWidth = (int)(original.Width * currentScale);
                int newHeight = (int)(original.Height * currentScale);

                pb.Image?.Dispose(); // 이전 이미지 제거
                pb.Image = ResizeImageHighQuality(original, new Size(newWidth, newHeight));
                pb.Size = pb.Image.Size;


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

                panel.Controls.Add(new Label()
                {
                    Text = $"편집 속성 {i + 1}",
                    Location = new Point(10, 10)
                });

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

            // 3. 이모지 PictureBox 추가 (패널 8번)
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

        // 모든 동적 버튼의 클릭 이벤트를 처리하는 단일 핸들러
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag; // 버튼의 Tag에서 인덱스 가져오기

                // 패널이 없는 버튼은 아무 동작도 하지 않음
                if (index >= dynamicPanels.Length)
                {
                    return;
                }

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel)
                {
                    // 현재 보이는 패널과 클릭된 패널이 같으면 토글 (숨김)
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else
                {
                    // 다른 패널이 보이고 있다면 숨기기
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }

                    // 클릭된 버튼에 해당하는 패널만 보이게 하기
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }

                // 이전 패널과 새 패널의 Paint 이벤트를 강제로 호출하여 테두리를 갱신
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

            // 현재 보이는 패널인 경우에만 테두리 그리기
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                // 테두리 색상을 검은색으로 변경
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // 패널 경계에 테두리 그리기
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
        private void Image_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb)
            {
                // 기존 선택 해제
                if (selectedImage != null && selectedImage != pb)
                {
                    selectedImage.Invalidate(); // 기존 테두리 제거
                }

                selectedImage = pb;
                showSelectionBorderForImage = true;
                pb.Invalidate(); // 테두리 그리기 위해 다시 그리기
            }
        }
        private void Image_Paint(object sender, PaintEventArgs e)
        {
            if (sender is PictureBox pb && pb == selectedImage && showSelectionBorderForImage)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, pb.Width - 2, pb.Height - 2);
                }
            }
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedImage != null)
            {
                showSelectionBorderForImage = false;
                selectedImage.Invalidate(); // 테두리 제거
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
                selectedImage.Size = selectedImage.Image.Size;  // 회전 후 크기 반영
                selectedImage.Invalidate();
            }
        }

        private void btn_righthegreeClick(object sender, EventArgs e)
        {
            if (selectedImage != null && selectedImage.Image != null)
            {
                selectedImage.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                selectedImage.Size = selectedImage.Image.Size;  // 회전 후 크기 반영
                selectedImage.Invalidate();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val)); // 0~1000 사이로 보정

                if (val != corrected)
                {
                    textBox1.TextChanged -= textBox1_TextChanged;
                    textBox1.Text = corrected.ToString();
                    textBox1.SelectionStart = textBox1.Text.Length; // 커서 끝으로 이동
                    textBox1.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            UpdateSelectedImageSize();
            if (int.TryParse(textBox1.Text, out int val))
            {
                int corrected = Math.Max(0, Math.Min(1000, val)); // 0~1000 사이로 보정

                if (val != corrected)
                {
                    textBox2.TextChanged -= textBox1_TextChanged;
                    textBox2.Text = corrected.ToString();
                    textBox2.SelectionStart = textBox2.Text.Length; // 커서 끝으로 이동
                    textBox2.TextChanged += textBox1_TextChanged;
                }
            }
        }

        private void UpdateSelectedImageSize()
        {
            if (selectedImage == null)
                return;

            if (int.TryParse(textBox1.Text, out int width) && int.TryParse(textBox2.Text, out int height))
            {
                // 범위 제한: 최소 16, 최대 1000
                width = Math.Max(16, Math.Min(1000, width));
                height = Math.Max(16, Math.Min(1000, height));

                //  텍스트박스도 반영되도록
                if (textBox1.Text != width.ToString())
                    textBox1.Text = width.ToString();
                if (textBox2.Text != height.ToString())
                    textBox2.Text = height.ToString();

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

        private void button3_Click_1(object sender, EventArgs e)   //원본 크기로 만드는 버튼
        {
            TabPage currentTab = tabControl1.SelectedTab;
            if (currentTab == null) return;

            foreach (Control ctrl in currentTab.Controls)
            {
                if (ctrl is PictureBox pb && pb.Tag is Bitmap originalBitmap)
                {
                    // 원본 이미지로 복원
                    Bitmap restored = new Bitmap(originalBitmap); // 복사본 사용
                    pb.Image = restored;
                    pb.Size = restored.Size;
                }
            }

            // 배율도 초기화
            currentScale = 1.0f;
        }

        private void button4_Click_1(object sender, EventArgs e)   //배경화면 설정 버튼
        {
            TabPage currentTab = tabControl1.SelectedTab;

            if (currentTab == null)
            {
                MessageBox.Show("탭이 선택되지 않았습니다.");
                return;
            }

            // 첫 번째 이미지 있는 PictureBox 찾기
            PictureBox pb = currentTab.Controls
                .OfType<PictureBox>()
                .FirstOrDefault(p => p.Image != null);

            if (pb == null)
            {
                MessageBox.Show("이미지가 없습니다.");
                return;
            }

            try
            {
                // 임시 경로에 이미지 저장
                string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.jpg");
                pb.Image.Save(tempPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                // 배경화면 설정
                bool result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, tempPath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (!result)
                {
                    MessageBox.Show("배경화면 설정 실패");
                }
                else
                {
                    MessageBox.Show("배경화면이 설정되었습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("오류: " + ex.Message);
            }
        }
    }
}