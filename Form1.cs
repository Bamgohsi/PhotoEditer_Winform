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


        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();

            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;

            this.WindowState = FormWindowState.Maximized; // 전체화면으로 시작

            this.BackColor = Color.FromArgb(255, 25, 25, 25); // 폼의 배경색을 #191919 (R:25, G:25, B:25)로 설정

            //CreateButtons(); // 버튼 생성 메서드 호출

            // btn_NewFile 버튼 스타일 설정
            btn_NewFile.BackColor = Color.FromArgb(255, 73, 80, 87); // 버튼 배경색을 #495057으로 설정
            btn_NewFile.ForeColor = Color.FromArgb(255, 134, 142, 150); // 버튼 폰트 색상을 #868e96로 설정
            btn_NewFile.FlatStyle = FlatStyle.Flat; // 버튼 스타일을 Flat으로 변경
            btn_NewFile.FlatAppearance.BorderSize = 1; // 테두리를 보이게 변경
            btn_NewFile.FlatAppearance.BorderColor = Color.FromArgb(255, 134, 142, 150); // 버튼 테두리 색상을 #868e96로 설정
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
                btn.BackColor = Color.FromArgb(255, 73, 80, 87); // 버튼 배경색을 #343a40으로 설정
                btn.ForeColor = Color.FromArgb(255, 134, 142, 150); // 버튼 폰트 색상을 #868e96로 설정
                btn.FlatStyle = FlatStyle.Flat; // 버튼 스타일을 Flat으로 변경
                btn.FlatAppearance.BorderSize = 1; // 테두리를 보이게 변경
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 134, 142, 150); // 버튼 테두리 색상을 #868e96로 설정

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
                    BackColor = Color.FromArgb(255, 73, 80, 87), // 패널 배경색을 변경
                    Visible = false
                };

                // 패널에 라벨 추가
                panel.Controls.Add(new Label()
                {
                    Text = $"편집 속성 {i + 1}",
                    Location = new Point(10, 10),
                    ForeColor = Color.White // 라벨 텍스트 색상을 흰색으로 설정
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

                // 패널이 없는 버튼은 아무 동작도 하지 않음
                if (index >= dynamicPanels.Length)
                {
                    return;
                }

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel)
                {
                    // 현재 보이는 패널과 클릭된 패널이 같으면 토글
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
                using (Pen pen = new Pen(Color.Gray, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // 패널 경계에 테두리 그리기
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        //// [새로 만들기] 버튼 클릭 시 실행
        //private void btn_NewFile_Click(object sender, EventArgs e)
        //{
        //    pictureBox1.Image = null;
        //}

        //// [열기] 버튼 클릭 시 실행
        //private void btn_Open_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Title = "이미지 열기";
        //    openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

        //    if (openFileDialog.ShowDialog() == DialogResult.OK)
        //    {
        //        try
        //        {
        //            pictureBox1.Image?.Dispose();
        //            Image img = Image.FromFile(openFileDialog.FileName);
        //            pictureBox1.Image = img;
        //            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
        //            pictureBox1.Size = img.Size;
        //            pictureBox1.Location = new Point(10, 10);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("이미지를 불러오는 중 오류 발생: " + ex.Message);
        //        }
        //    }
        //}

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
            this.BackColor = Color.FromArgb(255, 52, 58, 64);
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
