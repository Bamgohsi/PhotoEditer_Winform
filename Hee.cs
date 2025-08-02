using System;
using System.Drawing;
using System.Windows.Forms;

namespace photo
{
    /// <summary>
    /// PictureBox 클릭 → 테두리 표시, 드래그 기능
    /// 빈 공간 클릭 → 테두리 해제
    /// 재사용 가능한 일반 클래스
    /// </summary>
    public class ImageSelector
    {
        private readonly PictureBox pictureBox;
        private readonly Control container;
        private bool isDragging;
        private Point clickOffset;
        private Point lastMousePosition;
        private bool showSelectionBorder;

        public ImageSelector(PictureBox pictureBox, Control container)
        {
            this.pictureBox = pictureBox;
            this.container = container;

            // PictureBox: 페인트·드래그 이벤트
            pictureBox.Paint += PictureBox_Paint;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;

            // 컨테이너(폼/탭 등) 클릭 시 테두리 해제
            container.MouseDown += Container_MouseDown;
            HookMouseDown(container);
        }

        // 재귀: 모든 자식 컨트롤에 마우스다운 훅 걸기
        private void HookMouseDown(Control parent)
        {
            foreach (Control ctl in parent.Controls)
            {
                if (ctl != pictureBox)
                    ctl.MouseDown += Container_MouseDown;
                if (ctl.HasChildren)
                    HookMouseDown(ctl);
            }
        }

        // 빈 공간 클릭 시 호출 → 테두리 해제
        private void Container_MouseDown(object sender, MouseEventArgs e)
        {
            if (!showSelectionBorder)
                return;

            // 클릭 좌표를 컨테이너(Client) 기준으로 변환
            Control ctl = (Control)sender;
            var screenPt = ctl.PointToScreen(e.Location);
            var clientPt = container.PointToClient(screenPt);

            // PictureBox 영역 밖이면 테두리 해제
            if (!pictureBox.Bounds.Contains(clientPt))
            {
                showSelectionBorder = false;
                pictureBox.Invalidate();
            }
        }

        // PictureBox 클릭 → 드래그 시작 & 테두리 표시
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || pictureBox.Image == null)
                return;

            isDragging = true;
            clickOffset = e.Location;
            lastMousePosition = Control.MousePosition;
            showSelectionBorder = true;
            pictureBox.Invalidate();
        }

        // 드래그 중 → delta 기반 부드러운 이동
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging)
                return;

            var currentPos = Control.MousePosition;
            int dx = currentPos.X - lastMousePosition.X;
            int dy = currentPos.Y - lastMousePosition.Y;

            pictureBox.Location = new Point(
                pictureBox.Location.X + dx,
                pictureBox.Location.Y + dy
            );

            lastMousePosition = currentPos;
        }

        // 드래그 종료 → 상태만 해제, 테두리 유지
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            pictureBox.Invalidate();
        }

        // PictureBox 페인트 → 선택 테두리 그리기
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!showSelectionBorder)
                return;

            using (var pen = new Pen(Color.DeepSkyBlue, 2))
            {
                e.Graphics.DrawRectangle(pen,
                    new Rectangle(0, 0, pictureBox.Width - 1, pictureBox.Height - 1));
            }
        }
    }
}
