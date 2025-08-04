using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using Microsoft.VisualBasic;

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
        private double imageAspectRatio;  // 리사이즈 시작 시점의 스크린좌표(이진희)
        private Point resizeStartScreenPoint;  //리사이즈 시작 시점의 스크린 좌표(이진희)


        public Form1()
        {
            this.KeyPreview = true;
            InitializeComponent();

            lblWidth.Click += LblWidth_Click;
            lblHeight.Click += LblHeight_Click;


            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;

            
            // PictureBox 드래그 처리 이벤트 연결(이진희)
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;

            btnRotateRight.Click += BtnRotateRight_Click;
            btnRotateLeft.Click += BtnRotateLeft_Click;
            // 폼(및 지식컨트롤) 클릭 시 테두리 해제 이벤트 연결(이진희)
            this.MouseDown += Form1_MouseDown;

            // 재귀적으로 모든 지식 컨트롤에도 연결합니둥(이진희)
            HookMouseDown(this);
        }

        // ▶ 오른쪽으로 90° 회전(이진희)
        private void BtnRotateRight_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);

                int oldW = pictureBox1.Width;
                int oldH = pictureBox1.Height;
                pictureBox1.SuspendLayout();
                pictureBox1.Width = oldH;
                pictureBox1.Height = oldW;
                pictureBox1.ResumeLayout();

                // 3) 라벨에도 갱신된 크기를 표시
                lblWidth.Text = $"Width: {pictureBox1.Width}";
                lblHeight.Text = $"Height: {pictureBox1.Height}";

                pictureBox1.Refresh();
            }
        }

        // ▶ 왼쪽으로 90° 회전(이진희)
        private void BtnRotateLeft_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                // Rotate270FlipNone == 90° 반시계
                pictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                int oldW = pictureBox1.Width;
                int oldH = pictureBox1.Height;
                pictureBox1.SuspendLayout();
                pictureBox1.Width = oldH;
                pictureBox1.Height = oldW;
                pictureBox1.ResumeLayout();

                // 3) 라벨 갱신
                lblWidth.Text = $"Width: {pictureBox1.Width}";
                lblHeight.Text = $"Height: {pictureBox1.Height}";
                pictureBox1.Refresh();
            }
        }



        // ★추가: lblWidth 클릭 → 너비 입력
        private void LblWidth_Click(object sender, EventArgs e)
        {
            string input = Interaction.InputBox(
                "새 너비를 입력하세요:",
                "너비 변경",
                pictureBox1.Width.ToString()
            );
            if (int.TryParse(input, out int newW) && newW >= 20)
            {
                pictureBox1.Width = newW;
                lblWidth.Text = $"Width: {pictureBox1.Width}";
            }
        }

        // ★추가: lblHeight 클릭 → 높이 입력
        private void LblHeight_Click(object sender, EventArgs e)
        {
            string input = Interaction.InputBox(
                "새 높이를 입력하세요:",
                "높이 변경",
                pictureBox1.Height.ToString()
            );
            if (int.TryParse(input, out int newH) && newH >= 20)
            {
                pictureBox1.Height = newH;
                lblHeight.Text = $"Height: {pictureBox1.Height}";
            }
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
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
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
        // ──────────────────────────────────────────────────────────────────────────────
        // 1) pictureBox1_MouseDown: 모서리 vs 사이드 판별 → resizeDir 설정
        // ──────────────────────────────────────────────────────────────────────────────
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                const int edge = 5;
                bool atTop = e.Y <= edge;
                bool atBottom = e.Y >= pictureBox1.Height - edge;
                bool atLeft = e.X <= edge;
                bool atRight = e.X >= pictureBox1.Width - edge;

                // ▶ 모서리 먼저 판별
                if (atLeft && atTop)
                    resizeDir = ResizeDirection.TopLeft;
                else if (atRight && atTop)
                    resizeDir = ResizeDirection.TopRight;
                else if (atLeft && atBottom)
                    resizeDir = ResizeDirection.BottomLeft;
                else if (atRight && atBottom)
                    resizeDir = ResizeDirection.BottomRight;

                // ▶ 사이드 판별 (모서리가 아니면)
                else if (atLeft)
                    resizeDir = ResizeDirection.Left;    // X축만
                else if (atRight)
                    resizeDir = ResizeDirection.Right;   // X축만
                else if (atTop)
                    resizeDir = ResizeDirection.Top;     // Y축만
                else if (atBottom)
                    resizeDir = ResizeDirection.Bottom;  // Y축만

                else
                    resizeDir = ResizeDirection.None;

                // ★디버깅: 창 제목에 방향 찍어보기
                this.Text = $"Dir: {resizeDir}";

                if (resizeDir != ResizeDirection.None)
                {
                    isResizing = true;
                    resizeStartScreenPoint = Control.MousePosition;
                    originalBounds = pictureBox1.Bounds;
                    
                    // 드래그시작 시점의 원본이미지 비율 계산(이진희)
                     imageAspectRatio = (double)pictureBox1.Image.Width / pictureBox1.Image.Height;
                    return;  // 리사이즈 모드 진입
                }

                // ▼ 여긴 드래그(이동) 모드
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pictureBox1.Invalidate();
                lastMousePosition = Control.MousePosition;
            }
        }


        // ──────────────────────────────────────────────────────────────────────────────
        // 2) pictureBox1_MouseMove: resizeDir에 따라 축별/모서리 리사이즈 수행
        // ──────────────────────────────────────────────────────────────────────────────
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                Point cur = Control.MousePosition;
                int dx = cur.X - resizeStartScreenPoint.X;
                int dy = cur.Y - resizeStartScreenPoint.Y;

                Rectangle nb = originalBounds;
                switch (resizeDir)
                {
                    // ▶ 모서리: X·Y 둘 다
                    case ResizeDirection.TopLeft:
                        nb.X = originalBounds.X + dx;
                        nb.Width = originalBounds.Width - dx;
                        nb.Y = originalBounds.Y + dy;
                        nb.Height = originalBounds.Height - dy;
                        break;
                    case ResizeDirection.TopRight:
                        nb.Width = originalBounds.Width + dx;
                        nb.Y = originalBounds.Y + dy;
                        nb.Height = originalBounds.Height - dy;
                        break;
                    case ResizeDirection.BottomLeft:
                        nb.X = originalBounds.X + dx;
                        nb.Width = originalBounds.Width - dx;
                        nb.Height = originalBounds.Height + dy;
                        break;
                    case ResizeDirection.BottomRight:
                        nb.Width = originalBounds.Width + dx;
                        nb.Height = originalBounds.Height + dy;
                        break;

                    // ▶ 사이드 가로: X만
                    case ResizeDirection.Left:
                        nb.X = originalBounds.X + dx;
                        nb.Width = originalBounds.Width - dx;
                        // 수정: 세로고정-> 가로만 리사이즈함 (이진희)
                        nb.Height = originalBounds.Height;
                        break;
                
                    case ResizeDirection.Right:
                        nb.Width = originalBounds.Width + dx;
                        // 세로고정 -> 가로만 리사이즈함(이진희)
                        nb.Height = originalBounds.Height;
                        break;

                    // ▶ 사이드 세로: Y만
                  
                    case ResizeDirection.Top:
                        nb.Y = originalBounds.Y + dy;
                        nb.Height = originalBounds.Height - dy;
                        // 가로고정-> 세로만 리사이즈(이진희)
                        nb.Width = originalBounds.Width;
                        break;

                    case ResizeDirection.Bottom:
                        nb.Height = originalBounds.Height + dy;
                        // 가로고정 -> 세로만 리사이즈(이진희)
                        nb.Width = originalBounds.Width;
                        break;
                }

                // 최소 크기 제한
                if (nb.Width < 20) nb.Width = 20;
                if (nb.Height < 20) nb.Height = 20;

                pictureBox1.SuspendLayout();
                pictureBox1.Bounds = nb;
                pictureBox1.ResumeLayout();
                lblWidth.Text = $"Width: {pictureBox1.Width}";
                lblHeight.Text = $"Height: {pictureBox1.Height}";
                return;
            }

            // 리사이즈 모드가 아닐 땐 커서 모양 변경/드래그 유지
            const int edge = 5;
            bool atTop = e.Y <= edge;
            bool atBottom = e.Y >= pictureBox1.Height - edge;
            bool atLeft = e.X <= edge;
            bool atRight = e.X >= pictureBox1.Width - edge;

            if (!isDragging)
            {
                if (atTop && atLeft) pictureBox1.Cursor = Cursors.SizeNWSE;
                else if (atTop && atRight) pictureBox1.Cursor = Cursors.SizeNESW;
                else if (atBottom && atLeft) pictureBox1.Cursor = Cursors.SizeNESW;
                else if (atBottom && atRight) pictureBox1.Cursor = Cursors.SizeNWSE;
                else if (atTop || atBottom) pictureBox1.Cursor = Cursors.SizeNS;
                else if (atLeft || atRight) pictureBox1.Cursor = Cursors.SizeWE;
                else pictureBox1.Cursor = Cursors.Default;
            }
            else
            {
                // 이동(드래그)
                Point current = Control.MousePosition;
                int dx2 = current.X - lastMousePosition.X;
                int dy2 = current.Y - lastMousePosition.Y;
                pictureBox1.Location = new Point(
                    pictureBox1.Location.X + dx2,
                    pictureBox1.Location.Y + dy2
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