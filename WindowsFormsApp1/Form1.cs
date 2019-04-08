using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(int hProcess,
        Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        private Process process;
        private Process[] processes;
        private IntPtr processHandle;

        private long baseAddress = -1;

        private long[] offsets = { 0x047E6928, 0x1B0, 0x290, 0x180, 0xE8, 0x44 };

        private bool initializeCheck = false;

        private long startingPoint = -1;
        private long startingPointCheck = -1;

        private int[] hitDmg = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private float[] isHitting = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        private int[] isHittingCheck = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

        private int zoneDmg = 0;
        private int zoneDmgCheck = 0;
        private float dps = 0.0f;

        public Form1()
        {
            InitializeComponent();
            this.TopMost=true;
            timer4.Start();
            int temp = 3;
        }
        private void timer4_Tick(object sender, EventArgs e)
        {
            processes = Process.GetProcessesByName("MONSTERHUNTERWORLD");
            if ((processes.Length == 0) && (!initializeCheck))
            {

            }
            else if ((processes.Length != 0) && (!initializeCheck))
            {
                label2.Hide();
                label3.Show();
                label4.Show();
                label5.Show();
                label6.Show();
                process = processes[0];
                processHandle = OpenProcess(this.PROCESS_WM_READ, false, process.Id);

                foreach (ProcessModule M in process.Modules)
                {
                    if (M.ModuleName.ToLower() == "MonsterHunterWorld.exe".ToLower())
                    {
                        baseAddress = (long)M.BaseAddress;
                    }
                }

                read64bitPointerList();
                startingPointCheck = startingPoint;

                initializeDmg();

                this.timer1.Start();
                this.timer2.Start();
                this.timer3.Start();
                initializeCheck = true;
            }
            else if((processes.Length==0) && initializeCheck)
            {
                Application.Exit();
            }
            
        }
        private void initializeDmg()
        {
            for (int i = 0; i < 18; i++)
            {
                hitDmg[i] = read4byteAddress(startingPoint + (i * 0x90));
            }
            for (int i = 0; i < 18; i++)
            {
                isHitting[i] = readFloatAddress(startingPoint + (i * 0x90) - 0x4);
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            initializeDmg();

            for (int i = 0; i < 18; i++)
            {
                if ((Math.Round(isHitting[i], 1) != 0.0) && (isHittingCheck[i] == 0))
                {
                    zoneDmg = zoneDmg + hitDmg[i];
                    isHittingCheck[i] = 1;
                }
                if ((Math.Round(isHitting[i], 1) == 0.0) && (isHittingCheck[i] == 1))
                {
                    isHittingCheck[i] = 0;
                }
            }

            //debugPrintDmg();
            printDmg();
        }

        void printDmg()
        {
            label6.Text = zoneDmg.ToString();
            label5.Text = string.Format("{0:F1}", dps);
        }

        private void debugPrintDmg()
        {
            //label1.Text = string.Format("{0:F1}", isHitting[0]);
            //label1.Text = isHitting[0].ToString("0.####################################################################################");
            label1.Text = (Math.Round(isHitting[0], 1) != 0.0).ToString();
            //label1.Text = Math.Round(isHitting[0], 1).ToString();
            label5.Hide();
            label1.Show();
        }

        private void read()
        {  
        }

        private int readByteAddress(long address)
        {
            byte[] buffer = new byte[1];
            int bytesRead = new int();
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);

            return buffer[0];
        }

        private int read4byteAddress(long address)
        {
            byte[] buffer = new byte[4];
            int bytesRead = new int();
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToInt32(buffer, 0);
        }

        private float readFloatAddress(long address)
        {
            byte[] buffer = new byte[4];
            int bytesRead = new int();
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);

            return BitConverter.ToSingle(buffer, 0);
        }

        private long read8byteAddress(long address)
        {
            byte[] buffer = new byte[8];
            int bytesRead = new int();
            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);
               
            return BitConverter.ToInt64(buffer, 0);
        }

        private void read64bitPointerList()
        {
            long result = baseAddress;
            for (int i = 0; i < offsets.Length; i++)
            {
                if (i == (offsets.Length - 1))
                {
                    result = result + offsets[i];
                }
                else
                {
                    result = read8byteAddress(result + offsets[i]);
                }
            }
            startingPoint = result;
            
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            read64bitPointerList();
            if (startingPoint != startingPointCheck)
            {
                this.timer1.Stop();
                this.timer3.Stop();
                //reset
                zoneDmg = 0;
                dps = 0.0f;

                startingPointCheck = startingPoint;
                this.timer1.Start();
                this.timer3.Start();
            }
            
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            dps = ((float)zoneDmg - (float)zoneDmgCheck) / 5;
            zoneDmgCheck = zoneDmg;
            if(dps<0)
            {
                dps = 0.0f;
            }
        }

        
    }
}
