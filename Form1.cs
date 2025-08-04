using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
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

        // 원본 이미지를 저장할 필드
        private Image originalImage = null;

        // 모자이크 효과가 적용되었는지 추적하는 변수
        private bool isMosaicApplied = false;

        //



        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();

            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;

            this.WindowState = FormWindowState.Maximized; // 전체화면으로 시작

            this.BackColor = Color.FromArgb(255, 45,45,45); // 폼의 배경색을 변경
            tabControl1.BackColor = Color.Gray;
            tabPage2.BackColor = Color.LightGray;

            CreateButtons(); // 상단 버튼 생성 메서드 호출
        }

        /// 버튼과 패널을 동적으로 생성하고 초기화합니다.
        /// 왼쪽 버튼
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
                // 기본 Button 클래스의 인스턴스를 생성합니다.
                Button btn = new Button();
                btn.Text = $"{i + 1}"; // 버튼 텍스트를 숫자로 설정
                btn.Size = new Size(buttonWidth, buttonHeight);
                btn.BackColor = Color.FromArgb(255, 45,45,45); // 버튼 배경색을 변경
                btn.ForeColor = Color.FromArgb(255, 108, 117, 125); // 버튼 폰트 색상을 변경
                btn.FlatStyle = FlatStyle.Flat; // 버튼 스타일을 Flat으로 변경
                btn.FlatAppearance.BorderSize = 1; // 테두리를 보이게 변경
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 108, 117, 125); // 버튼 테두리 색상을 #868e96로 설정

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

            Point panelLocation = new Point(1600, 90);
            Size panelSize = new Size(300, 900);

            for (int i = 0; i < panelCount; i++)
            {
                Panel panel = new Panel()
                {
                    Location = panelLocation,
                    Size = panelSize,
                    BackColor = Color.FromArgb(255, 68,68,68), // 패널 배경색을 변경
                    Visible = false
                };

                // 패널에 라벨 추가
                panel.Controls.Add(new Label()
                {
                    Text = $"편집 속성 {i + 1}",
                    Location = new Point(5, 5),
                    ForeColor = Color.DarkGray // 라벨 텍스트 색상을 흰색으로 설정
                });

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

                // 왼쪽 첫 번째 버튼(인덱스 0)은 모자이크 효과를 토글합니다.
                if (index == 0)
                {
                    this.MosaicButton_Click(sender, e);
                    // 패널은 열지 않고, 현재 열려있는 패널이 있다면 닫음
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                        currentVisiblePanel = null;
                    }
                }
                // 나머지 버튼은 기존처럼 편집 패널을 토글
                else if (index < dynamicPanels.Length)
                {
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
                // 테두리 색상을 회색으로 변경
                using (Pen pen = new Pen(Color.Gray, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // 패널 경계에 테두리 그리기
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        /// 버튼 클릭 시 실행되는 모자이크 이벤트 핸들러입니다.
        private void MosaicButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("먼저 이미지를 불러와주세요.");
                return;
            }

            // 모자이크 효과가 적용된 상태라면 원본 이미지로 되돌립니다.
            if (isMosaicApplied)
            {
                // 원본 이미지를 PictureBox에 할당
                pictureBox1.Image = new Bitmap(originalImage);
                pictureBox1.Size = originalImage.Size;
                isMosaicApplied = false;
            }
            // 모자이크 효과가 적용되지 않은 상태라면 효과를 적용합니다.
            else
            {
                // 모자이크 크기
                int mosaicSize = 10;

                // 원본 이미지를 기반으로 모자이크 효과를 적용
                Bitmap originalBitmap = new Bitmap(originalImage);
                Bitmap mosaicBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);

                // 이중 for 루프를 사용하여 모자이크 효과를 적용합니다.
                for (int y = 0; y < originalBitmap.Height; y += mosaicSize)
                {
                    for (int x = 0; x < originalBitmap.Width; x += mosaicSize)
                    {
                        // 모자이크 블록의 평균 색상을 계산합니다.
                        Color averageColor = CalculateAverageColor(originalBitmap, x, y, mosaicSize);

                        // 모자이크 블록에 평균 색상을 채웁니다.
                        FillMosaicBlock(mosaicBitmap, averageColor, x, y, mosaicSize, originalBitmap.Width, originalBitmap.Height);
                    }
                }

                // PictureBox에 모자이크가 적용된 이미지를 할당합니다.
                pictureBox1.Image = mosaicBitmap;
                isMosaicApplied = true;
            }
        }

        /// for 반복문을 사용하여 버튼을 1열로 생성하고 배치하는 메서드입니다.
        /// 상단 버튼
        private void CreateButtons()
        {
            // 버튼 생성에 필요한 변수들
            int buttonWidth = 30;  // 버튼 너비를 25로 변경
            int buttonHeight = 30; // 버튼 높이를 25로 변경
            int spacing = 10;
            int startX = 15;   // 시작 X 위치를 15로 변경
            int startY = 32; // 시작 Y 위치를 32로 변경
            int buttonCount = 5; // 생성할 총 버튼 개수

            // 1열로 5개 버튼 배치
            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = new Button();

                // 버튼 스타일 설정
                btn.BackColor = Color.FromArgb(255, 73, 80, 87);
                btn.ForeColor = Color.FromArgb(255, 134, 142, 150);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 134, 142, 150);

                btn.Text = $"{i + 1}"; // 버튼 텍스트를 숫자로 설정
                btn.Size = new Size(buttonWidth, buttonHeight);

                // 버튼 위치 계산 (Y는 고정, X는 반복마다 증가)
                btn.Location = new Point(startX + i * (buttonWidth + spacing), startY);

                // 첫 번째 버튼 (i == 0)에 원본 이미지 복원 이벤트를 연결합니다.
                if (i == 0)
                {
                    btn.Click += FirstButton_Click;
                }
                // 두 번째 버튼 (i == 1)에 이미지 열기 이벤트를 연결합니다.
                else if (i == 1)
                {
                    btn.Click += SecondButton_Click;
                }
                else
                {
                    // 나머지 버튼에는 기존 이벤트를 연결합니다.
                    btn.Click += Button_Click;
                }

                // 폼에 버튼 추가
                this.Controls.Add(btn);
            }
        }

        /// 첫 번째 버튼 클릭 시 실행되는 이벤트 핸들러입니다.
        /// PictureBox의 이미지를 원본으로 되돌립니다.
        private void FirstButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1 != null && originalImage != null)
            {
                // 원본 이미지를 PictureBox에 할당
                pictureBox1.Image = originalImage;
                pictureBox1.Size = originalImage.Size;
                isMosaicApplied = false; // 모자이크 효과가 해제되었음을 명시
                pictureBox1.Invalidate(); // 변경 사항을 즉시 반영
            }
        }

        /// 두 번째 버튼 클릭 시 실행되는 이벤트 핸들러입니다.
        /// OpenFileDialog를 열어 이미지를 불러옵니다.
        private void SecondButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 열기";
            openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose();

                    // 새 이미지를 불러와 원본 이미지 변수에 저장
                    Image img = Image.FromFile(openFileDialog.FileName);
                    originalImage = new Bitmap(img); // 원본 이미지를 복사하여 저장

                    pictureBox1.Image = originalImage;
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;
                    pictureBox1.Location = new Point(10, 10);
                    isMosaicApplied = false; // 새 이미지를 불러오면 모자이크 상태 초기화
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// 지정된 영역의 평균 색상을 계산합니다.
        /// </summary>
        private Color CalculateAverageColor(Bitmap bitmap, int startX, int startY, int size)
        {
            long red = 0, green = 0, blue = 0;
            int pixelCount = 0;

            for (int y = startY; y < startY + size && y < bitmap.Height; y++)
            {
                for (int x = startX; x < startX + size && x < bitmap.Width; x++)
                {
                    Color pixelColor = bitmap.GetPixel(x, y);
                    red += pixelColor.R;
                    green += pixelColor.G;
                    blue += pixelColor.B;
                    pixelCount++;
                }
            }

            if (pixelCount > 0)
            {
                return Color.FromArgb((int)(red / pixelCount), (int)(green / pixelCount), (int)(blue / pixelCount));
            }

            return Color.Black;
        }

        /// <summary>
        /// 지정된 영역을 단일 색상으로 채웁니다.
        /// </summary>
        private void FillMosaicBlock(Bitmap bitmap, Color color, int startX, int startY, int size, int maxWidth, int maxHeight)
        {
            for (int y = startY; y < startY + size && y < maxHeight; y++)
            {
                for (int x = startX; x < startX + size && x < maxWidth; x++)
                {
                    bitmap.SetPixel(x, y, color);
                }
            }
        }

        // [저장] 버튼 클릭 시 실행 (추후 구현 예정)
        private void btn_Save_Click(object sender, EventArgs e)
        {
            // TODO: 저장 기능 구현
        }

        // 마우스 버튼을 누를 때 호출됨
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pictureBox1.Invalidate();
            }
        }

        // 마우스를 이동할 때 호출됨
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;
                pictureBox1.Location = newLocation;
            }
        }

        // 마우스 버튼을 놓을 때 호출됨
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            // showSelectionBorder = false; // 선택 해제하고 싶을 경우 주석 해제
            pictureBox1.Invalidate();
        }

        // 폼 로드 시 실행 (필요 시 초기화 처리 가능)
        private void Form1_Load(object sender, EventArgs e)
        {
            // 초기화 로직
        }

        // pictureBox1이 다시 그려질 때 호출됨 (선택 테두리 그림)
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }
    }
}
