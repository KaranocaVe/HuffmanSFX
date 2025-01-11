using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI;
using Microsoft.UI.Windowing;

// 要了解更多关于 WinUI、WinUI 项目结构和项目模板的信息，请访问：http://aka.ms/winui-project-info。

namespace SFXEncoder
{
    /// <summary>
    /// 一个空窗口，可以单独使用或在 Frame 中导航。
    /// </summary>
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent(); // 初始化组件
            ExtendsContentIntoTitleBar = true; // 将内容扩展到标题栏
            SetTitleBar(CustomTitleBar); // 设置自定义标题栏
            SystemBackdrop = new MicaBackdrop(); // 设置系统背景为 Mica 效果
            SetAdaptiveWindowSize();
        }

        private void SetAdaptiveWindowSize()
        {
            // 获取当前窗口的 DPI 缩放比例
            double dpiScaling = GetDpiScalingFactor();

            // 目标窗口大小（以缩放前的逻辑像素为基准）
            int targetWidth = (int)(700 * dpiScaling);
            int targetHeight = (int)(360 * dpiScaling);

            // 获取当前窗口的 AppWindow 对象
            AppWindow appWindow = GetAppWindowForCurrentWindow();
            appWindow.Resize(new Windows.Graphics.SizeInt32(targetWidth, targetHeight));
        }

        private double GetDpiScalingFactor()
        {
            // 获取当前窗口句柄
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            // 调用 Win32 API 获取 DPI
            uint dpi = GetDpiForWindow(hwnd);

            // 将 DPI 转换为缩放比例（96 为默认 DPI）
            return dpi / 96.0;
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);


        private bool _isTxtOk; // 标记是否已选择文本文件
        private bool _isOutputPathOk; // 标记是否已选择输出路径

        private StorageFolder? _outputFolder; // 输出文件夹
        private StorageFile? _txtFile; // 文本文件

        /// <summary>
        /// 选择一个文件。
        /// </summary>
        private async void SelectFile(object sender, RoutedEventArgs e)
        {
            try
            {
                FilePath.Text = ""; // 清空之前返回的文件路径

                // 创建文件选择器
                FileOpenPicker openPicker = new Windows.Storage.Pickers.FileOpenPicker();

                // 获取当前 WinUI 3 窗口的窗口句柄 (HWND)
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);

                // 使用窗口句柄初始化文件选择器
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                // 设置文件选择器的选项
                openPicker.ViewMode = PickerViewMode.Thumbnail; // 缩略图视图
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary; // 默认位置为图片库
                openPicker.FileTypeFilter.Add(".txt"); // 仅允许选择 .txt 文件

                // 打开文件选择器让用户选择文件
                _txtFile = await openPicker.PickSingleFileAsync();
                if (_txtFile != null)
                {
                    FilePath.Text = _txtFile.Path; // 显示选中文件的路径
                    _isTxtOk = true;
                    // 如果文本文件和输出路径都选择了，启用编码按钮
                    if (_isTxtOk && _isOutputPathOk)
                    {
                        LaunchEncodeButton.IsEnabled = true;
                    }
                }
                else
                {
                    FilePath.Text = "操作已取消"; // 用户取消操作
                    _isTxtOk = false;
                    LaunchEncodeButton.IsEnabled = false; // 禁用编码按钮
                }
            }
            catch (Exception exception)
            {
                // 显示错误对话框
                await ShowErrorDialogAsync("操作失败: " + exception.Message);
            }
        }

        /// <summary>
        /// 选择输出文件夹路径。
        /// </summary>
        private async void SelectOutputPath(object sender, RoutedEventArgs e)
        {
            try
            {
                OutputPath.Text = ""; // 清空之前返回的文件夹路径

                // 创建文件夹选择器
                FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

                // 获取当前窗口
                Window? window = App.MainWindow;

                // 获取当前 WinUI 3 窗口的窗口句柄 (HWND)
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                // 使用窗口句柄初始化文件夹选择器
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                // 设置文件夹选择器的选项
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop; // 默认位置为桌面
                openPicker.FileTypeFilter.Add("*"); // 允许选择所有类型

                // 打开文件夹选择器让用户选择文件夹
                _outputFolder = await openPicker.PickSingleFolderAsync();
                if (_outputFolder != null)
                {
                    // 保存选择的文件夹以便未来访问
                    //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", _outputFolder);
                    OutputPath.Text = _outputFolder.Path; // 显示选中文件夹的路径
                    _isOutputPathOk = true;
                    // 如果文本文件和输出路径都选择了，启用编码按钮
                    if (_isTxtOk && _isOutputPathOk)
                    {
                        LaunchEncodeButton.IsEnabled = true;
                    }
                }
                else
                {
                    OutputPath.Text = "操作已取消"; // 用户取消操作
                    _isOutputPathOk = false;
                    LaunchEncodeButton.IsEnabled = false; // 禁用编码按钮
                }
            }
            catch (Exception exception)
            {
                // 显示错误对话框
                await ShowErrorDialogAsync("操作失败: " + exception.Message);
            }
        }

        /// <summary>
        /// 开始编码操作。
        /// </summary>
        private async void LaunchEncode(object sender, RoutedEventArgs e)
        {
            try
            {
                PercentageBox.Text = ""; // 清空百分比文本
                InfoTextBlock.Text = ""; // 清空信息文本
                LaunchEncodeButton.IsEnabled = false; // 禁用编码按钮
                ProgressBar.IsIndeterminate = false; // 取消不确定状态
                ProgressBar.Value = 0; // 初始化进度条为 0

                string? name = _txtFile?.Name[..^4]; // 去掉文件扩展名
                string outputFileName = _outputFolder?.Path + "\\" + name + ".exe"; // 输出文件路径
                string txtContent;

                try
                {
                    // 异步读取文本文件内容
                    txtContent = await FileIO.ReadTextAsync(_txtFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                }
                catch (Exception ex)
                {
                    // 显示错误对话框并退出
                    await ShowErrorDialogAsync("读取文件失败，请检查文件是否是 UTF-8 编码: " + ex.Message);
                    return;
                }

                HuffmanCoding huffmanCoding = new HuffmanCoding();

                // 订阅哈夫曼编码的进度更新事件
                huffmanCoding.ProgressUpdated += (progress) =>
                {
                    // 确保进度更新在主线程执行
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        ProgressBar.Value = progress; // 更新进度条的值
                    });
                };

                ProgressBar.IsIndeterminate = true; // 设置进度条为不确定状态

                await Task.Run(() =>
                {
                    // 构建哈夫曼树
                    huffmanCoding.Build(txtContent);
                });

                ProgressBar.IsIndeterminate = false; // 取消不确定状态

                Dictionary<char, string> dictionary = huffmanCoding.GetEncodingDictionary();

                InfoTextBlock.Text += $"字典条目数：{dictionary.Count}\n";

                byte[] encodedData = [];

                try
                {
                    await Task.Run(() =>
                    {
                        // 编码文本为二进制数据
                        encodedData = huffmanCoding.EncodeToBinary(txtContent);
                    });

                    ProgressBar.IsIndeterminate = true; // 设置进度条为不确定状态

                    await Task.Run(() =>
                    {
                        // 保存编码字典为二进制文件
                        huffmanCoding.SaveDictionaryAsBinary(_outputFolder?.Path + "\\" + name + "_dic.bin");

                        // 保存编码后的内容为二进制文件
                        File.WriteAllBytes(_outputFolder?.Path + "\\" + name + "_enc.bin", encodedData);
                    });

                    // 合并模板文件和编码结果生成最终可执行文件
                    await CombineFilesAsync(
                        AppDomain.CurrentDomain.BaseDirectory + "HuffmanSFX.exe",
                        _outputFolder?.Path + "\\" + name + "_dic.bin",
                        _outputFolder?.Path + "\\" + name + "_enc.bin",
                        outputFileName
                    );

                    // 删除临时文件
                    File.Delete(_outputFolder?.Path + "\\" + name + "_dic.bin");
                    File.Delete(_outputFolder?.Path + "\\" + name + "_enc.bin");

                    // 显示成功信息


                    FileInfo txtFileInfo = new(_txtFile?.Path ?? string.Empty);
                    long txtFileSize = txtFileInfo.Length;

                    FileInfo exeFileInfo = new(outputFileName);
                    long exeFileSize = exeFileInfo.Length;

                    ProgressBar.IsIndeterminate = false; // 取消不确定状态

                    InfoTextBlock.Text += $"原文件大小：{txtFileSize:N0} 字节\n";
                    InfoTextBlock.Text += $"压缩后文件大小：{exeFileSize:N0} 字节\n";
                    InfoTextBlock.Text += $"自解压文件：{outputFileName}";
                    PercentageBox.Text = (exeFileSize * 100 / txtFileSize).ToString("F0") + "%";
                    ProgressBar.Value = (int)(exeFileSize * 100 / txtFileSize) > 100 ? 100 : (int)(exeFileSize * 100 / txtFileSize);
                }
                catch (Exception ex)
                {
                    // 显示错误对话框
                    await ShowErrorDialogAsync("操作失败: " + ex.Message);
                }
                finally
                {
                    // 重置进度条状态
                    ProgressBar.IsIndeterminate = false;
                    LaunchEncodeButton.IsEnabled = true; // 启用编码按钮
                }
            }
            catch (Exception exception)
            {
                // 显示错误对话框
                await ShowErrorDialogAsync("操作失败: " + exception.Message);
            }
        }

        /// <summary>
        /// 异步合并多个文档为一个可执行文档。
        /// </summary>
        private static async Task CombineFilesAsync(string exeFilePath, string binaryFile1Path, string binaryFile2Path, string outputFilePath)
        {
            await Task.Run(() =>
            {
                byte[] binaryFile1 = File.ReadAllBytes(binaryFile1Path);
                byte[] binaryFile2 = File.ReadAllBytes(binaryFile2Path);

                long binaryFile1Size = binaryFile1.Length;
                long binaryFile2Size = binaryFile2.Length;

                File.Copy(exeFilePath, outputFilePath, true);

                using FileStream exeStream = new FileStream(outputFilePath, FileMode.Append, FileAccess.Write);
                exeStream.Write(binaryFile1, 0, binaryFile1.Length);
                exeStream.Write(binaryFile2, 0, binaryFile2.Length);

                using BinaryWriter writer = new BinaryWriter(exeStream);
                writer.Write(binaryFile1Size);
                writer.Write(binaryFile2Size);
            });
        }

        /// <summary>
        /// 显示错误对话框。
        /// </summary>
        private async Task ShowErrorDialogAsync(string errorMessage)
        {
            ContentDialog errorDialog = new()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "错误",
                Content = errorMessage,
                CloseButtonText = "确定"
            };

            await errorDialog.ShowAsync();
        }
    }
}
