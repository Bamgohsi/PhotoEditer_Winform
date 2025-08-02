using System;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {

        //새로운 탭 번호를 세어주는 변수
        private int tabCount = 1;
        // 삭제된 번호 저장소
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();


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
        private void btn_Save_Click(object sender, EventArgs e)        // TODO: 저장 기능 구현 (찬송)
        {
            // 픽쳐박스에 넣은 사진일 없을 때
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("저장할 이미지가 없습니다.");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "이미지 저장";
            saveFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";


            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;
                    string extension = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                    switch (extension)
                    {
                        case ".jpg":
                        case ".jpeg":
                            format = System.Drawing.Imaging.ImageFormat.Jpeg;
                            break;
                        case ".bmp":
                            format = System.Drawing.Imaging.ImageFormat.Bmp;
                            break;
                        case ".png":
                            format = System.Drawing.Imaging.ImageFormat.Bmp;
                            break;
                        case ".gif":
                            format = System.Drawing.Imaging.ImageFormat.Bmp;
                            break;
                    }

                    pictureBox1.Image.Save(saveFileDialog.FileName, format);
                    MessageBox.Show("이미지가 성공적으로 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"이미지 저장 중 오류가 발생했습니다:\n{ex.Message}");
                }
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

        private void btnNewTabPage_Click(object sender, EventArgs e)       //탭페이지 추가 버튼 이벤트         
        {
            TabPage newTabPage = new TabPage($"tp {tabCount + 1}");

            if (deletedTabs.Count > 0)
            {
                // 최근 삭제된 탭을 복원
                newTabPage = deletedTabs.Pop();
            }
            else
            {
                // 새 탭 생성
                tabCount++;
            }
            newTabPage.BackColor = Color.White;
            // TabControl에 탭 추가
            tabControl1.TabPages.Add(newTabPage);
            // 새로 만든 탭으로 전환
            tabControl1.SelectedTab = newTabPage;
        }

        private void btnDltTabPage_Click(object sender, EventArgs e)   //탭페이지 삭제 버튼 이벤트
        {
            if (tabControl1.TabPages.Count <= 1)
            {
                MessageBox.Show("하나의 탭은 남아있어야 합니다.");
                return;
            }
            // 가장 마지막 탭 가져오기
            int lastIndex = tabControl1.TabPages.Count - 1;
            TabPage lastTab = tabControl1.TabPages[lastIndex];
            TabPage selectedTab = tabControl1.SelectedTab;
            // 탭 제거
            tabControl1.TabPages.Remove(lastTab);



            // 삭제하고 나서 마지막 탭을 자동으로 선택
            tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
            deletedTabs.Push(selectedTab); // 삭제된 탭 저장

        }
    }
}