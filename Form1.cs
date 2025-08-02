using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

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

        private Point lastMousePosition;

        public Form1()
        {
            InitializeComponent();

         

            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;

            
            // PictureBox 드래그 처리 이벤트 연결(이진희)
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;

            // 폼(및 지식컨트롤) 클릭 시 테두리 해제 이벤트 연결(이진희)
            this.MouseDown += Form1_MouseDown;

            // 재귀적으로 모든 지식 컨트롤에도 연결합니둥(이진희)
            HookMouseDown(this);
        }


        //재귀적으로 parent와 그 자식 컨트롤들에 Form1_MouseDown 훅을 겁니둥.(이진희)
        private void HookMouseDown(Control parent)
        {
            foreach (Control ctl in parent.Controls)
            {
                if (ctl != pictureBox1)
                    ctl.MouseDown += Form1_MouseDown;
                if (ctl.HasChildren)
                    HookMouseDown(ctl);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
           
            // 미사용 버튼 - 추후 기능 연결 가능
        }

        // [새로 만들기] 버튼 클릭 시 실행
        // pictureBox의 이미지 초기화
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose(); // 기존 이미지 메모리 해제
                pictureBox1.Image = null;
                showSelectionBorder = false; // 이미지 초기화 시 테두리도 숨김
                pictureBox1.Invalidate(); // 다시 그리기 요청
            }
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
            MessageBox.Show("저장 기능은 아직 구현되지 않았습니다.");
        }
        // 수정: 폼 또는 지식컨트롤 클릭시 호출, 클릭지점을 폼(client) 좌표로 변환하여 pictureBox외부검사(이진희)
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            Point clickPt;
            if (sender == this)
            {
                clickPt = e.Location;
            }
            else
            {
                Control ctl = (Control)sender;
                clickPt = this.PointToClient(ctl.PointToScreen(e.Location));
            }
            // pictureBox1 밖을 클릭했으면 테두리 끔
            if (pictureBox1.Image != null
                && showSelectionBorder
                && !pictureBox1.Bounds.Contains(e.Location))    // <- 수정: clicpt 사용
            {
                showSelectionBorder = false;
                pictureBox1.Invalidate();
            }
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

                // 드래그 시작 시점의 마우스 스크린 좌표 저장

                lastMousePosition = Control.MousePosition;
            }
        }

        // 마우스를 이동할 때 호출됨 (드래그 중일 때만)
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                if (!isDragging) return;

                Point currentMousePosition = Control.MousePosition;
                // 2) 이전 위치와 차이(delta) 계산
                int dx = currentMousePosition.X - lastMousePosition.X;
                int dy = currentMousePosition.Y - lastMousePosition.Y;
                // 3) PictureBox 위치에 델타만큼 더해 부드럽게 이동
                pictureBox1.Location = new Point(
                    pictureBox1.Location.X + dx,
                    pictureBox1.Location.Y + dy
                );
                // 4) 다음 델타 계산을 위해 위치 갱신
                lastMousePosition = currentMousePosition;
                //// PictureBox 위치 갱신
                //pictureBox1.Location = newLocation;
            }
        }

        // 마우스 버튼을 놓을 때 호출됨
        // 드래그 종료 및 선택 테두리 해제
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            // 선택 해제하고 싶을 경우 주석 해제
            //showSelectionBorder = false;

            // 다시 그리기 요청 (Paint 호출)
            pictureBox1.Invalidate();
        }

  

        // 폼 로드 시 실행 (필요 시 초기화 처리 가능)
        private void Form1_Load(object sender, EventArgs e)
        {

            // 현재는 비어 있음
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
    }
}