using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D; // DashStyle 사용을 위해 추가

namespace photo
{
    public partial class Form1 : Form
    {
        // Constants for layout
        private const int LeftMargin = 20; // 폼 왼쪽 여백
        private const int TopMargin = 90; // 폼 상단 여백 (tabControl 아래)
        private const int PanelWidth = 300; // 오른쪽 패널의 고정 너비
        private const int PanelRightMargin = 10; // 오른쪽 패널의 폼 오른쪽 여백
        private const int GapBetweenPictureBoxAndPanel = 20; // pictureBox1과 오른쪽 패널 사이의 간격
        private const int BottomMargin = 20; // 폼 하단 여백

        // 이미지 드래그 중 여부를 나타내는 플래그 (배경 이미지용)
        private bool isDragging = false;

        // 드래그 시작 시 마우스 클릭 지점 좌표 (배경 이미지용)
        private Point clickOffset;

        // 선택 테두리를 표시할지 여부 (마우스 클릭 시 true) (배경 이미지용)
        private bool showSelectionBorder = false;

        // 동적으로 생성할 버튼과 패널 배열
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;

        // 현재 표시된 패널을 추적하는 변수
        private Panel currentVisiblePanel = null;

        // --- [NEW] 합성/미리보기용 변수 ---
        private Image emojiPreviewImage = null;     // 현재 드래그 중 이모티콘 (원본 이미지)
        // 이모티콘 크기 입력 필드가 제거되었으므로, 기본 크기를 설정합니다.
        private int emojiPreviewWidth = 64;         // 기본 크기
        private int emojiPreviewHeight = 64;        // 기본 크기
        private Point emojiPreviewLocation = Point.Empty; // 점선 위치
        private bool showEmojiPreview = false;      // 점선 미리보기 표시 여부

        // 선택된 이모티콘 PictureBox (드래그, 크기 조절용)
        private PictureBox selectedEmoji = null;
        private Point dragOffset; // 이모티콘 드래그 시작 시 오프셋
        private bool resizing = false; // 이모티콘 크기 조절 중 여부
        private const int handleSize = 10; // 크기 조절 핸들 크기


        public Form1()
        {
            InitializeComponent();

            // Initial setup for pictureBox1
            // Set initial location and size to fill the available space, respecting margins and panel area
            pictureBox1.Location = new Point(LeftMargin, TopMargin);
            int availableWidthForPb1 = this.ClientSize.Width - LeftMargin - (PanelWidth + PanelRightMargin + GapBetweenPictureBoxAndPanel);
            int availableHeightForPb1 = this.ClientSize.Height - TopMargin - BottomMargin;
            pictureBox1.Size = new Size(Math.Max(100, availableWidthForPb1), Math.Max(100, availableHeightForPb1)); // 최소 크기 보장
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Use Zoom to fit image within bounds

            InitializeDynamicControls(); // This will also use the updated client size for panel positioning

            // PictureBox1에 드래그 앤 드롭 이벤트 연결
            pictureBox1.AllowDrop = true;
            pictureBox1.DragEnter += pictureBox1_DragEnter;
            pictureBox1.DragOver += pictureBox1_DragOver;
            pictureBox1.DragLeave += pictureBox1_DragLeave;
            pictureBox1.DragDrop += pictureBox1_DragDrop;

            // pictureBox1에 커스텀 그리기(Paint) 이벤트 연결
            pictureBox1.Paint += pictureBox1_Paint;

            this.WindowState = FormWindowState.Maximized; // 전체화면으로 시작
            this.Resize += Form1_Resize; // Add resize event handler for responsive layout

            // 폼 배경 클릭 시 모든 선택 해제 (이모티콘 및 배경 이미지)
            this.MouseDown += Form1_MouseDown;
            // 탭 페이지 클릭 시도 모든 선택 해제 (tabPage2가 Form1의 직접 자식이라고 가정)
            // 만약 TabControl 내부에 있다면, TabControl의 MouseDown 이벤트를 활용하거나
            // Form1_MouseDown이 대부분의 빈 공간 클릭을 처리할 것입니다.
            tabPage2.MouseDown += Form1_MouseDown;
        }

