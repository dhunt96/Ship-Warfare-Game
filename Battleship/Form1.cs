using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// To do
// -----
// Slow down shell travel rate and increase gun cool down period   DONE
// Show location of shells to hit   DONE
// Have ships release targets when out of range   DONE
// Have damage occur   DONE
// Show health on heads up display   DONE
// Have ships sink (and targets released)   DONE
// Animation for hit and miss   DONE
//   Miss   DONE
//   Hit    DONE
// Different ship images   DONE
// Debug range error - CORRECTED - was misplaced parenthesis   DONE
// Enhance hit error so in circle about target (low priority)
// Debug: misses showing even if hit - inside ship loop, what about next ship?   DONE
// Sinking animation - ship slowly fades away
// Ship speed changes with damage

// Enemy ship AI (stays on target and chases target down)
// Head any direction

// Land masses (too big for remaining time)
// Smaller guns

namespace Battleship
{
    public partial class Form1 : Form
    {

        class Explosion
        {
            float x, y;
            int age;
            // lifespan
            // color (then could use for smoke)
            // drift (waves, smoke etc)
            // elevation (if using for smoke too, ex: flak)
            public Explosion(float X, float Y)
            {
                age = 0;
                x = X;
                y = Y;
            }
            public float X
            {
                get { return x; }
            }
            public float Y
            {
                get { return y; }
            }
            public int Age
            {
                get { return age; }
                set { age = value; }
            }
            // public Color - transparency based on age and lifespan
        }



        class Foam
        {
            float x, y;
            int age;
            float radius;
            public Foam(float X, float Y, float Radius)
            {
                age = 0;
                x = X;
                y = Y;
                radius = Radius;
            }
            public float X
            {
                get { return x; }
            }
            public float Y
            {
                get { return y; }
            }
            public int Age
            {
                get { return age; }
                set { age = value; }
            }
            public float Radius
            {
                get { return radius; }
            }
        }



        class Shell
        {
            int when; // when shell hits (in Clock ticks)
            float x, y; // cartesian coordinates of location that shell will hit
            int damage;
            public Shell(int ArrivalTime, float X, float Y, int Damage)
            {
                when = ArrivalTime;
                x = X;
                y = Y;
                damage = Damage;
            }
            public int ArrivalTime
            {
                get { return when; }
            }
            public float X
            {
                get { return x; }
            }
            public float Y
            {
                get { return y; }
            }
            public int Damage
            {
                get { return damage; }
            }
        }



        class Ship
        {
            string name;
            // Cartesian coordinates for ship
            float x;
            float y;
            Bitmap image; // Ship bitmap (expects uniform color for background and ship facing up/north)
            // Direction
            float h; // Actual heading: 0 to 360 degrees
            float desiredHeading;
            float tr; // maximum turn rate
            // Ship speed
            float mv; // maximum speed
            float v; // current speed (cartesian units per timer tick)
            //float p; // speed as a percent of maximum
            float shipsize; // distance from center of ship to propellers (where wake is created)
            int alliance;
            int health; // if zero, ship sinks
            Ship target;
            int gunready; // gun cool down period
            public Ship(string Name, string BitmapName, float X, float Y, float Heading, int Alliance)
            {
                name = Name;
                x = X;
                y = Y;
                h = Heading;
                desiredHeading = Heading;
                image = new Bitmap(BitmapName);
                image.MakeTransparent(image.GetPixel(0, 0));
                tr = 0.05f;
                mv = 0.25f;
                //v = mv;
                v = 0.75f * mv;
                shipsize = 100;
                alliance = Alliance;
                health = 1000;
                target = null;
                gunready = 0;
            }
            public string Name
            {
                get { return name; }
            }
            public float X
            {
                get { return x; }
                set { x = value; }
            }
            public float Y
            {
                get { return y; }
                set { y = value; }
            }
            public float Heading
            {
                get { return h; }
                set { h = value; }
            }
            public float DesiredHeading
            {
                get { return desiredHeading; }
                set { desiredHeading = value; }
            }
            public Bitmap ShipImage // Read Only
            {
                get { return image; }
            }
            public float TurnRate // Read Only?
            {
                get { return tr; }
                set { tr = value; }
            }
            public float Speed
            {
                get { return v; }
                set { v = value; }
            }
            public float Percent // Speed as a percent of max speed = 0 to 100
            {
                get { return (v / mv) * 100; }
            }
            public float ShipSize
            {
                get { return shipsize; }
            }
            public int Alliance
            {
                get { return alliance; }
            }
            public int Health
            {
                get { return health; }
                set { health = value; }
            }
            public Ship Target
            {
                get { return target; }
                set { target = value; }
            }
            public int GunReady
            {
                get { return gunready; }
                set { gunready = value; }
            }
            public void Move()
            {
                if (h != desiredHeading)
                {
                    if (desiredHeading - h <= tr)
                        h = desiredHeading;
                    else
                        h = h + tr;
                }
                double Angle = 90 - h;
                // Translate from degrees to radians
                Angle = Angle * (Math.PI / 180);
                // Move ship
                x += v * (float)Math.Cos(Angle);
                y += v * (float)Math.Sin(Angle);
                // guns
                if (gunready > 0)
                    --gunready;
            }
        }


       

