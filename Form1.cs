using System.Reflection;
using System.Windows.Forms;

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
        public Form1()
        {
            InitializeComponent();

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning
            this.Resize += Form1_Resize;
            this.WindowState = FormWindowState.Maximized; // 실행 시 전체화면



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
                this.ClientSize.Width - totalLeft - PanelWidth - PanelRightMargin-15,
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

                    // 새 PictureBox 생성
                    PictureBox pb = new PictureBox();
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 30 + X); // 위치는 아래 함수 참고
                    EnableDoubleBuffering(pb);

                    using (var original = new Bitmap(Image.FromFile(filePath)))
                    {
                        Bitmap originalCopy = new Bitmap(original); // 이미지 원본 복사
                        pb.Image = new Bitmap(originalCopy);        // 화면 표시용 이미지
                        pb.Size = pb.Image.Size;

                        // 리스트에 원본 저장
                        imageList.Add((pb, originalCopy));
                    }

                    pb.Image = Image.FromFile(filePath);
                    pb.Size = pb.Image.Size;

                    pb.MouseDown += pictureBox_MouseDown;
                    pb.MouseMove += pictureBox_MouseMove;
                    pb.MouseUp += pictureBox_MouseUp;
                    pb.Paint += pictureBox_Paint;



                    // 현재 탭에 추가
                    currentTab.Controls.Add(pb);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생:\n" + ex.Message);
                }
            }
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
            // 현재는 비어 있음
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

            tabControl1.TabPages.Add(newTabPage);
            tabControl1.SelectedTab = newTabPage;


        }


        // 공통 핸들러들
        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb?.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pb.Invalidate(); // 다시 그리기
            }
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is PictureBox pb)
            {
                Point newLocation = pb.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;

                pb.Location = newLocation;
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
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
            // 버튼 관련 설정
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int startY = 95;
            int columns = 2; // 2열로 배치
            int buttonCount = 10; // 총 버튼 개수

            dynamicButtons = new Button[buttonCount];

            // 2열 5행으로 버튼 배치
            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = new Button();
                btn.Text = $"{i + 1}"; // 버튼 텍스트를 숫자로 설정
                btn.Size = new Size(buttonWidth, buttonHeight);

                // 버튼 위치 계산 (2열 5행)
                int col = i % columns;
                int row = i / columns;
                btn.Location = new Point(startX + col * (buttonWidth + spacing),
                                         startY + row * (buttonHeight + spacing));

                btn.Tag = i; // 버튼에 인덱스 저장
                btn.Click += Button_Click; // 클릭 이벤트 핸들러 연결
                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // 패널 관련 설정
            int panelCount = 10;
            dynamicPanels = new Panel[panelCount];

            // 패널 위치는 오른쪽 상단으로 고정
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
                    BorderStyle = BorderStyle.FixedSingle // 패널 경계선 추가
                };

                // 패널에 라벨 추가
                panel.Controls.Add(new Label() { Text = $"편집 속성 {i + 1}", Location = new Point(10, 10) });

                // 패널에 Paint 이벤트 핸들러 추가
                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel; // 생성한 패널을 배열에 저장
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

    }
}

