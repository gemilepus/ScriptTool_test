using MahApps.Metro.Controls;
using Ownskit.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Media;

using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using MahApps.Metro.Controls.Dialogs;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

using GameOverlay.Drawing;
using GameOverlay.Windows;

namespace Metro
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        // **************************************** Motion ******************************************
        // key actions
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
        public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag
        public const int VK_LCONTROL = 0xA2; //Left Control key code
        public const int A = 0x41; //A key code
        public const int C = 0x43; //C key code

        // Mouse move
        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        // Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        // GetActiveWindowTitle
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
        // End *********************************************************************************************

        // **************************************** Window Activate ******************************************
        // Set Foreground Window                        
        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // VkKeyScan Char to 0x00
        [DllImport("user32.dll")]
        static extern byte VkKeyScan(char ch);
        // End *********************************************************************************************

        // ******************************** PostMessage & SendMessage & FindWindows ********************************
        // FindWindows EX
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        // PostMessageA
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_CHAR = 0x0102;

        //const int WM_LBUTTONDOWN = 0x201;
        //const int WM_LBUTTONUP = 0x202;

        // SendMessage
        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);

        // End *********************************************************************************************

        // **************************************** OpenCV & Media ******************************************
        public static Bitmap makeScreenshot()
        {
            Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            System.Drawing.Graphics gfxScreenshot = System.Drawing.Graphics.FromImage(screenshot);

            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

            gfxScreenshot.Dispose();

            return screenshot;
        }

        public static Bitmap makeScreenshot_clip(int x, int y, int height, int width)
        {
            Bitmap screenshot = new Bitmap(width, height);

            System.Drawing.Graphics gfxScreenshot = System.Drawing.Graphics.FromImage(screenshot);

            gfxScreenshot.CopyFromScreen(x, y, 0 , 0 , screenshot.Size);

            gfxScreenshot.Dispose();

            return screenshot;
        }

        //  Multiple results
        private String RunTemplateMatch(Mat rec, Mat template)
        {
            string reText = "";

            using (Mat refMat = rec)
            using (Mat tplMat = template)
            using (Mat res = new Mat(refMat.Rows - tplMat.Rows + 1, refMat.Cols - tplMat.Cols + 1, MatType.CV_32FC1))
            {
                //Convert input images to gray
                Mat gref = refMat.CvtColor(ColorConversionCodes.BGR2GRAY);
                Mat gtpl = tplMat.CvtColor(ColorConversionCodes.BGR2GRAY);

                Cv2.MatchTemplate(gref, gtpl, res, TemplateMatchModes.CCoeffNormed);
                Cv2.Threshold(res, res, 0.8, 1.0, ThresholdTypes.Tozero);

                while (true)
                {
                    double minval, maxval, threshold = 0.8;
                    OpenCvSharp.Point minloc, maxloc;
                    Cv2.MinMaxLoc(res, out minval, out maxval, out minloc, out maxloc);

                    if (maxval >= threshold)
                    {
                        //Setup the rectangle to draw
                        //OpenCvSharp.Rect r = new OpenCvSharp.Rect(new OpenCvSharp.Point(maxloc.X, maxloc.Y), new OpenCvSharp.Size(tplMat.Width, tplMat.Height));

                        //Draw a rectangle of the matching area
                        //Cv2.Rectangle(refMat, r, Scalar.LimeGreen, 2);

                        //Fill in the res Mat so you don't find the same area again in the MinMaxLoc
                        OpenCvSharp.Rect outRect;
                        Cv2.FloodFill(res, maxloc, new Scalar(0), out outRect, new Scalar(0.1), new Scalar(1.0), FloodFillFlags.Link4);

                        reText = reText + maxloc.X.ToString() + "," + maxloc.Y.ToString() + ",";
                    }
                    else
                    {
                        break;
                    }

                }

                return reText;
            }
        }

        private Mat DetectFace_Mat(CascadeClassifier cascade, Mat src) // input Mat 
        {
            Mat result;
            using (var gray = new Mat())
            {
                result = src.Clone();
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
                // Detect faces
                OpenCvSharp.Rect[] faces = cascade.DetectMultiScale(gray, 1.08, 2, HaarDetectionType.ScaleImage, new OpenCvSharp.Size(30, 30));
                //Render all detected faces
                foreach (OpenCvSharp.Rect face in faces)
                {
                    var center = new OpenCvSharp.Point // get x,y
                    {
                        X = (int)(face.X + face.Width * 0.5),
                        Y = (int)(face.Y + face.Height * 0.5)
                    };
                    var axes = new OpenCvSharp.Size
                    {
                        Width = (int)(face.Width * 0.5),
                        Height = (int)(face.Height * 0.5)
                    };
                    Cv2.Ellipse(result, center, axes, 0, 0, 360, new Scalar(255, 0, 255), 4);

                }
            }
            return result;
        }


        // End ***************************************************************************************

        public DependencyProperty UnitIsCProperty = DependencyProperty.Register(
        "IsActive", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
        public bool IsActive
        {
            get { return (bool)this.GetValue(UnitIsCProperty); }
            set
            {
                this.SetValue(UnitIsCProperty, value);
            }
        }

        KeyboardListener KListener = new KeyboardListener();

        // DataGrid
        List<mTable> mDataTable = new List<mTable>();
        List<eTable> eDataTable = new List<eTable>();

        Thread MainThread = null;


        public MainWindow()
        {
            InitializeComponent();

            // Data Binding
            this.DataContext = this;

            //MainThread = Thread.CurrentThread;// get main thread 

            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
         

            // Combobox List
            List<string> mList = new List<string>() { "Move", "Loop", "Click", "Match", "Key", "Delay", "Get Point",
                "Run exe", "FindWindow", "ScreenClip", "Draw", "Clear Draw", "PostMessage", "PlaySound", "Shift"};
            mComboBoxColumn.ItemsSource = mList;

            //mDataTable.Add(new mTable() { mTable_IsEnable = true, mTable_Mode = "", mTable_Action = "", mTable_Event = "" });
            mDataGrid.DataContext = mDataTable;

            eDataTable.Add(new eTable() { eTable_Enable = true, eTable_Name = "Run", eTable_Key = "R", eTable_Note = "" });
            eDataTable.Add(new eTable() { eTable_Enable = true, eTable_Name = "Stop", eTable_Key = "E", eTable_Note = "" });
            eDataGrid.DataContext = eDataTable;


            //var data = new mTable { Test1 = "Test1", Test2 = "Test2" };          
            //mDataGrid.Items.Add(data);
            //mDataGrid.CurrentCell = mDataGrid.Rows(0).Cells(1);
            //mDataGrid.Focus();

        }

        private void mDataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = new mTable
            {
              mTable_IsEnable = true,
                        mTable_Mode = "",
                        mTable_Action = "",
                        mTable_Event = ""
            };
        }

        public class mTable
        {
            public bool   mTable_IsEnable { get; set; }
            public string mTable_Mode { get; set; }
            public string mTable_Action { get; set; }
            public string mTable_Event { get; set; }
        }

        public class eTable
        {
            public bool eTable_Enable { get; set; }
            public string eTable_Name { get; set; }
            public string eTable_Key { get; set; }
            public string eTable_Note { get; set; }
        }
        
        private void mDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex()).ToString(); 
        }

        public bool isRun = false;

        void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            KListener.Dispose();

            if (args.Key.ToString().Equals("F8"))// Get key
            {
                // do someting...
                Run_script();
                Thread_ON = true;
            }

            if (args.Key.ToString().Equals("F9"))// Get key
            {
                // do someting...
                
                mThread.Abort();//main thread aborting newly created thread.  
                Thread_ON = false;
                IsActive = false;
            }

             if (args.Key.ToString().Equals("F11"))// Get key
             {
                 // do someting...

                 isRun = false;
             }
             
              
            // Restart
            KListener = new KeyboardListener();
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);

            Console.WriteLine(args.Key.ToString());
            // Prints the text of pressed button, takes in account big and small letters. E.g. "Shift+a" => "A"
            Console.WriteLine(args.ToString());
        }

     
        public void run() {  // TODO: Delete
        
        }
       
        private void Btn_open_Click(object sender, RoutedEventArgs e)
        {
            string fileContent = string.Empty;
            string filePath = string.Empty;

            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();

            openFileDialog.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            openFileDialog.Filter = "txt files (*.txt)|*.txt"; // "txt files (*.txt)|*.txt|All files (*.*)|*.*"
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            try
            {
                //Get the path of specified file
                filePath = openFileDialog.FileName;
                //Read the contents of the file into a stream
                StreamReader reader = new StreamReader(filePath);

                // read test
                fileContent = reader.ReadToEnd();
                fileContent.Replace(";", "%;");
                string[] SplitStr = fileContent.Split(';');

                // Table Clear
                mDataGrid.DataContext = null;
                mDataTable.Clear();

                for (int i = 0; i < SplitStr.Length - 4; i = i + 4)
                {
                    mDataTable.Add(new mTable()
                    {
                        mTable_IsEnable = bool.Parse(SplitStr[i].Replace("%", "")),
                        mTable_Mode = SplitStr[i + 1].Replace("%", ""),
                        mTable_Action = SplitStr[i + 2].Replace("%", ""),
                        mTable_Event = SplitStr[i + 3].Replace("%", ""),
                    });
                }
                mDataGrid.DataContext = mDataTable;

            }
            catch
            {            
                this.ShowMessageAsync("", "ERROR!");
            }

        }

        private async void Btn_Save_Click(object sender, RoutedEventArgs e) // async
        {
            var result = await this.ShowInputAsync("Save", "input filename:");

            if (result == null) {
                return;
            }

            string out_string = "";
            for (int i = 0; i < mDataTable.Count; i++)
            {
                out_string += mDataTable[i].mTable_IsEnable.ToString() + ";" + mDataTable[i].mTable_Mode + ";"
                   + mDataTable[i].mTable_Action + ";" + mDataTable[i].mTable_Event.ToString() + ";" + "\n";
            }
            System.IO.File.WriteAllText(System.Windows.Forms.Application.StartupPath + "/" + result + ".txt", out_string);
      
        }


        bool Thread_ON = false;
        Thread mThread = null;

        bool Tempflag = false;
        Bitmap TempBitmap;

        Stack mStack = new Stack();
               

        private void Run_script() 
        {
            if (Thread_ON == true) {
                mThread.Abort();        

            }
            
            IsActive = true;

            mThread = new Thread(() =>
            {

                SortedList mDoSortedList = new SortedList();
                // key || value
                //mDoSortedList.Add("Point", "0,0");
                //mDoSortedList.Add("Point Array", "0,0,0,0");
                //mDoSortedList.Add("Sound", "");
                //mDoSortedList.Add("Draw", "");

                //mDoSortedList.RemoveAt(mDoSortedList.IndexOfKey("Draw"));

                // GameOverlay .Net
                OverlayWindow _window;
                GameOverlay.Drawing.Graphics _graphics;

                // Brush
                GameOverlay.Drawing.SolidBrush _red;
                GameOverlay.Drawing.Font _font;
                GameOverlay.Drawing.SolidBrush _black;

                // it is important to set the window to visible (and topmost) if you want to see it!
                _window = new OverlayWindow(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
                {
                    IsTopmost = true,
                    IsVisible = true
                };

                // handle this event to resize your Graphics surface
                //_window.SizeChanged += _window_SizeChanged;

                // initialize a new Graphics object
                // set everything before you call _graphics.Setup()
                _graphics = new GameOverlay.Drawing.Graphics
                {
                    //MeasureFPS = true,
                    Height = _window.Height,
                    PerPrimitiveAntiAliasing = true,
                    TextAntiAliasing = true,
                    UseMultiThreadedFactories = false,
                    VSync = true,
                    Width = _window.Width,
                    WindowHandle = IntPtr.Zero
                };

                _window.CreateWindow();
                _graphics.WindowHandle = _window.Handle; // set the target handle before calling Setup()         
                _graphics.Setup();

               
                _red = _graphics.CreateSolidBrush(GameOverlay.Drawing.Color.Red); // those are the only pre defined Colors
                // creates a simple font with no additional style
                _font = _graphics.CreateFont("Arial", 25);
                _black = _graphics.CreateSolidBrush(GameOverlay.Drawing.Color.Transparent);


               var gfx = _graphics; // little shortcut


                //Console.WriteLine("Thread Run");
                int n = 0;
                while (n < mDataTable.Count)
                {
                    string Command = mDataTable[n].mTable_Mode;
                    string CommandData = mDataTable[n].mTable_Action;
                    string CommandEvent = mDataTable[n].mTable_Event;

                    switch (Command)
                    {
                        case "Move":

                            if (!CommandEvent.Equals(""))                          
                            {

                                // Check Key
                                if (mDoSortedList.IndexOfKey(CommandEvent) != -1) {
                                    // Get SortedList Value by Key
                                    string mDoItem = mDoSortedList.GetByIndex(mDoSortedList.IndexOfKey(CommandEvent)).ToString();
                                    // Remove SortedList Value by key  
                                    mDoSortedList.RemoveAt(mDoSortedList.IndexOfKey(CommandEvent));

                                    string[] movestr = mDoItem.Split(',');

                                    SetCursorPos(int.Parse(movestr[0]), int.Parse(movestr[1]));
                                }
                            }
                            else {
                                string[] str_move = CommandData.Split(',');
                                SetCursorPos(int.Parse(str_move[0]), int.Parse(str_move[1]));
                            }
                           
                            break;

                        case "Shift":

                            string[] str_shift = CommandData.Split(',');
                            
                            System.Drawing.Point point_Shift = System.Windows.Forms.Control.MousePosition;

                            SetCursorPos(point_Shift.X + int.Parse(str_shift[0]), point_Shift.Y + int.Parse(str_shift[1]));
                            

                            break;


                        case "Delay":

                            Thread.Sleep(Int32.Parse(CommandData));

                            break;

                        case "Click":

                            if (CommandData.Equals("L"))
                            {
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                            }
                            else
                            {
                                mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                                mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                            }

                            break;

                        case "Match":

                            string TempPath = CommandData;
                            Mat matTarget;
                            if (TempPath.Equals("")) { 
                                TempPath = "s.png";
                            }

                            if (TempPath.IndexOf(',') != -1)
                            {
                                string[] mSize = TempPath.Split(',');
                                matTarget = BitmapConverter.ToMat(makeScreenshot_clip(int.Parse(mSize[1]), int.Parse(mSize[2]),
                                    int.Parse(mSize[3]), int.Parse(mSize[4])));
                                TempPath = mSize[0];
                            }
                            else {   
                                matTarget = BitmapConverter.ToMat(makeScreenshot());
                            }

                            
                            Mat matTemplate = new Mat(TempPath , ImreadModes.Color);
                            int temp_w = matTemplate.Width/2 , temp_h = matTemplate.Height/2; // center x y

                            //System.Windows.Forms.MessageBox.Show(RunTemplateMatch(matTarget, matTemplate));
                            string return_xy = RunTemplateMatch(matTarget, matTemplate);
                            if (!return_xy.Equals("")) {
                                string[] xy = return_xy.Split(',');
                                
                                if (!CommandEvent.Equals(""))
                                {
                                    mDoSortedList.Add(CommandEvent, (int.Parse(xy[0]) + temp_w).ToString() + "," + (int.Parse(xy[1]) + temp_h).ToString());
                                }
                                else {
                                    SetCursorPos(int.Parse(xy[0]) + temp_w, int.Parse(xy[1]) + temp_h);
                                }
                               
                            }


                            break;

                        case "Key":

                            //Char[] mChar =CommandData.ToCharArray();
                            //for (int j = 0; j < mChar.Length; j++)
                            //{
                            //    keybd_event(VkKeyScan(mChar[j]), 0, KEYEVENTF_EXTENDEDKEY, 0);
                            //    keybd_event(VkKeyScan(mChar[j]), 0, KEYEVENTF_KEYUP, 0);
                            //}

                            // {ENTER}
                            SendKeys.SendWait(CommandData);

                            break;
                        case "Get Point":

                            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
                            //TempIntA = point.X;
                            //TempIntB = point.Y;

                            if (CommandData.IndexOf('#') != -1)
                            {
                                mDoSortedList.Add(CommandData, point.X.ToString() + "," + point.Y.ToString());
                            }
                            break;
                        case "Run exe":

                            try
                            {
                                Process.Start(CommandData);
                            }
                            catch { 

                            }
                            break;

                        case "FindWindow":

                            // and window name were obtained using the Spy++ tool.
                            IntPtr calculatorHandle = FindWindow(null,CommandData);

                            // Verify that Calculator is a running process.
                            if (calculatorHandle == IntPtr.Zero)
                            {
                                //System.Windows.MessageBox.Show("is not running...");
                                //return;
                            }

                            // Make Calculator the foreground application and send it 
                            // a set of calculations.
                            SetForegroundWindow(calculatorHandle);
                            break;
                        case "ScreenClip":

                            string[] str_clip = CommandData.Split(',');
                            TempBitmap = makeScreenshot_clip(int.Parse(str_clip[0]), int.Parse(str_clip[1]), 
                                int.Parse(str_clip[2]), int.Parse(str_clip[3]));

                            break; 
                           
                        case "Loop":

                            n = -1;
               
                            break;
                        case "Draw":

                            string TempPathd =CommandData;
                            Mat matTargetd = null;
                            if (TempPathd.Equals("")) { 
                                TempPathd = "s.png";
                            }

                            if (TempPathd.IndexOf(',') != -1)
                            {
                                string[] mSize = TempPathd.Split(',');
                                matTargetd = BitmapConverter.ToMat(makeScreenshot_clip(int.Parse(mSize[1]), int.Parse(mSize[2]),
                                    int.Parse(mSize[3]), int.Parse(mSize[4])));
                                TempPathd = mSize[0];
                            }
                            else {

                                Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                                System.Drawing.Graphics gfxScreenshot = System.Drawing.Graphics.FromImage(screenshot);

                                gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                                gfxScreenshot.Dispose();

                                matTargetd = BitmapConverter.ToMat(screenshot);

                                screenshot.Dispose();
                            }

                            
                            Mat matTemplated = new Mat(TempPathd , ImreadModes.Color);
                            int temp_wd = matTemplated.Width/2 , temp_hd = matTemplated.Height/2; // center x y

                            //System.Windows.Forms.MessageBox.Show(RunTemplateMatch(matTarget, matTemplate));
                            string return_xyd = RunTemplateMatch(matTargetd, matTemplated);
                            if (!return_xyd.Equals(""))
                            {
                                string[] xy = return_xyd.Split(',');
                               

                                gfx.BeginScene(); // call before you start any drawing
                                // Draw
                                gfx.DrawTextWithBackground(_font, _red, _black, 10, 10, return_xyd.ToString());

                                gfx.DrawRoundedRectangle(_red, RoundedRectangle.Create(int.Parse(xy[0]), int.Parse(xy[1]), temp_wd*2, temp_hd*2, 6), 2);
                                gfx.EndScene();

                                Tempflag = true;

                                if (!CommandEvent.Equals(""))
                                {
                                    mDoSortedList.Add(CommandEvent, (int.Parse(xy[0]) + temp_wd).ToString() + "," + (int.Parse(xy[1]) + temp_hd).ToString());
                                }
                                else
                                {
                                    // Move
                                    SetCursorPos(int.Parse(xy[0]) + temp_wd, int.Parse(xy[1]) + temp_hd);
                                }
                            }

                            matTargetd.Dispose();
                            matTemplated.Dispose();

                            break;

                        case "Clear Draw":
     
                            // Clear Draw
                            gfx.BeginScene(); // call before you start any drawing
                            gfx.ClearScene();
                            gfx.EndScene();
                          
                            break;

                        case "PostMessage":

                            string[] send =CommandData.Split(',');

                            IntPtr windowHandle;
                            if (send[0].Equals("T"))
                            {                               
                                windowHandle = FindWindow(null, send[1]);  
                            }
                            else {
                                windowHandle = FindWindow(send[1], null);
                            }
                        
                            // Find child
                            //IntPtr editHandle = FindWindowEx(windowHandle, IntPtr.Zero, "EDIT", null);
                            //IntPtr editHandle = FindWindowEx(windowHandle, IntPtr.Zero, null, null);

                            IntPtr editHandle = windowHandle;

                            
                            if (editHandle == IntPtr.Zero)
                            {
                                System.Windows.MessageBox.Show("is not running...");
                            }
                            else {
                                //System.Windows.MessageBox.Show(windowHandle.ToString());

                                gfx.BeginScene(); // call before you start any drawing
                                // Draw
                                gfx.DrawTextWithBackground(_font, _red, _black, 10, 10, windowHandle.ToString());
                                gfx.EndScene();
                            }

                            //PostMessage(editHandle, WM_KEYDOWN, 0x0D, 0);               
                            //PostMessage(editHandle, 0x0100, 0x11, 0);
                            //PostMessage(editHandle, WM_KEYDOWN, 'F', 0);
                            //PostMessage(editHandle, WM_KEYDOWN, VkKeyScan('f'), 0);

                            //PostMessage(editHandle, 0x0204, 0, MakeLParam(100, 50));
                            //PostMessage(editHandle, 0x0205, 0, MakeLParam(100, 50)); 


                            for (int i = 2; i < send.Length; i++) {
                                // 0xXX string to Hex int
                                //int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString("0x25");
                                int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString(send[i]);


                                //PostMessage(editHandle, WM_KEYDOWN, value, 0x014B0001);
                                //Thread.Sleep(50);
                                //PostMessage(editHandle, WM_KEYUP, value, (int)new System.ComponentModel.Int32Converter().ConvertFromString("0xC14B0001"));



                                SendMessage((int)editHandle, WM_KEYDOWN, 0, "0x014B0001");
                                Thread.Sleep(50);
                                SendMessage((int)editHandle, WM_KEYUP, 0, "0xC14B0001");



                                //SendMessage((int)editHandle, value, 0,null );
                                Thread.Sleep(50);
                            }

                            //PostMessage(editHandle, WM_KEYDOWN, 0x25, 0);
                            //Thread.Sleep(50);
                            //PostMessage(editHandle, WM_KEYDOWN, 0x26, 0);
                            //Thread.Sleep(50);
                            //PostMessage(editHandle, WM_KEYDOWN, 0x27, 0);
                            //Thread.Sleep(50);
                            //PostMessage(editHandle, WM_KEYDOWN, 0x28, 0);
                            //Thread.Sleep(50);


                            // Get Child

                            //List result = new List();
                            //string listStr = "";
                            //int ct = 0;
                            //IntPtr prevChild = IntPtr.Zero;
                            //IntPtr currChild = IntPtr.Zero;
                            //while (true && ct < 5)
                            //{
                            //    currChild = FindWindowEx(editHandle, prevChild, null, null);
                            //    if (currChild == IntPtr.Zero) break;
                            //    //result.Add(currChild);
                            //    listStr += "  ~ " + currChild;
                            //    prevChild = currChild;
                            //    ++ct;

                            //    //for (int i = 2; i < send.Length; i++)
                            //    //{
                            //    //    // 0xXX string to Hex int
                            //    //    //int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString("0x25");
                            //    //    int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString(send[i]);

                            //    //    IntPtr ee = FindWindowEx(windowHandle, currChild, null, null);

                            //    //    PostMessage(ee, WM_KEYDOWN, value, 0);
                            //    //    Thread.Sleep(50);
                            //    //}


                            //}
                            //System.Windows.MessageBox.Show(listStr);


                            Process[] processlist = Process.GetProcesses();

                            string titleText = "";
                            foreach (Process process in processlist)
                            {
                                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                                {
                                    Console.WriteLine("Process: {0} ID: {1} Window title: {2}", process.ProcessName,
                                        process.Id, process.MainWindowTitle);

                                    titleText += process.MainWindowTitle.ToString();
                                }
                            }

                            string out_string = titleText;

                            System.IO.File.WriteAllText(System.Windows.Forms.Application.StartupPath + "/" + "out" + ".txt", out_string);

                            break;

                        case "PlaySound":

                            string SoundPath = CommandData;
                            if (Tempflag == true) {
                                // SoundPlayer
                                SoundPlayer mWaveFile = new SoundPlayer(SoundPath);
                                mWaveFile.PlaySync();
                                Tempflag = false;
                            }
                          
                            break;


                        default:

                            break;
                    }
                    n++;

                    //Ring.IsActive = false;

                }
               
            });
            mThread.Start();
            
        }
        
        public int MakeLParam(int LoWord, int HiWord)
        {
            return ((HiWord << 16) | (LoWord & 0xffff));
        }

        private void Btn_Run_Click(object sender, RoutedEventArgs ee)
        {
            Run_script();
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs ee)
        {
            mThread.Abort();//main thread aborting newly created thread.  
            Thread_ON = false;
            IsActive = false;
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Btn_About_Click(object sender, RoutedEventArgs e)
        {
            
            this.ShowMessageAsync("", "@");
        }

        private void mDataGrid_Setting_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {

        }

        private void mDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            int columnIndex = mDataGrid.Columns.IndexOf(mDataGrid.CurrentCell.Column);
            //this.ShowMessageAsync("", mDataGrid.Columns[columnIndex].Header.ToString());
            if (columnIndex < 0) {

                return;
            }


            if (mDataGrid.Columns[columnIndex].Header.ToString().Equals(" "))
            {
                //mDataGrid.SelectedIndex alaway -1
                int tableIndex = mDataGrid.Items.IndexOf(mDataGrid.CurrentItem);

                //this.ShowMessageAsync("", mDataGrid.Items.IndexOf(mDataGrid.CurrentItem).ToString());

                try
                {
                    if (tableIndex < mDataTable.Count())
                    {
                        //Table Clear
                        mDataGrid.DataContext = null;
                        mDataTable.RemoveAt(tableIndex);
                        mDataGrid.DataContext = mDataTable;
                    }
                }
                catch
                {

                }

            }

            if (mDataGrid.Columns[columnIndex].Header.ToString().Equals("+"))
            {
                // Get index
                int tableIndex = mDataGrid.Items.IndexOf(mDataGrid.CurrentItem);

                try
                {
                    if (tableIndex < mDataTable.Count() - 1)
                    {
                        // Insert Item
                        mDataGrid.DataContext = null;
                        mDataTable.Insert(tableIndex + 1, new mTable() { mTable_IsEnable = true, mTable_Mode = "", mTable_Action = "", mTable_Event = "" });
                        mDataGrid.DataContext = mDataTable;
                    }
                    else {
                        mDataGrid.DataContext = null;
                        mDataTable.Add(new mTable() { mTable_IsEnable = true, mTable_Mode = "", mTable_Action = "", mTable_Event = "" });
                        mDataGrid.DataContext = mDataTable;
                    }                  
                }
                catch
                {

                }

              
            }

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                mThread.Abort();
            }
            catch
            {

            }
        }
    }
}