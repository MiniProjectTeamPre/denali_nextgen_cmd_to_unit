using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace denali_nextgen_cmd_to_unit {
    class Program {
        private static string name_project = "denali_nextgen_cmd_to_unit";
        private static System.Threading.Timer close_program;
        private static ReadTxtFile read_txt_file = new ReadTxtFile();
        private static SerialPort_ serial_port = new SerialPort_();
        static void Main(string[] args) {
            //รออ่าน txt file ว่าเทสหัวไหน
            read_txt_file.wait_get_head();

            //set ตัวแปรเป็นค่าเริ่มต้น
            read_txt_file.set_default(name_project);

            //อ่าน txt file จากโปรแกรม main ที่ส่งมา
            read_txt_file.get_txt_main();

            //กำหนดเวลาปิดโปรแกรมอัตโนมัติ ป้องกันกรณีโปรแกรมขัดข้อง
            close_program = new System.Threading.Timer(TimerCallback, null, 0, read_txt_file.timeout);

            //print show step=?
            consoleWrite("step = " + read_txt_file.step);

            //setup serial port
            serial_port.setup(read_txt_file.port_name, read_txt_file.stepTest, 
                              read_txt_file.cmd, read_txt_file.send_every,
                              read_txt_file.retest, read_txt_file.head,
                              read_txt_file.rx, read_txt_file.min, read_txt_file.max);

            //print show port name
            consoleWrite("port name = " + serial_port.mySerialPort.PortName);

            //open serial port. if the serial port can not be opened, close the progarm. 
            if (!serial_port.open()) return;

            //print show baud rate
            consoleWrite("Baud Rate = " + serial_port.mySerialPort.BaudRate, true);

            //บางครั้งมันมีค่าที่ค้างอยู่ในบัฟเฟอร์ของ rs232 ใช้คำสั่งเคลียไม่หาย ต้องวนอ่านมันจนหมด
            serial_port.initial();

            //ถ้าเป็น step led โปรแกรมมันจะต้องส่ง cmd 1 ครั้ง แล้วไปอ่านกล้อง
            //ดังนั้นพอส่ง cmd เสร็จ โปรแกรมจะปิดตัวเองทิ้งไป
            //กรณี set mode debug ไว้ โปรแกรมจะไม่ปิดตัวเอง
            //และโปรแกรมจะรอรับคำสั่ง "end" เพื่อปิดตัวเอง
            //แต่หากเราพิมพ์อะไรที่ไม่ใช่ "end" โปรแกรมจะส่ง cmd 1 ครั้ง ให้ LED on
            if (read_txt_file.stepTest == StepTest.LED) {
                serial_port.test_led(read_txt_file.debug);
                Application.Exit();
                return;
            }

            //สำหรับวนกลับมาเทสซ้ำ ถ้ามันเฟวยังไม่เกินจำนวนที่กำหนด
            Retest:

            //ส่งคำสั่งไปให้ unit ลูกค้า ถ้าส่งแล้วไม่มีอะไรตอบกลับมา จะจบการทำงานทันที
            if (!serial_port.send()) {
                Application.Exit();
                return;
            }

            //อ่านค่าที่ตอบกลับมา โดยจะแปลงค่านั้นจาก ascii หรือ hex ออกมาเป็น string
            serial_port.read(Received.ASCII);

            //นำค่าที่อ่านได้มาตรวจสอบ ว่า unit ตอบกลับมาถูกต้องหรือไม่
            bool result = serial_port.check_result();

            //สำหรับต้องการดีบัค ดูค่าที่ unit ตอบกลับ
            if (read_txt_file.debug) Console.ReadLine();

            //ถ้าผลการตรวจสอบไม่ผ่าน จะมีการวนกลับไปเทสซ้ำ
            if (!result) {
                //วนกลับไปเทสซ้ำ
                if (serial_port.re < serial_port.retest) {
                    Console.WriteLine();
                    goto Retest;
                }
            }

            //เขียนผลเทสใส่ txt file เพื่อส่งให้ main progarm ต่อไป
            serial_port.write(result);
        }

        private static void consoleWrite(string str, bool newLine = false) {
            Console.WriteLine(str);
            if(newLine) Console.WriteLine();
        }
        private static bool flag_close = false;
        private static void TimerCallback(Object o) {
            if (!flag_close) {
                flag_close = true;
                return;
            }
            if (read_txt_file.debug || serial_port.flag_wait_discom) return;
            File.WriteAllText("test_head_" + read_txt_file.head + "_result.txt", "timeout main\r\nFAIL");
            Environment.Exit(0);
        }
    }
}
