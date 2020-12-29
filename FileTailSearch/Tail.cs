using System;
using System.IO;
using System.Text;
using System.Threading;

namespace FileTailSearch
{
    public class Tail
    {
        private long previousSeekPosition = 0;
        private int maxReadBytes = 1024 * 16;
        private long readBytesCount = 0;
        private Encoding encoding = Encoding.ASCII;
        private byte[] newLine = Encoding.ASCII.GetBytes("\n");
        private long readLineCount = 0;
        private AutoResetEvent signal = new AutoResetEvent(false);
        

        /// <summary>
        /// 读取文件中后几行数据
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="searchLineNum">要读取的最后行数</param>
        /// <returns></returns>
        public string ReadFirstTime(string path,int searchLineNum)
        {
            string data = string.Empty;
            byte[] readBytes = new byte[maxReadBytes];
            int numReadBytes = 0;

            this.previousSeekPosition = 0;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length > 0)
                {
                    CheckFileEncoding(fs);
                }

                this.previousSeekPosition = 0;
                if (fs.Length > maxReadBytes)
                {
                    this.previousSeekPosition = fs.Length - maxReadBytes;
                }

                this.previousSeekPosition = fs.Seek(this.previousSeekPosition, SeekOrigin.Begin);
                numReadBytes = fs.Read(readBytes, 0, (int)maxReadBytes);
                this.previousSeekPosition += numReadBytes;
            }

            if (numReadBytes > 0)
            {
                byte[] newLineBuffer = new byte[newLine.Length];
                byte[] reverseBytes = new byte[maxReadBytes];
                int reverseCount = 0;
                bool isNewLineEqual = true;

                for (int i = numReadBytes - 1; i >= 0; i--)
                {
                    Buffer.BlockCopy(readBytes, i, reverseBytes, reverseCount, 1);
                    reverseCount++;
                    readBytesCount++;

                    if (newLineBuffer.Length == 1)
                    {
                        Buffer.BlockCopy(readBytes, i, newLineBuffer, 0, newLineBuffer.Length);
                    }
                    else if (newLineBuffer.Length > 1 && numReadBytes - i >= newLineBuffer.Length)
                    {
                        Buffer.BlockCopy(readBytes, i, newLineBuffer, 0, newLineBuffer.Length);
                    }
                    isNewLineEqual = true;
                    for (int b = 0; b < newLine.Length; b++)
                    {
                        if (newLineBuffer[b] != newLine[b])
                        {
                            isNewLineEqual = false;
                            break;
                        }
                    }
                    if (isNewLineEqual)
                    {
                        readLineCount++;
                        if (readLineCount - 1 >= searchLineNum)
                        {
                            break;
                        }
                    }
                }

                if (reverseCount > 0)
                {
                    byte[] transferBuffer = new byte[reverseCount];
                    int transferIndex = 0;
                    for (int i = reverseCount - 1; i >= 0; i--)
                    {
                        Buffer.BlockCopy(reverseBytes, i, transferBuffer, transferIndex, 1);
                        transferIndex++;
                    }
                    data = encoding.GetString(transferBuffer, 0, reverseCount);
                }
            }
            return data;
        }


        /// <summary>
        /// 将信息打印到控制台上
        /// </summary>
        /// <param name="data"></param>
        private void OutputConsole(string data)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(data);
            Console.ResetColor();
        }

        /// <summary>
        /// 判断编码规则，并初始化编码规则
        /// </summary>
        /// <param name="fs"></param>
        private void CheckFileEncoding(FileStream fs)
        {
            byte[] checkEncodingBuffer = new byte[3];

            fs.Read(checkEncodingBuffer, 0, 3);

            if (checkEncodingBuffer[0] == 0xEF
              && checkEncodingBuffer[1] == 0xBB
              && checkEncodingBuffer[2] == 0xBF)
            {
                // UTF-8 : EF BB BF
                encoding = Encoding.UTF8;
                newLine = encoding.GetBytes(Environment.NewLine);
            }
            else if (checkEncodingBuffer[0] == 0xFF
              && checkEncodingBuffer[1] == 0xFE)
            {
                // Unicode Little Endian : FF FE
                encoding = Encoding.Unicode;
                newLine = encoding.GetBytes(Environment.NewLine);
            }
            else if (checkEncodingBuffer[0] == 0xFE
              && checkEncodingBuffer[1] == 0xFF)
            {
                // Unicode Big Endian : FE FF
                encoding = Encoding.BigEndianUnicode;
                newLine = encoding.GetBytes(Environment.NewLine);
            }
        }
    }
}