        // 폼 크기 변경 시 컨트롤들의 위치와 크기를 재조정
        private void Form1_Resize(object sender, EventArgs e)
        {
            // Recalculate and set the size and location of pictureBox1
            int availableWidthForPb1 = this.ClientSize.Width - LeftMargin - (PanelWidth + PanelRightMargin + GapBetweenPictureBoxAndPanel);
            int availableHeightForPb1 = this.ClientSize.Height - TopMargin - BottomMargin;
            pictureBox1.Size = new Size(Math.Max(100, availableWidthForPb1), Math.Max(100, availableHeightForPb1));
            pictureBox1.Location = new Point(LeftMargin, TopMargin);

            // Recalculate and set the location and size of dynamicPanels
            Point panelLocation = new Point(this.ClientSize.Width - (PanelWidth + PanelRightMargin), TopMargin);
            Size panelSize = new Size(PanelWidth, this.ClientSize.Height - TopMargin - BottomMargin);

            foreach (Panel panel in dynamicPanels)
            {
                panel.Location = panelLocation;
                panel.Size = panelSize;
                panel.Invalidate(); // Redraw panel borders if visible
            }

            // tabControl1의 위치도 필요하다면 재조정 (현재는 고정 위치로 가정)
            // tabControl1.Location = new Point(12, 12);
        }

        // 흰색 또는 유사 흰색 픽셀을 투명하게 만드는 함수 (기존 코드 유지, 현재 사용되지 않음)
        private Bitmap MakeWhiteLikeTransparent(Bitmap src, int tolerance = 50)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
                g.DrawImage(src, 0, 0);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            int transparentCount = 0;
            unsafe
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    byte* row = (byte*)data.Scan0 + y * data.Stride;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        // byte a = row[x * 4 + 3]; // 'a'는 사용되지 않으므로 주석 처리

