using System;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {

        //���ο� �� ��ȣ�� �����ִ� ����
        private int tabCount = 1;
        // ������ ��ȣ �����
        private Stack<TabPage> deletedTabs = new Stack<TabPage>();


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
        private void btn_Save_Click(object sender, EventArgs e)        // TODO: ���� ��� ���� (����)
        {
            // ���Ĺڽ��� ���� ������ ���� ��
            if (pictureBox1.Image == null)
            {
                MessageBox.Show("������ �̹����� �����ϴ�.");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "�̹��� ����";
            saveFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";


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
                    MessageBox.Show("�̹����� ���������� ����Ǿ����ϴ�.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"�̹��� ���� �� ������ �߻��߽��ϴ�:\n{ex.Message}");
                }
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

        private void btnNewTabPage_Click(object sender, EventArgs e)       //�������� �߰� ��ư �̺�Ʈ         
        {
            TabPage newTabPage = new TabPage($"tp {tabCount + 1}");

            if (deletedTabs.Count > 0)
            {
                // �ֱ� ������ ���� ����
                newTabPage = deletedTabs.Pop();
            }
            else
            {
                // �� �� ����
                tabCount++;
            }
            newTabPage.BackColor = Color.White;
            // TabControl�� �� �߰�
            tabControl1.TabPages.Add(newTabPage);
            // ���� ���� ������ ��ȯ
            tabControl1.SelectedTab = newTabPage;
        }

        private void btnDltTabPage_Click(object sender, EventArgs e)   //�������� ���� ��ư �̺�Ʈ
        {
            if (tabControl1.TabPages.Count <= 1)
            {
                MessageBox.Show("�ϳ��� ���� �����־�� �մϴ�.");
                return;
            }
            // ���� ������ �� ��������
            int lastIndex = tabControl1.TabPages.Count - 1;
            TabPage lastTab = tabControl1.TabPages[lastIndex];
            TabPage selectedTab = tabControl1.SelectedTab;
            // �� ����
            tabControl1.TabPages.Remove(lastTab);



            // �����ϰ� ���� ������ ���� �ڵ����� ����
            tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
            deletedTabs.Push(selectedTab); // ������ �� ����

        }
    }
}