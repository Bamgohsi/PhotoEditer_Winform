using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

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

        private Point lastMousePosition;

        public Form1()
        {
            InitializeComponent();

         

            // pictureBox1�� Ŀ���� �׸���(Paint) �̺�Ʈ ����
            pictureBox1.Paint += pictureBox1_Paint;

            
            // PictureBox �巡�� ó�� �̺�Ʈ ����(������)
            pictureBox1.MouseDown += pictureBox1_MouseDown;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
            pictureBox1.MouseUp += pictureBox1_MouseUp;

            // ��(�� ������Ʈ��) Ŭ�� �� �׵θ� ���� �̺�Ʈ ����(������)
            this.MouseDown += Form1_MouseDown;

            // ��������� ��� ���� ��Ʈ�ѿ��� �����մϵ�(������)
            HookMouseDown(this);
        }


        //��������� parent�� �� �ڽ� ��Ʈ�ѵ鿡 Form1_MouseDown ���� �̴ϵ�.(������)
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
           
            // �̻�� ��ư - ���� ��� ���� ����
        }

        // [���� �����] ��ư Ŭ�� �� ����
        // pictureBox�� �̹��� �ʱ�ȭ
        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose(); // ���� �̹��� �޸� ����
                pictureBox1.Image = null;
                showSelectionBorder = false; // �̹��� �ʱ�ȭ �� �׵θ��� ����
                pictureBox1.Invalidate(); // �ٽ� �׸��� ��û
            }
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
            MessageBox.Show("���� ����� ���� �������� �ʾҽ��ϴ�.");
        }
        // ����: �� �Ǵ� ������Ʈ�� Ŭ���� ȣ��, Ŭ�������� ��(client) ��ǥ�� ��ȯ�Ͽ� pictureBox�ܺΰ˻�(������)
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
            // pictureBox1 ���� Ŭ�������� �׵θ� ��
            if (pictureBox1.Image != null
                && showSelectionBorder
                && !pictureBox1.Bounds.Contains(e.Location))    // <- ����: clicpt ���
            {
                showSelectionBorder = false;
                pictureBox1.Invalidate();
            }
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

                // �巡�� ���� ������ ���콺 ��ũ�� ��ǥ ����

                lastMousePosition = Control.MousePosition;
            }
        }

        // ���콺�� �̵��� �� ȣ��� (�巡�� ���� ����)
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                if (!isDragging) return;

                Point currentMousePosition = Control.MousePosition;
                // 2) ���� ��ġ�� ����(delta) ���
                int dx = currentMousePosition.X - lastMousePosition.X;
                int dy = currentMousePosition.Y - lastMousePosition.Y;
                // 3) PictureBox ��ġ�� ��Ÿ��ŭ ���� �ε巴�� �̵�
                pictureBox1.Location = new Point(
                    pictureBox1.Location.X + dx,
                    pictureBox1.Location.Y + dy
                );
                // 4) ���� ��Ÿ ����� ���� ��ġ ����
                lastMousePosition = currentMousePosition;
                //// PictureBox ��ġ ����
                //pictureBox1.Location = newLocation;
            }
        }

        // ���콺 ��ư�� ���� �� ȣ���
        // �巡�� ���� �� ���� �׵θ� ����
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;

            // ���� �����ϰ� ���� ��� �ּ� ����
            //showSelectionBorder = false;

            // �ٽ� �׸��� ��û (Paint ȣ��)
            pictureBox1.Invalidate();
        }

  

        // �� �ε� �� ���� (�ʿ� �� �ʱ�ȭ ó�� ����)
        private void Form1_Load(object sender, EventArgs e)
        {

            // ����� ��� ����
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
    }
}