                        if (
                            Math.Abs(r - 255) <= tolerance &&
                            Math.Abs(g - 255) <= tolerance &&
                            Math.Abs(b - 255) <= tolerance
                        )
                        {
                            row[x * 4 + 3] = 0; // 완전 투명으로!
                            transparentCount++;
                        }
                    }
                }
            }
            bmp.UnlockBits(data);
            MessageBox.Show($"투명화된 픽셀 수: {transparentCount}");
            return bmp;
        }

        // PictureBox1 위에서 드래그 중일 때 호출됨 (점선 미리보기 업데이트)
        private void pictureBox1_DragOver(object sender, DragEventArgs e)
        {
            Point clientPos = pictureBox1.PointToClient(new Point(e.X, e.Y));
            emojiPreviewLocation = clientPos; // 마우스 위치를 미리보기 위치로 설정
            showEmojiPreview = true; // 미리보기 표시
            pictureBox1.Invalidate(); // PictureBox1을 다시 그려서 미리보기 업데이트
            e.Effect = DragDropEffects.Copy; // 드롭 효과를 복사로 설정
        }

        // PictureBox1에서 드래그가 벗어났을 때 호출됨 (점선 미리보기 숨김)
        private void pictureBox1_DragLeave(object sender, EventArgs e)
        {
            showEmojiPreview = false; // 미리보기 숨김
            pictureBox1.Invalidate(); // PictureBox1을 다시 그려서 미리보기 제거
        }

        // 폼 바탕 (또는 탭 페이지) 클릭 시 전체 선택 해제
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            // 1. 모든 이모티콘 PictureBox 선택 해제
            // PictureBox1의 자식 컨트롤들을 순회하며 이모티콘 PictureBox를 찾음
            if (pictureBox1 != null) // pictureBox1이 null이 아닐 때만 실행
            {
                foreach (Control c in pictureBox1.Controls)
                {
                    if (c is PictureBox pic)
                    {
                        pic.Tag = null; // 선택 해제
                        pic.Invalidate(); // 다시 그려서 테두리 제거
                    }
                }
            }
            selectedEmoji = null; // 선택된 이모티콘 참조 해제

            // 2. 배경 이미지의 파란 테두리 해제
            showSelectionBorder = false;
            pictureBox1.Invalidate(); // PictureBox1을 다시 그려서 테두리 제거
        }

        // 드래그 중 마우스가 pictureBox1 위에 들어왔을 때
        private void pictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            // 드래그하는 데이터가 이미지인지 확인
            if (e.Data.GetDataPresent(typeof(Bitmap)) || e.Data.GetDataPresent(typeof(Image)))
                e.Effect = DragDropEffects.Copy; // 이미지면 복사 효과 허용
            else
                e.Effect = DragDropEffects.None; // 아니면 허용 안 함
        }

        // PictureBox1에 이모티콘을 드롭했을 때
        private void pictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            // 배경 이미지가 없으면 드롭 불가
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("먼저 배경 이미지를 열어주세요!");
                showEmojiPreview = false; // 미리보기 숨김
                pictureBox1.Invalidate(); // 미리보기 제거를 위해 다시 그리기
                return;
            }

            // 드래그 중인 이모티콘 이미지가 없으면 리턴
            if (emojiPreviewImage == null)
            {
                showEmojiPreview = false; // 미리보기 숨김
                pictureBox1.Invalidate(); // 미리보기 제거를 위해 다시 그리기
                return;
            }

            // 새로운 PictureBox 컨트롤을 생성하여 드롭된 이모티콘을 표시
            PictureBox newEmojiPic = new PictureBox
            {
                Image = (Image)emojiPreviewImage.Clone(), // 원본 이미지 복제 (중요! 원본 리소스가 해제되지 않도록)
                SizeMode = PictureBoxSizeMode.StretchImage, // 지정된 크기에 맞춰 늘림
                Size = new Size(emojiPreviewWidth, emojiPreviewHeight), // 입력된 크기 적용 (기본값 64x64)
                Location = new Point(
                    emojiPreviewLocation.X - emojiPreviewWidth / 2, // 드롭 위치에 이모티콘 중앙 정렬
                    emojiPreviewLocation.Y - emojiPreviewHeight / 2
                ),
                BackColor = Color.Transparent, // PictureBox의 배경을 투명하게 설정
                Cursor = Cursors.SizeAll, // 드래그 가능한 커서로 변경
                Tag = "selected" // 초기에는 선택된 상태로 설정
            };

            // 새로 생성된 이모티콘 PictureBox에 이벤트 핸들러 연결
            newEmojiPic.MouseDown += Emoji_MouseDown;
            newEmojiPic.MouseMove += Emoji_MouseMove;
            newEmojiPic.MouseUp += Emoji_MouseUp;
            newEmojiPic.Paint += Emoji_Paint;

            // PictureBox1의 자식 컨트롤로 추가하여 배경 이미지 위에 떠다니게 함
            pictureBox1.Controls.Add(newEmojiPic);

            // 이전에 선택된 이모티콘이 있다면 선택 해제
            if (selectedEmoji != null && selectedEmoji != newEmojiPic)
            {
                selectedEmoji.Tag = null;
                selectedEmoji.Invalidate();
            }
            selectedEmoji = newEmojiPic; // 새로 드롭된 이모티콘을 선택된 이모티콘으로 설정

            // 점선 미리보기 숨김
            showEmojiPreview = false;
            pictureBox1.Invalidate(); // PictureBox1을 다시 그려서 점선 미리보기 제거
        }

        // 이모티콘 PictureBox의 마우스 다운 이벤트 (선택, 드래그, 크기 조절 시작)
        private void Emoji_MouseDown(object sender, MouseEventArgs e)
        {
            // 다른 모든 이모티콘 PictureBox의 선택을 해제
            foreach (Control c in pictureBox1.Controls)
            {
                if (c is PictureBox pic)
                {
                    pic.Tag = null;
                    pic.Invalidate();
                }
            }

            selectedEmoji = sender as PictureBox; // 클릭된 이모티콘을 선택된 이모티콘으로 설정
            if (selectedEmoji != null)
            {
                selectedEmoji.Tag = "selected"; // 선택됨으로 표시
                selectedEmoji.Invalidate(); // 다시 그려서 테두리 표시

                if (e.Button == MouseButtons.Left)
                {
                    // 크기 조절 핸들 영역 확인 (오른쪽 아래 모서리)
                    Rectangle resizeHandle = new Rectangle(
                        selectedEmoji.Width - handleSize,
                        selectedEmoji.Height - handleSize,
                        handleSize,
                        handleSize
                    );

                    if (resizeHandle.Contains(e.Location))
                    {
                        resizing = true; // 크기 조절 모드
                    }
                    else
                    {
                        resizing = false; // 드래그 모드
                        dragOffset = e.Location; // 드래그 시작 시 마우스 오프셋 저장
                    }
                }
            }
        }

        // 이모티콘 PictureBox의 마우스 이동 이벤트 (드래그 또는 크기 조절)
        private void Emoji_MouseMove(object sender, MouseEventArgs e)
        {
            var emoji = sender as PictureBox;
            if (e.Button == MouseButtons.Left && selectedEmoji == emoji)
            {
                if (resizing)
                {
                    // 최소 크기 32, 최대 크기 PictureBox1의 크기 (또는 적절한 최대값)
                    int newW = Math.Max(32, e.X);
                    int newH = Math.Max(32, e.Y);

                    // PictureBox1 경계를 넘지 않도록 크기 제한
                    newW = Math.Min(newW, pictureBox1.Width - emoji.Left);
                    newH = Math.Min(newH, pictureBox1.Height - emoji.Top);

                    emoji.Size = new Size(newW, newH);
                }
                else
                {
                    // 위치 이동
                    Point newLoc = emoji.Location;
                    newLoc.Offset(e.X - dragOffset.X, e.Y - dragOffset.Y);

                    // PictureBox1 경계를 넘지 않도록 위치 제한
                    newLoc.X = Math.Max(0, Math.Min(newLoc.X, pictureBox1.Width - emoji.Width));
                    newLoc.Y = Math.Max(0, Math.Min(newLoc.Y, pictureBox1.Height - emoji.Height));

                    emoji.Location = newLoc;
                }
                emoji.Invalidate(); // 변경된 이모티콘 PictureBox를 다시 그려서 업데이트
            }
        }

        // 이모티콘 PictureBox의 마우스 업 이벤트 (드래그 또는 크기 조절 종료)
        private void Emoji_MouseUp(object sender, MouseEventArgs e)
        {
            resizing = false; // 크기 조절 모드 해제
        }

        // 이모티콘 PictureBox의 Paint 이벤트 (선택 테두리 및 크기 조절 핸들 그리기)
        private void Emoji_Paint(object sender, PaintEventArgs e)
        {
            var emoji = sender as PictureBox;
            // Tag가 "selected"로 설정된 경우에만 테두리 및 핸들 그리기
            if (emoji.Tag != null && emoji.Tag.ToString() == "selected")
            {
                // 파란 테두리 그리기
                using (Pen p = new Pen(Color.DeepSkyBlue, 2))
                    e.Graphics.DrawRectangle(p, 1, 1, emoji.Width - 3, emoji.Height - 3); // 픽셀 오프셋 고려

                // 크기 조절용 핸들 (오른쪽 아래 사각형) 그리기
                e.Graphics.FillRectangle(Brushes.DeepSkyBlue, emoji.Width - handleSize, emoji.Height - handleSize, handleSize, handleSize);
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

            // 8번째 패널 (인덱스 7)에 이모티콘 관련 컨트롤 추가
            var panel8 = dynamicPanels[7];
            // 이모티콘 크기 입력 필드 제거
            // panel8.Controls.Add(new Label { Text = "이모티콘 크기:", Location = new Point(10, 40) });
            // TextBox txtEmojiWidth = new TextBox { Name = "txtEmojiWidth", Text = "64", Location = new Point(10, 60), Width = 50 };
            // TextBox txtEmojiHeight = new TextBox { Name = "txtEmojiHeight", Text = "64", Location = new Point(70, 60), Width = 50 };
            // panel8.Controls.Add(new Label { Text = "W:", Location = new Point(10, 80) });
            // panel8.Controls.Add(new Label { Text = "H:", Location = new Point(70, 80) });
            // panel8.Controls.Add(txtEmojiWidth);
            // panel8.Controls.Add(txtEmojiHeight);

            // 크기 입력이 바뀔 때마다 값 저장! (숫자 변환 실패 시 기본값 유지)
            // txtEmojiWidth.TextChanged += (s, e) => { if (!int.TryParse(txtEmojiWidth.Text, out emojiPreviewWidth)) emojiPreviewWidth = 64; };
            // txtEmojiHeight.TextChanged += (s, e) => { if (!int.TryParse(txtEmojiHeight.Text, out emojiPreviewHeight)) emojiPreviewHeight = 64; };

            panel8.AllowDrop = true;  // 드래그 가능
            panel8.AutoScroll = true; // 스크롤 가능

            // 리소스에 추가한 이모티콘 이미지 이름으로 배열 생성
            // (Properties.Resources.EmojiX는 사용자가 프로젝트 리소스에 추가했다고 가정)
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

            int iconSize = 48, padding = 8; // 이모티콘 아이콘 크기 및 패딩
            // 이모티콘 크기 입력 필드가 제거되었으므로, 이모티콘 목록의 시작 Y 위치를 조정합니다.
            int startYForEmojis = 40; // 패널 상단에서 적절한 시작 위치
            int iconsPerRow = (panel8.Width - padding * 2) / (iconSize + padding); // 한 줄에 표시될 아이콘 수 계산

            for (int i = 0; i < emojis.Length; i++)
            {
                var pic = new PictureBox
                {
                    Image = emojis[i],
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(iconSize, iconSize),
                    Cursor = Cursors.Hand, // 드래그 가능한 커서
                    Location = new Point(
                        padding + (i % iconsPerRow) * (iconSize + padding),
                        startYForEmojis + (i / iconsPerRow) * (iconSize + padding)
                    )
                };
                // 이모티콘 PictureBox의 마우스 다운 이벤트: 드래그 시작
                pic.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        emojiPreviewImage = ((PictureBox)s).Image; // 드래그할 이모티콘 이미지 저장
                        // DoDragDrop 호출하여 드래그 앤 드롭 작업 시작
                        (s as PictureBox).DoDragDrop((s as PictureBox).Image, DragDropEffects.Copy);
                    }
                };

                panel8.Controls.Add(pic);
            }

            // 첫 번째 패널을 초기 상태에서 보이게 설정하고, 테두리를 그리기 위해 Invalidate 호출
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

        // [새로 만들기] 버튼 클릭 시 실행
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            // 배경 이미지 제거
            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;

            // 추가된 모든 이모티콘 PictureBox 제거
            foreach (Control c in pictureBox1.Controls)
            {
                if (c is PictureBox pic)
                {
                    pic.Dispose();
                }
            }
            pictureBox1.Controls.Clear(); // 모든 자식 컨트롤 제거
            selectedEmoji = null; // 선택된 이모티콘 초기화
            showSelectionBorder = false; // 배경 이미지 테두리 숨김
            pictureBox1.Invalidate(); // PictureBox1 다시 그리기
        }

        // [열기] 버튼 클릭 시 실행
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "이미지 열기";
            openFileDialog.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose(); // 기존 이미지 해제
                    Image img = Image.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = img;
                    // PictureBox의 크기와 위치는 Form1_Resize 또는 초기 설정에서 이미 처리되므로,
                    // AutoSize와 직접적인 Size/Location 설정은 제거합니다.
                    // pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize; // 제거
                    // pictureBox1.Size = img.Size; // 제거
                    // pictureBox1.Location = new Point(10, 10); // 제거

                    // 배경 이미지 로드 시 기존 이모티콘 모두 제거
                    foreach (Control c in pictureBox1.Controls)
                    {
                        if (c is PictureBox pic)
                        {
                            pic.Dispose();
                        }
                    }
                    pictureBox1.Controls.Clear();
                    selectedEmoji = null;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지를 불러오는 중 오류 발생: " + ex.Message);
                }
            }
        }

        // [저장] 버튼 클릭 시 실행 (합성된 최종 이미지를 저장)
        private void btn_Save_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("저장할 이미지가 없습니다.");
                return;
            }

            // 최종 합성될 이미지를 위한 Bitmap 생성
            // PictureBox1의 현재 크기를 사용 (이미지 크기가 아님)
            Bitmap finalImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                // PictureBox1의 배경 이미지 그리기
                if (pictureBox1.Image != null)
                {
                    g.DrawImage(pictureBox1.Image, 0, 0, pictureBox1.Width, pictureBox1.Height);
                }

                // PictureBox1 위에 있는 모든 이모티콘 PictureBox 그리기
                foreach (Control control in pictureBox1.Controls)
                {
                    if (control is PictureBox emojiPic)
                    {
                        // 이모티콘 PictureBox의 이미지를 실제 위치와 크기로 그림
                        g.DrawImage(emojiPic.Image, emojiPic.Location.X, emojiPic.Location.Y, emojiPic.Width, emojiPic.Height);
                    }
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "이미지 저장";
            saveFileDialog.Filter = "PNG 이미지|*.png|JPEG 이미지|*.jpg|BMP 이미지|*.bmp";
            saveFileDialog.FileName = "합성된_이미지.png"; // 기본 파일 이름

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // 선택된 파일 형식에 따라 저장
                    switch (saveFileDialog.FilterIndex)
                    {
                        case 1: // PNG
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            break;
                        case 2: // JPEG
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            break;
                        case 3: // BMP
                            finalImage.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                            break;
                    }
                    MessageBox.Show("이미지가 성공적으로 저장되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("이미지 저장 중 오류 발생: " + ex.Message);
                }
            }
            finalImage.Dispose(); // 사용 후 Bitmap 해제
        }


        // 배경 이미지 (pictureBox1)의 마우스 버튼을 누를 때 호출됨
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // 이모티콘이 아닌 배경 이미지를 드래그 시작
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                // 다른 모든 이모티콘 PictureBox의 선택을 해제
                foreach (Control c in pictureBox1.Controls)
                {
                    if (c is PictureBox pic)
                    {
                        pic.Tag = null;
                        pic.Invalidate();
                    }
                }
                selectedEmoji = null; // 선택된 이모티콘 참조 해제

                isDragging = true; // 배경 이미지 드래그 시작 플래그
                clickOffset = e.Location; // 클릭 오프셋 저장
                showSelectionBorder = true; // 배경 이미지 선택 테두리 표시
                pictureBox1.Invalidate(); // PictureBox1을 다시 그려서 테두리 업데이트
            }
        }

        // 배경 이미지 (pictureBox1) 마우스를 이동할 때 호출됨
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;
                pictureBox1.Location = newLocation; // PictureBox1 위치 이동
            }
        }

        // 배경 이미지 (pictureBox1) 마우스 버튼을 놓을 때 호출됨
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false; // 배경 이미지 드래그 종료
            // showSelectionBorder = false; // 배경 이미지 선택 해제하고 싶을 경우 주석 해제 (클릭 시 해제되도록 Form1_MouseDown에서 처리)
            pictureBox1.Invalidate(); // PictureBox1 다시 그리기
        }

        // 폼 로드 시 실행 (필요 시 초기화 처리 가능)
        private void Form1_Load(object sender, EventArgs e)
        {
            // 초기화 로직 (현재는 특별히 없음)
        }

        // pictureBox1이 다시 그려질 때 호출됨 (점선 미리보기 및 선택 테두리 그림)
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // --- 점선 미리보기 그리기 ---
            if (showEmojiPreview && emojiPreviewImage != null)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = DashStyle.Dash; // 점선 스타일
                    Rectangle rect = new Rectangle(
                        emojiPreviewLocation.X - emojiPreviewWidth / 2, // 미리보기 위치 계산
                        emojiPreviewLocation.Y - emojiPreviewHeight / 2,
                        emojiPreviewWidth, emojiPreviewHeight);
                    e.Graphics.DrawRectangle(pen, rect); // 점선 사각형 그리기
                }
            }

            // --- 배경 이미지 선택 테두리 그리기 ---
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.Blue, 3)) // 파란색 두꺼운 테두리
                {
                    // PictureBox1의 경계에 테두리 그리기
                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);
                }
            }
        }
    }
}