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

        public Form1()
        {
            InitializeComponent();

            // pictureBox1�� Ŀ���� �׸���(Paint) �̺�Ʈ ����
            pictureBox1.Paint += pictureBox1_Paint;
            Rigth_Panel_GropBox.Visible = false;

            // ��ư���� Click �̺�Ʈ�� ������ �ڵ鷯�� ����
            button2.Click += button1_Click;
            button3.Click += button1_Click;
            button4.Click += button1_Click;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // �̻�� ��ư - ���� ��� ���� ����
        }

        // [���� �����] ��ư Ŭ�� �� ����
        // pictureBox�� �̹��� �ʱ�ȭ
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
        }

        // [����] ��ư Ŭ�� �� ����
        // �̹��� ������ �����ϰ� pictureBox�� �ε�
        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // ���� �̹����� ���� ��� �޸� ����
                    pictureBox1.Image?.Dispose();

                    // ���ο� �̹��� �ε�
                    Image img = Image.FromFile(openFileDialog.FileName);
                    pictureBox1.Image = img;

                    // �̹��� ũ�⿡ �°� PictureBox ũ�� ����
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;

                    // pictureBox ��ġ ���� (���� ��� ����)
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
        // �巡�� ���� ó�� �� ���� �׵θ� ǥ��
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;          // �巡�� ����
                clickOffset = e.Location;   // ���콺 Ŭ�� ��ǥ ����
                showSelectionBorder = true; // �׵θ� ǥ�� ON
                pictureBox1.Invalidate();   // �ٽ� �׸��� ��û (Paint ȣ��)
            }
        }

        // ���콺�� �̵��� �� ȣ��� (�巡�� ���� ����)
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // ���� ��ġ���� ���콺 �̵���ŭ offset ����
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;

                // PictureBox ��ġ ����
                pictureBox1.Location = newLocation;
            }
        }

        // ���콺 ��ư�� ���� �� ȣ���
        // �巡�� ���� �� ���� �׵θ� ����
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            // ���� �����ϰ� ���� ��� �ּ� ����
            showSelectionBorder = false;

            // �ٽ� �׸��� ��û (Paint ȣ��)
            pictureBox1.Invalidate();
        }

        // �� �ε� �� ���� (�ʿ� �� �ʱ�ȭ ó�� ����)
        private void Form1_Load(object sender, EventArgs e)
        {
            // ����� ��� ����

            this.WindowState = FormWindowState.Maximized; // ��üȭ�� ����
            //groupBox3.Visible = !groupBox1.Visible;
        }

        // pictureBox1�� �ٽ� �׷��� �� ȣ���
        // ���� �׵θ��� �׸�
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    // �Ǽ����� �׵θ� �׸��� (������ DashStyle.Dot �� ��� ����)
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

                    // �׸� �׵θ� �簢�� ���� (�̹��� ��ü)
                    Rectangle rect = new Rectangle(0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);

                    // �׵θ� �׸���
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        // ��ư�� ������ �� �׷�ڽ��� ����� ����� �ϴ� ���
        private void button1_Click(object sender, EventArgs e)
        {
            if (Rigth_Panel_GropBox.Visible == false)
            {
                // �׷�ڽ��� ������ �ִٸ�, ���̰� �մϴ�.
                Rigth_Panel_GropBox.Visible = true;
            }
            else
            {
                // �׷�ڽ��� ���δٸ�, ����ϴ�.
                Rigth_Panel_GropBox.Visible = false;
            }
        }
    }
}