using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace denali_nextgen_cmd_to_unit {
    public enum Received {
        HEX,
        ASCII
    }
    public enum CaseCheck {
        EQUAL,
        VALUE,
        DIGIT,
        FREQUENCY_HEX
    }
    class SerialPort_ {
        public SerialPort mySerialPort { get; set; }
        public bool received { get; set; }
        public StepTest step { get; set; }
        public List<string> receive_ascii { get; set; }
        public List<int> receive_hex = new List<int>();
        public string cmd { get; set; }
        public string rx { get; set; }//ค่าจาก main บอกว่า rx ต้องเป็นค่าอะไร
        public int send_every { get; set; }
        public int retest { get; set; }
        public int re { get; set; }
        public bool flag_discom { get; set; }
        public bool flag_wait_discom { get; set; }
        public string head { get; set; }
        public string result { get; set; }
        public string result_all { get; set; }
        public double min { get; set; }
        public double max { get; set; }

        public void setup(string portName, StepTest step_, string cmd_, int send_every_, int retest_, string head_,
                          string rx_, double min_, double max_) {
            step = step_;
            cmd = cmd_;
            rx = rx_;
            send_every = send_every_;
            retest = retest_;
            head = head_;
            re = 0;
            min = min_;
            max = max_;
            flag_discom = false;
            flag_wait_discom = false;
            mySerialPort = new SerialPort();
            mySerialPort.PortName = portName;
            mySerialPort.BaudRate = 9600;
            mySerialPort.DataBits = 8;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.Parity = Parity.None;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            received = false;
            receive_ascii = new List<string>();
            receive_hex = new List<int>();
        }
        public void initial() {
            Stopwatch time = new Stopwatch();
            time.Restart();
            while (time.ElapsedMilliseconds < 500) {
                if (received == true) {
                    mySerialPort.ReadExisting();
                    received = false;
                    time.Restart();
                }
                Thread.Sleep(25);
            }
        }
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            Thread.Sleep(50);
            if (step == StepTest.FREQUENCY2) {
                int length = 0;
                mySerialPort = (SerialPort)sender;
                try { length = mySerialPort.BytesToRead; } catch { return; }
                int buf = 0;
                for (int i = 0; i < length; i++) {
                    buf = mySerialPort.ReadByte();
                    receive_hex.Add(buf);
                }
                receive_ascii.Add("");
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                if (receive_hex.Count >= 2) received = true;
                return;
            }
            if (step == StepTest.INITIAL) {
                string zxc = mySerialPort.ReadExisting();
                zxc = zxc.Replace("\0", "");
                zxc = zxc.Replace("\r", "\n");
                zxc = zxc.Replace("\n\n", "\n");
                string[] xc = zxc.Split('\n');
                for (int i = 0; i < xc.Length; i++) {
                    if (xc[i] == "") continue;
                    string bbbn = xc[i].Trim();
                    if (bbbn == cmd && receive_ascii.Count == 0) {
                        receive_ascii.Add(bbbn);
                    }
                    if (bbbn == cmd && receive_ascii.Count == 1) {
                        receive_ascii[0] = bbbn;
                    }
                    if (receive_ascii.Count == 0) continue;
                    if (receive_ascii.Count == 1 && receive_ascii[0] != cmd) continue;
                    if (bbbn == cmd) continue;
                    receive_ascii.Add(bbbn);
                }
                if (receive_ascii.Count == 0) receive_ascii.Add("");
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                if(receive_ascii.Count >= 2) received = true;
                return;
            }
            string s = mySerialPort.ReadExisting();
            while (true) {
                Thread.Sleep(25);
                string waitBuff = mySerialPort.ReadExisting();
                if (waitBuff != "") s += waitBuff;
                else break;
            }
            s = s.Replace("\0", "");
            s = s.Replace("\r", "\n");
            s = s.Replace("\n\n", "\n");
            string[] ss = s.Split('\n');
            for (int i = 0; i < ss.Length; i++) {
                if (ss[i] == "") continue;
                receive_ascii.Add(ss[i].Trim());
                if (receive_ascii[0] != cmd && receive_ascii.Count != 1) {
                    receive_ascii[0] = receive_ascii[0] + receive_ascii[1];
                    receive_ascii.Remove(receive_ascii[1]);
                }
            }
            if (receive_ascii.Count == 0) receive_ascii.Add("");
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            if (receive_ascii.Count >= 2) received = true;
        }
        public bool open() {
            if (!open_port()) {
                try { mySerialPort.Close(); } catch { }
                discom("disable");
                discom("enable");
                if (!open_port()) {
                    try { mySerialPort.Close(); } catch { }
                    File.WriteAllText("test_head_" + head + "_result.txt", "can not open port\r\nFAIL");
                    return false;
                }
            }
            return true;
        }
        private bool open_port() {
            Stopwatch time = new Stopwatch();
            time.Restart();
            while (time.ElapsedMilliseconds < 5000) {
                try {
                    mySerialPort.Open();
                    time.Stop();
                    break;
                } catch {
                    Thread.Sleep(250);
                }
                try { mySerialPort.Close(); } catch { }
            }
            if (time.IsRunning) return false;
            return true;
        }
        private void discom(string cmd) {//enable disable//
            ManagementObjectSearcher objOSDetails2 = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
            foreach (ManagementObject usblist in osDetailsCollection2) {
                string arrport = usblist.GetPropertyValue("NAME").ToString();
                if (arrport.Contains(mySerialPort.PortName)) {
                    Process devManViewProc = new Process();
                    devManViewProc.StartInfo.FileName = "DevManView.exe";
                    devManViewProc.StartInfo.Arguments = "/" + cmd + " \"" + arrport + "\"";
                    devManViewProc.Start();
                    devManViewProc.WaitForExit();
                }
            }
        }
        public void test_led(bool debug) {
            while (true) {
                Console.WriteLine("send: " + cmd);
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                receive_ascii.Clear();
                try { mySerialPort.Write(cmd + "\r\n"); } catch { }
                if (debug) {
                    if (Console.ReadLine() == "end") {
                        Thread.Sleep(1000);
                        return;
                    }
                    continue;
                }
                mySerialPort.Close();
                mySerialPort.Dispose();
                break;
            }
        }
        public bool send() {
            send_label:
            re++;
            Console.WriteLine("Send >> " + cmd);
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            receive_ascii.Clear();
            receive_hex.Clear();
            result_all = "";
            received = false;
            try { mySerialPort.Write(cmd + "\r\n"); } catch { }
            Stopwatch time = new Stopwatch();
            time.Restart();
            while (time.ElapsedMilliseconds < send_every) {
                if (received != true) { Thread.Sleep(100); continue; }
                received = false;
                time.Stop();
                break;
            }
            if (time.IsRunning) {
                if (re < retest) goto send_label;
                if (!flag_discom) {
                    mySerialPort.Close();
                    flag_wait_discom = true;
                    discom("disable");
                    discom("enable");
                    flag_discom = true;
                    flag_wait_discom = false;
                    if (open()) { re = 0; goto send_label; }
                }
                Console.WriteLine("timeout!!!");
                File.WriteAllText("test_head_" + head + "_result.txt", "timeout readData\r\nFAIL");
                return false;
            }
            return true;
        }
        public void read(Received type) {
            if(type == Received.ASCII) {
                if (receive_ascii.Count != 0)
                    foreach (string rec in receive_ascii) {
                        result_all += rec + " ";
                        Console.WriteLine("Read << " + rec);
                    }
            }
            if(type == Received.HEX) {
                if (receive_hex.Count != 0)
                    foreach (int rec in receive_hex) {
                        result_all += rec + " ";
                        Console.WriteLine("Read << 0x" + rec.ToString("X2"));
                    }
            }   
        }
        private double convert_result(double value) {
            double result = 0;
            if (step == StepTest.TEMP)
                //result = ((value / 65536) * 175) - 45;
                result = ((value / 10.0) - 32) * (5.0 / 9.0);     //เนื่องจาก FW ลูกค้าอัพเดตใหม่ เขาเปลี่ยนค่าการแปลง
            else if (step == StepTest.HUMIDITY)
                //result = (value / 65536) * 100;
                result = value / 10.0;
            else if (step == StepTest.BATTERY)
                result = value / 1000.0;
            else
                return value;
            Console.WriteLine("convert = " + result);
            return result;
        }
        public bool check_result() {
            bool result_flag = true;//ผลลัพธิ์สุดท้าย ว่าจะผ่านหรือเฟว
            int count = 0;          //จำนวนข้อความที่จะได้รับจาก unit
            int num_confirm = 0;    //อาเรย์ข้อความที่ได้รับ ตัวที่เป็นการยืนยันจาก unit ว่าคำสั่งสำเร็จแล้ว
            int num_result = 0;     //อาเรย์ข้อความที่ได้รับ ตัวที่เป็นผลลัพธิ์จาก unit
            string result_confirm = "";//ข้อความที่จะเอาไปเทียบกับผลลัพธิ์จาก unit
            double offset = 0;      //บางคำสั่งอาจจะต้องใส่ offset เพื่อให้ตรงกับ spec ลูกค้า เช่น led
            CaseCheck caseCheck = CaseCheck.EQUAL;//เอาไว้แยก step อีกทีนึง มันมีบางอัน คอมม่อนกันได้


            switch (step) {
                case StepTest.OK:
                    count = 2;
                    num_confirm = 1;
                    num_result = 1;
                    result_confirm = "OK";
                    caseCheck = CaseCheck.EQUAL;
                    break;
                case StepTest.INITIAL:
                    count = 2;
                    num_confirm = 1;
                    num_result = 1;
                    result_confirm = "OK";
                    caseCheck = CaseCheck.EQUAL;
                    break;
                case StepTest.EQUAL:
                    count = 3;
                    num_confirm = 1;
                    num_result = 1;
                    result_confirm = rx;
                    caseCheck = CaseCheck.EQUAL;
                    break;
                case StepTest.VALUE:
                    count = 3;
                    num_result = 1;
                    caseCheck = CaseCheck.VALUE;
                    break;
                case StepTest.LIGHT:
                    count = 3;
                    num_result = 1;
                    offset = 1;
                    caseCheck = CaseCheck.VALUE;
                    break;
                case StepTest.TEMP:
                    count = 3;
                    num_result = 1;
                    caseCheck = CaseCheck.VALUE;
                    break;
                case StepTest.HUMIDITY:
                    count = 3;
                    num_result = 1;
                    caseCheck = CaseCheck.VALUE;
                    break;
                case StepTest.BATTERY:
                    count = 3;
                    num_result = 1;
                    caseCheck = CaseCheck.VALUE;
                    break;
                case StepTest.HARDWARE:
                    foreach (string sss in receive_ascii) {
                        if (sss.Contains("Invalid")) {
                            string[] spl = cmd.Split(',');
                            cmd = spl[0] + "," + "UNSAFE," + spl[1];
                            break;
                        }
                    }
                    count = 2;
                    num_confirm = 1;
                    num_result = 1;
                    result_confirm = rx;
                    caseCheck = CaseCheck.EQUAL;
                    break;
                case StepTest.DIGIT:
                    count = 3;
                    num_result = 1;
                    caseCheck = CaseCheck.DIGIT;
                    break;
                case StepTest.FREQUENCY:
                    count = 1;
                    num_confirm = 0;
                    num_result = 0;
                    result_confirm = rx;
                    caseCheck = CaseCheck.EQUAL;
                    break;
                case StepTest.FREQUENCY2:
                    count = 1;
                    num_result = 0;
                    caseCheck = CaseCheck.FREQUENCY_HEX;
                    break;
                case StepTest.SWITCH:
                    count = 3;
                    num_confirm = 1;
                    num_result = 1;
                    result_confirm = rx;
                    caseCheck = CaseCheck.EQUAL;
                    break;
            }

            switch (caseCheck) {
                case CaseCheck.EQUAL: {//===========================Equal================================//
                        if (receive_ascii.Count != count) {
                            result_flag = false;
                            break;
                        }
                        if (receive_ascii[num_confirm] != result_confirm) result_flag = false;
                        result = receive_ascii[num_result];
                        break;
                    }
                case CaseCheck.VALUE: {//===========================Value================================//
                        if (receive_ascii.Count != count) {
                            result_flag = false;
                            break;
                        }
                        Console.WriteLine("min = " + min);
                        Console.WriteLine("max = " + max);
                        double rx_double = 0;
                        try {
                            rx_double = Convert.ToDouble(receive_ascii[num_result]);
                        } catch {
                            result_flag = false;
                            result = receive_ascii[num_result];
                            break;
                        }
                        rx_double += offset;
                        rx_double = convert_result(rx_double);
                        if (rx_double < min || rx_double > max) result_flag = false;
                        result = rx_double.ToString("0.##");
                        break;
                    }
                case CaseCheck.DIGIT: {//===========================Digit================================//
                        if (receive_ascii.Count != count) {
                            result_flag = false;
                            break;
                        }
                        if (receive_ascii[num_result].Count() != Convert.ToInt32(rx)) result_flag = false;
                        //if (receive_ascii[num_result].Contains("Error") || receive_ascii[num_result].Contains("ERROR"))
                        //    result_flag = false;
                        result = receive_ascii[num_result];
                        break;
                    }
                case CaseCheck.FREQUENCY_HEX: {//===========================FREQUENCY_HEX================================//
                        if (receive_hex.Count != count) {
                            result_flag = false;
                            break;
                        }
                        byte rx_hex = Convert.ToByte(rx.Substring(2, 2), 16);
                        if (receive_hex[num_result] != rx_hex) result_flag = false;
                        result = "0x" + receive_hex[num_result].ToString("x2");
                        break;
                    }
            }

            return result_flag;
        }
        public void write(bool result_) {
            string res = "PASS";
            if (!result_) res = "FAIL";
            if (result == "" || result == null) result = result_all;
            File.WriteAllText("test_head_" + head + "_result.txt", result + "\r\n" + res);
            mySerialPort.Close();
            mySerialPort.Dispose();
        }
    }
}
