using System;
using System.Drawing;
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

        public Form1()
        {
            InitializeComponent();

            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;
            Rigth_Panel_GropBox.Visible = false;

            // 버튼들의 Click 이벤트에 동일한 핸들러를 연결
            button2.Click += button1_Click;
            button3.Click += button1_Click;
            button4.Click += button1_Click;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // 미사용 버튼 - 추후 기능 연결 가능
        }

        // [새로 만들기] 버튼 클릭 시 실행
        // pictureBox의 이미지 초기화
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
        }

        // [열기] 버튼 클릭 시 실행
        // 이미지 파일을 선택하고 pictureBox에 로드
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 열기";
            openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 기존 이미지가 있을 경우 메모리 해제
                    pictureBox1.Image?.Dispose();

                    // 새로운 이미지 로드
                    Image img = Image.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = img;

                    // 이미지 크기에 맞게 PictureBox 크기 조절
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;

                    // pictureBox 위치 설정 (좌측 상단 여백)
                    pictureBox1.Location = new Point(10, 10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생: " + ex.Message);
                }
            }
        }

        // [저장] 버튼 클릭 시 실행 (추후 구현 예정)
        private void btn_Save_Click(object sender, EventArgs e)
        {
            // TODO: 저장 기능 구현
        }

        // 마우스 버튼을 누를 때 호출됨
        // 드래그 시작 처리 및 선택 테두리 표시
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;          // 드래그 시작
                clickOffset = e.Location;   // 마우스 클릭 좌표 저장
                showSelectionBorder = true; // 테두리 표시 ON
                pictureBox1.Invalidate();   // 다시 그리기 요청 (Paint 호출)
            }
        }

        // 마우스를 이동할 때 호출됨 (드래그 중일 때만)
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // 기존 위치에서 마우스 이동만큼 offset 적용
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;

                // PictureBox 위치 갱신
                pictureBox1.Location = newLocation;
            }
        }

        // 마우스 버튼을 놓을 때 호출됨
        // 드래그 종료 및 선택 테두리 해제
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            // 선택 해제하고 싶을 경우 주석 해제
            showSelectionBorder = false;

            // 다시 그리기 요청 (Paint 호출)
            pictureBox1.Invalidate();
        }

        // 폼 로드 시 실행 (필요 시 초기화 처리 가능)
        private void Form1_Load(object sender, EventArgs e)
        {
            // 현재는 비어 있음

            this.WindowState = FormWindowState.Maximized; // 전체화면 시작
            //groupBox3.Visible = !groupBox1.Visible;
        }

        // pictureBox1이 다시 그려질 때 호출됨
        // 선택 테두리를 그림
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    // 실선으로 테두리 그리기 (점선은 DashStyle.Dot 등 사용 가능)
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

                    // 그림 테두리 사각형 정의 (이미지 전체)
                    Rectangle rect = new Rectangle(0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);

                    // 테두리 그리기
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        // 버튼을 눌렀을 때 그룹박스를 띄웠다 숨겼다 하는 기능
        private void button1_Click(object sender, EventArgs e)
        {
            if (Rigth_Panel_GropBox.Visible == false)
            {
                // 그룹박스가 숨겨져 있다면, 보이게 합니다.
                Rigth_Panel_GropBox.Visible = true;
            }
            else
            {
                // 그룹박스가 보인다면, 숨깁니다.
                Rigth_Panel_GropBox.Visible = false;
            }
        }
    }
}