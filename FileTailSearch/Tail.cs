using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FileTailSearch
{
    public class Tail
    {
        private long previousSeekPosition = 0;
        private int maxReadBytes = 1024 * 1024;//读取数据时读取1M数据到内存进行分析
        private long readBytesCount = 0;
        private Encoding encoding = Encoding.ASCII;
        private byte[] newLine = Encoding.ASCII.GetBytes("\n");
        private long readLineCount = 0;
        private AutoResetEvent signal = new AutoResetEvent(false);

        private string _valuePattern;
        private bool isFit = false;
        private string collectTime;
        private int _collectInterval;



        public Tail()
        {

        }

        /// <summary>
        /// 初始化采集周期和匹配内容
        /// </summary>
        /// <param name="valuePattern">日志匹配项</param>
        /// <param name="interval">采集周期</param>
        public Tail(string valuePattern,int interval)
        {
            _valuePattern = valuePattern;
            _collectInterval = interval;
        }

        #region 对外调用的方法

        /// <summary>
        /// 获取日志采集的值
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchLineNum"></param>
        /// <returns></returns>
        public string GetLogCollectValue(string path )
        {
            var data = FitLogContentFromLast(path);
            //var value = SearchPatternContent(data);
            if (!string.IsNullOrEmpty(data))
            {
                SetIsFit(true);
                return data;
            }
            return "没有找到匹配结果";
        }

        /// <summary>
        /// 获取规定日志的行数
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchLineNum"></param>
        /// <returns></returns>
        public string GetLogLines(string path, int searchLineNum)
        {
            return ReadFirstTime(path, searchLineNum);
        }
        #endregion

        /// <summary>
        /// 读取文件中后几行数据
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="searchLineNum">要读取的最后行数</param>
        /// <returns></returns>
        private string ReadFirstTime(string path,int searchLineNum =1000)
        {
            string data = string.Empty;
            //定义要缓存的文件内容
            byte[] readBytes = new byte[maxReadBytes];
            int numReadBytes = 0;
            //初始化要从后向前偏移的位置
            this.previousSeekPosition = 0;
            //将文件数据缓存到readBytes中
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length > 0)
                {
                    CheckFileEncoding(fs);
                }

                //this.previousSeekPosition = 0;
                if (fs.Length > maxReadBytes)
                {
                    this.previousSeekPosition = fs.Length - maxReadBytes;
                }

                this.previousSeekPosition = fs.Seek(this.previousSeekPosition, SeekOrigin.Begin);
                numReadBytes = fs.Read(readBytes, 0, (int)maxReadBytes);
                this.previousSeekPosition += numReadBytes;
            }
            //从后往前读缓存中的数据，并根据设置的行数返回固定行数的数据，
            //此时是从后向前读，字节数组中存储的是颠倒的数据。如：你好吗 -->吗好你。
            if (numReadBytes > 0)
            {
                byte[] newLineBuffer = new byte[newLine.Length];
                byte[] reverseBytes = new byte[maxReadBytes];//存储反转的数据，用于存所设置行数的数据
                int reverseCount = 0;
                byte[] newLineBytes = new byte[1024];//定义存储一行日志的buffer缓冲区
                int newLineCount = 0;
                bool isNewLineEqual = true;

                for (int i = numReadBytes - 1; i >= 0; i--)
                {
                    Buffer.BlockCopy(readBytes, i, reverseBytes, reverseCount, 1);
                    reverseCount++;
                    readBytesCount++;

                    Buffer.BlockCopy(readBytes, i, newLineBytes, newLineCount, 1);
                    newLineCount++;

                    #region 记录回车换行符，并判断是否为新的一行数据
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
                        //将这行数据反转
                        var newLineStr = encoding.GetString(newLineBytes.Reverse().ToArray(), 0, newLineCount);
                        newLineCount = 0;
                        //查找匹配结果
                        
                        readLineCount++;
                        if (readLineCount - 1 >= searchLineNum)//主要考虑到最后一行的换行符
                        {
                            break;
                        }
                    }
                    #endregion
                }
                //将反转数据转成正向存储,如：吗好-->好吗
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
        /// 读取文件中后几行数据
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="searchLineNum">要读取的最后行数</param>
        /// <returns></returns>
        private string FitLogContentFromLast(string path)
        {
            string data = string.Empty;
            //定义要缓存的文件内容
            byte[] readBytes = new byte[maxReadBytes];
            int numReadBytes = 0;
            //初始化要从后向前偏移的位置
            this.previousSeekPosition = 0;
            //将文件数据缓存到readBytes中
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length > 0)
                {
                    CheckFileEncoding(fs);
                }
                //this.previousSeekPosition = 0;
                if (fs.Length > maxReadBytes)
                {
                    this.previousSeekPosition = fs.Length - maxReadBytes;
                }
                this.previousSeekPosition = fs.Seek(this.previousSeekPosition, SeekOrigin.Begin);
                numReadBytes = fs.Read(readBytes, 0, (int)maxReadBytes);
            }
            //从后往前读缓存中的数据，并根据设置的行数返回固定行数的数据，
            //此时是从后向前读，字节数组中存储的是颠倒的数据。如：你好吗 -->吗好你。
            if (numReadBytes > 0)
            {
                byte[] newLineBuffer = new byte[newLine.Length];
                byte[] reverseBytes = new byte[maxReadBytes];//存储反转的数据，用于存所设置行数的数据
                int reverseCount = 0;
                byte[] newLineBytes = new byte[1024];//定义存储一行日志的buffer缓冲区
                int newLineCount = 0; //每一行的字节数量
                bool isNewLineEqual; //是否是新的一行
                for (int i = numReadBytes - 1; i >= 0; i--)
                {
                    Buffer.BlockCopy(readBytes, i, reverseBytes, reverseCount, 1);
                    reverseCount++;
                    readBytesCount++;

                    Buffer.BlockCopy(readBytes, i, newLineBytes, newLineCount, 1);
                    newLineCount++;
                    #region 记录回车换行符，并判断是否为新的一行数据
                    if (newLineBuffer.Length == 1)
                    {
                        Buffer.BlockCopy(readBytes, i, newLineBuffer, 0, newLineBuffer.Length);
                    }
                    else if (newLineBuffer.Length > 1 && numReadBytes - i >= newLineBuffer.Length)
                    {
                        Buffer.BlockCopy(readBytes, i, newLineBuffer, 0, newLineBuffer.Length);
                    }

                    isNewLineEqual = true;
                    //判断是否是新的一行
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
                        //将这行数据反转
                        var newLineStr = new string(encoding.GetString(newLineBytes, 0, newLineCount).Trim().Reverse<char>().ToArray<char>());
                        newLineCount = 0;
                        if (string.IsNullOrEmpty(newLineStr)) continue;
                        //查找匹配结果
                        data = FitMetric(_valuePattern, newLineStr, _collectInterval);
                        if (!string.IsNullOrEmpty(data)) 
                            return data;
                    }
                    #endregion
                }
            }
            return data;
        }

        /// <summary>
        /// 设置采集时间
        /// </summary>
        /// <param name="timeFormart"></param>
        private void SetCollectTime(string timeFormart)
        {
            collectTime = DateTime.Now.ToString(timeFormart);
        }

        /// <summary>
        /// 设置是否匹配成功
        /// </summary>
        /// <param name="isSuccess"></param>
        private void SetIsFit(bool isSuccess)
        {
            isFit = isSuccess;
            if (isFit)
            {
                //调用上报功能
                Console.WriteLine("上报指标数据");
            }
        }

        /// <summary>
        /// 从缓存文件中获取匹配内容
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string SearchPatternContent(string data)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));
            var newLineStr = encoding.GetString(newLine);
            var lines = data.Split(newLineStr);
            for (int i = lines.Length-1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var value = FitMetric(_valuePattern, lines[i], 5);
                if (!string.IsNullOrEmpty(value))
                {
                    Console.WriteLine($"倒数第{i+1}行数据,获得了指标的值");
                    return value;
                }
            }
            return string.Empty;

        }

        private string FitMetric(string regularContent ,string logContent, int interval)
        {
            if (string.IsNullOrEmpty(regularContent))
            {
                throw new ArgumentNullException(nameof(regularContent));
            }
            if (string.IsNullOrEmpty(logContent))
            {
                throw new ArgumentNullException(nameof(logContent));
            }

            //1.获取当前行日志的时间戳,并判断当前时间戳与日志的时间戳差值是否大于2倍的指标日志输出周期(暂时不考虑)，并且判断差值是否大于采集周期，
            //如果条件成立则退出采集，采集结果：未能采集到数据，不用向平台发送结果。
            var (_, timePattern) = GetTimeFormart("");
            var timeNow = DateTime.Now;
            var timeValue = GetValueByRegular(logContent, timePattern);
            if (string.IsNullOrEmpty(timeValue))
            {
                return string.Empty;
            }
            var logTime = DateTime.Parse(timeValue);
            if ((timeNow - logTime).TotalSeconds > interval)
            {
                return string.Empty;
            }
            //2.开始匹配日志数据如果数据匹配成功，调用数据发送功能，如果匹配不成功则放弃此条日志。
            return GetValueByRegular(logContent, regularContent,false);
        }

        /// <summary>
        /// 获取正则匹配结果
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string GetValueByRegular(string input,string pattern,bool isAllValue = true)
        {
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                if (isAllValue)
                {
                    return match.Value;
                }
                else
                {
                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }
            else
            {
                return string.Empty;
            }
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

        /// <summary>
        /// 获取时间匹配格式
        /// </summary>
        /// <param name="tf">时间格式如：yyyy-mm-dd HH:MM:SS</param>
        /// <returns>(时间格式，时间正则表达式)</returns>
        private (string,string) GetTimeFormart(string tf)
        {
            var timeFormart = string.Empty;
            var timePattern = string.Empty;
            switch (tf)
            {
                case "yyyy-mm-dd HH:MM:SS":
                    timePattern = @"(2[0-9]{3})-(0[1-9]|1[0-2])-([0-2][0-9]|3[01])\s([0-1][0-9]|2[0-3])(:[0-5][0-9]){2}";
                    timeFormart = "2020-01-01 12:59:59";
                    break;
                default:
                    timePattern = @"(2[0-9]{3})-(0[1-9]|1[0-2])-([0-2][0-9]|3[01])\s([0-1][0-9]|2[0-3])(:[0-5][0-9]){2}";
                    timeFormart = "2020-01-01 12:59:59";
                    break;
            }
            return (timeFormart, timePattern);
        } 
    }
}
