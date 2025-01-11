using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SFXEncoder
{
    public class HuffmanCoding
    {
        private HuffmanNode? _root;
        private readonly Dictionary<char, string> _huffmanCode = new();

    // 添加：进度事件
    public event Action<int>? ProgressUpdated;

    // 构建哈夫曼树并报告进度
    public void Build(string text)
    {
        Dictionary<char, int> frequencyDict = new Dictionary<char, int>();
        foreach (char ch in text.Where(ch => !frequencyDict.TryAdd(ch, 1)))
        {
            frequencyDict[ch]++;
        }

        PriorityQueue<HuffmanNode?, int> priorityQueue = new PriorityQueue<HuffmanNode?, int>();
        foreach (var kvp in frequencyDict)
        {
            priorityQueue.Enqueue(new HuffmanNode
            {
                Character = kvp.Key,
                Frequency = kvp.Value
            }, kvp.Value);
        }

        while (priorityQueue.Count > 1)
        {
            HuffmanNode? left = priorityQueue.Dequeue();
            HuffmanNode? right = priorityQueue.Dequeue();
            if (left == null)
                continue;
            if (right == null)
                continue;
            HuffmanNode newNode = new HuffmanNode
            {
                Character = '\0', // 内部节点
                Frequency = left.Frequency + right.Frequency,
                Left = left,
                Right = right
            };
            priorityQueue.Enqueue(newNode, newNode.Frequency);

        }

        _root = priorityQueue.Dequeue();
        GenerateHuffmanCodes(_root, "");
    }

    // 对文本进行二进制编码并报告进度
    public byte[] EncodeToBinary(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("输入文本不能为空！");

        long totalChars = text.Length;
        long processedChars = 0;

        // 检查点
        long checkPoint = Math.Max(totalChars / 150, 1);

        // 使用位操作直接构造字节数组
        int bitLength = text.Sum(ch => _huffmanCode.TryGetValue(ch, out string? code) ? code.Length : throw new Exception($"字符 {ch} 不在哈夫曼编码表中！"));
        int byteLength = (bitLength + 7) / 8; // 计算所需字节数（向上取整）
        byte[] result = new byte[byteLength];

        int bitPosition = 0; // 当前已写入的总位数
        foreach (char ch in text)
        {
            if (!_huffmanCode.TryGetValue(ch, out string binaryCode))
                throw new Exception($"字符 {ch} 不在哈夫曼编码表中！");

            foreach (char bit in binaryCode)
            {
                if (bit == '1')
                    result[bitPosition / 8] |= (byte)(1 << (7 - (bitPosition % 8))); // 设置对应位
                bitPosition++;
            }

            processedChars++;
            if (processedChars % checkPoint == 0)
            {
                ProgressUpdated?.Invoke((int)(processedChars * 100 / totalChars)); // 更新进度
            }

        }

        ProgressUpdated?.Invoke(100); // 确保编码完成

        return result;
    }


        // 生成哈夫曼编码表
        private void GenerateHuffmanCodes(HuffmanNode? node, string code)
        {
            if (node == null)
                return;

            if (node.Left == null && node.Right == null)
            {
                _huffmanCode[node.Character] = code;
                return;
            }

            GenerateHuffmanCodes(node.Left, code + "0");
            GenerateHuffmanCodes(node.Right, code + "1");
        }

        // 获取编码字典
        public Dictionary<char, string> GetEncodingDictionary()
        {
            return _huffmanCode;
        }

        // 保存字典为二进制文件
        public void SaveDictionaryAsBinary(string filePath)
        {
            using BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            writer.Write(_huffmanCode.Count); // 写入字典大小

            foreach ((char key, string? code) in _huffmanCode)
            {
                writer.Write(key); // 写入字符
                writer.Write((byte)code.Length); // 写入编码长度

                // 写入编码数据为字节形式
                int byteCount = (code.Length + 7) / 8;
                byte[] codeBytes = new byte[byteCount];
                for (int i = 0; i < code.Length; i++)
                {
                    if (code[i] == '1')
                    {
                        codeBytes[i / 8] |= (byte)(1 << (7 - (i % 8)));
                    }
                }
                writer.Write(codeBytes); // 写入字节数组
            }
        }

        // 从二进制文件加载字典
        public static Dictionary<char, string> LoadDictionaryFromBinary(string filePath)
        {
            using BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
            int dictionarySize = reader.ReadInt32(); // 读取字典大小
            Dictionary<char, string> dictionary = new Dictionary<char, string>();

            for (int i = 0; i < dictionarySize; i++)
            {
                char character = reader.ReadChar(); // 读取字符
                int codeLength = reader.ReadByte(); // 读取编码长度

                // 读取编码数据并还原为字符串
                int byteCount = (codeLength + 7) / 8;
                byte[] codeBytes = reader.ReadBytes(byteCount);
                StringBuilder code = new StringBuilder();
                for (int j = 0; j < codeLength; j++)
                {
                    int byteIndex = j / 8;
                    int bitIndex = 7 - (j % 8);
                    bool isBitSet = (codeBytes[byteIndex] & (1 << bitIndex)) != 0;
                    code.Append(isBitSet ? '1' : '0');
                }
                dictionary[character] = code.ToString();
            }

            return dictionary;
        }
    }

// 哈夫曼树节点
    public class HuffmanNode
    {
        public char Character { get; init; } // 字符
        public int Frequency { get; init; } // 频率
        public HuffmanNode? Left { get; init; }
        public HuffmanNode? Right { get; init; }
    }
}
