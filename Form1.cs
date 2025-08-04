using System;
using System.Drawing;
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


        public Form1()
        {
            InitializeComponent();
            InitializeDynamicControls();

            // pictureBox1�� Ŀ���� �׸���(Paint) �̺�Ʈ ����
            pictureBox1.Paint += pictureBox1_Paint;
            this.WindowState = FormWindowState.Maximized; // ��üȭ������ ����
        }

        /// <summary>
        /// ��ư�� �г��� �������� �����ϰ� �ʱ�ȭ�մϴ�.
        /// </summary>
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
                Button btn = new Button();
                btn.Text = $"{i + 1}"; // ��ư �ؽ�Ʈ�� ���ڷ� ����
                btn.Size = new Size(buttonWidth, buttonHeight);

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
                    Visible = false
                };

                // �гο� �� �߰�
                panel.Controls.Add(new Label() { Text = $"���� �Ӽ� {i + 1}", Location = new Point(10, 10) });

                // �гο� Paint �̺�Ʈ �ڵ鷯 �߰�
                panel.Paint += Panel_Paint;

                this.Controls.Add(panel);
                dynamicPanels[i] = panel; // ������ �г��� �迭�� ����
            }

            // ù ��° �г��� �ʱ� ���¿��� ���̰� �����ϰ�, �׵θ��� �׸��� ���� Invalidate ȣ��//
            if (dynamicPanels.Length > 0)
            {
                currentVisiblePanel = dynamicPanels[0];
                currentVisiblePanel.Visible = true;
                currentVisiblePanel.Invalidate();
            }
        }

        // ��� ���� ��ư�� Ŭ�� �̺�Ʈ�� ó���ϴ� ���� �ڵ鷯
        private void Button_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                int index = (int)clickedButton.Tag; // ��ư�� Tag���� �ε��� ��������

                // �г��� ���� ��ư�� �ƹ� ���۵� ���� ����
                if (index >= dynamicPanels.Length)
                {
                    return;
                }

                Panel targetPanel = dynamicPanels[index];
                Panel previousVisiblePanel = currentVisiblePanel;

                if (currentVisiblePanel == targetPanel)
                {
                    // ���� ���̴� �гΰ� Ŭ���� �г��� ������ ���
                    currentVisiblePanel.Visible = false;
                    currentVisiblePanel = null;
                }
                else
                {
                    // �ٸ� �г��� ���̰� �ִٸ� �����
                    if (currentVisiblePanel != null)
                    {
                        currentVisiblePanel.Visible = false;
                    }

                    // Ŭ���� ��ư�� �ش��ϴ� �гθ� ���̰� �ϱ�
                    targetPanel.Visible = true;
                    currentVisiblePanel = targetPanel;
                }

                // ���� �гΰ� �� �г��� Paint �̺�Ʈ�� ������ ȣ���Ͽ� �׵θ��� ����
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
        /// �г��� Paint �̺�Ʈ �ڵ鷯: Ȱ��ȭ�� �гο� �׵θ��� �׸��ϴ�.
        /// </summary>
        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            Panel paintedPanel = sender as Panel;

            // ���� ���̴� �г��� ��쿡�� �׵θ� �׸���
            if (paintedPanel != null && paintedPanel == currentVisiblePanel)
            {
                // �׵θ� ������ ���������� ����
                using (Pen pen = new Pen(Color.Black, 1))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    // �г� ��迡 �׵θ� �׸���
                    Rectangle rect = new Rectangle(0, 0, paintedPanel.Width - 1, paintedPanel.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        // [���� �����] ��ư Ŭ�� �� ����
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
        }

        // [����] ��ư Ŭ�� �� ����
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose();
                    Image img = Image.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = img;
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;
                    pictureBox1.Location = new Point(10, 10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�: " + ex.Message);
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
