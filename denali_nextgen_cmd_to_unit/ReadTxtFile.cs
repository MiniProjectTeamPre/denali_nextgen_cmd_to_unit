using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace denali_nextgen_cmd_to_unit {
    enum StepTest {
        LED,
        OK,
        INITIAL,
        EQUAL,
        VALUE,
        LIGHT,
        TEMP,
        HUMIDITY,
        BATTERY,
        HARDWARE,
        DIGIT,
        FREQUENCY,
        FREQUENCY2,
        SWITCH
    }
    class Support {
        public string step = "_step.txt";
        public string timeout = "_timeout.txt";
        public string debug = "_debug.txt";
        public string data_tx = "_data_tx.txt";
        public string comport = "_comport.txt";
        public string retest = "_retest.txt";
        public string send_every = "_send_every.txt";
        public string data_rx = "_data_rx.txt";
        public string min = "_data_rx_min.txt";
        public string max = "_data_rx_max.txt";
    }
    class ReadTxtFile {
        public string head { get; set; }
        public int timeout { get; set; }
        public bool debug { get; set; }
        public int send_every { get; set; }
        public int retest { get; set; }
        public string step { get; set; }
        public string cmd { get; set; }
        public string rx { get; set; }
        public string min_str { get; set; }
        public string max_str { get; set; }
        public string port_name { get; set; }
        public string path_file { get; set; }
        public string path_file_main { get; set; }
        public double min { get; set; }
        public double max { get; set; }
        public StepTest stepTest { get; set; }

        public ReadTxtFile() {
            head = "1";
        }
        public void wait_get_head() {
            while (true) {
                try {
                    head = File.ReadAllText("../../config/head.txt");
                    break;
                } catch { Thread.Sleep(50); }
            }
            File.Delete("../../config/head.txt");
        }
        public void set_default(string nameProject) {
            timeout = 15000;
            debug = true;
            send_every = 6500;
            retest = 2;
            step = "Hardware";
            cmd = "AT+VERSION=HARDWARE,2";
            rx = "OK";
            min = 15;
            max = 35;
            port_name = "COM5";
            path_file = "../../config/" + nameProject + "_" + head;
            path_file_main = "../../config/test_head_" + head;
        }//หากต้องการดีบัค แบบไม่ต้องรัน main progarm ให้มาปรับค่าในนี้
        public void get_txt_main() {
            Support support = new Support();

            try { step = File.ReadAllText(path_file + support.step); } catch { }
            try { timeout = Convert.ToInt32(File.ReadAllText(path_file_main + support.timeout)); } catch { }
            try { debug = Convert.ToBoolean(File.ReadAllText(path_file_main + support.debug)); } catch { }
            try { cmd = File.ReadAllText(path_file + support.data_tx); } catch { }
            try { port_name = File.ReadAllText(path_file + support.comport); } catch { }
            try { retest = Convert.ToInt32(File.ReadAllText(path_file + support.retest)); } catch { }
            try { send_every = Convert.ToInt32(File.ReadAllText(path_file + support.send_every)); } catch { }
            try { rx = File.ReadAllText(path_file + support.data_rx); } catch { }
            try { min_str = File.ReadAllText(path_file + support.min); } catch { }
            try { max_str = File.ReadAllText(path_file + support.max); } catch { }
            try { min = Convert.ToDouble(min_str); } catch { min = 0; }
            try { max = Convert.ToDouble(max_str); } catch { max = 0; }

            check_step_test();

            File.WriteAllText("call_exe_tric.txt", "");
        }
        private void check_step_test() {
            switch (step) {
                case "OK": stepTest = StepTest.OK; break;
                case "Led": stepTest = StepTest.LED; break;
                case "Initial": stepTest = StepTest.INITIAL; break;
                case "Equal": stepTest = StepTest.EQUAL; break;
                case "Value": stepTest = StepTest.VALUE; break;
                case "Light": stepTest = StepTest.LIGHT; break;
                case "Temp": stepTest = StepTest.TEMP; break;
                case "Humidity": stepTest = StepTest.HUMIDITY; break;
                case "Battery": stepTest = StepTest.BATTERY; break;
                case "Hardware": stepTest = StepTest.HARDWARE; break;
                case "Digit": stepTest = StepTest.DIGIT; break;
                case "Frequency": stepTest = StepTest.FREQUENCY; break;
                case "Frequency2": stepTest = StepTest.FREQUENCY2; break;
                case "Switch": stepTest = StepTest.SWITCH; break;
            }
        }
    }
}
