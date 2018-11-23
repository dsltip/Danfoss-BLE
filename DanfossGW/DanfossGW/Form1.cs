using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DanfossGW
{
    public partial class Form1 : Form
    {
        byte[] rxbuf = new byte[255];
        byte[] txbuf = new byte[255];
        String[] rxlines = new String[5];
        String curline;
        Boolean DoWrite;
        int rxlen;
        int stage;

        private static float sp_temp;
        private static float room_temp;
        private const int DELTA = -1640531527;
        private const byte BIG_ENDIAN = 1;
        private const byte LITTLE_ENDIAN = 0;
        private const int INT_BYTESIZE = 4;
        private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();
        //private static readonly sbyte[] secret = new sbyte[] { -25, 123, -6, 106, 102, 76, 106, 127, 121, -114, 16, 123, 40, 13, 1, -42 };
        private static sbyte[] secret = new sbyte[16];// { -25, 123, -6, 106, 102, 76, 106, 127, 121, -114, 16, 123, 40, 13, 1, -42 };

        public Form1()
        {
            InitializeComponent();
        }
        private static bool parseManualTemperature(sbyte[] value)
        {
            if (value.Length < 2)
            {
                return false;
            }
            sp_temp = ((float)value[0]) / 2.0f;
            room_temp = ((float)value[1]) / 2.0f;
            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                    button1.Text = "Open";
                }
                else
                {
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.Open();
                    textBox3.Clear();
                    button1.Text = "Close";
                }
            }
            catch (SystemException error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }
            comboBox1.Text = Properties.Settings.Default.port;
            secret = StringToByteArray(textBox4.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.port = comboBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            txbuf[0] = 1;
            serialPort1.Write(txbuf, 0, 1);
        }
        private void ProcessRx(object sender, EventArgs e)
        {
            int i;
            String rxtext = "";
            for (i = 0; i < rxlen; i++)
            {
                if ((char)rxbuf[i] == '\n')
                {
                    rxtext = rxtext + '\r';
                    rxlines[4] = rxlines[3];
                    rxlines[3] = rxlines[2];
                    rxlines[2] = rxlines[1];
                    rxlines[1] = rxlines[0];
                    rxlines[0] = curline;
                    if (rxlines[1] == "Read val"){
                        if ((txbuf[0] == 5) && (curline.Length == 16))
                        {
                            parseManualTemperature(decrypt(StringToByteArray(curline), secret));
                            label3.Text = "SP temp = " + sp_temp.ToString()+ " C";
                            label4.Text = "Cur.temp = " + room_temp.ToString() + " C";
                        };
                        if ((txbuf[0] == 4) && (curline.Length == 2))
                        {
                            label2.Text = "Bat = " + StringToByteArray(curline)[0].ToString() + "%";
                            
                        };
                    };
                    curline = "";

                }
                else curline = curline + (char)rxbuf[i];
                rxtext = rxtext + (char)rxbuf[i];
            };
            if(checkBox1.Checked) textBox3.AppendText(rxtext);
            rxlen = 0;
            
        }
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int rxlennew = serialPort1.BytesToRead;
            serialPort1.Read(rxbuf, rxlen, rxlennew);
            rxlen += rxlennew;
            this.Invoke(new EventHandler(ProcessRx));
        }
        private void button10_Click_1(object sender, EventArgs e)
        {
            txbuf[0] = 0; serialPort1.Write(txbuf, 0, 1);
        }

        private void button3_Click(object sender, EventArgs e)//connect
        {
            sbyte[] evalue = new sbyte[6];
            evalue = StringToByteArray(textBox2.Text);
            txbuf[0] = 2;
            txbuf[1] = (byte)evalue[5];
            txbuf[2] = (byte)evalue[4];
            txbuf[3] = (byte)evalue[3];
            txbuf[4] = (byte)evalue[2];
            txbuf[5] = (byte)evalue[1];
            txbuf[6] = (byte)evalue[0];
            for (int i = 0; i < 7; i++) { serialPort1.Write(txbuf, i, 1); Task.Delay(50); };

        }

        private void button4_Click(object sender, EventArgs e)
        {
            txbuf[0] = 3; serialPort1.Write(txbuf, 0, 1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txbuf[0] = 4; serialPort1.Write(txbuf, 0, 1);
            label2.Text = "Bat = *";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            txbuf[0] = 5; serialPort1.Write(txbuf, 0, 1);
            label3.Text = "SP = *";
            label4.Text = "Cur = *";
        }

        private void button7_Click(object sender, EventArgs e)//write setpoint
        {
            sbyte[] tval = new sbyte[2];
            sbyte[] evalue = new sbyte[8];
            tval[0] = convertTemperature(float.Parse(textBox1.Text));
            evalue = encrypt(tval);

            txbuf[0] = 6;//e9 33 95 3b 0c 4e 1a c7 - 25C
            txbuf[1] = (byte)evalue[0];
            txbuf[2] = (byte)evalue[1];
            txbuf[3] = (byte)evalue[2];
            txbuf[4] = (byte)evalue[3];
            txbuf[5] = (byte)evalue[4];
            txbuf[6] = (byte)evalue[5];
            txbuf[7] = (byte)evalue[6];
            txbuf[8] = (byte)evalue[7];

            for (int i = 0; i < 9; i++) { serialPort1.Write(txbuf, i, 1); Task.Delay(50); };
        }

        private void button8_Click(object sender, EventArgs e)
        {
            txbuf[0] = 6; serialPort1.Write(txbuf, 0, 1);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            txbuf[0] = 255; serialPort1.Write(txbuf, 0, 1);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            txbuf[0] = 49; serialPort1.Write(txbuf, 0, 1);
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            sbyte[] tval = new sbyte[2];
            sbyte[] evalue = new sbyte[8];
            tval[0] = convertTemperature(float.Parse(textBox1.Text));
            evalue = encrypt(tval);
            foreach (sbyte i in evalue)
            {
                textBox3.AppendText(i.ToString() + ',');
            }
            //textBox3.AppendText();
            /*int[] ii = new int[2];
            sbyte[] bb = {-25, 123, -6, 106, 102, 76, 106, 127, 121, -114, 16, 123, 40, 13, 1, -42};
            sbyte[] bb1 = {-11, -63, -91, -50, -70, 65, -34, 26};
            foreach (int i in bytesToInts(bb, LITTLE_ENDIAN))
            {
                textBox3.AppendText(i.ToString() + ',');
            }
            foreach (int i in bytesToInts(bb1, BIG_ENDIAN))
            {
                textBox3.AppendText(i.ToString() + ',');
            }

            ii[0] = 942866432;
            ii[1] = 0;
            foreach (sbyte i in intsToBytes(ii, BIG_ENDIAN))
            {
                textBox3.AppendText(i.ToString()+',');
            }*/


        }
        private static sbyte convertTemperature(float v)
        {
            return (sbyte)((int)Math.Min(Math.Max(2.0f * v, 0.0f), 255.0f));
        }
        private static sbyte[] encrypt(sbyte[] value)
        {
            int byteArraySize = value.Length + (value.Length % 4 == 0 ? 0 : 4 - (value.Length % 4));
            if (byteArraySize < 8)
            {
                byteArraySize = 8;
            }
            sbyte[] padded = new sbyte[byteArraySize];
            Array.Copy(value, 0, padded, 0, value.Length);
            return xencrypt(padded, secret);
        }
        private static sbyte[] xencrypt(sbyte[] data, sbyte[] key)
        {
            return intsToBytes(xxencrypt(bytesToInts(data, BIG_ENDIAN), bytesToInts(key, LITTLE_ENDIAN)), BIG_ENDIAN);
        }

        public static sbyte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => (sbyte)Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        private static sbyte[] intsToBytes(int[] ints, byte order)
        {
            sbyte[] array = new sbyte[ints.Length * INT_BYTESIZE];
            int k = 0;
            foreach (int i in ints)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                if (order == LITTLE_ENDIAN) for (int j = 0; j < 4; j++) { array[k] = (sbyte)bytes[j]; k++; }
                if (order == BIG_ENDIAN) for (int j = 0; j < 4; j++) { array[k] = (sbyte)bytes[3 - j]; k++; }
            }
            return array;
        }

        private static int[] bytesToInts(sbyte[] bytes, byte order)
        {

            if (bytes.Length % INT_BYTESIZE != 0)
            {
                throw new System.ArgumentException("Length of byte-array must be divisible by 4!");
            }
            int count = bytes.Length / INT_BYTESIZE;
            int[] ints = new int[count];
            byte[] array = new byte[INT_BYTESIZE];
            int k=0,j;
            
            for (int i = 0; i < count; i++)
            {
                if (order == LITTLE_ENDIAN) for (j = 0; j < 4; j++) { array[j] = (byte)bytes[k]; k++; };
                if (order == BIG_ENDIAN) for (j = 0; j < 4; j++) { array[3 - j] = (byte)bytes[k]; k++; };
                ints[i] = BitConverter.ToInt32(array, 0);
            }
            return ints;
        }

        private static sbyte[] decrypt(sbyte[] data, sbyte[] key)
        {
            return intsToBytes(xxdecrypt(bytesToInts(data, BIG_ENDIAN), bytesToInts(key, LITTLE_ENDIAN)), BIG_ENDIAN);

        }
        private static int MX(int sum, int y, int z, int p, int e, int[] k)
        {
            return ((((int)((uint)z >> 5)) ^ (y << 2)) + (((int)((uint)y >> 3)) ^ (z << 4))) ^ ((sum ^ y) + (k[(p & 3) ^ e] ^ z));
        }

        public static int[] xxdecrypt(int[] vi, int[] k)
        {
            int[] v = (int[])vi.Clone();
            int n = v.Length - 1;
            if (n >= 1)
            {
                int q = (52 / (n + 1)) + 6;
                int y = v[0];
                for (int sum = q * DELTA; sum != 0; sum -= DELTA)
                {
                    int e = ((int)((uint)sum >> 2)) & 3;
                    int p = n;
                    while (p > 0)
                    {
                        y = v[p] - MX(sum, y, v[p - 1], p, e, k);
                        v[p] = y;
                        p--;
                    }
                    y = v[0] - MX(sum, y, v[n], p, e, k);
                    v[0] = y;
                }
            }
            return v;
        }
        public static int[] xxencrypt(int[] vi, int[] k)
        {
            int[] v = (int[])vi.Clone();
            int n = v.Length - 1;
            if (n >= 1)
            {
                int q = (52 / (n + 1)) + 6;
                int z = v[n];
                int sum = 0;
                int q2 = q;
                while (true)
                {
                    q = q2 - 1;
                    if (q2 <= 0)
                    {
                        break;
                    }
                    sum += DELTA;
                    int e = ((int)((uint)sum >> 2)) & 3;
                    int p = 0;
                    while (p < n)
                    {
                        z = v[p] + MX(sum, v[p + 1], z, p, e, k);
                        v[p] = z;
                        p++;
                    }
                    z = v[n] + MX(sum, v[0], z, p, e, k);
                    v[n] = z;
                    q2 = q;
                }
            }
            return v;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            stage = 1;
        }
        private void button8_Click_2(object sender, EventArgs e)
        {
            stage = 1;
            DoWrite = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(stage>0)
                switch(stage){
                    case 1: button3.PerformClick(); stage++;  break;
                    case 2: if (rxlines[0] == "Connected!") { button4.PerformClick(); stage++; }; break;
                    case 3: if (rxlines[0] == "Write OK") { button5.PerformClick(); stage++; }; break;
                    case 4: if (rxlines[1] == "Read val") { button6.PerformClick(); stage++; }; break;
                    case 5: if (rxlines[1] == "Read val") if (DoWrite) { button7.PerformClick(); stage++; } else stage = 7; break;
                    case 6: if (rxlines[0] == "Write OK") stage = 7; break;
                    case 7: button3.PerformClick(); stage = 0; DoWrite = false; break;
                }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (textBox4.Text.Length == 32)  secret = StringToByteArray(textBox4.Text);
        }

        private void button13_Click(object sender, EventArgs e)
        {//2F-6B-47-55-38-C2-7A-17
             sbyte[] temp = new sbyte[] { 47,107,71,85,56,-62,122,23 };
             sbyte[] evalue = new sbyte[16];
             evalue = decrypt(temp, secret);
             for (int i = 0; i < 8; i++) { textBox3.AppendText(evalue[i].ToString() + " "); };
            /*sbyte[] tval = new sbyte[2];
            sbyte[] evalue = new sbyte[8];
            tval[0] = convertTemperature(float.Parse(textBox1.Text));
            evalue = encrypt(tval);

            txbuf[0] = 6;//e9 33 95 3b 0c 4e 1a c7 - 25C
            txbuf[1] = (byte)evalue[0];
            txbuf[2] = (byte)evalue[1];
            txbuf[3] = (byte)evalue[2];
            txbuf[4] = (byte)evalue[3];
            txbuf[5] = (byte)evalue[4];
            txbuf[6] = (byte)evalue[5];
            txbuf[7] = (byte)evalue[6];
            txbuf[8] = (byte)evalue[7];

            for (int i = 0; i < 9; i++) { textBox3.AppendText(txbuf[i].ToString() + " "); };*/

            //txbuf[0] = 7; serialPort1.Write(txbuf, 0, 1);
            //tval[0] = convertTemperature(float.Parse(textBox1.Text));
            //textBox3.AppendText(convertTemperature(float.Parse(textBox1.Text)).ToString());
            /*sbyte[] tval = new sbyte[2];
            sbyte[] evalue = new sbyte[8];
            tval[0] = convertTemperature(float.Parse(textBox1.Text));
            int byteArraySize = tval.Length + (tval.Length % 4 == 0 ? 0 : 4 - (tval.Length % 4));
            textBox3.AppendText(tval.Length.ToString());
            textBox3.AppendText(byteArraySize.ToString());

            evalue = encrypt(tval);*/

            /*int[] ui = new int[2];
            ui[0] = 235442;
            ui[1] = 345345;
            sbyte[] ub = new sbyte[8];
            ub = intsToBytes(ui, BIG_ENDIAN);
            for (int i = 0; i < 8; i++) textBox3.AppendText(ub[i].ToString()+" ");*/
            /*sbyte[] ub = new sbyte[8];
            ub[0] = 1;
            ub[1] = 2;
            ub[2] = 3;
            ub[3] = 4;
            ub[4] = 5;
            ub[5] = 6;
            ub[6] = 7;
            ub[7] = 8;
            int[] ui = new int[2];
            ui = bytesToInts(ub, BIG_ENDIAN);
            for (int i = 0; i < 2; i++) textBox3.AppendText(ui[i].ToString()+" ");*/
            
            /*int[] ui = new int[2];
            ui[0] = 235442;
            ui[1] = 345345;
            int[] rui = new int[10];
            rui = xxencrypt(ui, bytesToInts(secret, LITTLE_ENDIAN));
            for (int i = 0; i < 2; i++) textBox3.AppendText(rui[i].ToString() + " ");*/
            //textBox3.AppendText(ui[i].ToString() + " ");

        }

        private void button12_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
        }




    }
}
