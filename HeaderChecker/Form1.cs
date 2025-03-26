using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FileSignatures;

namespace HeaderChecker
{
    public partial class Form1 : Form
    {
        private readonly FileFormatInspector inspector;
        private bool sortAscending = true; // 정렬 방향 (true = 오름차순, false = 내림차순)

        public Form1()
        {
            InitializeComponent();
            var assembly = typeof(Form1).GetTypeInfo().Assembly;
            var allFormats = FileFormatLocator.GetFormats(assembly, includeDefaults: true);
            inspector = new FileFormatInspector(allFormats);
            SetupUI();
        }

        private void SetupUI()
        {
            listView1.Columns.Add("파일 이름", 400);
            listView1.Columns.Add("상태", 200);
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.OwnerDraw = true;

            listView1.DrawColumnHeader += ListView1_DrawColumnHeader;
            listView1.DrawSubItem += ListView1_DrawSubItem;
            listView1.ColumnClick += ListView1_ColumnClick; // 컬럼 클릭 이벤트 추가

            label1.Text = "진행 상태: 0%";
        }

        private void ListView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) => e.DrawDefault = true;

        private void ListView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                if (e.SubItem.Text == "Unknown") e.Graphics.FillRectangle(Brushes.Red, e.Bounds);
                else if (e.SubItem.Text == "OK") e.Graphics.FillRectangle(Brushes.Green, e.Bounds);
                else if (e.SubItem.Text == "Corrupted") e.Graphics.FillRectangle(Brushes.Violet, e.Bounds);
                else if (e.SubItem.Text == "Mismatch") e.Graphics.FillRectangle(Brushes.Yellow, e.Bounds);
                else e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, e.Bounds, e.SubItem.ForeColor, TextFormatFlags.Left);
            }
            else e.DrawDefault = true;
        }

        // 🔹 파일 이름 컬럼 클릭 시 정렬 기능 추가
        private void ListView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0) // 파일 이름 열 클릭 시
            {
                sortAscending = !sortAscending; // 정렬 방향 반전
                listView1.ListViewItemSorter = new NaturalSortComparer(e.Column, sortAscending);
                listView1.Sort();
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFolder = dialog.SelectedPath;
                    listView1.Items.Clear();
                    label1.Text = "진행 상태: 0%"; // 진행 상태 초기화
                    Task.Run(() => ProcessFiles(selectedFolder)); // 파일 처리 작업을 백그라운드 스레드에서 실행
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e) => listView1.Items.Clear();

        private async Task ProcessFiles(string folderPath)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath);
                var results = new StringWriter(); // 체크 결과를 저장할 StringWriter

                // 폴더 생성
                string mismatchFolder = Path.Combine(folderPath, "Mismatch");
                string corruptedFolder = Path.Combine(folderPath, "Corrupted");
                string okFolder = Path.Combine(folderPath, "OK");

                // 폴더가 없으면 생성
                if (!Directory.Exists(mismatchFolder))
                    Directory.CreateDirectory(mismatchFolder);

                if (!Directory.Exists(corruptedFolder))
                    Directory.CreateDirectory(corruptedFolder);

                if (!Directory.Exists(okFolder))
                    Directory.CreateDirectory(okFolder);

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    string fileType = GetFileType(file); // 헤더 기반 파일 형식
                    string extension = Path.GetExtension(file).TrimStart('.').ToUpperInvariant(); // 확장자
                    string status;

                    if (fileType == "Unknown")
                    {
                        status = "Unknown"; // Unknown은 그냥 넘어가도록 처리
                    }
                    else if (fileType.Equals(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        status = "OK";
                        File.Move(file, Path.Combine(okFolder, Path.GetFileName(file)));
                    }
                    else if (fileType == "Corrupted")
                    {
                        status = "Corrupted";
                        File.Move(file, Path.Combine(corruptedFolder, Path.GetFileName(file)));
                    }
                    else
                    {
                        status = "Mismatch";
                        File.Move(file, Path.Combine(mismatchFolder, Path.GetFileName(file)));
                    }

                    Invoke(new Action(() =>
                    {
                        var listViewItem = new ListViewItem(new[] { Path.GetFileName(file), status });
                        listView1.Items.Add(listViewItem);
                    }));

                    results.WriteLine($"File: {file}");
                    results.WriteLine($"Detected Type: {fileType}");
                    results.WriteLine($"Extension: {extension}");
                    results.WriteLine($"Status: {status}");
                    results.WriteLine();

                    int progress = (int)((i + 1) / (float)files.Length * 100);
                    Invoke(new Action(() => label1.Text = $"진행 상태: {progress}%"));
                }

                File.WriteAllText(Path.Combine(Application.StartupPath, "체크결과.txt"), results.ToString());

                Invoke(new Action(() =>
                {
                    MessageBox.Show("체크 결과가 체크결과.txt 파일로 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    label1.Text = "진행 상태: 100%";
                }));
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    MessageBox.Show($"오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    label1.Text = "진행 상태: 오류 발생";
                }));
            }
        }

        private string GetFileType(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0) return "Empty";

            using (FileStream stream = File.OpenRead(filePath))
            {
                try
                {
                    var format = inspector.DetermineFileFormat(stream);
                    if (format != null) return format.Extension.ToUpperInvariant(); // 파일 형식이 제대로 인식되면 확장자 반환

                    // 파일 형식을 인식하지 못한 경우, ZIP 형식으로 검사하여 XLSX 형식 확인
                    byte[] zipSignature = { 0x50, 0x4B, 0x03, 0x04 }; // ZIP 파일 시그니처
                    byte[] headerBytes = new byte[4];
                    stream.Seek(0, SeekOrigin.Begin);
                    int bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);

                    if (bytesRead == 4 && headerBytes.SequenceEqual(zipSignature)) return "XLSX"; // ZIP 시그니처를 확인했을 경우, XLSX로 반환

                    // HWP 서명 확인
                    byte[] hwpSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }; // HWP 시그니처
                    int hwpHeaderLength = hwpSignature.Length;

                    stream.Seek(0, SeekOrigin.Begin);
                    headerBytes = new byte[hwpHeaderLength];
                    bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);

                    if (bytesRead == hwpHeaderLength && headerBytes.SequenceEqual(hwpSignature)) return "HWP"; // HWP 서명 인식
                }
                catch (Exception)
                {
                    return "Corrupted";
                }
            }

            // 어떤 형식도 인식되지 않으면 Unknown 반환
            return "Unknown";
        }
    }
    // 🔹 윈도우 탐색기와 느낌으로 "자연 정렬"을 구현한 Comparer
    // 수정 필요함. 윈도우 탐색기와 동일하지 않기 때문.
    public class NaturalSortComparer : IComparer
    {
        private int columnIndex;
        private bool ascending;

        public NaturalSortComparer(int column, bool ascending)
        {
            this.columnIndex = column;
            this.ascending = ascending;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = x as ListViewItem;
            ListViewItem itemY = y as ListViewItem;

            string textX = itemX.SubItems[columnIndex].Text;
            string textY = itemY.SubItems[columnIndex].Text;

            int result = WindowsExplorerCompare(textX, textY);

            return ascending ? result : -result;
        }

        private int WindowsExplorerCompare(string str1, string str2)
        {
            // 숫자와 문자 부분을 분리하는 정규 표현식
            Regex regex = new Regex(@"(\d+)|(\D+)");
            var parts1 = regex.Matches(str1);
            var parts2 = regex.Matches(str2);

            int minParts = Math.Min(parts1.Count, parts2.Count);

            for (int i = 0; i < minParts; i++)
            {
                string part1 = parts1[i].Value;
                string part2 = parts2[i].Value;

                bool isNumber1 = int.TryParse(part1, out int num1);
                bool isNumber2 = int.TryParse(part2, out int num2);

                if (isNumber1 && isNumber2)
                {
                    int numberCompare = num1.CompareTo(num2);
                    if (numberCompare != 0)
                        return numberCompare;
                }
                else
                {
                    int textCompare = string.Compare(part1, part2, StringComparison.OrdinalIgnoreCase);
                    if (textCompare != 0)
                        return textCompare;
                }
            }

            return str1.Length.CompareTo(str2.Length);
        }
    }
}