        // Global declaration of bitmap and graphics object
        Bitmap View;
        Graphics g;

        Random R;

        // Cartesian coordinates for center of view bitmap
        float CartesianCenterX;
        float CartesianCenterY;
        float BitmapCenterX;
        float BitmapCenterY;
        int OldMouseX, OldMouseY;
        bool MouseIsDown;

        Ship CurrentShip;
        System.Collections.ArrayList Ships;
        System.Collections.ArrayList Wakes;
        System.Collections.ArrayList Shells;
        System.Collections.ArrayList Explosions;

        enum Phase { Waxing, Waning };
        Phase CurrentPhase;
        int PhaseTicker;
        int Clock;

        // Top left corner of Minimap
        int MinimapX;
        int MinimapY;
        float MinimapCartesianWidth;
        int MinimapBitmapWidth;

        const int Allied = 1;
        const int Enemy = 2;




        public Form1()
        {
            InitializeComponent();

            this.Text = "Battleship";
            Ships = new System.Collections.ArrayList();
            Wakes = new System.Collections.ArrayList();
            Shells = new System.Collections.ArrayList();
            Explosions = new System.Collections.ArrayList();
            Clock = 0;

            R = new Random();

            // Set cartesian coordinates for center of view window
            CartesianCenterX = 400;
            CartesianCenterY = 300;

            // Create ships
            Ship Missouri = new Ship("Missouri", "missouri-s1.bmp",400, 300, 225, Allied);
            CurrentShip = Missouri;
            Ships.Add(Missouri);
            Ships.Add(new Ship("Iowa", "missouri-s1.bmp", 400, 200, 45, Allied));

            Ships.Add(new Ship("Yamoto", "hood-s1.bmp", 2500, 2500, 225, Enemy));

            // Make picturebox fill entire form interior
            pictureBox1.Top = 0;
            pictureBox1.Left = 0;
            pictureBox1.Width = this.Width - 16;
            pictureBox1.Height = this.Height - 38;

            // Initialize graphics objects
            View = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(View);

            CurrentPhase = Phase.Waxing;
            PhaseTicker = 128;

            // Initialize Minimap
            MinimapCartesianWidth = 10000;
            MinimapBitmapWidth = 200;
            MinimapX = pictureBox1.Width - 300;
            MinimapY = 100;

            // Turn on timer
            timer1.Interval = 25;
            timer1.Enabled = true;
        }



        private Bitmap rotateImage(Bitmap b, float angle)
        {
            //create a new empty bitmap to hold rotated image
            Bitmap returnBitmap = new Bitmap(b.Width, b.Height);
            //make a graphics object from the empty bitmap
            Graphics g = Graphics.FromImage(returnBitmap);
            //move rotation point to center of image
            g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
            //rotate
            g.RotateTransform(angle);
            //move image back
            g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
            //draw passed in image onto graphics object
            g.DrawImage(b, new Point(0, 0));
            return returnBitmap;
        }



