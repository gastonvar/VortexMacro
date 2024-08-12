using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MousePositionConsoleApp
{
    class Program
    {
        // Imports
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo); // Mouse click

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey); // Hotkey

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint); // Get cursor position

        // Struct for storing cursor coordinates
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // Class variables
        //down finger on click
        const uint LEFTDOWN = 0x02;
        //lift finger on click
        const uint LEFTUP = 0x04;
        //scroll
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        // Up arrow key (hotkey to start program)
        const int HOTKEY = 0x26;

        // Set it to false before click out hotkey
        static bool enableClicker = false;

        // How many milli-seconds we wait before clicking again, arbitrary, I use different values
        static int clickInterval = 500;

        //------------------------------------------------------------------------------------------------------------------------------------
        //MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP - MANUAL SETUP
        //------------------------------------------------------------------------------------------------------------------------------------

        //Insert the (clickable) X coordenate in Vortex in order to click Download but pass right between the SKIP and CANCEL buttons
        const int VORTEX_X = 1517;

        //Insert the Y coordenate in Vortex to slide up and down the mouse in order to surely click any Download button
        const int VORTEX_Y_START = 270;
        const int VORTEX_Y_END = 430;

        //Insert the X coordenate of the (clickable) top left corner of the free download button
        const int NEXUS_X_START = -1000;
        //Insert the X coordenate of the (clickable) bottom right corner of the free download button
        const int NEXUS_X_END = -790;
        //Insert the Y coordenate of the (clickable) top left corner of the free download button 
        const int NEXUS_Y_START = 779;
        //Insert the Y coordenate of the (clickable) bottom right corner of the free download button
        const int NEXUS_Y_END = 802;

        //Insert the X and Y coordenates to click on a non AD non affecting space in the NexusMods page (any point that doesn't lead to anything, just to set foot in the page)
        const int NEXUS_PAGE_X = -182;
        const int NEXUS_PAGE_Y = 390;

        //Insert the X and Y coordenates of the Close Program in Google (big red X on the top right)
        const int CLOSE_GOOGLE_X = -22;
        const int CLOSE_GOOGLE_Y = 22;


        static void Main(string[] args)
        {
            // Main loop for autoclicker
            //1500 iterations (you can set it to the amount of mods to download)
            int i = 0;
            Console.WriteLine("Press ARROW UP KEY in order to start the auto clicker, might need a few pushes and/or holding the key down");
            while (i < 150000) 
            {
                // If hotkey is down (up arrow key), click on the console app and must hold hotkey a few times in order for it to work
                if (GetAsyncKeyState(HOTKEY) < 0) 
                {
                    // Enable or disable depending on the bool value
                    enableClicker = !enableClicker;
                    // A little delay between hotkey usage
                    Thread.Sleep(300); 
                }
                if (enableClicker)
                {
                    if (i == 0)
                    {
                        Console.WriteLine("ENABLED");
                    }
                    //Actions to click on the vortex app
                    ClickVortex();
                    
                    //Actions to click on the nexusmods google page
                    ClickNexus();
                    if (i % 10 == 0)
                    {
                        //Closes all google tabs so your pc doesn't die, go for a lower number than 10 if you lack RAM
                        ClearGoogle(); 
                    }
                    //Used for setting up this script, in order to set this up you better comment ClickVortex(), ClickNexus() and ClearGoogle()
                    CurrentMousePosition();
                    i++;
                }
                Thread.Sleep(clickInterval);
            }
        }

        static void ClickVortex()
        {
            //Looks up for every single position the download button could be on
            //*slides* up and down clicking every 10 pixels, goes right in between close and skip buttons
            //Usually opens about 4 of the same tab in google (hence the need to clean google)
            for(int i = VORTEX_Y_START; i< VORTEX_Y_END; i=i+10)
            {
                MoveAndClick(VORTEX_X, i); 
            }
            Thread.Sleep(4000); //Delay to wait for google to load
        }
        static void ClickNexus()
        {
            //Sets the pointer in a clear position in the nexusmods page
            MoveAndClick(NEXUS_PAGE_X, NEXUS_PAGE_Y);
            ScrollPage("down",100);
            Thread.Sleep(500);
            //Scrolls three times up and then gets the position of my free download button (picks a random position between coords)
            ScrollPage("up",3);
            Thread.Sleep(1000);
            int[] coordenadasNexusXY = PickPosition(NEXUS_X_START, NEXUS_X_END, NEXUS_Y_START, NEXUS_Y_END);
            MoveAndClick(coordenadasNexusXY[0], coordenadasNexusXY[1]);
            Thread.Sleep(8000); //Delay to wait for vortex to show next download button
        }

        static void ClearGoogle()
        {
            //Sets the pointer in the X in google and clicks
            MoveAndClick(CLOSE_GOOGLE_X,CLOSE_GOOGLE_Y);
            Thread.Sleep(3000);
        }
        static void MoveAndClick(int x, int y)
        {
            //Sets the position of the pointer in those coordenates then clicks
            SetCursorPos(x, y);
            Thread.Sleep(100);
            MouseClick();
        }

        //Delivers the current X and Y coordenates of the pointer
        static void CurrentMousePosition()
        {
            // Get and display the current mouse position
            POINT point;
            GetCursorPos(out point);
            Console.WriteLine($"Mouse Position: X={point.X}, Y={point.Y}");
        }


        // Create mouse click
        static void MouseClick()
        {
            mouse_event(LEFTDOWN, 0, 0, 0, IntPtr.Zero); // We don't need any more information than the LEFTDOWN constant
            mouse_event(LEFTUP, 0, 0, 0, IntPtr.Zero); // Press down, then up
        }

        //Picks a random coordenate in between input values
        //Basically draws a square with the given coordinates a picks a point inside
        public static int[] PickPosition(int x1, int x2, int y1, int y2)
        {   //random object
            Random random = new Random();
            //Picks a random X and Y that's in between the parameters
            int randomX = random.Next(Math.Min(x1, x2), Math.Max(x1, x2) + 1);
            int randomY = random.Next(Math.Min(y1, y2), Math.Max(y1, y2) + 1);

            //Returns an array with those coordenates
            return [randomX, randomY];
        }
        //Scrolls a certain input ammount in a certain direction
        static void ScrollPage(string direction, int steps)
        {
            int scrollAmount = steps * 120; // Each step is conventionally 120 units
            if (direction.ToLower() == "up")
            {
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)scrollAmount, IntPtr.Zero);
            }
            else if (direction.ToLower() == "down")
            {
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)(-scrollAmount), IntPtr.Zero);
            }
        }
    }
}
