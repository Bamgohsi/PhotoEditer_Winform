using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
        // �̹��� �巡�� �� ���θ� ��Ÿ���� �÷���
        private bool isDragging = false;

        // �巡�� ���� �� ���콺 Ŭ�� ���� ��ǥ
        private Point clickOffset;

        // ���� �׵θ��� ǥ������ ���� (���콺 Ŭ�� �� true)
        private bool showSelectionBorder = false;

        // �������� ������ ��ư�� �г� �迭
        private Button[] dynamicButtons;
        private Panel[] dynamicPanels;

        // ���� ǥ�õ� �г��� �����ϴ� ����
        private Panel currentVisiblePanel = null;

        // ���� �̹����� ������ �ʵ�
        private Image originalImage = null;

        // ������ũ ȿ���� ����Ǿ����� �����ϴ� ����
        private bool isMosaicApplied = false;

        //



        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();

            // pictureBox1�� Ŀ���� �׸���(Paint) �̺�Ʈ ����
            pictureBox1.Paint += pictureBox1_Paint;

            this.WindowState = FormWindowState.Maximized; // ��üȭ������ ����

            this.BackColor = Color.FromArgb(255, 45,45,45); // ���� ������ ����
            tabControl1.BackColor = Color.Gray;
            tabPage2.BackColor = Color.LightGray;

            CreateButtons(); // ��� ��ư ���� �޼��� ȣ��
        }

        /// ��ư�� �г��� �������� �����ϰ� �ʱ�ȭ�մϴ�.
        /// ���� ��ư
        private void InitializeDynamicControls()
        {
            // ��ư ���� ����
            int buttonWidth = 40;
            int buttonHeight = 40;
            int spacing = 10;
            int startX = 15;
            int startY = 95;
            int columns = 2; // 2���� ��ġ
            int buttonCount = 10; // �� ��ư ����

            dynamicButtons = new Button[buttonCount];

            // 2�� 5������ ��ư ��ġ
            for (int i = 0; i < buttonCount; i++)
            {
                // �⺻ Button Ŭ������ �ν��Ͻ��� �����մϴ�.
                Button btn = new Button();
                btn.Text = $"{i + 1}"; // ��ư �ؽ�Ʈ�� ���ڷ� ����
                btn.Size = new Size(buttonWidth, buttonHeight);
                btn.BackColor = Color.FromArgb(255, 45,45,45); // ��ư ������ ����
                btn.ForeColor = Color.FromArgb(255, 108, 117, 125); // ��ư ��Ʈ ������ ����
                btn.FlatStyle = FlatStyle.Flat; // ��ư ��Ÿ���� Flat���� ����
                btn.FlatAppearance.BorderSize = 1; // �׵θ��� ���̰� ����
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 108, 117, 125); // ��ư �׵θ� ������ #868e96�� ����

                // ��ư ��ġ ��� (2�� 5��)
                int col = i % columns;
                int row = i / columns;
                btn.Location = new Point(startX + col * (buttonWidth + spacing),
                                         startY + row * (buttonHeight + spacing));

                btn.Tag = i; // ��ư�� �ε��� ����
                btn.Click += Button_Click; // Ŭ�� �̺�Ʈ �ڵ鷯 ����
                this.Controls.Add(btn);
                dynamicButtons[i] = btn;
            }

            // �г� ���� ����
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
                    BackColor = Color.FromArgb(255, 68,68,68), // �г� ������ ����
                    Visible = false
                };

                // �гο� �� �߰�
                panel.Controls.Add(new Label()
                {
                    Text = $"���� �Ӽ� {i + 1}",
                    Location = new Point(5, 5),
                    ForeColor = Color.DarkGray // �� �ؽ�Ʈ ������ ������� ����
                });

                // �гο� Paint �̺�Ʈ �ڵ鷯 �߰�
                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel; // ������ �г��� �迭�� ����
            }
        }

        // ��� ���� ��ư�� Ŭ�� �̺�Ʈ�� ó���ϴ� ���� �ڵ鷯
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag; // ��ư�� Tag���� �ε��� ��������

                // ���� ù ��° ��ư(�ε��� 0)�� ������ũ ȿ���� ����մϴ�.
                if (index == 0)
                {
                    this.MosaicButton_Click(sender, e);
                    // �г��� ���� �ʰ�, ���� �����ִ� �г��� �ִٸ� ����
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                        currentVisiblePanel = null;
                    }
                }
                // ������ ��ư�� ����ó�� ���� �г��� ���
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
        /// �г��� Paint �̺�Ʈ �ڵ鷯: Ȱ��ȭ�� �гο� �׵θ��� �׸��ϴ�.
        /// </summary>
        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel paintedPanel = sender as Panel;

            // ���� ���̴� �г��� ��쿡�� �׵θ� �׸���
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                // �׵θ� ������ ȸ������ ����
                using (Pen pen = new Pen(Color.Gray, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // �г� ��迡 �׵θ� �׸���
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        /// ��ư Ŭ�� �� ����Ǵ� ������ũ �̺�Ʈ �ڵ鷯�Դϴ�.
        private void MosaicButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("���� �̹����� �ҷ����ּ���.");
                return;
            }

            // ������ũ ȿ���� ����� ���¶�� ���� �̹����� �ǵ����ϴ�.
            if (isMosaicApplied)
            {
                // ���� �̹����� PictureBox�� �Ҵ�
                pictureBox1.Image = new Bitmap(originalImage);
                pictureBox1.Size = originalImage.Size;
                isMosaicApplied = false;
            }
            // ������ũ ȿ���� ������� ���� ���¶�� ȿ���� �����մϴ�.
            else
            {
                // ������ũ ũ��
                int mosaicSize = 10;

                // ���� �̹����� ������� ������ũ ȿ���� ����
                Bitmap originalBitmap = new Bitmap(originalImage);
                Bitmap mosaicBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);

                // ���� for ������ ����Ͽ� ������ũ ȿ���� �����մϴ�.
                for (int y = 0; y < originalBitmap.Height; y += mosaicSize)
                {
                    for (int x = 0; x < originalBitmap.Width; x += mosaicSize)
                    {
                        // ������ũ ����� ��� ������ ����մϴ�.
                        Color averageColor = CalculateAverageColor(originalBitmap, x, y, mosaicSize);

                        // ������ũ ��Ͽ� ��� ������ ä��ϴ�.
                        FillMosaicBlock(mosaicBitmap, averageColor, x, y, mosaicSize, originalBitmap.Width, originalBitmap.Height);
                    }
                }

                // PictureBox�� ������ũ�� ����� �̹����� �Ҵ��մϴ�.
                pictureBox1.Image = mosaicBitmap;
                isMosaicApplied = true;
            }
        }

        /// for �ݺ����� ����Ͽ� ��ư�� 1���� �����ϰ� ��ġ�ϴ� �޼����Դϴ�.
        /// ��� ��ư
        private void CreateButtons()
        {
            // ��ư ������ �ʿ��� ������
            int buttonWidth = 30;  // ��ư �ʺ� 25�� ����
            int buttonHeight = 30; // ��ư ���̸� 25�� ����
            int spacing = 10;
            int startX = 15;   // ���� X ��ġ�� 15�� ����
            int startY = 32; // ���� Y ��ġ�� 32�� ����
            int buttonCount = 5; // ������ �� ��ư ����

            // 1���� 5�� ��ư ��ġ
            for (int i = 0; i < buttonCount; i++)
            {
                Button btn = new Button();

                // ��ư ��Ÿ�� ����
                btn.BackColor = Color.FromArgb(255, 73, 80, 87);
                btn.ForeColor = Color.FromArgb(255, 134, 142, 150);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(255, 134, 142, 150);

                btn.Text = $"{i + 1}"; // ��ư �ؽ�Ʈ�� ���ڷ� ����
                btn.Size = new Size(buttonWidth, buttonHeight);

                // ��ư ��ġ ��� (Y�� ����, X�� �ݺ����� ����)
                btn.Location = new Point(startX + i * (buttonWidth + spacing), startY);

                // ù ��° ��ư (i == 0)�� ���� �̹��� ���� �̺�Ʈ�� �����մϴ�.
                if (i == 0)
                {
                    btn.Click += FirstButton_Click;
                }
                // �� ��° ��ư (i == 1)�� �̹��� ���� �̺�Ʈ�� �����մϴ�.
                else if (i == 1)
                {
                    btn.Click += SecondButton_Click;
                }
                else
                {
                    // ������ ��ư���� ���� �̺�Ʈ�� �����մϴ�.
                    btn.Click += Button_Click;
                }

                // ���� ��ư �߰�
                this.Controls.Add(btn);
            }
        }

        /// ù ��° ��ư Ŭ�� �� ����Ǵ� �̺�Ʈ �ڵ鷯�Դϴ�.
        /// PictureBox�� �̹����� �������� �ǵ����ϴ�.
        private void FirstButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1 != null && originalImage != null)
            {
                // ���� �̹����� PictureBox�� �Ҵ�
                pictureBox1.Image = originalImage;
                pictureBox1.Size = originalImage.Size;
                isMosaicApplied = false; // ������ũ ȿ���� �����Ǿ����� ���
                pictureBox1.Invalidate(); // ���� ������ ��� �ݿ�
            }
        }

        /// �� ��° ��ư Ŭ�� �� ����Ǵ� �̺�Ʈ �ڵ鷯�Դϴ�.
        /// OpenFileDialog�� ���� �̹����� �ҷ��ɴϴ�.
        private void SecondButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose();

                    // �� �̹����� �ҷ��� ���� �̹��� ������ ����
                    Image img = Image.FromFile(openFileDialog.FileName);
                    originalImage = new Bitmap(img); // ���� �̹����� �����Ͽ� ����

                    pictureBox1.Image = originalImage;
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;
                    pictureBox1.Location = new Point(10, 10);
                    isMosaicApplied = false; // �� �̹����� �ҷ����� ������ũ ���� �ʱ�ȭ
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// ������ ������ ��� ������ ����մϴ�.
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
        /// ������ ������ ���� �������� ä��ϴ�.
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

        // [����] ��ư Ŭ�� �� ���� (���� ���� ����)
        private void btn_Save_Click(object sender, EventArgs e)
        {
            // TODO: ���� ��� ����
        }

        // ���콺 ��ư�� ���� �� ȣ���
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

        // ���콺�� �̵��� �� ȣ���
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

        // ���콺 ��ư�� ���� �� ȣ���
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            // showSelectionBorder = false; // ���� �����ϰ� ���� ��� �ּ� ����
            pictureBox1.Invalidate();
        }

        // �� �ε� �� ���� (�ʿ� �� �ʱ�ȭ ó�� ����)
        private void Form1_Load(object sender, EventArgs e)
        {
            // �ʱ�ȭ ����
        }

        // pictureBox1�� �ٽ� �׷��� �� ȣ��� (���� �׵θ� �׸�)
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