        private void DrawView()
        {
            // Fill background with ocean color
            g.FillRectangle(new SolidBrush(Color.Blue), 0, 0, View.Width, View.Height);

            // Only need when resizing?
            BitmapCenterX = View.Width / 2;
            BitmapCenterY = View.Height / 2;

            // Draw foam objects
            System.Collections.ArrayList ExpiredFoam = new System.Collections.ArrayList();
            foreach (Foam f in Wakes)
            {
                int Alpha = 255 - f.Age / 10;
                if (Alpha < 0) Alpha = 0;
                SolidBrush FoamColor = new SolidBrush(Color.FromArgb(Alpha, Color.White));
                // converts from cartesian units to bitmap units
                float bx = BitmapCenterX + (f.X - CartesianCenterX);
                float by = BitmapCenterY + (CartesianCenterY - f.Y);
                // Draw Foam circle
                g.FillEllipse(FoamColor, bx - f.Radius / 2, by - f.Radius / 2, f.Radius * 2, f.Radius * 2);
                ++f.Age;
                if (f.Age >= 2550)
                    ExpiredFoam.Add(f);
            }
            foreach (Foam f in ExpiredFoam)
                Wakes.Remove(f);

            // Draw ships
            foreach (Ship ship in Ships)
            {
                // Compute bitmap coordinates for ship using cartesian coordinates
                float BitmapShipX = BitmapCenterX + (ship.X - CartesianCenterX);
                float BitmapShipY = BitmapCenterY + (CartesianCenterY - ship.Y); // reverse terms to account for difference in cartesian y and bitmap y

                // Draw Ship (after rotating to proper heading)
                Bitmap CurrentShipImage = rotateImage(ship.ShipImage, ship.Heading);
                g.DrawImage(CurrentShipImage, BitmapShipX - ship.ShipImage.Width / 2, BitmapShipY - ship.ShipImage.Height / 2);

                // Mark current ship
                if (CurrentShip == ship)
                {
                    int Alpha = 0;
                    if (PhaseTicker > 0)
                    {
                        if (CurrentPhase == Phase.Waxing)
                        {
                            Alpha = (128 - PhaseTicker) * 2;
                            --PhaseTicker;
                        }
                        if (CurrentPhase == Phase.Waning)
                        {
                            Alpha = (PhaseTicker - 1) * 2;
                            --PhaseTicker;
                        }
                    }
                    else
                    {
                        if (CurrentPhase == Phase.Waxing)
                        {
                            CurrentPhase = Phase.Waning;
                            PhaseTicker = 128;
                        }
                        else
                        //if (CurrentPhase == Phase.Waning)
                        {
                            CurrentPhase = Phase.Waxing;
                            PhaseTicker = 128;
                        }
                    }
                    Pen p = new Pen(Color.FromArgb(Alpha, Color.Red), 3);
                    g.DrawEllipse(p, BitmapShipX - 125, BitmapShipY - 125, 250, 250);
                }
            }

            // Create Wake
            foreach (Ship ship in Ships)
            {
                double angle = 90 - ship.Heading; // angle of heading in cartesian degrees
                angle = angle + 180; // angle to propeller in cartesian degrees
                angle = angle * (Math.PI / 180); // convert to redians

                // Compute offset values relative to ship center
                float xo = ship.ShipSize * (float)Math.Cos(angle);
                float yo = ship.ShipSize * (float)Math.Sin(angle);
                // Calculates actual cartesian coordinates
                xo = ship.X + xo;
                yo = ship.Y + yo;
                // Add Foam to Wake collection
                if (R.Next(5) == 0)
                {
                    float xerror = 3.0f - R.Next(61) / 10.0f;
                    float yerror = 3.0f - R.Next(61) / 10.0f;
                    float radius = 1.0f + R.Next(21) / 10.0f;
                    Wakes.Add(new Foam(xo + xerror, yo + yerror, radius));
                }
                // converts from cartesian units to bitmap units
                xo = BitmapCenterX + (xo - CartesianCenterX);
                yo = BitmapCenterY + (CartesianCenterY - yo);
                // DEBUG - draw dot for stern location
                g.FillEllipse(new SolidBrush(Color.Red), xo - 2.5f, yo - 2.5f, 5f, 5f);
            }

            // Draw Shell Location - DEBUG
            foreach (Shell s in Shells)
            {
                float bx = BitmapCenterX + (s.X - CartesianCenterX);
                float by = BitmapCenterY + (CartesianCenterY - s.Y);
                g.FillEllipse(new SolidBrush(Color.Red), bx - 2, by - 2, 4, 4);
            }

            // Draw Explosions
            System.Collections.ArrayList ExpiredExplosions = new System.Collections.ArrayList();
            foreach (Explosion explosion in Explosions)
            {
                float bx = BitmapCenterX + (explosion.X - CartesianCenterX);
                float by = BitmapCenterY + (CartesianCenterY - explosion.Y);
                float radius = 0;
                switch (explosion.Age)
                {
                    case 0: radius = 4; break;
                    case 1: radius = 6; break;
                    case 2: radius = 8; break;
                    case 3: radius = 9; break;
                    case 4: radius = 10; break;
                    default: radius = 0; break;
                }
                ++explosion.Age;
                if (explosion.Age >= 5)
                    ExpiredExplosions.Add(explosion);
                g.FillEllipse(new SolidBrush(Color.OrangeRed), bx - radius, by - radius, 2*radius, 2*radius);
            }
            foreach (Explosion explosion in ExpiredExplosions)
                Explosions.Remove(explosion);

            // Heads up display
            Font font = new Font("Courier New", 20.0f);
            SolidBrush brush = new SolidBrush(Color.White);
            int RowGap = 35;
            g.DrawString(CurrentShip.Name + "  ("+CurrentShip.Health.ToString()+")", font, brush, new PointF(100, 100));
            g.DrawString(CurrentShip.X.ToString("f0") + "," + CurrentShip.Y.ToString("f0"), font, brush, new PointF(100, 100 + RowGap));
            g.DrawString(CurrentShip.Heading.ToString("f0"), font, brush, new PointF(100, 100 + RowGap*2));
            g.DrawString(CurrentShip.Percent.ToString("f0"), font, brush, new PointF(100, 100 + RowGap * 3));
            if (CurrentShip.Target != null)
                g.DrawString(CurrentShip.Target.Name, font, brush, new PointF(100, 100 + RowGap * 4));
            //g.DrawString(CartesianCenterX.ToString("f0") + "," + CartesianCenterY.ToString("f0"), font, brush, new PointF(100, 100 + RowGap * 4));

            // Debug code for shells
            g.DrawString(Clock.ToString(), font, brush, new PointF(100, 365));
            int n = 0;
            foreach (Shell shell in Shells)
            {
                g.DrawString(shell.ArrivalTime.ToString() + " (" + shell.X.ToString() + "," + shell.Y.ToString() + ")", font, brush, new PointF(100, 400 + RowGap * n));
                ++n;
            }

            // Draw Mini-map
            Bitmap Minimap = new Bitmap(MinimapBitmapWidth, MinimapBitmapWidth);
            Graphics mg = Graphics.FromImage(Minimap);
            mg.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Cyan)), 0, 0, Minimap.Width, Minimap.Height);
            //mg.DrawLine(new Pen(Color.LightGray), 0, 100, 200, 100);
            //mg.DrawLine(new Pen(Color.LightGray), 100, 0, 100, 200);
            float CartesianUnitsPerPixel = MinimapCartesianWidth / MinimapBitmapWidth;
            // Rectangle for location of view
            int ViewRectangleBitmapTopLeftX = Convert.ToInt32(MinimapBitmapWidth / 2 + (CartesianCenterX - View.Width / 2) / CartesianUnitsPerPixel);
            int ViewRectangleBitmapTopLeftY = Convert.ToInt32(MinimapBitmapWidth / 2 - (CartesianCenterY + View.Height / 2) / CartesianUnitsPerPixel);
            int ViewRectangleBitampWidth = Convert.ToInt32(View.Width / CartesianUnitsPerPixel);
            int ViewRectangleBitmapHeight = Convert.ToInt32(View.Height / CartesianUnitsPerPixel);
            mg.DrawRectangle(new Pen(Color.Red), ViewRectangleBitmapTopLeftX, ViewRectangleBitmapTopLeftY, ViewRectangleBitampWidth, ViewRectangleBitmapHeight);
            // Draw Ships
            foreach (Ship ship in Ships)
            {
                float BitmapShipX = MinimapBitmapWidth / 2 + ship.X / CartesianUnitsPerPixel;
                float BitmapShipY = MinimapBitmapWidth / 2 - ship.Y / CartesianUnitsPerPixel; // reverse terms to account for difference in cartesian y and bitmap y
                Minimap.SetPixel(Convert.ToInt32(BitmapShipX), Convert.ToInt32(BitmapShipY), Color.Black);
            }
            // Display Minimap
            g.DrawImage(Minimap, MinimapX, MinimapY);

            // Display view
            pictureBox1.Image = View;
        }



        private void Form1_Resize(object sender, EventArgs e)
        {
            // resize picture box
            pictureBox1.Width = this.Width - 16;
            pictureBox1.Height = this.Height - 38;

            // Re-instatiate bitmap and graphics object
            View = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            g = Graphics.FromImage(View);

            // Update location of Minimap
            MinimapX = pictureBox1.Width - 300;
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            ++Clock;

            // Move ships (Operate?)
            foreach (Ship ship in Ships)
                ship.Move();

            // Acquire Targets
            foreach (Ship ship in Ships)
            {
                ship.Target = null; // unassign target,then reassign
                foreach (Ship ship2 in Ships)
                {
                    if (ship.Alliance != ship2.Alliance)
                    {
                        double range = Math.Sqrt((ship.X - ship2.X) * (ship.X - ship2.X) + (ship.Y - ship2.Y) * (ship.Y - ship2.Y));
                        if (range < 2000)
                        {
                            ship.Target = ship2;
                            break;
                        }
                    }
                }
            }

            // Fire main guns
            foreach (Ship ship in Ships)
            {
                if ((ship.Target != null) && (ship.GunReady == 0))
                {
                    // calc time to target
                    float d = (float)Math.Sqrt((ship.X - ship.Target.X) * (ship.X - ship.Target.X) + (ship.Y - ship.Target.Y) * (ship.Y - ship.Target.Y));
                    int t = Convert.ToInt32(d / 2.5f);
                    // calc where enemy will be on current heading at the end of that time
                    double Angle = 90 - ship.Target.Heading;
                    Angle = Angle * (Math.PI / 180); // Translate from degrees to radians
                    float x = ship.Target.X + ship.Target.Speed * t * (float)Math.Cos(Angle);
                    float y = ship.Target.Y + ship.Target.Speed * t * (float)Math.Sin(Angle);
                    // randomize target location
                    float error = d / 50;
                    x += (error * 10 - R.Next(Convert.ToInt32(2 * error + 1) * 10)) / 10.0f;
                    y += (error * 10 - R.Next(Convert.ToInt32(2 * error + 1) * 10)) / 10.0f;
                    // shoot at that location
                    Shells.Add(new Shell(Clock + t, x, y, 200));
                    ship.GunReady = 400;
                }
            }

            // Check to see if shells have arrived
            System.Collections.ArrayList DeadShells = new System.Collections.ArrayList();
            System.Collections.ArrayList DeadShips = new System.Collections.ArrayList();
            foreach (Shell shell in Shells)
            {
                if (shell.ArrivalTime<=Clock)
                {
                    Ship ShipDamaged = null;
                    foreach (Ship ship in Ships)
                    {
                        double d = Math.Sqrt((ship.X - shell.X) * (ship.X - shell.X) + (ship.Y - shell.Y) * (ship.Y - shell.Y));
                        if (d<15)
                        {
                            ShipDamaged = ship;
                            break;
                        }
                    }
                    if (ShipDamaged!=null)
                    {
                        // Hit animation
                        Explosions.Add(new Explosion(shell.X, shell.Y));
                        ShipDamaged.Health -= shell.Damage;
                        if (ShipDamaged.Health <= 0)
                            DeadShips.Add(ShipDamaged);
                    }
                    else
                    {
                        // Miss animation
                        Wakes.Add(new Foam(shell.X, shell.Y, 7.5f));
                    }
                    DeadShells.Add(shell);
                }
            }
            foreach (Shell shell in DeadShells)
                Shells.Remove(shell);
            foreach (Ship ship in DeadShips)
                Ships.Remove(ship);

            // Display new view
            DrawView();
        }



        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseIsDown==true)
            {
                CartesianCenterX += (OldMouseX - e.X);
                CartesianCenterY -= (OldMouseY - e.Y);
                OldMouseX = e.X;
                OldMouseY = e.Y;
            }
        }



        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            MouseIsDown = false;
        }



        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Store mouse click location
            int MouseX = e.X;
            int MouseY = e.Y;

            if ((MouseX>MinimapX)&&(MouseX <MouseX + MinimapBitmapWidth)&&(MouseY>MinimapY)&&(MouseY<MinimapY+MinimapBitmapWidth))
            {
                int MinimapBitmapOffsetX = MouseX - MinimapX;
                int MinimapBitmapOffsetY = MouseY - MinimapY;
                float CartesianUnitsPerPixel = MinimapCartesianWidth / MinimapBitmapWidth;
                CartesianCenterX = (MinimapBitmapOffsetX - MinimapBitmapWidth / 2.0f) * CartesianUnitsPerPixel;
                CartesianCenterY = (MinimapBitmapWidth / 2.0f - MinimapBitmapOffsetY) * CartesianUnitsPerPixel;
                return;
            }

            // Click top right corner: Turn right
            if ((MouseY>=0)&&(MouseY<=75)&&(MouseX<=View.Width)&&(MouseX>=View.Width-75))
            {
                CurrentShip.DesiredHeading += 45.0f;
                return;
            }

            // Switch current ship to next ship in collection
            if (MouseX >= View.Width - 75)
            {
                for (int i = 0; i < Ships.Count; i++)
                {
                    if ((Ship)Ships[i] == CurrentShip)
                    {
                        int j = i;
                        if (i == Ships.Count - 1)
                            CurrentShip = (Ship)Ships[0];
                        else
                            CurrentShip = (Ship)Ships[i + 1];
                        while (CurrentShip.Alliance == Enemy)
                        {
                            i = i + 1;
                            if (i == Ships.Count)
                            {
                                i = 0;
                                CurrentShip = (Ship)Ships[i];
                            }
                            else
                            {
                                CurrentShip = (Ship)Ships[i];
                            }
                        }
                        return;
                    }
                }
            }

            // Click on a ship: Click on ship to set it as the current ship
            foreach (Ship ship in Ships)
            {
                if (ship.Alliance == Allied)
                {
                    float BitmapShipX = BitmapCenterX + (ship.X - CartesianCenterX);
                    float BitmapShipY = BitmapCenterY + (CartesianCenterY - ship.Y);
                    double d = Math.Sqrt((MouseX - BitmapShipX) * (MouseX - BitmapShipX) + (MouseY - BitmapShipY) * (MouseY - BitmapShipY));
                    if (d < 50)
                    {
                        CurrentShip = ship;
                        return;
                    }
                }
            }

            OldMouseX = MouseX;
            OldMouseY = MouseY;
            MouseIsDown = true;

        } // end method pictureBox1_Mouse_Down

    }
}
