using KursovayaDB.DataBaseServices;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Xceed.Words.NET;
using Alignment = Xceed.Document.NET.Alignment;

namespace KursovayaDB.Services.ExportServices;

public class WordExport
{
    public static async void WordImportAndOpen(DataGrid dataGrid, string name, string title, bool isNewFile)
    {
        string wordPath = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE"; // Путь к исполняемому файлу Microsoft Word
        string filePathWord = isNewFile ? $@"ExportedFiles\Word\Exported{name}_{DateTime.Now:yyyyMMdd_HHmmss}.docx" : 
            $@"ExportedFiles\Word\Exported{name}.docx";

        List<string> columnHeaders = new List<string>();//Названия столбцов
        foreach (var column in dataGrid.Columns)
        {
            if (column is DataGridTextColumn textColumn)
            {
                columnHeaders.Add(textColumn.Header.ToString());
            }
        }

        List<string> cellValues = new List<string>();//Названия столбцов

        foreach (var item in dataGrid.Items)
        {
            if (item is DataRowView row)
            {
                foreach (var cellValue in row.Row.ItemArray)
                {
                    cellValues.Add(cellValue.ToString());
                }
                cellValues.Add("end");
            }
        }
        var rowCount = (await SQLScripts.GetAllCategories()).Count;
        using (var doc = DocX.Create(filePathWord))
        {
            var table = doc.AddTable(dataGrid.Items.Count+2, columnHeaders.Count);
            table.Rows[0].MergeCells(0, columnHeaders.Count - 1);
            table.Rows[0].Cells[0].Paragraphs.First().Append(title).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;

            for (int i = 0; i < columnHeaders.Count; i++)
            {
                table.Rows[1].Cells[i].Paragraphs.First().Append(columnHeaders.ElementAt(i)).Bold().FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            }

            int row = 2;
            int cell = 0;
            foreach (var cellValue in cellValues)
            {
                if (cellValue.Equals("end"))
                {
                    row++; cell = 0;
                    continue;
                }
                table.Rows[row].Cells[cell].Paragraphs.First().Append(cellValue).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                cell++;
            }

            doc.InsertTable(table);

            try
            {
                doc.Save();
            }
            catch (IOException)
            {
                MessageBox.Show("Перед перезаписью сперва закройте файл.");
            }
        }
        ExcelExport.OpenFile(filePathWord, wordPath);
    }

}
