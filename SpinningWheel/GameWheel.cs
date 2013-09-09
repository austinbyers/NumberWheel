/* GameWheel class
 * A class for drawing and spinning a game wheel (like the wheel of fortune)
 * Austin Byers, 2013
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SpinningWheel
{
    class GameWheel
    {
        int finalSector;            // the winning sector
        public string result;       // the result of the spin

        // a game wheel is an array of sectors
        Sector[] wheel;

        // wheel geometry variables
        Point center;               // center of the wheel
        int radius;                 // radius of the wheel
        float sectorAngle;          // angle (in degrees) of each sector
        Rectangle boundRect;        // bounding rectangle for wheel
        Point[] trianglePoints;     // verticies that define the top triangle

        // drawing tools
        Graphics surface;
        Pen borderPen, numberPen;
        SolidBrush triangleBrush;

        // wheel animation
        const int TEXT_INTVL_SMALL = 35;          // small radial distance between each character
        const int TEXT_INTVL_LARGE = 75;          // large radial distance between each character
        const float SLOWDOWN_INTVL = 0.15F;   // decrease the rotateAngle by this amount every cycle during slowdown
        const int APPROX_INITIAL_ROTATE = 15; // approximately how many degrees are in each cycle of the initial rotation
        float rotateAngle;                  // how far (in degrees) to rotate on the next repaint
        int initialRotations;               // (random) number of rotations in the first stage
        int repaintCounter;               // internal counter 
        bool highlight;                 // during the highlight phase - true if currently highlighting; false if un-highlighting

        // the wheel goes through four different states
        public int state;
        public const int STATE_NOT_STARTED = 0;
        public const int STATE_INITIAL_SPIN = 1;
        public const int STATE_SLOWING_DOWN = 2;
        public const int STATE_HIGHLIGHTING = 3;

        #region Constructors

        /// <summary>
        /// Create a new instance of the GameWheel class.
        /// </summary>
        public GameWheel() {
        }

        /// <summary>
        /// Create a new instance of the GameWheel class.
        /// </summary>
        /// <param name="center">The center of the GameWheel to be drawn</param>
        /// <param name="radius">The radius (in pixels) of the GameWheel</param>
        /// <param name="minValue">The lowest number on the wheel; must be a non-negative integer less than 10,000</param>
        /// <param name="maxValue">The maximum allowable number on the wheel; must be a positive integer greater than minValue but less than 10,000</param>
        /// <param name="interval">The interval between each successive value on the wheel</param>
        public GameWheel(Point center, int radius, int minValue, int maxValue, int interval) {
            int numSectors = (maxValue - minValue) / interval + 1;
            commonConstructor(center, radius, numSectors);
            for (int i = 0; i < numSectors; i++) {
                string value = (minValue + interval * i).ToString();
                Color brushColor;

                if (numSectors <= 10) {
                    brushColor = Color.FromArgb(i * 25, 0, 255 - (i * 25));
                } else {
                    brushColor = Color.FromArgb(i * 5, 0, 255 - (i * 5));
                }

                this.wheel[i] = new Sector(i, value, this.sectorAngle, brushColor);
            }
        }

        /// <summary>
        /// Create a new instance of the GameWheel class.
        /// </summary>
        /// <param name="center">The center of the GameWheel to be drawn</param>
        /// <param name="radius">The radius (in pixels) of the GameWheel</param>
        /// <param name="values">An array of strings to be displayed on the wheel</param>
        public GameWheel(Point center, int radius, string[] values) {
            commonConstructor(center, radius, values.Length);
            for (int i = 0; i < values.Length; i++) {
                Color brushColor = Color.Blue;
                if (values.Length <=7) brushColor = Color.FromArgb(0, i * 35, 255 - (i * 35));
                this.wheel[i] = new Sector(i, values[i].ToUpper(), this.sectorAngle, brushColor);
            }
        }

        // this is the core constructor used by all overloads of the class constructor
        private void commonConstructor(Point center, int radius, int numSectors) {
            this.result = "";
            this.center = center;
            this.radius = radius;
            this.sectorAngle = 360F / numSectors;
            this.wheel = new Sector[numSectors];

            this.boundRect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            int triLength = 25;     // length of each side of the triangle       
            Point triVertex1 = new Point(center.X, center.Y - (radius + 3) + triLength);
            Point triVertex2 = Sector.polar(triVertex1, triLength, 60);
            Point triVertex3 = Sector.polar(triVertex1, triLength, 120);
            this.trianglePoints = new Point[3] { triVertex1, triVertex2, triVertex3 };

            this.borderPen = new Pen(Color.White, 2);
            this.numberPen = new Pen(Color.White, 3);
            this.triangleBrush = new SolidBrush(Color.White);

            this.state = STATE_NOT_STARTED;
        }

        #endregion

        /// <summary>
        /// Get the last value on the wheel. For numbered wheels, this will be the maximum value on the wheel.
        /// </summary>
        /// <returns>the value in the last sector of the wheel</returns>
        public string GetLastValue() {
            int lastIndex = wheel.Length - 1;
            if (lastIndex == -1) {
                return null;
            } else {
                return wheel[lastIndex].value;
            }
        }

        #region Animation

        // public spin function - returns resulting value after spin
        public string Spin(ref Random randGen) {
            int randomRotation = randGen.Next(360, 720); // rotate between 360 and 719 degrees before slowing down
            this.initialRotations = randomRotation / APPROX_INITIAL_ROTATE;
            this.rotateAngle = (float) randomRotation / initialRotations;
            this.state = STATE_INITIAL_SPIN;

            return this.Simulate();
        }

        // calculate the answer by simulating the whole spin
        string Simulate() {
            GameWheel simulation = new GameWheel();
            simulation.sectorAngle = this.sectorAngle;
            simulation.rotateAngle = this.rotateAngle;
            simulation.initialRotations = this.initialRotations;
            simulation.repaintCounter = 0;

            simulation.wheel = new Sector[this.wheel.Length];
            for (int i = 0; i < this.wheel.Length; i++) {
                simulation.wheel[i] = new Sector(i, this.wheel[i].value, this.sectorAngle, Color.Blue);
            }

            simulation.state = STATE_INITIAL_SPIN;
            while (simulation.state != STATE_HIGHLIGHTING) {
                simulation.Refresh(false);
            }
            return simulation.result;
        }

        public void Refresh(bool repaint) {
            switch (this.state) {
                case STATE_INITIAL_SPIN:
                    Rotate();
                    repaintCounter++;
                    if (repaintCounter == initialRotations) {
                        repaintCounter = 0;
                        this.state = STATE_SLOWING_DOWN;
                    }
                    break;
                case STATE_SLOWING_DOWN:
                    rotateAngle -= SLOWDOWN_INTVL;
                    if (rotateAngle <= 0) {
                        // stop spinning
                        this.state = STATE_HIGHLIGHTING;
                        this.finalSector = this.wheel.Length - (int)(((wheel[0].offsetAngle + 90) % 360) / this.sectorAngle) - 1;
                        this.result = wheel[finalSector].value;
                        wheel[finalSector].fillBrush.Color = Color.Black;
                    } else {
                        Rotate();
                    }
                    break;
                case STATE_HIGHLIGHTING:
                    // the game is over - highlight winning sector
                    Color old = wheel[finalSector].fillBrush.Color;
                    if (old.R > 235 && highlight == true) {
                        highlight = false;
                    } else if (old.R < 20 && highlight == false) {
                        highlight = true;
                    }

                    if (highlight == true) {
                        wheel[finalSector].fillBrush.Color = Color.FromArgb(old.R + 15, old.R + 15, 0);
                    } else {
                        wheel[finalSector].fillBrush.Color = Color.FromArgb(old.R - 15, old.R - 15, 0);
                    }
                    break;
            }
            if (repaint == true) Draw();
        }

        public void Draw() {
            // the 'using' statement takes care of the proper disposal of the objects
            using (Bitmap bufl = new Bitmap(boundRect.Width + 60, boundRect.Height + 60)) {
                using (Graphics g = Graphics.FromImage(bufl)) {
                    foreach (Sector s in wheel)
                        s.drawSector(g, this.borderPen, this.boundRect, this.sectorAngle);
                    foreach (Sector s in wheel)
                        s.drawValue(g, this.numberPen, this.center, this.sectorAngle, this.radius);

                    g.FillPolygon(this.triangleBrush, this.trianglePoints);
                    this.surface.DrawImageUnscaled(bufl, 0, 0);
                }
            }
        }

        public void SetGraphics(Graphics surface) {
            this.surface = surface;
        }

        private void Rotate() {
            foreach (Sector s in wheel) {
                s.offsetAngle = (s.offsetAngle + rotateAngle) % 360;
            }
        }

        #endregion Animation

        class Sector
        {
            public int index;           // index of the sector
            public string value;
            public SolidBrush fillBrush;
            public float offsetAngle;    // angle (in degrees) clockwise from x-axis until first side of pie shape 

            // return the point defined by a fixed distance and counter-clockwise angle away from a starting point
            public static Point polar(Point start, int length, float angle) {
                double radians = (angle * Math.PI) / 180.0; // counter-clockwise angle in radians
                return new Point((int)(start.X + length * Math.Cos(radians)), (int)(start.Y - length * Math.Sin(radians)));
            }

            public Sector(int index, string value, float sectorAngle, Color brushColor) {
                this.index = index;
                this.value = value;
                this.fillBrush = new SolidBrush(brushColor);
                this.offsetAngle = (270 + index * sectorAngle) % 360;
            }

            public void drawSector(Graphics g, Pen borderPen, Rectangle bounds, float sectorAngle) {
                // draw outline
                g.DrawPie(borderPen, bounds, this.offsetAngle, sectorAngle);

                // draw fill
                g.FillPie(this.fillBrush, bounds, this.offsetAngle, sectorAngle);
            }

            public void drawValue(Graphics g, Pen numberPen, Point center, float sectorAngle, int wheelRadius) {
                // the COUNTER_CLOCKWISE angle of the center of the sector
                float centerAngle = 360 - (this.offsetAngle + (sectorAngle / (float)2.0));
                float digitAngle = 90 - centerAngle;

                int charIndex = 1, numSectors = (int)(360 / sectorAngle);
                foreach (char c in this.value) {
                    Point charCenter; int size;
                    if (numSectors <= 10) {
                        charCenter = polar(center, wheelRadius - TEXT_INTVL_LARGE * charIndex, centerAngle);
                        size = 2;
                    } else {
                        charCenter = polar(center, wheelRadius - TEXT_INTVL_SMALL * charIndex, centerAngle);
                        size = 1;
                    }
                    drawChar(c, g, numberPen, digitAngle, charCenter, size);
                    charIndex++;
                }
            }

            #region Char Drawing

            // dispatch to appropriate draw function
            void drawChar(char c, Graphics g, Pen p, float angle, Point center, int size) {
                switch (c) {
                    case '0':
                        draw0(g, p, angle, center, size);
                        break;
                    case '1':
                        draw1(g, p, angle, center, size);
                        break;
                    case '2':
                        draw2(g, p, angle, center, size);
                        break;
                    case '3':
                        draw3(g, p, angle, center, size);
                        break;
                    case '4':
                        draw4(g, p, angle, center, size);
                        break;
                    case '5':
                        draw5(g, p, angle, center, size);
                        break;
                    case '6':
                        draw6(g, p, angle, center, size);
                        break;
                    case '7':
                        draw7(g, p, angle, center, size);
                        break;
                    case '8':
                        draw8(g, p, angle, center, size);
                        break;
                    case '9':
                        draw9(g, p, angle, center, size);
                        break;
                    case 'A':
                        drawA(g, p, angle, center, size);
                        break;
                    case 'B':
                        drawB(g, p, angle, center, size);
                        break;
                    case 'C':
                        drawC(g, p, angle, center, size);
                        break;
                    case 'D':
                        drawD(g, p, angle, center, size);
                        break;
                    case 'E':
                        drawE(g, p, angle, center, size);
                        break;
                    case 'F':
                        drawF(g, p, angle, center, size);
                        break;
                    case 'G':
                        drawG(g, p, angle, center, size);
                        break;
                }
            }

            void drawCharArc(Graphics g, Pen p, Point center, int radius, float startAngle, float sweepAngle) {
                Rectangle boundRect = new Rectangle(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
                g.DrawArc(p, boundRect, startAngle, sweepAngle);
            }

            #region Digits

            void draw0(Graphics g, Pen p, float angle, Point center, int size) {
                drawCharArc(g, p, center, 10 * size, 0, 360);
                /*const int half_width = 8;
                const int half_height = 5;

                Point cntrLeft = polar(center, half_width, 180 - angle);
                Point topLeft = polar(cntrLeft, half_height, 90 - angle);
                Point btmLeft = polar(cntrLeft, half_height, 270 - angle);

                Point cntrRight = polar(center, half_width, 0 - angle);
                Point topRight = polar(cntrRight, half_height, 90 - angle);
                Point btmRight = polar(cntrRight, half_height, 270 - angle);

                Point arcCenterTop = polar(center, half_height, 90 - angle);
                Point arcCenterBtm = polar(center, half_height, 270 - angle);

                g.DrawLine(p, btmLeft, topLeft);
                g.DrawLine(p, btmRight, topRight);

                drawCharArc(g, p, arcCenterTop, half_width, 180 + angle, 180);
                drawCharArc(g, p, arcCenterBtm, half_width, 0 + angle, 180); */
            }

            void draw1(Graphics g, Pen p, float angle, Point center, int size) {
                Point topCenter = polar(center, 10 * size, 90 - angle);
                Point btmCenter = polar(center, 10 * size, 270 - angle);
                Point tip = polar(topCenter, 7 * size, 225 - angle);
                Point btmLeft = polar(btmCenter, 7 * size, 180 - angle);
                Point btmRight = polar(btmCenter, 7 * size, -angle);

                g.DrawLine(p, tip, topCenter);
                g.DrawLine(p, topCenter, btmCenter);
                g.DrawLine(p, btmLeft, btmRight);
            }

            void draw2(Graphics g, Pen p, float angle, Point center, int size) {


                Point topRight = polar(center, 10 * size, 45 - angle);
                Point btmLeft = polar(center, 10 * size, 225 - angle);
                Point btmRight = polar(btmLeft, 15 * size, -angle);

                int arcRadius = 7 * size;
                Point arcCenter = polar(topRight, arcRadius, 180 - angle);

                g.DrawLine(p, btmLeft, topRight);
                g.DrawLine(p, btmLeft, btmRight);
                drawCharArc(g, p, arcCenter, arcRadius, 180 + angle, 180);
            }

            void draw3(Graphics g, Pen p, float angle, Point center, int size) {
                int radius = 7 * size;
                Point topArcCenter = polar(center, radius, 90 - angle);
                Point btmArcCenter = polar(center, radius, 270 - angle);

                drawCharArc(g, p, topArcCenter, radius, 170 + angle, 260);
                drawCharArc(g, p, btmArcCenter, radius, 260 + angle, 260);
            }

            void draw4(Graphics g, Pen p, float angle, Point center, int size) {
                Point intersect = polar(center, 3 * size, 315 - angle);
                Point bottom = polar(intersect, 8 * size, 270 - angle);
                Point right = polar(intersect, 8 * size, -angle);
                Point top = polar(intersect, 15 * size, 90 - angle);
                Point left = polar(intersect, 15 * size, 180 - angle);


                g.DrawLine(p, bottom, top);
                g.DrawLine(p, left, top);
                g.DrawLine(p, left, right);
            }

            void draw5(Graphics g, Pen p, float angle, Point center, int size) {
                Point middleLeft = polar(center, 3 * size, 135 - angle);
                Point topLeft = polar(middleLeft, 11 * size, 80 - angle);
                Point topRight = polar(topLeft, 12 * size, -angle);

                int arcRadius = 8 * size;
                Point arcCenter = polar(center, 5 * size, 270 - angle);

                g.DrawLine(p, topLeft, topRight);
                g.DrawLine(p, middleLeft, topLeft);
                drawCharArc(g, p, arcCenter, arcRadius, 245 + angle, 250);
            }

            void draw6(Graphics g, Pen p, float angle, Point center, int size) {
                Point left = polar(center, 7 * size, 180 - angle);
                Point top = polar(left, 20 * size, 60 - angle);

                int arcRadius = 7 * size;
                Point arcCenter = polar(center, 2 * size, 270 - angle);

                g.DrawLine(p, left, top);
                drawCharArc(g, p, arcCenter, arcRadius, 0, 360);
            }

            void draw7(Graphics g, Pen p, float angle, Point center, int size) {
                Point topLeft = polar(center, 15 * size, 135 - angle);
                Point topRight = polar(center, 15 * size, 45 - angle);
                Point btmLeft = polar(topRight, 28 * size, 225 - angle);

                g.DrawLine(p, topLeft, topRight);
                g.DrawLine(p, topRight, btmLeft);
            }

            void draw8(Graphics g, Pen p, float angle, Point center, int size) {
                int radius = 7 * size;
                Point topArcCenter = polar(center, radius, 90 - angle);
                Point btmArcCenter = polar(center, radius, 270 - angle);

                drawCharArc(g, p, topArcCenter, radius, 0, 360);
                drawCharArc(g, p, btmArcCenter, radius, 0, 360);
            }

            void draw9(Graphics g, Pen p, float angle, Point center, int size) {
                Point right = polar(center, 7 * size, -angle);
                Point btm = polar(right, 20 * size, 240 - angle);

                int arcRadius = 7 * size;
                Point arcCenter = polar(center, 2 * size, 90 - angle);

                g.DrawLine(p, right, btm);
                drawCharArc(g, p, arcCenter, arcRadius, 0, 360);
            }

            #endregion

            #region Letters

            void drawA(Graphics g, Pen p, float angle, Point center, int size) {
                Point left = polar(center, 7 * size, 180 - angle);
                Point right = polar(center, 7 * size, 0 - angle);
                Point top = polar(center, 10 * size, 90 - angle);

                float slantAngle = (float) Math.Atan(7.0 / 10);

                Point btmLeft = polar(top, 20 * size, 240 - angle);
                Point btmRight = polar(top, 20 * size, 300 - angle);

                g.DrawLine(p, btmLeft, top);
                g.DrawLine(p, btmRight, top);
                g.DrawLine(p, left, right);
            }

            void drawB(Graphics g, Pen p, float angle, Point center, int size) {
                Point topCenter = polar(center, 10 * size, 90 - angle);
                Point btmCenter = polar(center, 10 * size, 270 - angle);

                Point cntrLeft = polar(center, 7 * size, 180 - angle);
                Point topLeft = polar(cntrLeft, 10 * size, 90 - angle);
                Point btmLeft = polar(cntrLeft, 10 * size, 270 - angle);

                Point arcCenterTop = polar(center, 5 * size, 90 - angle);
                Point arcCenterBtm = polar(center, 5 * size, 270 - angle);

                g.DrawLine(p, topLeft, btmLeft);
                g.DrawLine(p, topLeft, topCenter);
                g.DrawLine(p, cntrLeft, center);
                g.DrawLine(p, btmLeft, btmCenter);

                drawCharArc(g, p, arcCenterTop, 5 * size, 270 + angle, 180);
                drawCharArc(g, p, arcCenterBtm, 5 * size, 270 + angle, 180);
            }

            void drawC(Graphics g, Pen p, float angle, Point center, int size) {
                drawCharArc(g, p, center, 11 * size, 45 + angle, 270);
            }

            void drawD(Graphics g, Pen p, float angle, Point center, int size) {
                int radius = 12 * size;
                Point left = polar(center, radius - 5, 180 - angle);
                Point btm = polar(left, radius, 270 - angle);
                Point top = polar(left, radius, 90 - angle);

                g.DrawLine(p, btm, top);
                drawCharArc(g, p, left, radius, 270 + angle, 180); 
            }

            void drawE(Graphics g, Pen p, float angle, Point center, int size) {
                Point cntrLeft = polar(center, 7 * size, 180 - angle);
                Point topLeft = polar(cntrLeft, 10 * size, 90 - angle);
                Point btmLeft = polar(cntrLeft, 10 * size, 270 - angle);

                Point topCenter = polar(topLeft, 10 * size, 0 - angle);
                Point btmCenter = polar(btmLeft, 10 * size, 0 - angle);

                g.DrawLine(p, topLeft, btmLeft);
                g.DrawLine(p, topLeft, topCenter);
                g.DrawLine(p, cntrLeft, center);
                g.DrawLine(p, btmLeft, btmCenter);
            }

            void drawF(Graphics g, Pen p, float angle, Point center, int size) {
                Point cntrLeft = polar(center, 9 * size, 180 - angle);
                Point topLeft = polar(cntrLeft, 10 * size, 90 - angle);
                Point btmLeft = polar(cntrLeft, 10 * size, 270 - angle);

                Point topCenter = polar(topLeft, 12 * size, 0 - angle);

                g.DrawLine(p, topLeft, btmLeft);
                g.DrawLine(p, topLeft, topCenter);
                g.DrawLine(p, cntrLeft, center);
            }

            void drawG(Graphics g, Pen p, float angle, Point center, int size) {
                Point leftEnd = polar(center, 4 * size, 0 - angle);
                Point rightEnd = polar(leftEnd, 11 * size, 0 - angle);

                g.DrawLine(p, leftEnd, rightEnd);
                drawCharArc(g, p, center, 10 * size, 0 + angle, 290);
            }

            #endregion

            #endregion
        }
    }
}
