using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

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

        // 1) 크기조절 방향 및 상태 변수 선언,리사이즈 방향 및 상태 변수(이진희)
        private enum ResizeDirection { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
        private ResizeDirection resizeDir = ResizeDirection.None;
        private bool isResizing = false;
        private Point resizeStartPoint; // 리사이즈 시작 좌표(이진희)
        private Rectangle originalBounds; // 리사이즈 시작시 PictureBox 원래 위치/크기(이진희)
                                          // 기존 멤버 변수 아래쯤
        private Point resizeStartScreenPoint;  // ★추가: 리사이즈 시작 시점의 스크린 좌표


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
                    //사진이 크게불러와져서 size를 AutoSize에서 Zoom으로 변경 추후에 필요시 다시 변경(이진희)
                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    // pictureBox1.Size = img.Size;


                    // pictureBox 위치 설정 (좌측 상단 여백)
                    // pictureBox1.Location = new Point(10, 10);(여백이 보여 주석처리함(이진희)
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
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                const int edge = 5;
                bool atTop = e.Y <= edge;
                bool atBottom = e.Y >= pictureBox1.Height - edge;
                bool atLeft = e.X <= edge;
                bool atRight = e.X >= pictureBox1.Width - edge;

                // 1) 리사이즈 방향 판별
                if (atTop && atLeft) resizeDir = ResizeDirection.TopLeft;
                else if (atTop && atRight) resizeDir = ResizeDirection.TopRight;
                else if (atBottom && atLeft) resizeDir = ResizeDirection.BottomLeft;
                else if (atBottom && atRight) resizeDir = ResizeDirection.BottomRight;
                else if (atTop) resizeDir = ResizeDirection.Top;
                else if (atBottom) resizeDir = ResizeDirection.Bottom;
                else if (atLeft) resizeDir = ResizeDirection.Left;
                else if (atRight) resizeDir = ResizeDirection.Right;
                else resizeDir = ResizeDirection.None;

                if (resizeDir != ResizeDirection.None)
                {
                    // 2) 리사이즈 시작
                    isResizing = true;
                    // 클라이언트+스크린 기준(이진희)
                    resizeStartScreenPoint = Control.MousePosition;
                    // 시작 크기 , 위치저장(이진희)
                    originalBounds = pictureBox1.Bounds;
                
                    return;
                }

                // 기존 이동(드래그) 로직
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pictureBox1.Invalidate();
                lastMousePosition = Control.MousePosition;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            // ★1) 리사이즈 중이면 스크린 좌표 차이로 계산
            if (isResizing)
            {
                //스크린 좌표 수정(이진희)
                Point curScreen = Control.MousePosition;
                int dx = curScreen.X - resizeStartScreenPoint.X;
                int dy = curScreen.Y - resizeStartScreenPoint.Y;

                // 기존 originalBounds 기반으로 newBounds 계산 코드수정(이진희)
                Rectangle nb = originalBounds;
                switch (resizeDir)
                {
                    case ResizeDirection.Left:
                        nb.X += dx;
                        nb.Width = originalBounds.Width - dx;
                        break;
                    case ResizeDirection.Right:
                        nb.Width = originalBounds.Width + dx;
                        break;
                    case ResizeDirection.Top:
                        nb.Y += dy;
                        nb.Height = originalBounds.Height - dy;
                        break;
                    case ResizeDirection.Bottom:
                        nb.Height = originalBounds.Height + dy;
                        break;
                    case ResizeDirection.TopLeft:
                        nb.X += dx;
                        nb.Width = originalBounds.Width - dx;
                        nb.Y += dy;
                        nb.Height = originalBounds.Height - dy;
                        break;
                    case ResizeDirection.TopRight:
                        nb.Width = originalBounds.Width + dx;
                        nb.Y += dy;
                        nb.Height = originalBounds.Height - dy;
                        break;
                    case ResizeDirection.BottomLeft:
                        nb.X += dx;
                        nb.Width = originalBounds.Width - dx;
                        nb.Height = originalBounds.Height + dy;
                        break;
                    case ResizeDirection.BottomRight:
                        nb.Width = originalBounds.Width + dx;
                        nb.Height = originalBounds.Height + dy;
                        break;
                }

                // 최소 크기 제한
                if (nb.Width < 20) nb.Width = 20;
                if (nb.Height < 20) nb.Height = 20;

                //flicker 방지용 Layout suspend/resume (이진희)
                pictureBox1.SuspendLayout();
                pictureBox1.Bounds = nb;
                pictureBox1.ResumeLayout();

                return;
            }

            // 2) 리사이즈가 아니고 드래그도 아닐 때 → 커서 모양 변경
            const int edge = 5;
            bool atTop = e.Y <= edge;
            bool atBottom = e.Y >= pictureBox1.Height - edge;
            bool atLeft = e.X <= edge;
            bool atRight = e.X >= pictureBox1.Width - edge;

            if (!isDragging)
            {
                if (atTop && atLeft)
                    pictureBox1.Cursor = Cursors.SizeNWSE;
                else if (atTop && atRight)
                    pictureBox1.Cursor = Cursors.SizeNESW;
                else if (atBottom && atLeft)
                    pictureBox1.Cursor = Cursors.SizeNESW;
                else if (atBottom && atRight)
                    pictureBox1.Cursor = Cursors.SizeNWSE;
                else if (atTop || atBottom)
                    pictureBox1.Cursor = Cursors.SizeNS;
                else if (atLeft || atRight)
                    pictureBox1.Cursor = Cursors.SizeWE;
                else
                    pictureBox1.Cursor = Cursors.Default;
            }
            // 3) 드래그 중이면 이동
            else
            {
                Point current = Control.MousePosition;
                int dx = current.X - lastMousePosition.X;
                int dy = current.Y - lastMousePosition.Y;
                pictureBox1.Location = new Point(
                    pictureBox1.Location.X + dx,
                    pictureBox1.Location.Y + dy
                );
                lastMousePosition = current;
            }
        }




        // 마우스 버튼을 놓을 때 호출됨
        // 드래그 종료 및 선택 테두리 해제
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            // 선택 해제하고 싶을 경우 주석 해제
            //showSelectionBorder = false;

            isResizing = false;

            resizeDir = ResizeDirection.None;

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