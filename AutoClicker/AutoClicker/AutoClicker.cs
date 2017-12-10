using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace AutoClicker
{
    public partial class AutoClicker : Form
    {
        //These are to be set whenever the mouse position is moved by the user. 
        private int centerMouseX;
        private int centerMouseY;

        private bool run = false;
        private int minTimeDefault = 1600;
        private int maxTimeDefault = 2600;
        private int minClickBeforeMouseMoveDefault = 1;
        private int maxClickBeforeMouseMoveDefault = 30;
        private int mousePixelsToMoveFromCenter = 3;

        private static object threadLocker = new object();

        delegate void UpdateMouseCoordLabelsCallback(string xCoord, string yCoord);

        public AutoClicker()
        { 
            InitializeComponent();
            InitializeWaitTimes(minTimeDefault, maxTimeDefault, minClickBeforeMouseMoveDefault, maxClickBeforeMouseMoveDefault);

            //Mouse coordinate label thread
            Thread mouseCoordinateLabelThread = new Thread(() => ContinuallyUpdateMouseCoordLabel());
            mouseCoordinateLabelThread.IsBackground = true;
            mouseCoordinateLabelThread.Start();
        }

        private void ContinuallyUpdateMouseCoordLabel (int precisionMilliseconds = 60)
        {
            while (true)
            {
                System.Threading.Thread.Sleep(precisionMilliseconds); //Update every 60 milliseconds

                UpdateMouseCoordLabelsCallback updateMouseCoordInvoker = new UpdateMouseCoordLabelsCallback(SetMouseCoordLabels);

                string currentXCoord = Cursor.Position.X.ToString();
                string currentYCoord = Cursor.Position.Y.ToString();

                try
                {
                    this.Invoke(updateMouseCoordInvoker, new object[] { currentXCoord, currentYCoord });
                }
                catch (Exception) { };
            }
        }

        /// <summary>
        /// Set the min/max wait time and mouse movement clicks. 
        /// Min wait time indicates the minimum amount of time waited before the next click. 
        /// Max wait time indicates the maximum amount of time waitied before the next click.
        /// minClickBeforeMouseMove indicates the minimum number of clicks needed before mouse jumps position.
        /// maxClicksBeforeMouseMove indicates the maximum number of clicks needed before mouse jumps position.
        /// </summary>
        /// <param name="minTime"></param>
        /// <param name="maxTime"></param>
        /// <param name="minClicksBeforeMouseMove"></param>
        /// <param name="maxClicksBeforeMouseMove"></param>
        private void InitializeWaitTimes(int minTime, int maxTime, int minClicksBeforeMouseMove, int maxClicksBeforeMouseMove)
        {
            this.minWait.Text = minTimeDefault + "";
            this.maxWait.Text = maxTimeDefault + "";
            this.minClicksBetweenMovement.Text = minClickBeforeMouseMoveDefault + "";
            this.maxClicksBetweenMovement.Text = maxClickBeforeMouseMoveDefault + "";
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            StartAutoClicker();
        }

        /// <summary>
        /// This is a gatekeeper method to keep only a single auto-clicker thread running 
        /// </summary>
        private void StartAutoClicker()
        {
            if (ValidFieldData())
            {
                if (!this.run)
                    AutoClickOnNewThread();
                this.run = true;
                DisableSettingFields();
            }
            else
            {
                string errorMessage = "Wait Time and Mouse Clicks need to be non-decimal numbers!";
                string caption = "Invalid input(s)";
                MessageBoxButtons errorButtons = MessageBoxButtons.OK;
                MessageBox.Show(errorMessage, caption, errorButtons);
            }
        }

        /// <summary>
        /// Verifies that minClicksBetweenMovement, maxClicksBetweenMovement, minWait, and maxWait have integer values in them. 
        /// </summary>
        /// <returns></returns>
        private bool ValidFieldData()
        {
            int irrelevantData; //Needed for TryParse
            bool hasValidData = true;

            hasValidData = hasValidData && int.TryParse(this.minClicksBetweenMovement.Text.ToString(), out irrelevantData);
            hasValidData = hasValidData && int.TryParse(this.maxClicksBetweenMovement.Text.ToString(), out irrelevantData);
            hasValidData = hasValidData && int.TryParse(this.minWait.Text.ToString(), out irrelevantData);
            hasValidData = hasValidData && int.TryParse(this.maxWait.Text.ToString(), out irrelevantData);

            return hasValidData;
        }

        /// <summary>
        /// Creates a new background thread and runs AutoClick() on that thread. 
        /// </summary>
        private void AutoClickOnNewThread()
        {
            centerMouseX = Cursor.Position.X;
            centerMouseY = Cursor.Position.Y;

            Thread t = new Thread(AutoClick);
            t.IsBackground = true;
            t.Start();
        }

        private void AutoClick()
        {

            int minWaitTime = int.Parse(minWait.Text.ToString());
            int maxWaitTime = int.Parse(maxWait.Text.ToString());

            int minClicks = int.Parse(minClicksBetweenMovement.Text.ToString());
            int maxClicks = int.Parse(maxClicksBetweenMovement.Text.ToString());

            while (this.run)
            {
                Random rnd = new Random();
                int numbersToClickBeforeUpdatingPosition = rnd.Next(minClicks, maxClicks); //Number of times to click before changing mouse position
                int counter = 0;

                //Number of clicks before randomly moving mouse position.
                while (counter < numbersToClickBeforeUpdatingPosition && this.run)
                {
                    int timeBetweenClicks = rnd.Next(minWaitTime, maxWaitTime);
                    DoMouseClick();

                    Thread.Sleep(timeBetweenClicks);
                    counter++;
                }

                //Also need an auto clicking thing.
                MethodInvoker mouseCoordUpdateInvoker = new MethodInvoker(RandomlyUpdateMouseCoordinates);
                this.Invoke(mouseCoordUpdateInvoker);

                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// The point of the random mouse movements is to not click the exact same coordinate every click. 
        /// If there was a game, for example, and you wanted to hide the fact that you were using an auto-clicker
        /// Moving the mouse coordinates just a little bit every so often could help mask this fact.
        /// </summary>
        private void RandomlyUpdateMouseCoordinates()
        {

            if (this.run)
            {
                UpdateMouseCenterIfUserMovedMouse();
             
                Random rnd = new Random();
                int randX = this.centerMouseX + rnd.Next(1, mousePixelsToMoveFromCenter);
                int randY = this.centerMouseY + rnd.Next(1, mousePixelsToMoveFromCenter);

                Win32.POINT p = new Win32.POINT();

                int sleepTime = rnd.Next(1000, 4500); //Sleep between 1 and 4.5 seconds..?

                Win32.ClientToScreen(this.Handle, ref p);
                Win32.SetCursorPos(randX, randY);
                System.Threading.Thread.Sleep(sleepTime);

                SetMouseCoordLabels(Cursor.Position.X.ToString(), Cursor.Position.Y.ToString());

            }
        }

        /// <summary>
        /// Updates the form's Mouse X and Y Label with the new string values 
        /// for x and y respectively
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        private void SetMouseCoordLabels(string xPos, string yPos)
        {
            this.MouseXLabel.Text = "Mouse X Position: " + xPos;
            this.MouseYLabel.Text = "Mouse Y position: " + yPos;
        }

        /// <summary>
        /// Simulates a click at the cursor's current location
        /// </summary>
        private void DoMouseClick()
        {
            if (this.run)
            {
                //Call the imported function with the cursor's current position
                uint X = (uint)Cursor.Position.X;
                uint Y = (uint)Cursor.Position.Y;
                Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN | Win32.MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
            }
        }

        private void UpdateMouseCenterIfUserMovedMouse()
        {
            if (Math.Abs(Cursor.Position.X - centerMouseX) > 10 || Math.Abs(Cursor.Position.Y - centerMouseY) > 10)
            {
                centerMouseX = Cursor.Position.X;
                centerMouseY = Cursor.Position.Y;
            }
        }

        public class Win32
        {
            [DllImport("User32.Dll")]
            public static extern long SetCursorPos(int x, int y);

            [DllImport("User32.Dll")]
            public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
            //Mouse actions
            public const int MOUSEEVENTF_LEFTDOWN = 0x02;
            public const int MOUSEEVENTF_LEFTUP = 0x04;
            public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
            public const int MOUSEEVENTF_RIGHTUP = 0x10;

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            this.run = false;
            EnableSettingFields();
        }

        #region Enable/Disable Setting Fields

        /// <summary>
        /// Allows the user to click the startButton, 
        /// maxClicksBetweenMovement text field, 
        /// minClicksBetweenMovement text field, 
        /// minWait text field,
        /// maxWait text field. 
        /// </summary>
        private void EnableSettingFields()
        {
            this.startButton.Enabled = true;
            this.maxClicksBetweenMovement.Enabled = true;
            this.minClicksBetweenMovement.Enabled = true;
            this.minWait.Enabled = true;
            this.maxWait.Enabled = true;
        }

        /// <summary>
        /// Disables the fields so user can't edit 
        /// maxClicksBetweenMovement text field, 
        /// minClicksBetweenMovement text field, 
        /// minWait text field,
        /// maxWait text field. 
        /// </summary>
        private void DisableSettingFields()
        {
            this.startButton.Enabled = false;
            this.maxClicksBetweenMovement.Enabled = false;
            this.minClicksBetweenMovement.Enabled = false;
            this.minWait.Enabled = false;
            this.maxWait.Enabled = false;
        }
        #endregion

    }
}
