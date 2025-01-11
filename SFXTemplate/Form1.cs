using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SFXTemplate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // 确保窗口完全显示
            await Task.Delay(100);

            // 初始化进度条
            progressBar1.Value = 0;

            await Task.Run(async () =>
            {
                try
                {
                    // 获取当前可执行文件路径
                    string exeFilePath = Application.ExecutablePath;

                    // 步骤 1：分离文件
                    Invoke((Action)(() => UpdateProgress("分离文件中...", 0, 100)));
                    SplitFiles(exeFilePath, "dictionary.bin", "encoded.bin");

                    // 步骤 2：加载字典
                    Invoke((Action)(() => UpdateProgress("加载字典中...", 25, 100)));
                    Dictionary<char, string> dictionary = LoadDictionaryFromBinary("dictionary.bin");

                    // 步骤 3：读取编码数据
                    byte[] encodedData = File.ReadAllBytes("encoded.bin");

                    // 步骤 4：解码文本并实时更新进度
                    DecodeText(dictionary, encodedData, Path.ChangeExtension(exeFilePath, ".txt"));

                    // 清除临时文件
                    File.Delete("dictionary.bin");
                    File.Delete("encoded.bin");

                    Text = "";

                    label1.Text = "操作成功";

                    await Task.Delay(3000);

                    Invoke((Action)(Close));
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() =>
                    {
                        MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            });
        }

        private void UpdateProgress(string message, int current, int total)
        {
            double progress = (double)current / total * 100;
            Invoke((Action)(() =>
            {
                progressBar1.Value = Math.Min(progressBar1.Maximum, current);
                Text = $"{message} ({progressBar1.Value * 100 / progressBar1.Maximum:F2}%)";
            }));
        }

        private static void SplitFiles(string exeFilePath, string outputFilePath1, string outputFilePath2)
        {
            using (FileStream exeStream = new FileStream(exeFilePath, FileMode.Open, FileAccess.Read))
            {
                exeStream.Seek(-16, SeekOrigin.End);
                using (BinaryReader reader = new BinaryReader(exeStream))
                {
                    long binaryFile1Size = reader.ReadInt64();
                    long binaryFile2Size = reader.ReadInt64();
                    long dataStartPosition = exeStream.Length - (binaryFile1Size + binaryFile2Size + 16);

                    exeStream.Seek(dataStartPosition, SeekOrigin.Begin);
                    byte[] binaryFile1 = new byte[binaryFile1Size];
                    exeStream.Read(binaryFile1, 0, binaryFile1.Length);

                    byte[] binaryFile2 = new byte[binaryFile2Size];
                    exeStream.Read(binaryFile2, 0, binaryFile2.Length);

                    File.WriteAllBytes(outputFilePath1, binaryFile1);
                    File.WriteAllBytes(outputFilePath2, binaryFile2);
                }
            }
        }

        static Dictionary<char, string> LoadDictionaryFromBinary(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                int dictionarySize = reader.ReadInt32();
                Dictionary<char, string> dictionary = new Dictionary<char, string>();

                for (int i = 0; i < dictionarySize; i++)
                {
                    char character = reader.ReadChar();
                    int codeLength = reader.ReadByte();
                    int byteCount = (codeLength + 7) / 8;
                    byte[] codeBytes = reader.ReadBytes(byteCount);

                    StringBuilder code = new StringBuilder();
                    for (int j = 0; j < codeLength; j++)
                    {
                        int byteIndex = j / 8;
                        int bitIndex = 7 - (j % 8);
                        code.Append((codeBytes[byteIndex] & (1 << bitIndex)) != 0 ? '1' : '0');
                    }
                    dictionary[character] = code.ToString();
                }

                return dictionary;
            }
        }

        private void DecodeText(Dictionary<char, string> dictionary, byte[] encodedData, string outputFilePath)
        {
            // 构建反向字典：哈夫曼编码 -> 对应字符
            Dictionary<string, char> reversedDictionary = new Dictionary<string, char>();
            foreach (KeyValuePair<char, string> kvp in dictionary)
            {
                reversedDictionary[kvp.Value] = kvp.Key;
            }

            // 准备解码变量
            StringBuilder tempCode = new StringBuilder();
            long totalBits = (long)encodedData.Length * 8; // 总位数，使用 long 防止溢出
            int progressUpdateInterval = Math.Max((int)(totalBits / 100), 1); // 每处理 1% 的数据更新一次进度条

            using (StreamWriter writer = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                long bitIndex = 0; // 当前处理的总位数
                foreach (byte b in encodedData)
                {
                    // 处理当前字节的每一位
                    for (int i = 7; i >= 0; i--)
                    {
                        // 提取当前位的值
                        int bit = (b >> i) & 1;
                        tempCode.Append(bit == 1 ? '1' : '0');

                        // 检查是否匹配字典
                        if (reversedDictionary.TryGetValue(tempCode.ToString(), out char decodedChar))
                        {
                            writer.Write(decodedChar); // 写入解码的字符
                            tempCode.Clear(); // 重置临时编码
                        }

                        bitIndex++;

                        // 更新进度条
                        if (bitIndex % progressUpdateInterval == 0 || bitIndex == totalBits)
                        {
                            // 使用 double 确保计算结果精确
                            double progress = (double)bitIndex / totalBits * 100;
                            Invoke((Action)(() =>
                            {
                                progressBar1.Value = (int)progress;
                                Text = $"解码中... ({progress:F2}%)";
                            }));
                        }
                    }
                }
            }
        }


    }
}
